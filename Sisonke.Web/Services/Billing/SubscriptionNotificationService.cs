using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;
using Sisonke.Web.Services.Notifications;

namespace Sisonke.Web.Services.Billing;

/// <summary>
/// The single place subscription-billing notifications are composed and sent — over the
/// existing NotificationEnqueuer/NotificationDispatchService/channel-sender pipeline (Phase 1),
/// never a parallel one. Recipients are always the billing administrator (whoever accepted the
/// subscription terms) plus every active office bearer — never ordinary members (brief
/// requirement). Every send also writes one SubscriptionEvent so support can prove what was sent
/// and when.
///
/// COMPLIANCE FLAG: trial/renewal notice timing and cancellation wording here are configurable
/// placeholders, not reviewed copy — brief rule 10 requires South African consumer-protection
/// review before launch (in particular the cancellation and renewal-notice sections).
///
/// Idempotency: NotificationEnqueuer already dedupes by (type, channel, entityType, entityId,
/// recipient) — periodKey is folded into entityType so the same trigger firing again for a new
/// billing period/dunning episode is a fresh send, while a re-run within the same period is not.
/// </summary>
public sealed class SubscriptionNotificationService(
    MemberAccessService memberAccessService,
    NotificationEnqueuer notificationEnqueuer,
    ILogger<SubscriptionNotificationService> logger)
{
    public Task SendTrialStartedAsync(ApplicationDbContext context, OrganisationSubscription subscription, SubscriptionPlan plan, CancellationToken ct = default)
    {
        var subject = "Your Sisonke trial has started";
        var body =
            $"<p>Thanks for choosing Sisonke. Your 60-day free trial of the <strong>{plan.Name}</strong> plan is now active.</p>" +
            $"<p>Trial started: {subscription.TrialStartedAt:d MMMM yyyy}<br/>" +
            $"Trial ends: {subscription.TrialEndsAt:d MMMM yyyy}<br/>" +
            $"First billing date: {subscription.NextBillingAt:d MMMM yyyy}<br/>" +
            $"Amount: {FormatZar(plan.MonthlyPrice)} / month</p>" +
            $"<p>You can cancel any time before your first billing date at no charge from your " +
            $"<a href=\"/subscription\">billing page</a>.</p>";

        return DispatchAsync(context, subscription, SubscriptionNotificationTrigger.TrialStarted,
            periodKey: subscription.Id.ToString("N"), subject, body,
            SubscriptionEventType.TrialStarted, "Trial started notification sent.", ct);
    }

    public Task SendTrialReminderAsync(
        ApplicationDbContext context, OrganisationSubscription subscription, SubscriptionPlan plan, int daysRemainingBucket, CancellationToken ct = default)
    {
        var subject = $"{daysRemainingBucket} day{(daysRemainingBucket == 1 ? "" : "s")} left in your Sisonke trial";
        var body =
            $"<p>Your Sisonke trial of the <strong>{plan.Name}</strong> plan has {daysRemainingBucket} day{(daysRemainingBucket == 1 ? "" : "s")} remaining.</p>" +
            $"<p>First billing date: {subscription.NextBillingAt:d MMMM yyyy}<br/>" +
            $"Amount: {FormatZar(plan.MonthlyPrice)} / month</p>" +
            $"<p>To avoid being charged, cancel before {subscription.NextBillingAt:d MMMM yyyy} from your " +
            $"<a href=\"/subscription\">billing page</a>.</p>";

        return DispatchAsync(context, subscription, SubscriptionNotificationTrigger.TrialReminder,
            periodKey: $"{subscription.TrialEndsAt:yyyyMMdd}:{daysRemainingBucket}", subject, body,
            SubscriptionEventType.TrialReminderSent, $"Trial reminder sent ({daysRemainingBucket} days remaining).", ct);
    }

    public Task SendDebitSucceededAsync(
        ApplicationDbContext context, OrganisationSubscription subscription, SubscriptionPlan plan, SubscriptionPayment payment, CancellationToken ct = default)
    {
        var subject = "Sisonke payment received";
        var body =
            $"<p>We successfully charged your card {FormatZar(payment.Amount)} for your <strong>{plan.Name}</strong> plan.</p>" +
            $"<p>Your invoice and receipt are available on your <a href=\"/subscription\">billing page</a>.</p>" +
            $"<p>Next billing date: {subscription.NextBillingAt:d MMMM yyyy}</p>";

        return DispatchAsync(context, subscription, SubscriptionNotificationTrigger.DebitSucceeded,
            periodKey: payment.ProviderReference, subject, body,
            SubscriptionEventType.PaymentSucceeded, "Payment-received notification sent.", ct);
    }

    public Task SendDebitFailedAsync(
        ApplicationDbContext context, OrganisationSubscription subscription, SubscriptionPlan plan, string reasonPlainLanguage, DateTime? nextRetryDate, CancellationToken ct = default)
    {
        var subject = "Sisonke payment failed — action needed";
        var body =
            $"<p>We could not charge your card {FormatZar(plan.MonthlyPrice)} for your <strong>{plan.Name}</strong> plan.</p>" +
            $"<p>Reason: {reasonPlainLanguage}</p>" +
            (nextRetryDate is not null ? $"<p>We'll try again on {nextRetryDate:d MMMM yyyy}.</p>" : "") +
            $"<p>Please <a href=\"/subscription\">update your card</a> to avoid an interruption.</p>" +
            $"<p>If payment keeps failing: your account moves to a read/export-only mode after 7 days, and is fully suspended after 14 days. " +
            $"Your data is never deleted — full access returns as soon as payment succeeds.</p>";

        return DispatchAsync(context, subscription, SubscriptionNotificationTrigger.DebitFailed,
            periodKey: subscription.DunningStartedAt?.ToString("yyyyMMdd") ?? DateTime.UtcNow.ToString("yyyyMMdd"), subject, body,
            SubscriptionEventType.PaymentFailed, "Payment-failed notification sent.", ct);
    }

    public Task SendMovedToRestrictedAsync(ApplicationDbContext context, OrganisationSubscription subscription, CancellationToken ct = default)
    {
        var subject = "Sisonke account restricted — payment still needed";
        var body =
            "<p>Because payment has not succeeded, this account is now restricted.</p>" +
            "<p><strong>Blocked:</strong> adding members, creating claims, starting loans, creating meetings, sending bulk notifications, processing payouts, creating new financial transactions.</p>" +
            "<p><strong>Still works:</strong> viewing existing records, exporting reports, invoices, billing and support.</p>" +
            "<p>Restore full access any time by <a href=\"/subscription\">updating your card</a>.</p>";

        return DispatchAsync(context, subscription, SubscriptionNotificationTrigger.MovedToRestricted,
            periodKey: subscription.DunningStartedAt?.ToString("yyyyMMdd") ?? DateTime.UtcNow.ToString("yyyyMMdd"), subject, body,
            SubscriptionEventType.MovedToRestricted, "Restricted notification sent.", ct);
    }

    public Task SendMovedToSuspendedAsync(ApplicationDbContext context, OrganisationSubscription subscription, CancellationToken ct = default)
    {
        var subject = "Sisonke account suspended";
        var body =
            "<p>This account is now suspended due to non-payment — it is read-only for authorised administrators.</p>" +
            "<p><strong>Your data is retained and can be exported at any time</strong> — nothing is deleted.</p>" +
            "<p>Restore full access any time by <a href=\"/subscription\">updating your card</a>.</p>";

        return DispatchAsync(context, subscription, SubscriptionNotificationTrigger.MovedToSuspended,
            periodKey: subscription.DunningStartedAt?.ToString("yyyyMMdd") ?? DateTime.UtcNow.ToString("yyyyMMdd"), subject, body,
            SubscriptionEventType.MovedToSuspended, "Suspended notification sent.", ct);
    }

    public Task SendPaymentRecoveredAsync(ApplicationDbContext context, OrganisationSubscription subscription, CancellationToken ct = default)
    {
        var subject = "Sisonke payment received — access restored";
        var body =
            "<p>Your payment succeeded and full access to your account has been restored.</p>" +
            $"<p>Next billing date: {subscription.NextBillingAt:d MMMM yyyy}</p>";

        return DispatchAsync(context, subscription, SubscriptionNotificationTrigger.PaymentRecovered,
            periodKey: $"{DateTime.UtcNow:yyyyMMdd}:{subscription.LastSuccessfulPaymentAt:O}", subject, body,
            SubscriptionEventType.Activated, "Payment-recovered notification sent.", ct);
    }

    public Task SendPlanChangedAsync(
        ApplicationDbContext context, OrganisationSubscription subscription, string oldPlanName, string newPlanName, DateTime effectiveDate, string prorationNote, CancellationToken ct = default)
    {
        var subject = "Your Sisonke plan has changed";
        var body =
            $"<p>Your plan changed from <strong>{oldPlanName}</strong> to <strong>{newPlanName}</strong>, effective {effectiveDate:d MMMM yyyy}.</p>" +
            $"<p>{prorationNote}</p>";

        return DispatchAsync(context, subscription, SubscriptionNotificationTrigger.PlanChanged,
            periodKey: $"{effectiveDate:yyyyMMdd}:{newPlanName}", subject, body,
            SubscriptionEventType.Upgraded, "Plan-changed notification sent.", ct);
    }

    public Task SendCancellationAsync(
        ApplicationDbContext context, OrganisationSubscription subscription, DateTime effectiveDate, DateTime accessUntilDate, CancellationToken ct = default)
    {
        var subject = "Your Sisonke subscription has been cancelled";
        var body =
            $"<p>Your subscription is cancelled, effective {effectiveDate:d MMMM yyyy}.</p>" +
            $"<p>You keep full access until {accessUntilDate:d MMMM yyyy}.</p>" +
            "<p>Your data is retained and can be exported at any time from your billing page — nothing is deleted when a subscription ends.</p>";

        return DispatchAsync(context, subscription, SubscriptionNotificationTrigger.Cancellation,
            periodKey: $"{effectiveDate:yyyyMMdd}", subject, body,
            SubscriptionEventType.Cancelled, "Cancellation notification sent.", ct);
    }

    public Task SendUsageWarningAsync(
        ApplicationDbContext context, OrganisationSubscription subscription, string featureDescription, int usedValue, int limitValue, int thresholdPercent, CancellationToken ct = default)
    {
        var subject = thresholdPercent >= 100
            ? $"Sisonke: {featureDescription} limit reached"
            : $"Sisonke: {featureDescription} approaching its limit";
        var body =
            $"<p>You are using {usedValue} of {limitValue} {featureDescription} ({thresholdPercent}% of your plan's limit).</p>" +
            "<p>Consider upgrading your plan from your <a href=\"/subscription\">billing page</a> to avoid being blocked.</p>";

        return DispatchAsync(context, subscription, SubscriptionNotificationTrigger.UsageWarning,
            periodKey: $"{DateTime.UtcNow:yyyyMMdd}:{featureDescription}:{thresholdPercent}", subject, body,
            SubscriptionEventType.UsageWarningSent, $"Usage warning sent ({featureDescription}, {thresholdPercent}%).", ct);
    }

    public Task SendLegacyMigrationReminderAsync(ApplicationDbContext context, OrganisationSubscription subscription, DateTime cutoverDate, CancellationToken ct = default)
    {
        var subject = "Action needed: choose a Sisonke plan";
        var body =
            $"<p>Sisonke is moving to paid plans. Choose a subscription plan before {cutoverDate:d MMMM yyyy} to keep uninterrupted access.</p>" +
            "<p>Select a plan from your <a href=\"/subscription\">billing page</a> — a 60-day free trial starts only once you authorise a card.</p>";

        return DispatchAsync(context, subscription, SubscriptionNotificationTrigger.LegacyMigrationReminder,
            periodKey: $"{DateTime.UtcNow:yyyyMMdd}", subject, body,
            SubscriptionEventType.LegacyMigrationReminderSent, "Legacy migration reminder sent.", ct);
    }

    private async Task DispatchAsync(
        ApplicationDbContext context,
        OrganisationSubscription subscription,
        SubscriptionNotificationTrigger trigger,
        string periodKey,
        string subject,
        string body,
        SubscriptionEventType auditEventType,
        string auditNote,
        CancellationToken ct)
    {
        var recipients = await ResolveRecipientsAsync(context, subscription, ct);
        if (recipients.Count == 0)
        {
            logger.LogWarning(
                "No billing notification recipients found for subscription {SubscriptionId} ({Trigger}).", subscription.Id, trigger);
            return;
        }

        var entityType = $"OrgSub:{trigger}:{periodKey}";

        foreach (var recipient in recipients)
        {
            await notificationEnqueuer.EnqueueAsync(
                context, MapNotificationType(trigger), recipient.Id, subscription.StokvelId, entityType, subscription.Id, subject, body, ct);
        }

        context.SubscriptionEvents.Add(new SubscriptionEvent
        {
            Id = Guid.NewGuid(),
            OrganisationSubscriptionId = subscription.Id,
            EventType = auditEventType,
            ActorUserId = null,
            Notes = $"{auditNote} Recipients: {recipients.Count}.",
            OccurredAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync(ct);
    }

    private async Task<List<Member>> ResolveRecipientsAsync(ApplicationDbContext context, OrganisationSubscription subscription, CancellationToken ct)
    {
        var recipients = await memberAccessService.GetOfficeBearersAsync(subscription.StokvelId);

        if (!string.IsNullOrEmpty(subscription.TermsAcceptedByUserId) &&
            !recipients.Any(m => m.ApplicationUserId == subscription.TermsAcceptedByUserId))
        {
            var stokvel = await context.Stokvels.AsNoTracking().FirstOrDefaultAsync(s => s.Id == subscription.StokvelId, ct);
            if (stokvel is not null)
            {
                var billingAdmin = await context.Members.FirstOrDefaultAsync(
                    m => m.TenantId == stokvel.TenantId && m.ApplicationUserId == subscription.TermsAcceptedByUserId, ct);
                if (billingAdmin is not null)
                {
                    recipients.Add(billingAdmin);
                }
            }
        }

        return recipients;
    }

    private static NotificationType MapNotificationType(SubscriptionNotificationTrigger trigger) => trigger switch
    {
        SubscriptionNotificationTrigger.TrialStarted => NotificationType.TrialStarted,
        SubscriptionNotificationTrigger.TrialReminder => NotificationType.TrialReminder,
        SubscriptionNotificationTrigger.DebitSucceeded => NotificationType.InvoiceReceiptIssued,
        SubscriptionNotificationTrigger.DebitFailed => NotificationType.PaymentFailed,
        SubscriptionNotificationTrigger.MovedToRestricted => NotificationType.SubscriptionRestricted,
        SubscriptionNotificationTrigger.MovedToSuspended => NotificationType.SubscriptionSuspended,
        SubscriptionNotificationTrigger.PaymentRecovered => NotificationType.PaymentRecovered,
        SubscriptionNotificationTrigger.PlanChanged => NotificationType.PlanChanged,
        SubscriptionNotificationTrigger.Cancellation => NotificationType.SubscriptionCancelled,
        SubscriptionNotificationTrigger.UsageWarning => NotificationType.UsageWarning,
        SubscriptionNotificationTrigger.LegacyMigrationReminder => NotificationType.LegacyMigrationReminder,
        _ => throw new ArgumentOutOfRangeException(nameof(trigger), trigger, null)
    };

    private static string FormatZar(decimal amount) => $"R{amount:N2}";
}
