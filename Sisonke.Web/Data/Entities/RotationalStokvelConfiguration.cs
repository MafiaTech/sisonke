using System.ComponentModel.DataAnnotations;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Data.Entities;

public class RotationalStokvelConfiguration
{
    public Guid Id { get; set; }

    public Guid StokvelId { get; set; }
    public Stokvel Stokvel { get; set; } = default!;

    public decimal ContributionAmount { get; set; }
    public RotationalFrequency ContributionFrequency { get; set; } = RotationalFrequency.Monthly;
    public int ContributionDueDay { get; set; }

    public decimal PayoutAmount { get; set; }
    public RotationalFrequency PayoutFrequency { get; set; } = RotationalFrequency.Monthly;

    public DateTime RotationStartDate { get; set; }
    public RotationOrderMethod RotationOrderMethod { get; set; } = RotationOrderMethod.Manual;
    public bool AllowPayoutTurnSwap { get; set; }

    public LatePenaltyType LatePenaltyType { get; set; } = LatePenaltyType.None;
    public decimal? LatePenaltyAmount { get; set; }
    public int GracePeriodDays { get; set; }

    public decimal MinimumBalanceBeforePayout { get; set; }
    public bool MissedContributionBlocksPayout { get; set; }
    public bool TreasurerConfirmationRequired { get; set; } = true;

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    [MaxLength(450)]
    public string? CreatedBy { get; set; }

    [MaxLength(450)]
    public string? UpdatedBy { get; set; }
}
