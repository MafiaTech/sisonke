namespace Sisonke.Web.Data.Enums;

/// <summary>
/// LegacyUnsubscribed is 0 (the CLR default) so a subscription row that is ever left with its
/// Status unset fails safe into the least-privileged legacy state rather than silently granting
/// paid access. Trialing keeps the historical int value 1 (previously named Trial) so existing
/// TenantSubscription rows continue to deserialize correctly.
/// </summary>
public enum SubscriptionStatus
{
    LegacyUnsubscribed = 0,
    Trialing = 1,
    Active = 2,
    Suspended = 3,
    Cancelled = 4,
    Expired = 5,
    PendingPaymentMethod = 6,
    PastDue = 7,
    Restricted = 8
}
