using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Data.Entities;

public class TenantSubscription
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = default!;

    public Guid SubscriptionPlanId { get; set; }
    public SubscriptionPlan SubscriptionPlan { get; set; } = default!;

    public SubscriptionStatus Status { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public DateTime? NextBillingDate { get; set; }

    public bool IsTrial { get; set; }
}
