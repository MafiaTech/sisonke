using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Services;

public sealed class RotationalPayoutService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    ILogger<RotationalPayoutService> logger)
{
    public async Task<RotationalPayout?> GetCurrentPayoutAsync(Guid stokvelId)
    {
        await using var context = await dbFactory.CreateDbContextAsync();
        return await CurrentPayoutQuery(context, stokvelId).FirstOrDefaultAsync();
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

        var now = DateTime.UtcNow;
        var payout = new RotationalPayout
        {
            Id = Guid.NewGuid(),
            StokvelId = cycle.StokvelId,
            CycleId = cycle.Id,
            PayoutMemberId = cycle.PayoutMemberId,
            PayoutAmount = cycle.ExpectedPayoutAmount,
            PayoutStatus = RotationalPayoutStatus.ReadyForApproval,
            RequestedAt = now,
            RequestedBy = currentUserId,
            IsActive = true,
            CreatedAt = now,
            CreatedBy = currentUserId
        };
        context.RotationalPayouts.Add(payout);
        cycle.Status = RotationalCycleStatus.PayoutPending;
        cycle.UpdatedAt = now; cycle.UpdatedBy = currentUserId;
        await context.SaveChangesAsync();
        logger.LogInformation("Payout {PayoutId} created for cycle {CycleId}", payout.Id, cycleId);
        return PayoutOperationResult.Succeeded(payout);
    }

    public Task<PayoutOperationResult> ApprovePayoutAsync(Guid payoutId, string currentUserId) =>
        DecidePayoutAsync(payoutId, currentUserId, true, null);

    public Task<PayoutOperationResult> RejectPayoutAsync(Guid payoutId, string currentUserId, string reason) =>
        DecidePayoutAsync(payoutId, currentUserId, false, reason);

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

    private async Task<PayoutOperationResult> DecidePayoutAsync(
        Guid payoutId, string currentUserId, bool approve, string? reason)
    {
        if (!approve && string.IsNullOrWhiteSpace(reason))
            return PayoutOperationResult.Failed(["Rejection reason is required."]);
        await using var context = await dbFactory.CreateDbContextAsync();
        var payout = await context.RotationalPayouts.FirstOrDefaultAsync(item => item.Id == payoutId && item.IsActive);
        if (payout is null) return PayoutOperationResult.Failed(["Payout not found."]);
        if (await GetCurrentRoleAsync(context, payout.StokvelId, currentUserId) != SisonkeRole.Chairperson)
            return PayoutOperationResult.Failed(["Only the Chairperson can approve or reject payouts."]);
        if (payout.PayoutStatus != RotationalPayoutStatus.ReadyForApproval)
            return PayoutOperationResult.Failed(["Only payouts ready for approval can be approved or rejected."]);
        var now = DateTime.UtcNow;
        if (approve)
        {
            payout.PayoutStatus = RotationalPayoutStatus.Approved;
            payout.ApprovedByChairpersonId = currentUserId; payout.ApprovedAt = now;
        }
        else
        {
            payout.PayoutStatus = RotationalPayoutStatus.Rejected;
            payout.RejectedByChairpersonId = currentUserId; payout.RejectedAt = now;
            payout.RejectionReason = reason!.Trim();
        }
        payout.UpdatedAt = now; payout.UpdatedBy = currentUserId;
        await context.SaveChangesAsync();
        logger.LogInformation("Payout {PayoutId} status changed to {Status}", payoutId, payout.PayoutStatus);
        return PayoutOperationResult.Succeeded(payout);
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
}

public sealed record ConfirmPayoutPaidRequest(
    PaymentMethod? PaymentMethod, string? PaymentReference, DateTime? PaidAt, string? Notes);
public sealed record PayoutOperationResult(bool Success, RotationalPayout? Payout, List<string> Errors)
{
    public static PayoutOperationResult Succeeded(RotationalPayout payout) => new(true, payout, []);
    public static PayoutOperationResult Failed(List<string> errors) => new(false, null, errors);
}
public sealed record PayoutSummary(
    RotationalPayout? Payout, StokvelBankingDetails? BankingDetails,
    CycleContributionSummary ContributionSummary, bool CanApprove, bool CanConfirmPaid, bool IsRecipient);
