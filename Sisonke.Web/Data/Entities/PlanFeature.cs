namespace Sisonke.Web.Data.Entities;

/// <summary>
/// One row per (SubscriptionPlan, FeatureDefinition). LimitValue is only meaningful when the
/// feature's DataType is Numeric (null = unlimited); ConfigurationJson carries the value when
/// DataType is Enum, e.g. {"value":"Limited"} for ONLINE_MEMBER_PAYMENTS.
/// </summary>
public class PlanFeature
{
    public Guid Id { get; set; }

    public Guid SubscriptionPlanId { get; set; }
    public SubscriptionPlan SubscriptionPlan { get; set; } = default!;

    public Guid FeatureDefinitionId { get; set; }
    public FeatureDefinition FeatureDefinition { get; set; } = default!;

    public bool IsEnabled { get; set; }

    public int? LimitValue { get; set; }

    public string? ConfigurationJson { get; set; }
}
