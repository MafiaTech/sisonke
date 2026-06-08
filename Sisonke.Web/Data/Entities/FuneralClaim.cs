using System.ComponentModel.DataAnnotations;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Data.Entities;

public class FuneralClaim
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = default!;

    public Guid MemberId { get; set; }
    public Member Member { get; set; } = default!;

    public FuneralClaimSubjectType SubjectType { get; set; }

    public Guid? DependentId { get; set; }
    public MemberDependent? Dependent { get; set; }

    [Required]
    [MaxLength(150)]
    public string DeceasedFullName { get; set; } = string.Empty;

    public DateTime? DateOfDeath { get; set; }

    public FuneralClaimStatus Status { get; set; } = FuneralClaimStatus.Draft;

    [MaxLength(50)]
    public string? ClaimReference { get; set; }

    [MaxLength(1000)]
    public string? ClaimReason { get; set; }

    [MaxLength(1000)]
    public string? ReviewNotes { get; set; }

    public bool IsWaitingPeriodSatisfied { get; set; }

    public bool IsMemberStatusEligible { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? SubmittedAt { get; set; }

    [MaxLength(150)]
    public string? SubmittedByName { get; set; }

    public DateTime? SecretaryReviewedAt { get; set; }

    [MaxLength(150)]
    public string? SecretaryReviewedByName { get; set; }

    public bool? SecretaryRecommendedApproval { get; set; }

    [MaxLength(1000)]
    public string? SecretaryReviewNotes { get; set; }

    public DateTime? ChairpersonDecisionAt { get; set; }

    [MaxLength(150)]
    public string? ChairpersonDecisionByName { get; set; }

    [MaxLength(1000)]
    public string? ChairpersonDecisionNotes { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public DateTime? RejectedAt { get; set; }

    public decimal? PayoutAmount { get; set; }

    public DateTime? PayoutPaidAt { get; set; }

    [MaxLength(100)]
    public string? PayoutReference { get; set; }

    [MaxLength(1000)]
    public string? PayoutNotes { get; set; }

    public Guid? PayoutCapturedByMemberId { get; set; }

    public ICollection<FuneralClaimDocument> Documents { get; set; } = new List<FuneralClaimDocument>();
}
