using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Services;

public class RotationalContributionCycleService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    MemberAccessService memberAccess,
    ILogger<RotationalContributionCycleService> logger)
{
    public async Task<List<RotationalContributionCycle>> GetActiveCyclesAsync(Guid stokvelId)
    {
        await using var context = await dbFactory.CreateDbContextAsync();

        return await context.RotationalContributionCycles
            .AsNoTracking()
            .Include(c => c.PayoutMember)
            .Where(c => c.StokvelId == stokvelId && c.IsActive)
            .OrderBy(c => c.CycleNumber)
            .ToListAsync();
    }

    public async Task<RotationalContributionCycle?> GetCurrentCycleAsync(Guid stokvelId)
    {
        await using var context = await dbFactory.CreateDbContextAsync();

        return await context.RotationalContributionCycles
            .AsNoTracking()
            .Include(c => c.PayoutMember)
            .Where(c => c.StokvelId == stokvelId && c.IsActive &&
                        (c.Status == RotationalCycleStatus.Open ||
                         c.Status == RotationalCycleStatus.ContributionsDue ||
                         c.Status == RotationalCycleStatus.ReadyForPayout ||
                         c.Status == RotationalCycleStatus.PayoutPending))
            .OrderBy(c => c.CycleNumber)
            .FirstOrDefaultAsync();
    }

    public async Task<RotationalContributionCycle?> GetCycleByIdAsync(Guid cycleId)
    {
        await using var context = await dbFactory.CreateDbContextAsync();

        return await context.RotationalContributionCycles
            .AsNoTracking()
            .Include(c => c.PayoutMember)
            .Include(c => c.Configuration)
            .FirstOrDefaultAsync(c => c.Id == cycleId);
    }

    public async Task<bool> CanManageContributionCyclesAsync(Guid stokvelId, string userId)
        => await memberAccess.IsOfficeBearerAsync(userId, stokvelId);

    public async Task<CycleSummary> GetCycleSummaryAsync(Guid stokvelId, string? userId)
    {
        await using var context = await dbFactory.CreateDbContextAsync();

        var cycles = await context.RotationalContributionCycles
            .AsNoTracking()
            .Include(c => c.PayoutMember)
            .Where(c => c.StokvelId == stokvelId && c.IsActive)
            .OrderBy(c => c.CycleNumber)
            .ToListAsync();

        if (cycles.Count == 0)
            return new CycleSummary(false, 0, 0, null, null, null, null, []);

        var current = cycles.FirstOrDefault(c =>
            c.Status == RotationalCycleStatus.Open ||
            c.Status == RotationalCycleStatus.ContributionsDue ||
            c.Status == RotationalCycleStatus.ReadyForPayout ||
            c.Status == RotationalCycleStatus.PayoutPending);

        current ??= cycles.FirstOrDefault(c => c.Status == RotationalCycleStatus.Pending);

        var completed = cycles.Count(c =>
            c.Status == RotationalCycleStatus.PaidOut || c.Status == RotationalCycleStatus.Closed);

        bool canManage = userId != null && await memberAccess.IsOfficeBearerAsync(userId, stokvelId);

        return new CycleSummary(
            IsGenerated: true,
            TotalCycles: cycles.Count,
            CompletedCycles: completed,
            CurrentCycle: current,
            NextCycle: current != null
                ? cycles.FirstOrDefault(c => c.CycleNumber == current.CycleNumber + 1)
                : null,
            CurrentCycleContributionDue: current?.ContributionDueDate,
            CurrentCyclePayoutDate: current?.ScheduledPayoutDate,
            AllCycles: cycles);
    }

    public async Task<CycleGenerationResult> GenerateContributionCyclesAsync(
        Guid stokvelId, string currentUserId)
    {
        if (!await memberAccess.IsOfficeBearerAsync(currentUserId, stokvelId))
            return CycleGenerationResult.Failed(["You do not have permission to generate cycles."]);

        await using var context = await dbFactory.CreateDbContextAsync();

        var existingCycles = await context.RotationalContributionCycles
            .Where(c => c.StokvelId == stokvelId && c.IsActive)
            .AnyAsync();

        if (existingCycles)
            return CycleGenerationResult.Failed(["Contribution cycles already exist. Use regenerate to replace them."]);

        return await GenerateCyclesInternalAsync(context, stokvelId, currentUserId);
    }

    public async Task<CycleGenerationResult> RegenerateContributionCyclesAsync(
        Guid stokvelId, string currentUserId)
    {
        if (!await memberAccess.IsOfficeBearerAsync(currentUserId, stokvelId))
            return CycleGenerationResult.Failed(["You do not have permission to regenerate cycles."]);

        await using var context = await dbFactory.CreateDbContextAsync();

        await DeactivateExistingCyclesInternalAsync(context, stokvelId, currentUserId);
        return await GenerateCyclesInternalAsync(context, stokvelId, currentUserId);
    }

    public async Task DeactivateExistingCyclesAsync(Guid stokvelId, string currentUserId)
    {
        await using var context = await dbFactory.CreateDbContextAsync();
        await DeactivateExistingCyclesInternalAsync(context, stokvelId, currentUserId);
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    private static async Task DeactivateExistingCyclesInternalAsync(
        ApplicationDbContext context, Guid stokvelId, string currentUserId)
    {
        var existing = await context.RotationalContributionCycles
            .Where(c => c.StokvelId == stokvelId && c.IsActive)
            .ToListAsync();

        var now = DateTime.UtcNow;
        var cycleIds = existing.Select(c => c.Id).ToList();

        foreach (var cycle in existing)
        {
            cycle.IsActive = false;
            cycle.UpdatedAt = now;
            cycle.UpdatedBy = currentUserId;
        }

        // Deactivate associated payment records
        var payments = await context.RotationalContributionPayments
            .Where(p => cycleIds.Contains(p.CycleId) && p.IsActive)
            .ToListAsync();

        foreach (var payment in payments)
        {
            payment.IsActive = false;
            payment.UpdatedAt = now;
            payment.UpdatedBy = currentUserId;
        }

        await context.SaveChangesAsync();
    }

    private async Task<CycleGenerationResult> GenerateCyclesInternalAsync(
        ApplicationDbContext context, Guid stokvelId, string currentUserId)
    {
        var config = await context.RotationalStokvelConfigurations
            .AsNoTracking()
            .Where(c => c.StokvelId == stokvelId && c.IsActive)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync();

        if (config is null)
            return CycleGenerationResult.Failed(["No active configuration found. Configure the stokvel first."]);

        var errors = ValidateConfiguration(config);
        if (errors.Count > 0)
            return CycleGenerationResult.Failed(errors);

        var rotationOrder = await context.RotationalPayoutOrders
            .AsNoTracking()
            .Include(o => o.Member)
            .Where(o => o.StokvelId == stokvelId && o.IsActive)
            .OrderBy(o => o.Position)
            .ToListAsync();

        if (rotationOrder.Count < 2)
            return CycleGenerationResult.Failed(["At least 2 members must be in the rotation order."]);

        var cycles = BuildCycles(config, rotationOrder, currentUserId);
        context.RotationalContributionCycles.AddRange(cycles);

        try
        {
            await context.SaveChangesAsync();
            logger.LogInformation("Generated {Count} contribution cycles for stokvel {StokvelId}", cycles.Count, stokvelId);

            // Auto-generate payment records for every cycle × every member in order
            var now = DateTime.UtcNow;
            foreach (var cycle in cycles)
            {
                foreach (var member in rotationOrder)
                {
                    context.RotationalContributionPayments.Add(new RotationalContributionPayment
                    {
                        Id = Guid.NewGuid(),
                        StokvelId = stokvelId,
                        CycleId = cycle.Id,
                        MemberId = member.MemberId,
                        ExpectedAmount = cycle.ContributionAmountPerMember,
                        PaidAmount = 0,
                        PenaltyAmount = 0,
                        PaymentStatus = ContributionPaymentStatus.Unpaid,
                        IsActive = true,
                        CreatedAt = now,
                        CreatedBy = currentUserId
                    });
                }
            }

            await context.SaveChangesAsync();
            logger.LogInformation("Generated payment records for {CycleCount} cycles, {MemberCount} members each", cycles.Count, rotationOrder.Count);

            return CycleGenerationResult.Succeeded(cycles.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save contribution cycles for stokvel {StokvelId}", stokvelId);
            return CycleGenerationResult.Failed(["Failed to save contribution cycles. Please try again."]);
        }
    }

    private static List<string> ValidateConfiguration(RotationalStokvelConfiguration config)
    {
        var errors = new List<string>();

        if (config.ContributionAmount <= 0)
            errors.Add("Contribution amount must be greater than zero.");

        if (config.PayoutAmount <= 0)
            errors.Add("Payout amount must be greater than zero.");

        if (config.RotationStartDate == default)
            errors.Add("Rotation start date must be set.");

        if (config.ContributionDueDay <= 0)
            errors.Add("Contribution due day must be set.");

        return errors;
    }

    private static List<RotationalContributionCycle> BuildCycles(
        RotationalStokvelConfiguration config,
        List<RotationalPayoutOrder> order,
        string createdBy)
    {
        var cycles = new List<RotationalContributionCycle>();
        var memberCount = order.Count;
        var now = DateTime.UtcNow;
        var startDate = config.RotationStartDate;

        for (int i = 0; i < memberCount; i++)
        {
            var member = order[i];
            var cycleStart = CalculateCycleStart(startDate, config.ContributionFrequency, i);
            var cycleEnd = CalculateCycleEnd(startDate, config.ContributionFrequency, i);
            var dueDate = CalculateDueDate(cycleStart, cycleEnd, config.ContributionFrequency, config.ContributionDueDay);
            var payoutDate = cycleEnd;

            cycles.Add(new RotationalContributionCycle
            {
                Id = Guid.NewGuid(),
                StokvelId = config.StokvelId,
                ConfigurationId = config.Id,
                PayoutOrderId = member.Id,
                PayoutMemberId = member.MemberId,
                CycleNumber = i + 1,
                CycleName = $"Cycle {i + 1} — {member.Member.FullName}",
                CycleStartDate = cycleStart,
                CycleEndDate = cycleEnd,
                ContributionDueDate = dueDate,
                ScheduledPayoutDate = payoutDate,
                ContributionAmountPerMember = config.ContributionAmount,
                ExpectedTotalContributionAmount = config.ContributionAmount * memberCount,
                ExpectedPayoutAmount = config.PayoutAmount,
                Status = i == 0 ? RotationalCycleStatus.Open : RotationalCycleStatus.Pending,
                IsActive = true,
                CreatedAt = now,
                CreatedBy = createdBy
            });
        }

        return cycles;
    }

    private static DateTime CalculateCycleStart(DateTime baseStart, RotationalFrequency frequency, int index)
        => frequency switch
        {
            RotationalFrequency.Weekly => baseStart.AddDays(7 * index),
            RotationalFrequency.Fortnightly => baseStart.AddDays(14 * index),
            _ => baseStart.AddMonths(index)
        };

    private static DateTime CalculateCycleEnd(DateTime baseStart, RotationalFrequency frequency, int index)
        => frequency switch
        {
            RotationalFrequency.Weekly => baseStart.AddDays(7 * (index + 1)).AddDays(-1),
            RotationalFrequency.Fortnightly => baseStart.AddDays(14 * (index + 1)).AddDays(-1),
            _ => baseStart.AddMonths(index + 1).AddDays(-1)
        };

    private static DateTime CalculateDueDate(
        DateTime cycleStart, DateTime cycleEnd,
        RotationalFrequency frequency, int dueDayConfig)
    {
        if (frequency == RotationalFrequency.Monthly)
        {
            // dueDayConfig is day-of-month; clamp to last day of cycle month
            var year = cycleStart.Year;
            var month = cycleStart.Month;
            var lastDay = DateTime.DaysInMonth(year, month);
            var day = Math.Min(dueDayConfig, lastDay);
            var candidate = new DateTime(year, month, day);
            // If due day already passed in this month, push to next month's due day
            return candidate >= cycleStart ? candidate : new DateTime(year, month, lastDay);
        }

        // For weekly/fortnightly: dueDayConfig is 1=Mon … 7=Sun (ISO day-of-week)
        // Find the matching day within the cycle window
        var target = (DayOfWeek)((dueDayConfig % 7)); // ISO 7=Sun → DayOfWeek.Sunday=0
        var d = cycleStart;
        while (d.DayOfWeek != target && d <= cycleEnd)
            d = d.AddDays(1);

        return d <= cycleEnd ? d : cycleEnd;
    }
}

public sealed record CycleSummary(
    bool IsGenerated,
    int TotalCycles,
    int CompletedCycles,
    RotationalContributionCycle? CurrentCycle,
    RotationalContributionCycle? NextCycle,
    DateTime? CurrentCycleContributionDue,
    DateTime? CurrentCyclePayoutDate,
    List<RotationalContributionCycle> AllCycles);

public sealed record CycleGenerationResult(bool Success, int CyclesGenerated, List<string> Errors)
{
    public static CycleGenerationResult Succeeded(int count) => new(true, count, []);
    public static CycleGenerationResult Failed(List<string> errors) => new(false, 0, errors);
}
