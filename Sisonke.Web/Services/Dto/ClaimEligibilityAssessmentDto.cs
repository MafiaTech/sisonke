namespace Sisonke.Web.Services.Dto;

public class ClaimEligibilityAssessmentDto
{
    public Guid ClaimId { get; set; }
    public Guid MemberId { get; set; }
    public Guid? DependentId { get; set; }
    public Guid StokvelId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public string ClaimSubjectName { get; set; } = string.Empty;
    public bool IsMemberActive { get; set; }
    public bool IsMemberSuspended { get; set; }
    public bool IsDependentClaim { get; set; }
    public bool IsDependentCovered { get; set; }
    public bool WaitingPeriodSatisfied { get; set; }
    public bool HasOutstandingContributionArrears { get; set; }
    public decimal OutstandingContributionBalance { get; set; }
    public bool HasOutstandingFines { get; set; }
    public decimal OutstandingFinesBalance { get; set; }
    public bool HasRequiredDocuments { get; set; }
    public bool HasDuplicateActiveClaim { get; set; }
    public bool IsEligible { get; set; }
    public string EligibilityStatus { get; set; } = "RequiresReview";
    public List<string> PassedChecks { get; set; } = [];
    public List<string> FailedChecks { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
    public DateTime AssessedAt { get; set; }
}
