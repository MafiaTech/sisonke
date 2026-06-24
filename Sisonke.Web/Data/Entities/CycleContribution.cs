using System.ComponentModel.DataAnnotations;

namespace Sisonke.Web.Data.Entities;

public class CycleContribution
{
    public Guid Id { get; set; }

    public Guid RotationCycleId { get; set; }
    public RotationCycle RotationCycle { get; set; } = default!;

    public Guid MemberId { get; set; }
    public Member Member { get; set; } = default!;

    public decimal AmountDue { get; set; }

    public decimal AmountPaid { get; set; }

    [MaxLength(20)]
    public string PaymentStatus { get; set; } = "Unpaid";

    public DateTime? PaidDate { get; set; }

    [MaxLength(100)]
    public string? PaymentReference { get; set; }

    [MaxLength(450)]
    public string? MarkedByUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    [MaxLength(450)]
    public string? CreatedBy { get; set; }

    [MaxLength(450)]
    public string? UpdatedBy { get; set; }
}
