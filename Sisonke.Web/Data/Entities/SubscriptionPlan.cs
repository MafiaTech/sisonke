using System.ComponentModel.DataAnnotations;

namespace Sisonke.Web.Data.Entities;

/// <summary>
/// Legacy member-count tier plans (Pilot/Basic/Standard/Premium, MinMembers/MaxMembers,
/// TenantSubscription) predate the Code-based STARTER/GROWING/PROFESSIONAL/ENTERPRISE catalogue
/// (see PlanCodes/FeatureCodes) and are kept only so existing TenantSubscription rows keep
/// resolving — Code is null for those rows. New code must go through OrganisationSubscription
/// and reference plans by Code, never by Name or MinMembers/MaxMembers.
/// </summary>
public class SubscriptionPlan
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Null for legacy member-count tier rows (Pilot/Basic/Standard/Premium).</summary>
    [MaxLength(30)]
    public string? Code { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public int MinMembers { get; set; }

    public int? MaxMembers { get; set; }

    /// <summary>Null = unlimited. Supersedes MaxMembers for Code-based plans.</summary>
    public int? MaximumMembers { get; set; }

    /// <summary>Null = unlimited/custom.</summary>
    public int? MaximumAdministrators { get; set; }

    /// <summary>Null = unlimited/custom.</summary>
    public int? MaximumSchemes { get; set; }

    /// <summary>Null = unlimited.</summary>
    public int? StorageLimitMb { get; set; }

    public decimal MonthlyPrice { get; set; }

    public decimal? AnnualPrice { get; set; }

    [MaxLength(3)]
    public string Currency { get; set; } = "ZAR";

    /// <summary>True for Enterprise & Federations — price is negotiated, not catalogue-driven.</summary>
    public bool IsCustomPricing { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Paystack plan code, created once (lazily, on first subscriber) and shared by every
    /// stokvel that subscribes to this plan — never created per-organisation. Null until the
    /// first CompleteCardAuthorisationAsync call for this plan.
    /// </summary>
    [MaxLength(100)]
    public string? ProviderPlanCode { get; set; }

    public ICollection<TenantSubscription> TenantSubscriptions { get; set; } = new List<TenantSubscription>();

    public ICollection<PlanFeature> PlanFeatures { get; set; } = new List<PlanFeature>();

    public ICollection<OrganisationSubscription> OrganisationSubscriptions { get; set; } = new List<OrganisationSubscription>();
}
