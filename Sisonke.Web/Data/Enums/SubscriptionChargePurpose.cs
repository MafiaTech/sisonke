namespace Sisonke.Web.Data.Enums;

/// <summary>
/// Business reason for a subscription payment. Explicit values are persisted and must not be
/// renumbered. Null on SubscriptionPayment represents a historic row created before this field.
/// </summary>
public enum SubscriptionChargePurpose
{
    InitialSubscription = 1,
    RecurringSubscription = 2,
    Retry = 3,
    ManualCollection = 4,
    Adjustment = 5
}
