using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Data.Entities;

public class ContributionRule
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = default!;

    public decimal Amount { get; set; }

    public ContributionFrequency Frequency { get; set; } = ContributionFrequency.Monthly;

    public int DueDayOfMonth { get; set; } = 5;

    public bool AllowPartialPayments { get; set; } = true;

    public decimal LatePaymentFineAmount { get; set; }

    public int GracePeriodDays { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime EffectiveFrom { get; set; } = DateTime.Today;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
