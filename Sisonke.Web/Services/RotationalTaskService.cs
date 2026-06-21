using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Services;

public sealed class RotationalTaskService(IDbContextFactory<ApplicationDbContext> dbFactory)
{
    /// <summary>
    /// Returns all task-relevant state for a rotational stokvel in one efficient call.
    /// Use for office bearer dashboards and task pages.
    /// </summary>
    public async Task<RotationalTaskState> GetTaskStateAsync(Guid stokvelId)
    {
        await using var context = await dbFactory.CreateDbContextAsync();

        // Active non-terminal payouts — ReadyForApproval or Approved
        var pendingPayouts = await context.RotationalPayouts
            .AsNoTracking()
            .Include(p => p.PayoutMember)
            .Include(p => p.Cycle)
            .Where(p => p.StokvelId == stokvelId && p.IsActive &&
                        (p.PayoutStatus == RotationalPayoutStatus.ReadyForApproval ||
                         p.PayoutStatus == RotationalPayoutStatus.Approved))
            .ToListAsync();

        var pendingApproval = pendingPayouts
            .FirstOrDefault(p => p.PayoutStatus == RotationalPayoutStatus.ReadyForApproval);
        var approved = pendingPayouts
            .FirstOrDefault(p => p.PayoutStatus == RotationalPayoutStatus.Approved);

        // Current open cycle
        var currentCycle = await context.RotationalContributionCycles
            .AsNoTracking()
            .Where(c => c.StokvelId == stokvelId && c.IsActive &&
                        (c.Status == RotationalCycleStatus.Open ||
                         c.Status == RotationalCycleStatus.ContributionsDue ||
                         c.Status == RotationalCycleStatus.ReadyForPayout ||
                         c.Status == RotationalCycleStatus.PayoutPending))
            .OrderBy(c => c.CycleNumber)
            .FirstOrDefaultAsync();

        var unpaidCount = 0;
        var totalCount = 0;
        var outstanding = 0m;

        if (currentCycle is not null)
        {
            var payments = await context.RotationalContributionPayments
                .AsNoTracking()
                .Where(p => p.CycleId == currentCycle.Id && p.IsActive)
                .Select(p => new { p.PaymentStatus, p.ExpectedAmount, p.PaidAmount })
                .ToListAsync();

            unpaidCount = payments.Count(p =>
                p.PaymentStatus == ContributionPaymentStatus.Unpaid ||
                p.PaymentStatus == ContributionPaymentStatus.PartiallyPaid);
            totalCount = payments.Count;
            outstanding = payments.Sum(p => Math.Max(0m, p.ExpectedAmount - p.PaidAmount));
        }

        var hasConfig = await context.RotationalStokvelConfigurations
            .AnyAsync(c => c.StokvelId == stokvelId && c.IsActive);
        var hasOrder = await context.RotationalPayoutOrders
            .AnyAsync(o => o.StokvelId == stokvelId && o.IsActive);
        var hasCycles = await context.RotationalContributionCycles
            .AnyAsync(c => c.StokvelId == stokvelId && c.IsActive);
        var hasBanking = await context.StokvelBankingDetails
            .AnyAsync(b => b.StokvelId == stokvelId && b.IsActive);

        return new RotationalTaskState(
            PendingApprovalPayout: pendingApproval,
            ApprovedPayout: approved,
            CurrentCycle: currentCycle,
            UnpaidContributionCount: unpaidCount,
            TotalContributionCount: totalCount,
            OutstandingContributionAmount: outstanding,
            HasActiveConfig: hasConfig,
            HasActiveOrder: hasOrder,
            HasActiveCycles: hasCycles,
            HasBankingDetails: hasBanking);
    }

    /// <summary>
    /// Returns the rotational view data for an ordinary member: their current cycle, payment,
    /// banking details for deposit, and current payout status.
    /// </summary>
    public async Task<MemberRotationalView> GetMemberRotationalViewAsync(Guid stokvelId, Guid memberId)
    {
        await using var context = await dbFactory.CreateDbContextAsync();

        var currentCycle = await context.RotationalContributionCycles
            .AsNoTracking()
            .Where(c => c.StokvelId == stokvelId && c.IsActive &&
                        (c.Status == RotationalCycleStatus.Open ||
                         c.Status == RotationalCycleStatus.ContributionsDue ||
                         c.Status == RotationalCycleStatus.ReadyForPayout ||
                         c.Status == RotationalCycleStatus.PayoutPending))
            .OrderBy(c => c.CycleNumber)
            .FirstOrDefaultAsync();

        RotationalContributionPayment? myPayment = null;
        if (currentCycle is not null)
        {
            myPayment = await context.RotationalContributionPayments
                .AsNoTracking()
                .FirstOrDefaultAsync(p =>
                    p.CycleId == currentCycle.Id && p.MemberId == memberId && p.IsActive);
        }

        var banking = await context.StokvelBankingDetails
            .AsNoTracking()
            .Where(b => b.StokvelId == stokvelId && b.IsActive && b.IsPrimary)
            .OrderByDescending(b => b.CreatedAt)
            .FirstOrDefaultAsync();

        // Most recent non-cancelled payout (could be awaiting approval, approved, or paid)
        var currentPayout = await context.RotationalPayouts
            .AsNoTracking()
            .Include(p => p.Cycle)
            .Where(p => p.StokvelId == stokvelId && p.IsActive &&
                        p.PayoutStatus != RotationalPayoutStatus.Cancelled)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();

        var isCurrentRecipient = currentPayout is not null &&
                                  currentPayout.PayoutMemberId == memberId;

        return new MemberRotationalView(currentCycle, myPayment, banking, currentPayout, isCurrentRecipient);
    }
}

public sealed record RotationalTaskState(
    RotationalPayout? PendingApprovalPayout,
    RotationalPayout? ApprovedPayout,
    RotationalContributionCycle? CurrentCycle,
    int UnpaidContributionCount,
    int TotalContributionCount,
    decimal OutstandingContributionAmount,
    bool HasActiveConfig,
    bool HasActiveOrder,
    bool HasActiveCycles,
    bool HasBankingDetails)
{
    public bool HasAnyPayoutTask =>
        PendingApprovalPayout is not null || ApprovedPayout is not null;

    public bool HasContributionTask =>
        UnpaidContributionCount > 0 && CurrentCycle is not null;

    public int RotationalPendingCount =>
        (PendingApprovalPayout is not null ? 1 : 0) +
        (ApprovedPayout is not null ? 1 : 0) +
        (HasContributionTask ? 1 : 0);
}

public sealed record MemberRotationalView(
    RotationalContributionCycle? CurrentCycle,
    RotationalContributionPayment? MyPayment,
    StokvelBankingDetails? BankingDetails,
    RotationalPayout? CurrentPayout,
    bool IsCurrentRecipient);
