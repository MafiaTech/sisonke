namespace Sisonke.Web.Data.Enums;

public enum SubscriptionEventType
{
    PlanSelected = 1,
    TermsAccepted = 2,
    PaymentMethodAuthorised = 3,
    TrialStarted = 4,
    TrialEnding = 5,
    TrialEnded = 6,
    Activated = 7,
    Renewed = 8,
    PaymentSucceeded = 9,
    PaymentFailed = 10,
    RetryScheduled = 11,
    MovedToRestricted = 12,
    MovedToSuspended = 13,
    Upgraded = 14,
    Downgraded = 15,
    Cancelled = 16,
    Reactivated = 17,
    TrialReminderSent = 18,
    UsageWarningSent = 19,
    LegacyMigrationReminderSent = 20,
    NotificationSent = 21,

    // Phase 5 — admin manual actions (AdminBillingService). Appended, not interleaved, so
    // existing persisted int values are never renumbered.
    TrialExtended = 22,
    PromotionalTrialApplied = 23,
    InvoiceWaived = 24,
    ManualRetryTriggered = 25,
    MovedToEnterprise = 26,
    ReinstatedByAdmin = 27
}
