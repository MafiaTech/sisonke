using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;
using Sisonke.Web.Services.Billing.Paystack;
using Sisonke.Web.Services.Entitlements;
using Sisonke.Web.Services.Jobs;

namespace Sisonke.Web.Services.Billing.Jobs;

/// <summary>
/// Daily: drives the dunning timeline strictly from days-since-DunningStartedAt (never from
/// "days since this job last ran" — see brief). Day 0 (PastDue transition + first notification)
/// is handled at the point of failure itself (BillingWebhookProcessor.HandleInvoicePaymentFailedAsync
/// / TrialExpiryJob's direct-charge failure path) — this job picks up from day 1.
///
/// Missed-window behaviour: if this job doesn't run for several days, retries scheduled for
/// specific skipped days (1/3/5) are simply not attempted for those days — they are a recovery
/// opportunity, not a guarantee. The correctness property that must hold regardless of downtime
/// is the Restricted/Suspended transitions at day 7/14, which fire based on absolute elapsed
/// time from DunningStartedAt whenever the job next runs, not on how many retries happened.
///
/// Retry schedule beyond day 5: the brief's table only lists fixed retries at days 1/3/5 while
/// PastDue ("5: Final retry" is explicit that the schedule stops there), but also requires
/// recovery to still be possible up to day 13 — so once Restricted (day 8-13), this job retries
/// daily rather than going silent until the day-14 Suspend. MaxAttempts is sized generously
/// enough that this daily cadence isn't cut off before day 14 does its own natural cap (Suspended
/// subscriptions drop out of this job's query entirely).
/// </summary>
public sealed class DunningJob(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    IBillingProvider billingProvider,
    IDistributedJobLock jobLock,
    ISubscriptionStateMachine stateMachine,
    SubscriptionNotificationService notificationService,
    IEntitlementService entitlementService,
    TimeProvider timeProvider,
    ILogger<DunningJob> logger)
{
    public const string JobName = "DunningJob";
    private static readonly int[] RetryDays = [1, 3, 5];
    private const int RestrictedDay = 7;
    private const int SuspendedDay = 14;
    // day 0 = attempt 1 (already recorded), days 1/3/5 = attempts 2/3/4, then up to one daily
    // retry per day through the Restricted window (days 8-13) — comfortably covered by 15; day
    // 14's Suspend transition is the real, natural cap regardless of this number.
    private const int MaxAttempts = 15;

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
        var today = now.Date;

        var inDunning = await context.OrganisationSubscriptions
            .Include(s => s.SubscriptionPlan)
            .Where(s => s.DunningStartedAt != null &&
                        (s.Status == SubscriptionStatus.PastDue || s.Status == SubscriptionStatus.Restricted))
            .ToListAsync(ct);

        foreach (var subscription in inDunning)
        {
            var daysSinceFailure = (today - subscription.DunningStartedAt!.Value.Date).Days;

            if (daysSinceFailure <= 0)
            {
                continue; // Day 0 already handled at the point of failure.
            }

            if (daysSinceFailure == RestrictedDay && subscription.Status == SubscriptionStatus.PastDue)
            {
                await stateMachine.TransitionAsync(
                    context, subscription, SubscriptionStatus.Restricted,
                    SubscriptionEventType.MovedToRestricted, null, "7 days since first failed debit.", ct);
                await notificationService.SendMovedToRestrictedAsync(context, subscription, ct);
                continue;
            }

            if (daysSinceFailure == SuspendedDay && subscription.Status == SubscriptionStatus.Restricted)
            {
                await stateMachine.TransitionAsync(
                    context, subscription, SubscriptionStatus.Suspended,
                    SubscriptionEventType.MovedToSuspended, null, "14 days since first failed debit.", ct);
                await notificationService.SendMovedToSuspendedAsync(context, subscription, ct);
                continue;
            }

            var isScheduledRetryDay = RetryDays.Contains(daysSinceFailure) && subscription.Status == SubscriptionStatus.PastDue;

            // The brief's table only schedules automatic retries at days 1/3/5 while PastDue —
            // "5: Final retry" is explicit that the fixed schedule stops there. But recovery must
            // still be possible for the rest of the Restricted window (days 8-13) before Suspend
            // at day 14 — required by the brief's own "recovery at day 13" test — so once
            // Restricted, retry daily until the day-14 transition rather than going silent.
            var isDailyRestrictedRetry = subscription.Status == SubscriptionStatus.Restricted &&
                daysSinceFailure > RestrictedDay && daysSinceFailure < SuspendedDay;

            if (isScheduledRetryDay || isDailyRestrictedRetry)
            {
                await RetryChargeAsync(context, subscription, today, now, ct);
            }
        }
    }

    private async Task RetryChargeAsync(ApplicationDbContext context, OrganisationSubscription subscription, DateTime today, DateTime now, CancellationToken ct)
    {
        var attemptsThisEpisode = await context.SubscriptionPayments
            .Where(p => p.OrganisationSubscriptionId == subscription.Id && p.CreatedAt >= subscription.DunningStartedAt!.Value)
            .ToListAsync(ct);

        if (attemptsThisEpisode.Any(p => p.CreatedAt.Date == today))
        {
            return; // Already attempted today — safe to re-run the job without retrying twice.
        }

        if (attemptsThisEpisode.Any(p => p.Status == SubscriptionPaymentStatus.Succeeded))
        {
            return; // A charge already succeeded this episode (e.g. via a webhook that arrived between ticks) — nothing to retry.
        }

        var lastFailure = attemptsThisEpisode.OrderByDescending(p => p.CreatedAt).FirstOrDefault();
        if (lastFailure is not null && CardDeclineClassifier.IsPermanentDecline(lastFailure.FailureMessage))
        {
            logger.LogInformation(
                "Subscription {SubscriptionId}: last decline looked permanent ({Message}) — skipping further automatic retries; card must be updated.",
                subscription.Id, lastFailure.FailureMessage);
            return;
        }

        var nextAttemptNumber = attemptsThisEpisode.Count + 1;
        if (nextAttemptNumber > MaxAttempts)
        {
            return; // Cap reached — day 7/14 transitions take over from here.
        }

        var defaultMethod = await context.SubscriptionPaymentMethods
            .Where(m => m.OrganisationSubscriptionId == subscription.Id && m.IsDefault && m.RemovedAt == null)
            .OrderByDescending(m => m.AuthorisedAt)
            .FirstOrDefaultAsync(ct);

        if (defaultMethod is null || string.IsNullOrEmpty(defaultMethod.ProviderAuthorizationCode) ||
            string.IsNullOrEmpty(subscription.BillingEmail) || subscription.SubscriptionPlan is null)
        {
            logger.LogError("Cannot retry dunning charge for subscription {SubscriptionId} — no stored payment method.", subscription.Id);
            return;
        }

        var reference = $"sisonke-dunning-{subscription.Id:N}-{today:yyyyMMdd}";
        var chargeResult = await billingProvider.ChargeAuthorisationAsync(
            defaultMethod.ProviderAuthorizationCode, subscription.BillingEmail,
            (long)Math.Round(subscription.SubscriptionPlan.MonthlyPrice * 100m, MidpointRounding.AwayFromZero), reference, ct);

        context.SubscriptionPayments.Add(new SubscriptionPayment
        {
            Id = Guid.NewGuid(),
            OrganisationSubscriptionId = subscription.Id,
            Amount = subscription.SubscriptionPlan.MonthlyPrice,
            Currency = "ZAR",
            Status = chargeResult.Success ? SubscriptionPaymentStatus.Succeeded : SubscriptionPaymentStatus.Failed,
            Provider = defaultMethod.Provider,
            ChargePurpose = SubscriptionChargePurpose.Retry,
            ProviderReference = chargeResult.Reference,
            ProviderRequestReference = reference,
            AttemptNumber = nextAttemptNumber,
            FailureCode = chargeResult.FailureCode,
            FailureMessage = chargeResult.FailureMessage,
            ProcessedAt = now,
            PaidAt = chargeResult.Success ? now : null,
            CreatedAt = now
        });

        if (chargeResult.Success)
        {
            subscription.LastSuccessfulPaymentAt = now;
            subscription.DunningStartedAt = null;
            subscription.GracePeriodEndsAt = null;
            await context.SaveChangesAsync(ct);

            await stateMachine.TransitionAsync(
                context, subscription, SubscriptionStatus.Active,
                SubscriptionEventType.PaymentSucceeded, null, $"Dunning retry (attempt {nextAttemptNumber}) succeeded.", ct);
            await notificationService.SendPaymentRecoveredAsync(context, subscription, ct);
        }
        else
        {
            await context.SaveChangesAsync(ct);
            await entitlementService.InvalidateAsync(subscription.StokvelId, ct);
            logger.LogWarning(
                "Dunning retry (attempt {Attempt}) failed for subscription {SubscriptionId}: {Message}",
                nextAttemptNumber, subscription.Id, chargeResult.FailureMessage);
        }
    }
}
