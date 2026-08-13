using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Enums;
using Sisonke.Web.Services.Billing.Paystack;
using Sisonke.Web.Services.Jobs;

namespace Sisonke.Web.Services.Billing.Jobs;

/// <summary>
/// Daily: for trials ending today, Paystack's own subscription (created with a start_date 60
/// days out — see SubscriptionService.CompleteCardAuthorisationAsync) should auto-debit and
/// arrive via charge.success/invoice.payment_failed webhooks, which drive the Active/PastDue
/// transition. This job is the safety net for the case that never happened as designed — no
/// ProviderSubscriptionCode was recorded — where it charges the stored authorisation directly.
/// Paystack has no confirmed "get subscription status" call (this phase's API research), so
/// "does Paystack have an active subscription" is inferred from whether we ever recorded one,
/// not queried live.
/// </summary>
public sealed class TrialExpiryJob(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    IBillingProvider billingProvider,
    IDistributedJobLock jobLock,
    ISubscriptionStateMachine stateMachine,
    SubscriptionNotificationService notificationService,
    TimeProvider timeProvider,
    ILogger<TrialExpiryJob> logger)
{
    public const string JobName = "TrialExpiryJob";

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

        var expiringToday = await context.OrganisationSubscriptions
            .Include(s => s.SubscriptionPlan)
            .Where(s => s.Status == SubscriptionStatus.Trialing && s.TrialEndsAt != null && s.TrialEndsAt.Value.Date == today)
            .ToListAsync(ct);

        foreach (var subscription in expiringToday)
        {
            if (subscription.SubscriptionPlan is null)
            {
                continue;
            }

            if (!string.IsNullOrEmpty(subscription.ProviderSubscriptionCode))
            {
                // Paystack has an active subscription on record — trust its automatic first
                // debit and the resulting webhook to drive the transition. Nothing to do here.
                continue;
            }

            logger.LogWarning(
                "Subscription {SubscriptionId} trial ends today with no ProviderSubscriptionCode on record — attempting a direct charge as a safety net.",
                subscription.Id);

            var defaultMethod = await context.SubscriptionPaymentMethods
                .Where(m => m.OrganisationSubscriptionId == subscription.Id && m.IsDefault && m.RemovedAt == null)
                .OrderByDescending(m => m.AuthorisedAt)
                .FirstOrDefaultAsync(ct);

            if (defaultMethod is null || string.IsNullOrEmpty(defaultMethod.ProviderAuthorizationCode) ||
                string.IsNullOrEmpty(subscription.BillingEmail))
            {
                logger.LogError("Cannot attempt trial-expiry charge for subscription {SubscriptionId} — no stored payment method.", subscription.Id);
                continue;
            }

            var reference = $"sisonke-trialexpiry-{subscription.Id:N}-{today:yyyyMMdd}";
            var chargeResult = await billingProvider.ChargeAuthorisationAsync(
                defaultMethod.ProviderAuthorizationCode, subscription.BillingEmail,
                (long)Math.Round(subscription.SubscriptionPlan.MonthlyPrice * 100m, MidpointRounding.AwayFromZero), reference, ct);

            if (chargeResult.Success)
            {
                subscription.LastSuccessfulPaymentAt = now;
                await context.SaveChangesAsync(ct);
                await stateMachine.TransitionAsync(
                    context, subscription, SubscriptionStatus.Active,
                    SubscriptionEventType.PaymentSucceeded, null, $"Trial-expiry direct charge succeeded ({reference}).", ct);
            }
            else
            {
                subscription.DunningStartedAt ??= now;
                subscription.GracePeriodEndsAt = now.AddDays(14);
                await context.SaveChangesAsync(ct);
                await stateMachine.TransitionAsync(
                    context, subscription, SubscriptionStatus.PastDue,
                    SubscriptionEventType.PaymentFailed, null, $"Trial-expiry direct charge failed: {chargeResult.FailureMessage}", ct);
                await notificationService.SendDebitFailedAsync(
                    context, subscription, subscription.SubscriptionPlan,
                    chargeResult.FailureMessage ?? "The card was declined.", now.AddDays(1), ct);
            }
        }
    }
}
