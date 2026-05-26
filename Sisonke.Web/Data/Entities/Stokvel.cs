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

    public StokvelType Type { get; set; }

    [MaxLength(100)]
    public string? Province { get; set; }

    [MaxLength(100)]
    public string? TownOrArea { get; set; }

    public DateTime? EstablishedDate { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsSetupComplete { get; set; }

    public DateTime? SetupCompletedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
