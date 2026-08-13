using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Services.Entitlements;

public enum EntitlementDenialReason
{
    Allowed,
    FeatureNotInPlan,
    LimitExceeded,
    SubscriptionRestricted,
    SubscriptionSuspended,
    NoSubscription,
    TrialExpired
}

/// <summary>
/// The single result type every enforcement surface (application service, API endpoint filter,
/// UI helper, background job guard) reads to decide whether an operation may proceed. Never
/// construct one by hand outside EntitlementService — always come from AuthorizeAsync/HasFeatureAsync.
/// </summary>
public sealed record EntitlementDecision(
    bool Allowed,
    EntitlementDenialReason Reason,
    string FeatureCode,
    string? PlanCode,
    string? PlanName,
    int? Limit,
    int CurrentUsage,
    string Message,
    string? UpgradeToPlanCode);

/// <summary>One feature's resolved value for a stokvel's effective plan, as carried in EntitlementSnapshot.</summary>
public sealed record FeatureValue(
    string FeatureCode,
    FeatureDataType DataType,
    bool IsEnabled,
    int? LimitValue,
    string? EnumValue);

/// <summary>
/// Cached per stokvel (see EntitlementOptions.CacheTtlMinutes). Never carries live usage counts —
/// those are always read fresh by AuthorizeAsync so a race cannot exceed a numeric cap.
/// </summary>
public sealed record EntitlementSnapshot(
    Guid StokvelId,
    string? PlanCode,
    string? PlanName,
    SubscriptionStatus Status,
    DateTime? TrialEndsAt,
    DateTime? NextBillingAt,
    IReadOnlyDictionary<string, FeatureValue> Features);
