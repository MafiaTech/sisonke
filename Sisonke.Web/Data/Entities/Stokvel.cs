using System.ComponentModel.DataAnnotations;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Data.Entities;

public class Stokvel
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = default!;

    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Code { get; set; }

    public StokvelType Type { get; set; }

    public StokvelArchetype Archetype { get; set; } = StokvelArchetype.BurialSociety;

    public bool EnableClaims { get; set; } = true;

    public bool EnableDependents { get; set; } = true;

    public bool EnableRotation { get; set; }

    public bool EnableLending { get; set; }

    public bool EnableInventory { get; set; }

    public bool EnableInvestmentTracking { get; set; }

    public bool EnableEducationPayouts { get; set; }

    public bool EnableTravelPlanning { get; set; }

    public bool EnableSocialEvents { get; set; }

    [MaxLength(100)]
    public string? Province { get; set; }

    [MaxLength(100)]
    public string? TownOrArea { get; set; }

    public DateTime? EstablishedDate { get; set; }

    public int? ExpectedMemberCount { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    [MaxLength(450)]
    public string? DeletedBy { get; set; }

    [MaxLength(500)]
    public string? DeleteReason { get; set; }

    public bool IsSetupComplete { get; set; }

    public DateTime? SetupCompletedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    [MaxLength(450)]
    public string? UpdatedBy { get; set; }
}
