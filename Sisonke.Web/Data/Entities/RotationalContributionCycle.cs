using System.ComponentModel.DataAnnotations;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Data.Entities;

public class RotationalContributionCycle
{
    public Guid Id { get; set; }

    public Guid StokvelId { get; set; }
    public Stokvel Stokvel { get; set; } = default!;

    public Guid ConfigurationId { get; set; }
    public RotationalStokvelConfiguration Configuration { get; set; } = default!;

    public Guid? PayoutOrderId { get; set; }
    public RotationalPayoutOrder? PayoutOrder { get; set; }

    public Guid PayoutMemberId { get; set; }
    public Member PayoutMember { get; set; } = default!;

    public int CycleNumber { get; set; }

    [MaxLength(100)]
    public string CycleName { get; set; } = string.Empty;

    public DateTime CycleStartDate { get; set; }

    public DateTime CycleEndDate { get; set; }

    public DateTime ContributionDueDate { get; set; }

    public DateTime ScheduledPayoutDate { get; set; }

    public decimal ContributionAmountPerMember { get; set; }

    public decimal ExpectedTotalContributionAmount { get; set; }

    public decimal ExpectedPayoutAmount { get; set; }

    public RotationalCycleStatus Status { get; set; } = RotationalCycleStatus.Pending;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(450)]
    public string? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    [MaxLength(450)]
    public string? UpdatedBy { get; set; }
}
