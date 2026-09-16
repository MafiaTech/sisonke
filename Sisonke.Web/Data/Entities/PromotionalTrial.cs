using System.ComponentModel.DataAnnotations;

namespace Sisonke.Web.Data.Entities;

/// <summary>The default 60-day trial is seeded as Code = "LAUNCH60".</summary>
public class PromotionalTrial
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(30)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? Description { get; set; }

    public int TrialDays { get; set; }

    /// <summary>Null = applies to all plans.</summary>
    public Guid? AppliesToPlanId { get; set; }
    public SubscriptionPlan? AppliesToPlan { get; set; }

    public DateTime? ValidFrom { get; set; }

    public DateTime? ValidTo { get; set; }

    /// <summary>Null = unlimited redemptions.</summary>
    public int? MaxRedemptions { get; set; }

    public int RedemptionCount { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
