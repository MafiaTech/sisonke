using System.ComponentModel.DataAnnotations;

namespace Sisonke.Web.Data.Entities;

public class SubscriptionUsage
{
    public Guid Id { get; set; }

    public Guid OrganisationSubscriptionId { get; set; }
    public OrganisationSubscription OrganisationSubscription { get; set; } = default!;

    [Required]
    [MaxLength(60)]
    public string FeatureCode { get; set; } = string.Empty;

    public DateTime PeriodStart { get; set; }

    public DateTime PeriodEnd { get; set; }

    public int UsedValue { get; set; }

    /// <summary>Null = unlimited at the time this row was calculated.</summary>
    public int? LimitValue { get; set; }

    public DateTime LastCalculatedAt { get; set; } = DateTime.UtcNow;
}
