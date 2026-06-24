using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;

namespace Sisonke.Web.Services;

public class RotationalStokvelService(ApplicationDbContext context)
{
    // ── Settings ─────────────────────────────────────────────────────────────

    public async Task<RotationalStokvelSetting?> GetSettingsAsync(Guid stokvelId)
    {
        return await context.RotationalStokvelSettings
            .SingleOrDefaultAsync(s => s.StokvelId == stokvelId);
    }

    /// <summary>
    /// Creates or updates the rotational settings for a stokvel.
    /// Only office bearers / admin should call this.
    /// </summary>
    public async Task<RotationalStokvelSetting> SaveSettingsAsync(
        Guid stokvelId,
        decimal contributionAmount,
        string contributionFrequency,
        DateTime startDate,
        int payoutDay,
        string rotationMethod,
        string missedPaymentRule,
        string createdOrUpdatedBy)
    {
        var existing = await context.RotationalStokvelSettings
            .SingleOrDefaultAsync(s => s.StokvelId == stokvelId);

        if (existing is null)
        {
            var setting = new RotationalStokvelSetting
            {
                Id = Guid.NewGuid(),
                StokvelId = stokvelId,
                ContributionAmount = contributionAmount,
                ContributionFrequency = contributionFrequency,
                StartDate = startDate,
                PayoutDay = payoutDay,
                RotationMethod = rotationMethod,
                MissedPaymentRule = missedPaymentRule,
                CyclesGenerated = false,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdOrUpdatedBy
            };
            context.RotationalStokvelSettings.Add(setting);
            await context.SaveChangesAsync();
            return setting;
        }

        existing.ContributionAmount = contributionAmount;
        existing.ContributionFrequency = contributionFrequency;
        existing.StartDate = startDate;
        existing.PayoutDay = payoutDay;
        existing.RotationMethod = rotationMethod;
        existing.MissedPaymentRule = missedPaymentRule;
        existing.UpdatedAt = DateTime.UtcNow;
        existing.UpdatedBy = createdOrUpdatedBy;
        await context.SaveChangesAsync();
        return existing;
    }

    // ── Rotation order ────────────────────────────────────────────────────────

    public async Task<List<RotationOrder>> GetRotationOrderAsync(Guid stokvelId)
    {
        return await context.RotationOrders
            .Where(ro => ro.StokvelId == stokvelId)
            .OrderBy(ro => ro.Position)
            .ToListAsync();
    }

    /// <summary>
    /// Replaces the full rotation order for a stokvel.
    /// Only office bearers / admin should call this.
    /// Cannot be called once cycles have been generated.
    /// </summary>
    public async Task<List<RotationOrder>> SaveRotationOrderAsync(
        Guid stokvelId,
        List<(Guid MemberId, int Position)> orderedMembers,
        string createdBy)
    {
        var settings = await context.RotationalStokvelSettings
            .SingleOrDefaultAsync(s => s.StokvelId == stokvelId);

        if (settings?.CyclesGenerated == true)
            throw new InvalidOperationException("Cannot change rotation order after cycles have been generated.");

        var existing = await context.RotationOrders
            .Where(ro => ro.StokvelId == stokvelId)
            .ToListAsync();

        context.RotationOrders.RemoveRange(existing);

        var newOrders = new List<RotationOrder>();
        foreach (var (memberId, position) in orderedMembers)
        {
            var order = new RotationOrder
            {
                Id = Guid.NewGuid(),
                StokvelId = stokvelId,
                MemberId = memberId,
                Position = position,
                HasReceivedPayout = false,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy
            };
            context.RotationOrders.Add(order);
            newOrders.Add(order);
        }

        await context.SaveChangesAsync();
        return newOrders;
    }

    // ── Cycle generation ──────────────────────────────────────────────────────

    public async Task<List<RotationCycle>> GetCyclesAsync(Guid stokvelId)
    {
        return await context.RotationCycles
            .Where(rc => rc.StokvelId == stokvelId)
            .OrderBy(rc => rc.CycleNumber)
            .ToListAsync();
    }

    /// <summary>
    /// Generates one RotationCycle per member in the rotation order, plus
    /// CycleContribution records for every member in every cycle.
    /// Only office bearers / admin should call this.
    /// </summary>
    public async Task<List<RotationCycle>> GenerateCyclesAsync(Guid stokvelId, string createdBy)
    {
        var settings = await context.RotationalStokvelSettings
            .SingleOrDefaultAsync(s => s.StokvelId == stokvelId);

        if (settings is null)
            throw new InvalidOperationException("Rotational settings not found for this stokvel.");

        if (settings.CyclesGenerated)
            throw new InvalidOperationException("Cycles have already been generated for this stokvel.");

        var rotationOrders = await context.RotationOrders
            .Where(ro => ro.StokvelId == stokvelId)
            .OrderBy(ro => ro.Position)
            .ToListAsync();

        if (rotationOrders.Count == 0)
            throw new InvalidOperationException("No rotation order defined. Add members to the rotation order first.");

        var memberCount = rotationOrders.Count;
        var cycles = new List<RotationCycle>();
        var now = DateTime.UtcNow;

        for (var i = 0; i < rotationOrders.Count; i++)
        {
            var order = rotationOrders[i];

            var dueDate = settings.ContributionFrequency == "Weekly"
                ? settings.StartDate.AddDays(7 * i)
                : settings.StartDate.AddMonths(i);

            var cycle = new RotationCycle
            {
                Id = Guid.NewGuid(),
                StokvelId = stokvelId,
                CycleNumber = i + 1,
                DueDate = dueDate,
                PayoutMemberId = order.MemberId,
                ExpectedAmount = settings.ContributionAmount * memberCount,
                ActualCollectedAmount = 0,
                Status = "Pending",
                CreatedAt = now,
                CreatedBy = createdBy
            };

            context.RotationCycles.Add(cycle);
            cycles.Add(cycle);

            foreach (var memberOrder in rotationOrders)
            {
                context.CycleContributions.Add(new CycleContribution
                {
                    Id = Guid.NewGuid(),
                    RotationCycleId = cycle.Id,
                    MemberId = memberOrder.MemberId,
                    AmountDue = settings.ContributionAmount,
                    AmountPaid = 0,
                    PaymentStatus = "Unpaid",
                    CreatedAt = now,
                    CreatedBy = createdBy
                });
            }
        }

        settings.CyclesGenerated = true;
        settings.UpdatedAt = now;
        settings.UpdatedBy = createdBy;

        await context.SaveChangesAsync();
        return cycles;
    }

    // ── Contributions ─────────────────────────────────────────────────────────

    public async Task<List<CycleContribution>> GetCycleContributionsAsync(Guid rotationCycleId)
    {
        return await context.CycleContributions
            .Where(cc => cc.RotationCycleId == rotationCycleId)
            .OrderBy(cc => cc.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Records a contribution payment. Sets status to Paid or Partial.
    /// Only Treasurer should call this.
    /// </summary>
    public async Task<CycleContribution?> MarkContributionPaidAsync(
        Guid cycleContributionId,
        decimal amountPaid,
        string? paymentReference,
        string markedByUserId)
    {
        var contribution = await context.CycleContributions
            .Include(cc => cc.RotationCycle)
            .SingleOrDefaultAsync(cc => cc.Id == cycleContributionId);

        if (contribution is null) return null;

        contribution.AmountPaid = amountPaid;
        contribution.PaymentReference = paymentReference;
        contribution.MarkedByUserId = markedByUserId;
        contribution.PaidDate = DateTime.UtcNow;
        contribution.UpdatedAt = DateTime.UtcNow;
        contribution.UpdatedBy = markedByUserId;
        contribution.PaymentStatus = amountPaid >= contribution.AmountDue ? "Paid" : "Partial";

        // Recalculate cycle total from other contributions + this payment
        var otherPaid = await context.CycleContributions
            .Where(cc => cc.RotationCycleId == contribution.RotationCycleId && cc.Id != cycleContributionId)
            .SumAsync(cc => cc.AmountPaid);

        contribution.RotationCycle.ActualCollectedAmount = otherPaid + amountPaid;
        contribution.RotationCycle.UpdatedAt = DateTime.UtcNow;
        contribution.RotationCycle.UpdatedBy = markedByUserId;

        await context.SaveChangesAsync();
        return contribution;
    }

    // ── Payouts ───────────────────────────────────────────────────────────────

    public async Task<List<CyclePayout>> GetCyclePayoutsAsync(Guid rotationCycleId)
    {
        return await context.CyclePayouts
            .Where(cp => cp.RotationCycleId == rotationCycleId)
            .OrderBy(cp => cp.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Records the payout for a cycle as paid and marks the rotation order entry as received.
    /// Only Treasurer should call this.
    /// </summary>
    public async Task<CyclePayout> MarkPayoutPaidAsync(
        Guid rotationCycleId,
        decimal amount,
        string? paymentReference,
        string markedByUserId)
    {
        var cycle = await context.RotationCycles
            .SingleOrDefaultAsync(rc => rc.Id == rotationCycleId);

        if (cycle is null)
            throw new InvalidOperationException("Rotation cycle not found.");

        var now = DateTime.UtcNow;

        // Upsert payout record
        var existing = await context.CyclePayouts
            .SingleOrDefaultAsync(cp => cp.RotationCycleId == rotationCycleId && cp.MemberId == cycle.PayoutMemberId);

        CyclePayout payout;
        if (existing is not null)
        {
            existing.Amount = amount;
            existing.PayoutDate = now;
            existing.PaymentReference = paymentReference;
            existing.Status = "Paid";
            existing.MarkedByUserId = markedByUserId;
            existing.UpdatedAt = now;
            existing.UpdatedBy = markedByUserId;
            payout = existing;
        }
        else
        {
            payout = new CyclePayout
            {
                Id = Guid.NewGuid(),
                RotationCycleId = rotationCycleId,
                MemberId = cycle.PayoutMemberId,
                Amount = amount,
                PayoutDate = now,
                PaymentReference = paymentReference,
                Status = "Paid",
                MarkedByUserId = markedByUserId,
                CreatedAt = now,
                CreatedBy = markedByUserId
            };
            context.CyclePayouts.Add(payout);
        }

        // Mark cycle as paid out
        cycle.Status = "PaidOut";
        cycle.PayoutDate = now;
        cycle.UpdatedAt = now;
        cycle.UpdatedBy = markedByUserId;

        // Mark this member's rotation order position as received
        var rotationOrder = await context.RotationOrders
            .SingleOrDefaultAsync(ro => ro.StokvelId == cycle.StokvelId && ro.MemberId == cycle.PayoutMemberId);

        if (rotationOrder is not null)
        {
            rotationOrder.HasReceivedPayout = true;
            rotationOrder.ReceivedCycleId = rotationCycleId;
            rotationOrder.UpdatedAt = now;
            rotationOrder.UpdatedBy = markedByUserId;
        }

        await context.SaveChangesAsync();
        return payout;
    }
}
