using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;
using Sisonke.Web.Services.Jobs;

namespace Sisonke.Web.Services.Billing.Jobs;

/// <summary>
/// Daily reconciliation safety net — renewals are primarily webhook-driven (charge.success /
/// invoice.payment_failed). This job never charges a card itself; it only flags, for internal
/// investigation, a subscription whose NextBillingAt passed with no corresponding
/// SubscriptionPayment row after a grace window (time for the webhook to arrive). Charging here
/// too would risk a double-charge if the webhook is merely late rather than lost.
/// </summary>
public sealed class RenewalJob(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    IDistributedJobLock jobLock,
    TimeProvider timeProvider,
    ILogger<RenewalJob> logger)
{
    public const string JobName = "RenewalJob";
    private static readonly TimeSpan GracePeriod = TimeSpan.FromHours(24);
    private static readonly TimeSpan ReflagCooldown = TimeSpan.FromDays(1);

    public async Task RunAsync(CancellationToken ct = default)
    {
        await using var lockHandle = await jobLock.TryAcquireAsync(JobName, TimeSpan.FromMinutes(30), ct);
        if (lockHandle is null)
        {
            logger.LogInformation("{Job} is already running on another instance — skipping this tick.", JobName);
            return;
        }

        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var cutoff = now - GracePeriod;

        var candidates = await context.OrganisationSubscriptions
            .Where(s =>
                (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trialing || s.Status == SubscriptionStatus.PastDue) &&
                s.NextBillingAt != null && s.NextBillingAt < cutoff)
            .ToListAsync(ct);

        foreach (var subscription in candidates)
        {
            var hasPaymentSinceDue = await context.SubscriptionPayments
                .AnyAsync(p => p.OrganisationSubscriptionId == subscription.Id && p.CreatedAt >= subscription.NextBillingAt!.Value, ct);

            if (hasPaymentSinceDue)
            {
                continue; // Reconciled — a payment attempt (success or failure) was recorded.
            }

            var alreadyFlaggedRecently = await context.SubscriptionEvents
                .AnyAsync(e =>
                    e.OrganisationSubscriptionId == subscription.Id &&
                    e.EventType == SubscriptionEventType.NotificationSent &&
                    e.Notes != null && e.Notes.StartsWith("RenewalJob:") &&
                    e.OccurredAt >= now - ReflagCooldown, ct);

            if (alreadyFlaggedRecently)
            {
                continue; // Already flagged within the cooldown — don't spam the audit log every tick.
            }

            logger.LogWarning(
                "Subscription {SubscriptionId}: NextBillingAt {NextBillingAt} passed with no payment record — needs investigation (webhook may be lost, not just late).",
                subscription.Id, subscription.NextBillingAt);

            context.SubscriptionEvents.Add(new SubscriptionEvent
            {
                Id = Guid.NewGuid(),
                OrganisationSubscriptionId = subscription.Id,
                EventType = SubscriptionEventType.NotificationSent,
                Notes = $"RenewalJob: NextBillingAt ({subscription.NextBillingAt:yyyy-MM-dd}) passed with no SubscriptionPayment record. Needs manual investigation — not auto-charged to avoid a double charge.",
                OccurredAt = now
            });

            await context.SaveChangesAsync(ct);
        }
    }
}
