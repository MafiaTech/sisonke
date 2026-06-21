using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Services;

public class RotationalContributionPaymentService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    MemberAccessService memberAccess,
    RotationalPayoutService payoutService,
    ILogger<RotationalContributionPaymentService> logger)
{
    // ── Queries ────────────────────────────────────────────────────────────

    public async Task<List<RotationalContributionPayment>> GetPaymentsForCycleAsync(Guid cycleId)
    {
        await using var context = await dbFactory.CreateDbContextAsync();

        return await context.RotationalContributionPayments
            .AsNoTracking()
            .Include(p => p.Member)
            .Where(p => p.CycleId == cycleId && p.IsActive)
            .OrderBy(p => p.Member.FullName)
            .ToListAsync();
    }

    public async Task<RotationalContributionPayment?> GetMyPaymentForCycleAsync(Guid cycleId, Guid memberId)
    {
        await using var context = await dbFactory.CreateDbContextAsync();

        return await context.RotationalContributionPayments
            .AsNoTracking()
            .Include(p => p.Member)
            .FirstOrDefaultAsync(p => p.CycleId == cycleId && p.MemberId == memberId && p.IsActive);
    }

    public async Task<CycleContributionSummary> GetCycleContributionSummaryAsync(Guid cycleId)
    {
        await using var context = await dbFactory.CreateDbContextAsync();

        var payments = await context.RotationalContributionPayments
            .AsNoTracking()
            .Where(p => p.CycleId == cycleId && p.IsActive)
            .ToListAsync();

        if (payments.Count == 0)
            return new CycleContributionSummary(0, 0, 0, 0, 0, 0, 0, 0, 0);

        var expectedTotal = payments.Sum(p => p.ExpectedAmount);
        var receivedTotal = payments.Sum(p => p.PaidAmount);
        var penaltyTotal = payments.Sum(p => p.PenaltyAmount);

        return new CycleContributionSummary(
            ExpectedTotal: expectedTotal,
            ReceivedTotal: receivedTotal,
            OutstandingTotal: Math.Max(0, expectedTotal - receivedTotal),
            PenaltyTotal: penaltyTotal,
            PaidCount: payments.Count(p => p.PaymentStatus == ContributionPaymentStatus.Paid),
            UnpaidCount: payments.Count(p => p.PaymentStatus == ContributionPaymentStatus.Unpaid),
            PartialCount: payments.Count(p => p.PaymentStatus == ContributionPaymentStatus.PartiallyPaid),
            LateCount: payments.Count(p => p.PaymentStatus == ContributionPaymentStatus.Late),
            TotalExpected: payments.Count);
    }

    // ── Payment record generation ──────────────────────────────────────────

    public async Task<PaymentEnsureResult> EnsurePaymentRecordsForCycleAsync(
        Guid cycleId, string currentUserId)
    {
        await using var context = await dbFactory.CreateDbContextAsync();

        var cycle = await context.RotationalContributionCycles
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == cycleId && c.IsActive);

        if (cycle is null)
            return new PaymentEnsureResult(false, 0, ["Cycle not found or inactive."]);

        if (!await memberAccess.IsOfficeBearerAsync(currentUserId, cycle.StokvelId))
            return new PaymentEnsureResult(false, 0, ["Only office bearers can prepare payment records."]);

        var rotationOrder = await context.RotationalPayoutOrders
            .AsNoTracking()
            .Where(o => o.StokvelId == cycle.StokvelId && o.IsActive)
            .OrderBy(o => o.Position)
            .ToListAsync();

        if (rotationOrder.Count == 0)
            return new PaymentEnsureResult(false, 0, ["No active rotation order found."]);

        var existing = await context.RotationalContributionPayments
            .Where(p => p.CycleId == cycleId && p.IsActive)
            .Select(p => p.MemberId)
            .ToHashSetAsync();

        var now = DateTime.UtcNow;
        var created = 0;

        foreach (var member in rotationOrder)
        {
            if (existing.Contains(member.MemberId)) continue;

            context.RotationalContributionPayments.Add(new RotationalContributionPayment
            {
                Id = Guid.NewGuid(),
                StokvelId = cycle.StokvelId,
                CycleId = cycleId,
                MemberId = member.MemberId,
                ExpectedAmount = cycle.ContributionAmountPerMember,
                PaidAmount = 0,
                PenaltyAmount = 0,
                PaymentStatus = ContributionPaymentStatus.Unpaid,
                IsActive = true,
                CreatedAt = now,
                CreatedBy = currentUserId
            });
            created++;
        }

        if (created > 0)
        {
            await context.SaveChangesAsync();
            logger.LogInformation("Created {Count} payment records for cycle {CycleId}", created, cycleId);
        }

        return new PaymentEnsureResult(true, created, []);
    }

    public async Task<PaymentEnsureResult> EnsurePaymentRecordsForStokvelAsync(
        Guid stokvelId, string currentUserId)
    {
        if (!await memberAccess.IsOfficeBearerAsync(currentUserId, stokvelId))
            return new PaymentEnsureResult(false, 0, ["Only office bearers can prepare payment records."]);

        await using var context = await dbFactory.CreateDbContextAsync();

        var cycles = await context.RotationalContributionCycles
            .AsNoTracking()
            .Where(c => c.StokvelId == stokvelId && c.IsActive)
            .ToListAsync();

        var totalCreated = 0;
        var errors = new List<string>();

        foreach (var cycle in cycles)
        {
            var result = await EnsurePaymentRecordsForCycleAsync(cycle.Id, currentUserId);
            if (result.Success) totalCreated += result.RecordsCreated;
            else errors.AddRange(result.Errors);
        }

        return new PaymentEnsureResult(errors.Count == 0, totalCreated, errors);
    }

    // ── Payment confirmation (Treasurer only) ──────────────────────────────

    public async Task<PaymentConfirmResult> ConfirmPaymentAsync(
        Guid paymentId, ConfirmPaymentRequest request, string currentUserId)
    {
        var errors = ValidateConfirmRequest(request);
        if (errors.Count > 0)
            return PaymentConfirmResult.Failed(errors);

        await using var context = await dbFactory.CreateDbContextAsync();

        var payment = await context.RotationalContributionPayments
            .Include(p => p.Cycle)
            .FirstOrDefaultAsync(p => p.Id == paymentId && p.IsActive);

        if (payment is null)
            return PaymentConfirmResult.Failed(["Payment record not found."]);

        if (!await memberAccess.CanManagePaymentsAsync(currentUserId, payment.StokvelId))
            return PaymentConfirmResult.Failed(["Only the Treasurer can confirm contribution payments."]);

        var stokvel = await context.Stokvels
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == payment.StokvelId && !s.IsDeleted);

        if (stokvel is null || !stokvel.IsActive)
            return PaymentConfirmResult.Failed(["Cannot confirm payment for an inactive stokvel."]);

        if (payment.Cycle.Status == RotationalCycleStatus.Cancelled)
            return PaymentConfirmResult.Failed(["Cannot confirm payment for a cancelled cycle."]);

        var config = await context.RotationalStokvelConfigurations
            .AsNoTracking()
            .Where(c => c.StokvelId == payment.StokvelId && c.IsActive)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync();

        var penalty = CalculatePenalty(payment, request.PaymentDate, config);
        var isLate = penalty > 0;

        ContributionPaymentStatus newStatus;
        if (request.PaymentStatus != ContributionPaymentStatus.Waived && request.PaidAmount <= 0)
            return PaymentConfirmResult.Failed(["Payment amount cannot be less than zero."]);

        if (request.PaidAmount >= payment.ExpectedAmount + penalty)
            newStatus = ContributionPaymentStatus.Paid;
        else if (request.PaidAmount > 0)
            newStatus = isLate ? ContributionPaymentStatus.Late : ContributionPaymentStatus.PartiallyPaid;
        else
            newStatus = ContributionPaymentStatus.Unpaid;

        if (request.PaymentStatus == ContributionPaymentStatus.Waived)
            newStatus = ContributionPaymentStatus.Waived;

        await using var transaction = await context.Database.BeginTransactionAsync();
        var now = DateTime.UtcNow;
        payment.PaidAmount = request.PaidAmount;
        payment.PaymentDate = request.PaymentDate;
        payment.PaymentMethod = request.PaymentMethod;
        payment.ReferenceNumber = request.ReferenceNumber;
        payment.Notes = request.Notes;
        payment.PenaltyAmount = penalty;
        payment.PaymentStatus = newStatus;
        payment.ConfirmedByTreasurerId = currentUserId;
        payment.ConfirmedAt = now;
        payment.UpdatedAt = now;
        payment.UpdatedBy = currentUserId;

        var surplus = request.PaymentStatus == ContributionPaymentStatus.Waived
            ? 0
            : Math.Max(0, request.PaidAmount - payment.ExpectedAmount - penalty);
        if (surplus > 0 && stokvel.Archetype != StokvelArchetype.BurialSociety)
        {
            var alreadyCredited = await context.MemberSurplusWalletTransactions.AnyAsync(entry =>
                entry.SourceReferenceId == payment.Id &&
                entry.SourceType == WalletTransactionSourceType.ContributionOverpayment &&
                entry.TransactionType == WalletTransactionType.Credit);
            if (!alreadyCredited)
            {
                var wallet = await context.MemberSurplusWallets.FirstOrDefaultAsync(item =>
                    item.StokvelId == payment.StokvelId && item.MemberId == payment.MemberId && item.IsActive);
                if (wallet is null)
                {
                    wallet = new MemberSurplusWallet
                    {
                        Id = Guid.NewGuid(), StokvelId = payment.StokvelId, MemberId = payment.MemberId,
                        IsActive = true, CreatedAt = now, CreatedBy = currentUserId
                    };
                    context.MemberSurplusWallets.Add(wallet);
                }
                wallet.AvailableBalance += surplus;
                wallet.TotalCredits += surplus;
                wallet.UpdatedAt = now;
                wallet.UpdatedBy = currentUserId;
                context.MemberSurplusWalletTransactions.Add(new MemberSurplusWalletTransaction
                {
                    Id = Guid.NewGuid(), StokvelId = payment.StokvelId, WalletId = wallet.Id,
                    MemberId = payment.MemberId, TransactionType = WalletTransactionType.Credit,
                    Amount = surplus, BalanceAfterTransaction = wallet.AvailableBalance,
                    SourceType = WalletTransactionSourceType.ContributionOverpayment,
                    SourceReferenceId = payment.Id, Description = "Contribution overpayment",
                    CreatedAt = now, CreatedBy = currentUserId
                });
            }
        }

        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        logger.LogInformation("Payment {PaymentId} confirmed by {UserId} — status={Status}", paymentId, currentUserId, newStatus);

        await UpdateCycleStatusFromPaymentsAsync(payment.CycleId, currentUserId);

        return PaymentConfirmResult.Succeeded(newStatus, penalty);
    }

    // ── Cycle status update ────────────────────────────────────────────────

    public async Task UpdateCycleStatusFromPaymentsAsync(Guid cycleId, string currentUserId)
    {
        await using var context = await dbFactory.CreateDbContextAsync();

        var cycle = await context.RotationalContributionCycles
            .FirstOrDefaultAsync(c => c.Id == cycleId && c.IsActive);

        if (cycle is null) return;

        var payments = await context.RotationalContributionPayments
            .Where(p => p.CycleId == cycleId && p.IsActive)
            .ToListAsync();

        if (payments.Count == 0) return;

        var allSettled = payments.All(p =>
            p.PaymentStatus == ContributionPaymentStatus.Paid ||
            p.PaymentStatus == ContributionPaymentStatus.Waived);

        var newStatus = allSettled
            ? RotationalCycleStatus.ReadyForPayout
            : RotationalCycleStatus.Open;

        if (cycle.Status != newStatus)
        {
            cycle.Status = newStatus;
            cycle.UpdatedAt = DateTime.UtcNow;
            cycle.UpdatedBy = currentUserId;
            await context.SaveChangesAsync();
            logger.LogInformation("Cycle {CycleId} status updated to {Status}", cycleId, newStatus);
        }

        if (newStatus == RotationalCycleStatus.ReadyForPayout)
        {
            var payoutResult = await payoutService.EnsurePayoutForReadyCycleAsync(cycleId, currentUserId);
            if (!payoutResult.Success)
                logger.LogWarning("Payout could not be prepared for cycle {CycleId}: {Errors}",
                    cycleId, string.Join("; ", payoutResult.Errors));
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private static decimal CalculatePenalty(
        RotationalContributionPayment payment,
        DateTime? paymentDate,
        RotationalStokvelConfiguration? config)
    {
        if (config is null || paymentDate is null) return 0;
        if (config.LatePenaltyType == LatePenaltyType.None) return 0;

        var deadline = payment.Cycle.ContributionDueDate.AddDays(config.GracePeriodDays);
        if (paymentDate.Value <= deadline) return 0;

        return config.LatePenaltyType switch
        {
            LatePenaltyType.FixedAmount => config.LatePenaltyAmount ?? 0,
            LatePenaltyType.Percentage => Math.Round(
                payment.ExpectedAmount * (config.LatePenaltyAmount ?? 0) / 100, 2),
            _ => 0
        };
    }

    private static List<string> ValidateConfirmRequest(ConfirmPaymentRequest request)
    {
        var errors = new List<string>();

        if (request.PaymentStatus == ContributionPaymentStatus.Waived)
            return errors;

        if (request.PaidAmount < 0)
            errors.Add("Payment amount cannot be less than zero.");

        if (request.PaymentDate is null)
            errors.Add("Payment date is required.");

        if (request.PaymentMethod is null)
            errors.Add("Payment method is required.");

        return errors;
    }
}

public sealed record CycleContributionSummary(
    decimal ExpectedTotal,
    decimal ReceivedTotal,
    decimal OutstandingTotal,
    decimal PenaltyTotal,
    int PaidCount,
    int UnpaidCount,
    int PartialCount,
    int LateCount,
    int TotalExpected);

public sealed record ConfirmPaymentRequest(
    decimal PaidAmount,
    DateTime? PaymentDate,
    ContributionPaymentMethod? PaymentMethod,
    string? ReferenceNumber,
    string? Notes,
    ContributionPaymentStatus PaymentStatus = ContributionPaymentStatus.Paid);

public sealed record PaymentConfirmResult(
    bool Success,
    ContributionPaymentStatus? NewStatus,
    decimal PenaltyApplied,
    List<string> Errors)
{
    public static PaymentConfirmResult Succeeded(ContributionPaymentStatus status, decimal penalty)
        => new(true, status, penalty, []);

    public static PaymentConfirmResult Failed(List<string> errors)
        => new(false, null, 0, errors);
}

public sealed record PaymentEnsureResult(bool Success, int RecordsCreated, List<string> Errors);
