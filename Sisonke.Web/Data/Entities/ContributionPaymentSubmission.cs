using System.ComponentModel.DataAnnotations;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Data.Entities;

public class ContributionPaymentSubmission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = default!;
    public Guid StokvelId { get; set; }
    public Stokvel Stokvel { get; set; } = default!;
    public Guid MemberId { get; set; }
    public Member Member { get; set; } = default!;
    // Retain the legacy table/entity name to preserve applied migrations and proof links.
    public PaymentObligationType ObligationType { get; set; } = PaymentObligationType.Contribution;
    public Guid? MemberContributionId { get; set; }
    public MemberContribution? MemberContribution { get; set; }
    public Guid? MemberFineId { get; set; }
    public MemberFine? MemberFine { get; set; }
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string PaymentFor => ObligationType == PaymentObligationType.Contribution
        ? $"{MemberContribution?.ContributionCycle.PeriodStart:MMMM yyyy} Contribution"
        : $"{MemberFine?.FineType.Name}: {MemberFine?.Reason}";
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public decimal Outstanding => MemberContribution?.OutstandingAmount ?? (MemberFine?.Status == FineStatus.Unpaid ? MemberFine.Amount : 0);
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    [MaxLength(100)] public string? PaymentReference { get; set; }
    [MaxLength(1000)] public string? Notes { get; set; }
    public ContributionPaymentSubmissionStatus Status { get; set; } = ContributionPaymentSubmissionStatus.Pending;
    [MaxLength(450)] public string SubmittedByUserId { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    [MaxLength(450)] public string? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAt { get; set; }
    [MaxLength(1000)] public string? RejectionReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public Guid? PaymentId { get; set; }
    public Payment? Payment { get; set; }
    public ICollection<PaymentProofDocument> Documents { get; set; } = new List<PaymentProofDocument>();
}
