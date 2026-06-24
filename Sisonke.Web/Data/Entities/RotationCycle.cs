using System.ComponentModel.DataAnnotations;

namespace Sisonke.Web.Data.Entities;

public class RotationCycle
{
    public Guid Id { get; set; }

    public Guid StokvelId { get; set; }
    public Stokvel Stokvel { get; set; } = default!;

    public int CycleNumber { get; set; }

    public DateTime DueDate { get; set; }

    public Guid PayoutMemberId { get; set; }
    public Member PayoutMember { get; set; } = default!;

    public decimal ExpectedAmount { get; set; }

    public decimal ActualCollectedAmount { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = "Pending";

    public DateTime? PayoutDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    [MaxLength(450)]
    public string? CreatedBy { get; set; }

    [MaxLength(450)]
    public string? UpdatedBy { get; set; }

    public ICollection<CycleContribution> CycleContributions { get; set; } = new List<CycleContribution>();
    public ICollection<CyclePayout> CyclePayouts { get; set; } = new List<CyclePayout>();
}
