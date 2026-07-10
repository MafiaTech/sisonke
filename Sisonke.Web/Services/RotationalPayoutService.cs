using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;
using Sisonke.Web.Services.Notifications;

namespace Sisonke.Web.Services;

public sealed class RotationalPayoutService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    ILogger<RotationalPayoutService> logger,
    AuditLogService auditLogService,
    NotificationEnqueuer notificationEnqueuer)
{
    public async Task<RotationalPayout?> GetCurrentPayoutAsync(Guid stokvelId)
    {
        await using var context = await dbFactory.CreateDbContextAsync();
        return await CurrentPayoutQuery(context, stokvelId).FirstOrDefaultAsync();
    }

    public async Task<List<RotationalPayout>> GetRecentPaidPayoutsAsync(Guid stokvelId, int take = 10)
    {
        await using var context = await dbFactory.CreateDbContextAsync();
        return await context.RotationalPayouts.AsNoTracking()
            .Include(payout => payout.Cycle)
            .Include(payout => payout.PayoutMember)
            .Where(payout => payout.StokvelId == stokvelId &&
                payout.IsActive &&
                payout.PayoutStatus == RotationalPayoutStatus.Paid)
            .OrderByDescending(payout => payout.PaidAt ?? payout.UpdatedAt ?? payout.CreatedAt)
            .Take(Math.Max(1, take))
            .ToListAsync();
    }

    public async Task<PayoutSummary> GetPayoutSummaryAsync(Guid stokvelId, string? currentUserId)
    {
        await using var context = await dbFactory.CreateDbContextAsync();
        var payout = await CurrentPayoutQuery(context, stokvelId).FirstOrDefaultAsync();
        var role = await GetCurrentRoleAsync(context, stokvelId, currentUserId);
        var banking = await context.StokvelBankingDetails.AsNoTracking()
            .Where(details => details.StokvelId == stokvelId && details.IsActive && details.IsPrimary)
            .OrderByDescending(details => details.CreatedAt).FirstOrDefaultAsync();

        CycleContributionSummary summary = new(0, 0, 0, 0, 0, 0, 0, 0, 0);
        if (payout is not null)
        {
            summary = await context.RotationalContributionPayments.AsNoTracking()
                .Where(payment => payment.CycleId == payout.CycleId && payment.IsActive)
                .GroupBy(_ => 1)
                .Select(group => new CycleContributionSummary(
                    group.Sum(payment => payment.ExpectedAmount), group.Sum(payment => payment.PaidAmount),
                    group.Sum(payment => payment.ExpectedAmount) - group.Sum(payment => payment.PaidAmount),
                    group.Sum(payment => payment.PenaltyAmount),
                    group.Count(payment => payment.PaymentStatus == ContributionPaymentStatus.Paid),
                    group.Count(payment => payment.PaymentStatus == ContributionPaymentStatus.Unpaid),
                    group.Count(payment => payment.PaymentStatus == ContributionPaymentStatus.PartiallyPaid),
                    group.Count(payment => payment.PaymentStatus == ContributionPaymentStatus.Late), group.Count()))
                .FirstOrDefaultAsync() ?? summary;
        }

        return new(payout, banking, summary,
            role == SisonkeRole.Chairperson,
            role == SisonkeRole.Treasurer,
            role == SisonkeRole.Secretary || role is SisonkeRole.Creator or SisonkeRole.StokvelAdmin,
            payout?.PayoutMember.ApplicationUserId == currentUserId);
    }

    public async Task<PayoutOperationResult> EnsurePayoutForReadyCycleAsync(Guid cycleId, string currentUserId)
    {
        await using var context = await dbFactory.CreateDbContextAsync();
        var cycle = await context.RotationalContributionCycles
            .FirstOrDefaultAsync(item => item.Id == cycleId && item.IsActive);
        if (cycle is null) return PayoutOperationResult.Failed(["Cycle not found or inactive."]);
        if (cycle.Status != RotationalCycleStatus.ReadyForPayout)
            return PayoutOperationResult.Failed(["The cycle is not ready for payout."]);
        if (!await IsOfficeBearerAsync(context, cycle.StokvelId, currentUserId))
            return PayoutOperationResult.Failed(["Only office bearers can prepare a payout."]);

        var existing = await context.RotationalPayouts
            .FirstOrDefaultAsync(payout => payout.CycleId == cycleId && payout.IsActive);
        if (existing is not null) return PayoutOperationResult.Succeeded(existing);

        var stokvelTenantId = await context.Stokvels
            .Where(stokvel => stokvel.Id == cycle.StokvelId)
            .Select(stokvel => stokvel.TenantId)
            .SingleOrDefaultAsync();

        var now = DateTime.UtcNow;
        var payout = new RotationalPayout
        {
            Id = Guid.NewGuid(),
            StokvelId = cycle.StokvelId,
            CycleId = cycle.Id,
            PayoutMemberId = cycle.PayoutMemberId,
            PayoutAmount = cycle.ExpectedPayoutAmount,
            PayoutStatus = RotationalPayoutStatus.PendingSecretaryReview,
            RequestedAt = now,
            RequestedBy = currentUserId,
            IsActive = true,
            CreatedAt = now,
            CreatedBy = currentUserId
        };
        context.RotationalPayouts.Add(payout);
        cycle.Status = RotationalCycleStatus.PayoutPending;
        cycle.UpdatedAt = now; cycle.UpdatedBy = currentUserId;

        var secretaries = await context.Members
            .Where(member => member.TenantId == stokvelTenantId &&
                member.DefaultRole == SisonkeRole.Secretary &&
                member.Status == MemberStatus.Active)
            .ToListAsync();
        foreach (var secretary in secretaries)
        {
            await notificationEnqueuer.EnqueueTaskAssignedAsync(
                context, secretary.Id, cycle.StokvelId,
                nameof(RotationalPayout), payout.Id,
                "A payout is awaiting your review",
                $"A rotational payout for {cycle.CycleName} is awaiting Secretary review.");
        }

        await context.SaveChangesAsync();
        await auditLogService.RecordAsync(
            currentUserId,
            cycle.StokvelId,
            "RotationalPayoutScheduled",
            "RotationalPayout",
            payout.Id,
            $"Payout prepared for {cycle.CycleName}.");
        logger.LogInformation("Payout {PayoutId} created for cycle {CycleId}", payout.Id, cycleId);
        return PayoutOperationResult.Succeeded(payout);
    }

    public async Task<PayoutOperationResult> ReviewPayoutAsSecretaryAsync(
        Guid payoutId,
        string currentUserId,
        bool recommendApproval,
        string? notes)
    {
        var trimmedNotes = NullIfWhiteSpace(notes);
        if (!recommendApproval && string.IsNullOrWhiteSpace(trimmedNotes))
            return PayoutOperationResult.Failed(["Secretary notes are required when recommending rejection."]);

        await using var context = await dbFactory.CreateDbContextAsync();
        var payout = await context.RotationalPayouts
            .Include(item => item.Cycle)
            .FirstOrDefaultAsync(item => item.Id == payoutId && item.IsActive);
        if (payout is null) return PayoutOperationResult.Failed(["Payout not found."]);
        var role = await GetCurrentRoleAsync(context, payout.StokvelId, currentUserId);
        if (role is not (SisonkeRole.Secretary or SisonkeRole.Creator or SisonkeRole.StokvelAdmin))
            return PayoutOperationResult.Failed(["Only the Secretary or stokvel admins can review payout readiness."]);
        if (payout.PayoutStatus is not (RotationalPayoutStatus.PendingSecretaryReview or RotationalPayoutStatus.ReturnedToSecretary))
            return PayoutOperationResult.Failed(["Only payouts awaiting Secretary review can be reviewed."]);
        if (await IsPayoutRecipientAsync(context, payout, currentUserId))
            return PayoutOperationResult.Failed(["You cannot review your own payout."]);

        var now = DateTime.UtcNow;
        payout.SecretaryReviewedAt = now;
        payout.SecretaryReviewedByUserId = currentUserId;
        payout.SecretaryRecommendedApproval = recommendApproval;
        payout.SecretaryReviewNotes = trimmedNotes;
        payout.PayoutStatus = recommendApproval
            ? RotationalPayoutStatus.PendingChairpersonApproval
            : RotationalPayoutStatus.Rejected;
        if (!recommendApproval)
        {
            payout.RejectedAt = now;
            payout.RejectedByChairpersonId = null;
            payout.RejectionReason = trimmedNotes;
        }
        payout.Cycle.Status = recommendApproval
            ? RotationalCycleStatus.PayoutPending
            : RotationalCycleStatus.ReadyForPayout;
        payout.Cycle.UpdatedAt = now;
        payout.Cycle.UpdatedBy = currentUserId;
        payout.UpdatedAt = now;
        payout.UpdatedBy = currentUserId;
        await context.SaveChangesAsync();
        await auditLogService.RecordAsync(
            currentUserId,
            payout.StokvelId,
            "RotationalPayoutSecretaryReviewed",
            "RotationalPayout",
            payout.Id,
            recommendApproval ? "Secretary recommended payout approval." : "Secretary recommended payout rejection.");
        logger.LogInformation("Payout {PayoutId} secretary review recorded", payoutId);
        return PayoutOperationResult.Succeeded(payout);
    }

    public Task<PayoutOperationResult> ApprovePayoutAsync(Guid payoutId, string currentUserId) =>
        SubmitChairpersonDecisionAsync(payoutId, currentUserId, RotationalPayoutDecision.Approve, null);

    public Task<PayoutOperationResult> RejectPayoutAsync(Guid payoutId, string currentUserId, string reason) =>
        SubmitChairpersonDecisionAsync(payoutId, currentUserId, RotationalPayoutDecision.Reject, reason);

    public Task<PayoutOperationResult> RequestPayoutChangesAsync(Guid payoutId, string currentUserId, string notes) =>
        SubmitChairpersonDecisionAsync(payoutId, currentUserId, RotationalPayoutDecision.RequestChanges, notes);

    public async Task<PayoutOperationResult> ResubmitPayoutForApprovalAsync(Guid payoutId, string currentUserId)
    {
        await using var context = await dbFactory.CreateDbContextAsync();
        var payout = await context.RotationalPayouts
            .Include(item => item.Cycle)
            .FirstOrDefaultAsync(item => item.Id == payoutId && item.IsActive);
        if (payout is null) return PayoutOperationResult.Failed(["Payout not found."]);
        var role = await GetCurrentRoleAsync(context, payout.StokvelId, currentUserId);
        if (role is not (SisonkeRole.Secretary or SisonkeRole.Creator or SisonkeRole.StokvelAdmin))
            return PayoutOperationResult.Failed(["Only the Secretary or stokvel admins can resubmit payout changes for approval."]);
        if (payout.PayoutStatus != RotationalPayoutStatus.ReturnedToSecretary)
            return PayoutOperationResult.Failed(["Only payouts returned to the Secretary can be resubmitted."]);

        var now = DateTime.UtcNow;
        payout.PayoutStatus = RotationalPayoutStatus.PendingChairpersonApproval;
        payout.RequestedAt = now;
        payout.RequestedBy = currentUserId;
        payout.Cycle.Status = RotationalCycleStatus.PayoutPending;
        payout.Cycle.UpdatedAt = now;
        payout.Cycle.UpdatedBy = currentUserId;
        payout.UpdatedAt = now;
        payout.UpdatedBy = currentUserId;
        await context.SaveChangesAsync();
        await auditLogService.RecordAsync(
            currentUserId,
            payout.StokvelId,
            "RotationalPayoutSecretaryResubmitted",
            "RotationalPayout",
            payout.Id,
            "Secretary resubmitted payout for chairperson approval.");
        logger.LogInformation("Payout {PayoutId} resubmitted for chairperson approval", payoutId);
        return PayoutOperationResult.Succeeded(payout);
    }

    public async Task<PayoutOperationResult> SubmitChairpersonDecisionAsync(
        Guid payoutId,
        string currentUserId,
        RotationalPayoutDecision decision,
        string? notes)
    {
        var trimmedNotes = NullIfWhiteSpace(notes);
        if (decision is RotationalPayoutDecision.Reject or RotationalPayoutDecision.RequestChanges &&
            string.IsNullOrWhiteSpace(trimmedNotes))
            return PayoutOperationResult.Failed(["Decision notes are required for reject or request changes."]);

        await using var context = await dbFactory.CreateDbContextAsync();
        var payout = await context.RotationalPayouts
            .Include(item => item.Cycle)
            .FirstOrDefaultAsync(item => item.Id == payoutId && item.IsActive);
        if (payout is null) return PayoutOperationResult.Failed(["Payout not found."]);
        if (await GetCurrentRoleAsync(context, payout.StokvelId, currentUserId) != SisonkeRole.Chairperson)
            return PayoutOperationResult.Failed(["Only the Chairperson can submit payout decisions."]);
        if (payout.PayoutStatus is not (RotationalPayoutStatus.PendingChairpersonApproval or RotationalPayoutStatus.ReadyForApproval))
            return PayoutOperationResult.Failed(["Only payouts ready for approval can receive a chairperson decision."]);
        if (await IsPayoutRecipientAsync(context, payout, currentUserId))
            return PayoutOperationResult.Failed(["You cannot approve or reject your own payout."]);

        var now = DateTime.UtcNow;
        payout.ChairpersonDecision = decision.ToString();
        payout.ChairpersonReviewedAt = now;
        payout.ChairpersonReviewedByUserId = currentUserId;
        payout.ChairpersonReviewNotes = trimmedNotes;

        switch (decision)
        {
            case RotationalPayoutDecision.Approve:
                payout.PayoutStatus = RotationalPayoutStatus.Approved;
                payout.ApprovedByChairpersonId = currentUserId;
                payout.ApprovedAt = now;
                break;
            case RotationalPayoutDecision.Reject:
                payout.PayoutStatus = RotationalPayoutStatus.Rejected;
                payout.RejectedByChairpersonId = currentUserId;
                payout.RejectedAt = now;
                payout.RejectionReason = trimmedNotes;
                break;
            case RotationalPayoutDecision.RequestChanges:
                payout.PayoutStatus = RotationalPayoutStatus.ReturnedToSecretary;
                payout.Cycle.Status = RotationalCycleStatus.ReadyForPayout;
                payout.Cycle.UpdatedAt = now;
                payout.Cycle.UpdatedBy = currentUserId;
                break;
            default:
                return PayoutOperationResult.Failed(["Unsupported payout decision."]);
        }

        payout.UpdatedAt = now;
        payout.UpdatedBy = currentUserId;

        if (decision == RotationalPayoutDecision.Approve)
        {
            await notificationEnqueuer.EnqueueChairpersonApprovedAsync(
                context, payout.PayoutMemberId, payout.StokvelId,
                nameof(RotationalPayout), payout.Id,
                "Your payout was approved",
                $"Your rotational payout of {payout.PayoutAmount:C} has been approved by the Chairperson.");
        }
        else if (decision == RotationalPayoutDecision.Reject)
        {
            await notificationEnqueuer.EnqueueChairpersonRejectedAsync(
                context, payout.PayoutMemberId, payout.StokvelId,
                nameof(RotationalPayout), payout.Id,
                "Your payout was rejected",
                $"Your rotational payout of {payout.PayoutAmount:C} was rejected by the Chairperson.{(trimmedNotes is null ? string.Empty : $" Reason: {trimmedNotes}")}");
        }

        await context.SaveChangesAsync();
        await auditLogService.RecordAsync(
            currentUserId,
            payout.StokvelId,
            decision == RotationalPayoutDecision.Approve ? "RotationalPayoutChairpersonApproved" :
            decision == RotationalPayoutDecision.Reject ? "RotationalPayoutChairpersonRejected" :
            "RotationalPayoutChairpersonReturned",
            "RotationalPayout",
            payout.Id,
            $"Chairperson decision recorded as {decision}.");
        logger.LogInformation("Payout {PayoutId} chairperson decision recorded as {Decision}", payoutId, decision);
        return PayoutOperationResult.Succeeded(payout);
    }

    public async Task<PayoutOperationResult> ConfirmPayoutPaidAsync(
        Guid payoutId, string currentUserId, ConfirmPayoutPaidRequest request)
    {
        var errors = ValidateConfirmation(request);
        if (errors.Count > 0) return PayoutOperationResult.Failed(errors);
        await using var context = await dbFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var payout = await context.RotationalPayouts.Include(item => item.Cycle)
            .FirstOrDefaultAsync(item => item.Id == payoutId && item.IsActive);
        if (payout is null) return PayoutOperationResult.Failed(["Payout not found."]);
        if (await GetCurrentRoleAsync(context, payout.StokvelId, currentUserId) != SisonkeRole.Treasurer)
            return PayoutOperationResult.Failed(["Only the Treasurer can confirm a payout payment."]);
        if (payout.PayoutStatus != RotationalPayoutStatus.Approved)
            return PayoutOperationResult.Failed(["The payout must be approved by the Chairperson before payment."]);
        if (await IsPayoutRecipientAsync(context, payout, currentUserId))
            return PayoutOperationResult.Failed(["You cannot pay your own payout."]);
        if (!await context.Stokvels.AsNoTracking().AnyAsync(stokvel =>
                stokvel.Id == payout.StokvelId && stokvel.IsActive && !stokvel.IsDeleted))
            return PayoutOperationResult.Failed(["Payout cannot be confirmed for an inactive stokvel."]);

        var now = DateTime.UtcNow;
        var paidAt = request.PaidAt!.Value;
        payout.PayoutStatus = RotationalPayoutStatus.Paid;
        payout.PaidByTreasurerId = currentUserId; payout.PaidAt = paidAt;
        payout.PaymentMethod = request.PaymentMethod; payout.PaymentReference = request.PaymentReference!.Trim();
        payout.Notes = NullIfWhiteSpace(request.Notes); payout.UpdatedAt = now; payout.UpdatedBy = currentUserId;
        payout.Cycle.Status = RotationalCycleStatus.PaidOut;
        payout.Cycle.UpdatedAt = now; payout.Cycle.UpdatedBy = currentUserId;

        var order = await context.RotationalPayoutOrders.FirstOrDefaultAsync(item =>
            item.StokvelId == payout.StokvelId && item.MemberId == payout.PayoutMemberId && item.IsActive);
        if (order is not null)
        {
            order.HasReceivedPayout = true; order.LastPayoutDate = paidAt;
            order.UpdatedAt = now; order.UpdatedBy = currentUserId;
        }

        var nextCycle = await context.RotationalContributionCycles
            .Where(item => item.StokvelId == payout.StokvelId && item.IsActive &&
                           item.CycleNumber > payout.Cycle.CycleNumber && item.Status == RotationalCycleStatus.Pending)
            .OrderBy(item => item.CycleNumber).FirstOrDefaultAsync();
        if (nextCycle is not null)
        {
            nextCycle.Status = RotationalCycleStatus.Open;
            nextCycle.UpdatedAt = now; nextCycle.UpdatedBy = currentUserId;
        }

        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        await auditLogService.RecordAsync(
            currentUserId,
            payout.StokvelId,
            "RotationalPayoutPaid",
            "RotationalPayout",
            payout.Id,
            $"Treasurer recorded payout payment for {FormatMoney(payout.PayoutAmount)}.");
        logger.LogInformation("Payout {PayoutId} confirmed paid", payoutId);
        return PayoutOperationResult.Succeeded(payout);
    }

    public async Task<bool> CanApprovePayoutAsync(Guid stokvelId, string currentUserId)
    {
        await using var context = await dbFactory.CreateDbContextAsync();
        return await GetCurrentRoleAsync(context, stokvelId, currentUserId) == SisonkeRole.Chairperson;
    }

    public async Task<bool> CanConfirmPayoutPaymentAsync(Guid stokvelId, string currentUserId)
    {
        await using var context = await dbFactory.CreateDbContextAsync();
        return await GetCurrentRoleAsync(context, stokvelId, currentUserId) == SisonkeRole.Treasurer;
    }

    private static IQueryable<RotationalPayout> CurrentPayoutQuery(ApplicationDbContext context, Guid stokvelId) =>
        context.RotationalPayouts.AsNoTracking()
            .Include(payout => payout.Cycle).Include(payout => payout.PayoutMember)
            .Where(payout => payout.StokvelId == stokvelId && payout.IsActive)
            .OrderByDescending(payout => payout.Cycle.CycleNumber);

    private static async Task<SisonkeRole?> GetCurrentRoleAsync(
        ApplicationDbContext context, Guid stokvelId, string? currentUserId)
    {
        if (string.IsNullOrWhiteSpace(currentUserId)) return null;
        return await context.Members.AsNoTracking()
            .Where(member => member.ApplicationUserId == currentUserId &&
                context.Stokvels.Any(stokvel => stokvel.Id == stokvelId && stokvel.TenantId == member.TenantId))
            .Select(member => (SisonkeRole?)member.DefaultRole).FirstOrDefaultAsync();
    }

    private static async Task<bool> IsPayoutRecipientAsync(
        ApplicationDbContext context,
        RotationalPayout payout,
        string currentUserId)
    {
        if (string.IsNullOrWhiteSpace(currentUserId))
            return false;

        return await context.Members.AsNoTracking().AnyAsync(member =>
            member.Id == payout.PayoutMemberId &&
            member.ApplicationUserId == currentUserId);
    }

    private static async Task<bool> IsOfficeBearerAsync(
        ApplicationDbContext context, Guid stokvelId, string currentUserId)
    {
        var role = await GetCurrentRoleAsync(context, stokvelId, currentUserId);
        return role is SisonkeRole.Creator or SisonkeRole.StokvelAdmin or SisonkeRole.Chairperson
            or SisonkeRole.Secretary or SisonkeRole.Treasurer;
    }

    private static List<string> ValidateConfirmation(ConfirmPayoutPaidRequest request)
    {
        var errors = new List<string>();
        if (request.PaymentMethod is null) errors.Add("Payment method is required.");
        if (request.PaidAt is null) errors.Add("Paid date is required.");
        if (request.PaymentMethod is PaymentMethod.EFT or PaymentMethod.BankDeposit &&
            string.IsNullOrWhiteSpace(request.PaymentReference)) errors.Add("Payment reference is required for EFT or bank deposit.");
        else if (string.IsNullOrWhiteSpace(request.PaymentReference)) errors.Add("Payment reference is required.");
        return errors;
    }

    private static string? NullIfWhiteSpace(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string FormatMoney(decimal value) =>
        value.ToString("C2", new System.Globalization.CultureInfo("en-ZA"));
}

public sealed record ConfirmPayoutPaidRequest(
    PaymentMethod? PaymentMethod, string? PaymentReference, DateTime? PaidAt, string? Notes);
public enum RotationalPayoutDecision
{
    Approve = 1,
    Reject = 2,
    RequestChanges = 3
}
public sealed record PayoutOperationResult(bool Success, RotationalPayout? Payout, List<string> Errors)
{
    public static PayoutOperationResult Succeeded(RotationalPayout payout) => new(true, payout, []);
    public static PayoutOperationResult Failed(List<string> errors) => new(false, null, errors);
}
public sealed record PayoutSummary(
    RotationalPayout? Payout, StokvelBankingDetails? BankingDetails,
    CycleContributionSummary ContributionSummary, bool CanApprove, bool CanConfirmPaid, bool CanReviewAsSecretary, bool IsRecipient);
