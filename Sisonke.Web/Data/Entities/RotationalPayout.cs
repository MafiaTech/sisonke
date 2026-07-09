using System.ComponentModel.DataAnnotations;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Data.Entities;

public class RotationalPayout
{
    public Guid Id { get; set; }

    public Guid StokvelId { get; set; }

    public Guid CycleId { get; set; }
    public RotationalContributionCycle Cycle { get; set; } = default!;

    public Guid PayoutMemberId { get; set; }
    public Member PayoutMember { get; set; } = default!;

    public decimal PayoutAmount { get; set; }

    public RotationalPayoutStatus PayoutStatus { get; set; } = RotationalPayoutStatus.PendingSecretaryReview;

    public DateTime? SecretaryReviewedAt { get; set; }

    [MaxLength(450)]
    public string? SecretaryReviewedByUserId { get; set; }

    public bool? SecretaryRecommendedApproval { get; set; }

    [MaxLength(1000)]
    public string? SecretaryReviewNotes { get; set; }

    [MaxLength(50)]
    public string? ChairpersonDecision { get; set; }

    public DateTime? ChairpersonReviewedAt { get; set; }

    [MaxLength(450)]
    public string? ChairpersonReviewedByUserId { get; set; }

    [MaxLength(1000)]
    public string? ChairpersonReviewNotes { get; set; }

    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(450)]
    public string? RequestedBy { get; set; }

    [MaxLength(450)]
    public string? ApprovedByChairpersonId { get; set; }

    public DateTime? ApprovedAt { get; set; }

    [MaxLength(450)]
    public string? RejectedByChairpersonId { get; set; }

    public DateTime? RejectedAt { get; set; }

    [MaxLength(1000)]
    public string? RejectionReason { get; set; }

    [MaxLength(450)]
    public string? PaidByTreasurerId { get; set; }

    public DateTime? PaidAt { get; set; }

    public PaymentMethod? PaymentMethod { get; set; }

    [MaxLength(100)]
    public string? PaymentReference { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(450)]
    public string? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    [MaxLength(450)]
    public string? UpdatedBy { get; set; }
}
