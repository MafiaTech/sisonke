using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;
using Sisonke.Web.Services.Jobs;

namespace Sisonke.Web.Services.Billing.Jobs;

/// <summary>
/// Daily: reminds Trialing subscriptions at 30/14/7/3/1 days remaining. Idempotent per
/// (subscription, bucket) via TrialReminderSent — safe to re-run same day (nothing unsent left)
/// and safe after a missed window (sends only the most urgent unsent bucket, never the backlog).
/// </summary>
public sealed class TrialReminderJob(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    IDistributedJobLock jobLock,
    SubscriptionNotificationService notificationService,
    TimeProvider timeProvider,
    ILogger<TrialReminderJob> logger)
{
    public const string JobName = "TrialReminderJob";
    private static readonly int[] Buckets = [30, 14, 7, 3, 1];

    public async Task RunAsync(CancellationToken ct = default)
    {
        await using var lockHandle = await jobLock.TryAcquireAsync(JobName, TimeSpan.FromMinutes(30), ct);
        if (lockHandle is null)
        {
            logger.LogInformation("{Job} is already running on another instance — skipping this tick.", JobName);
            return;
        }

        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var today = timeProvider.GetUtcNow().UtcDateTime.Date;

        var trialing = await context.OrganisationSubscriptions
            .Include(s => s.SubscriptionPlan)
            .Where(s => s.Status == SubscriptionStatus.Trialing && s.TrialEndsAt != null)
            .ToListAsync(ct);

        foreach (var subscription in trialing)
        {
            if (subscription.SubscriptionPlan is null)
            {
                continue;
            }

            var daysRemaining = (subscription.TrialEndsAt!.Value.Date - today).Days;
            if (daysRemaining <= 0 || daysRemaining > Buckets.Max())
            {
                continue; // Day 0/negative is TrialExpiryJob's job; beyond the furthest bucket, nothing to send yet.
            }

            var sentReminders = await context.TrialReminderSents
                .Where(t => t.OrganisationSubscriptionId == subscription.Id)
                .ToListAsync(ct);

            if (sentReminders.Any(t => t.SentAt.Date == today))
            {
                // Already sent a reminder today — even if another bucket is technically still
                // "due" (e.g. day 7 and day 14 were both skipped and we're catching up), only one
                // reminder goes out per day. Re-running the job the same day must send nothing
                // extra; the next unsent bucket waits for tomorrow's run.
                continue;
            }

            var sentBuckets = sentReminders.Select(t => t.Bucket).ToList();

            // Most recent (smallest, i.e. most urgent) unsent bucket that we're within — not the
            // whole backlog of buckets that may have been skipped since the job last ran.
            var bucketsDue = Buckets.Where(bucket => daysRemaining <= bucket && !sentBuckets.Contains(bucket)).ToList();
            if (bucketsDue.Count == 0)
            {
                continue;
            }

            var bucketToSend = bucketsDue.Min();

            context.TrialReminderSents.Add(new TrialReminderSent
            {
                Id = Guid.NewGuid(),
                OrganisationSubscriptionId = subscription.Id,
                Bucket = bucketToSend,
                SentAt = timeProvider.GetUtcNow().UtcDateTime
            });
            await context.SaveChangesAsync(ct);

            await notificationService.SendTrialReminderAsync(context, subscription, subscription.SubscriptionPlan, bucketToSend, ct);
        }
    }
}
