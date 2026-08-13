namespace Sisonke.Web.Services.Billing;

public enum SubscriptionNotificationTrigger
{
    TrialStarted,
    TrialReminder,
    DebitSucceeded,
    DebitFailed,
    MovedToRestricted,
    MovedToSuspended,
    PaymentRecovered,
    PlanChanged,
    Cancellation,
    UsageWarning,
    LegacyMigrationReminder
}
