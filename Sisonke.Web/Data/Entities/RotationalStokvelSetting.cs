using System.ComponentModel.DataAnnotations;

namespace Sisonke.Web.Data.Entities;

public class RotationalStokvelSetting
{
    public Guid Id { get; set; }

    public Guid StokvelId { get; set; }
    public Stokvel Stokvel { get; set; } = default!;

    public decimal ContributionAmount { get; set; }

    [MaxLength(20)]
    public string ContributionFrequency { get; set; } = "Monthly";

    public DateTime StartDate { get; set; }

    public int PayoutDay { get; set; }

    [MaxLength(50)]
    public string RotationMethod { get; set; } = "Manual";

    [MaxLength(50)]
    public string MissedPaymentRule { get; set; } = "ExecutiveDecision";

    public bool CyclesGenerated { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    [MaxLength(450)]
    public string? CreatedBy { get; set; }

    [MaxLength(450)]
    public string? UpdatedBy { get; set; }
}
