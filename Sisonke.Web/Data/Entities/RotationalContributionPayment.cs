using System.ComponentModel.DataAnnotations;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Data.Entities;

public class RotationalContributionPayment
{
    public Guid Id { get; set; }

    public Guid StokvelId { get; set; }

    public Guid CycleId { get; set; }
    public RotationalContributionCycle Cycle { get; set; } = default!;

    public Guid MemberId { get; set; }
    public Member Member { get; set; } = default!;

    public decimal ExpectedAmount { get; set; }

    public decimal PaidAmount { get; set; }

    public decimal PenaltyAmount { get; set; }

    public ContributionPaymentStatus PaymentStatus { get; set; } = ContributionPaymentStatus.Unpaid;

    public DateTime? PaymentDate { get; set; }

    public ContributionPaymentMethod? PaymentMethod { get; set; }

    [MaxLength(100)]
    public string? ReferenceNumber { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    [MaxLength(450)]
    public string? ConfirmedByTreasurerId { get; set; }

    public DateTime? ConfirmedAt { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(450)]
    public string? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    [MaxLength(450)]
    public string? UpdatedBy { get; set; }
}
