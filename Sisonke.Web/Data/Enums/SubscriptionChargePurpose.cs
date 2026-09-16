namespace Sisonke.Web.Data.Enums;

/// <summary>
/// Business reason for a Sisonke SaaS subscription payment. Explicit values are persisted and
/// must not be renumbered. Operational purposes (contribution, loan, payout, claim or transfer)
/// are forbidden here. Null represents a historic row created before this field.
/// </summary>
public enum SubscriptionChargePurpose
{
    InitialSubscription = 1,
    RecurringSubscription = 2,
    Retry = 3,
    ManualCollection = 4,
    Adjustment = 5
}
