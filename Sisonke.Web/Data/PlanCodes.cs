namespace Sisonke.Web.Data;

/// <summary>
/// Canonical SubscriptionPlan.Code values. Application code must reference these constants
/// rather than plan-name string literals — see FeatureCodes / IEntitlementService (Phase 2).
/// </summary>
public static class PlanCodes
{
    public const string Starter = "STARTER";
    public const string Growing = "GROWING";
    public const string Professional = "PROFESSIONAL";
    public const string Enterprise = "ENTERPRISE";
}
