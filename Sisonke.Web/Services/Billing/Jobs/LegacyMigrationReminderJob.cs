using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Enums;
using Sisonke.Web.Services.Entitlements;
using Sisonke.Web.Services.Jobs;

namespace Sisonke.Web.Services.Billing.Jobs;

/// <summary>
/// Daily: reminds LegacyUnsubscribed organisations to choose a plan ahead of the configured
/// migration cutover date, at most once every EntitlementOptions.LegacyMigrationReminderIntervalDays.
/// </summary>
public sealed class LegacyMigrationReminderJob(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    IDistributedJobLock jobLock,
    SubscriptionNotificationService notificationService,
    EntitlementOptions entitlementOptions,
    TimeProvider timeProvider,
    ILogger<LegacyMigrationReminderJob> logger)
{
    public const string JobName = "LegacyMigrationReminderJob";

    public async Task RunAsync(CancellationToken ct = default)
    {
        await using var lockHandle = await jobLock.TryAcquireAsync(JobName, TimeSpan.FromMinutes(30), ct);
        if (lockHandle is null)
        {
            logger.LogInformation("{Job} is already running on another instance — skipping this tick.", JobName);
            return;
        }

        if (entitlementOptions.LegacyGraceCutoverUtc == DateTime.MaxValue)
        {
            return; // No cutover configured yet — nothing to remind anyone about.
        }

        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var now = timeProvider.GetUtcNow().UtcDateTime;

        if (now >= entitlementOptions.LegacyGraceCutoverUtc)
        {
            return; // Past cutover — EntitlementService already denies these; a reminder is moot.
        }

        var legacySubscriptions = await context.OrganisationSubscriptions
            .Where(s => s.Status == SubscriptionStatus.LegacyUnsubscribed)
            .ToListAsync(ct);

        var cooldown = TimeSpan.FromDays(Math.Max(1, entitlementOptions.LegacyMigrationReminderIntervalDays));

        foreach (var subscription in legacySubscriptions)
        {
            var lastReminder = await context.SubscriptionEvents
                .Where(e => e.OrganisationSubscriptionId == subscription.Id && e.EventType == SubscriptionEventType.LegacyMigrationReminderSent)
                .OrderByDescending(e => e.OccurredAt)
                .Select(e => (DateTime?)e.OccurredAt)
                .FirstOrDefaultAsync(ct);

            if (lastReminder is not null && now - lastReminder.Value < cooldown)
            {
                continue;
            }

            await notificationService.SendLegacyMigrationReminderAsync(context, subscription, entitlementOptions.LegacyGraceCutoverUtc, ct);
        }
    }
}
