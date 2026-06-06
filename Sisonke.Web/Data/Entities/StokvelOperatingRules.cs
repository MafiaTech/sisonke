using System.ComponentModel.DataAnnotations;

namespace Sisonke.Web.Data.Entities;

public class StokvelOperatingRules
{
    public Guid Id { get; set; }

    public Guid StokvelId { get; set; }
    public Stokvel? Stokvel { get; set; }

    [MaxLength(100)]
    public string StokvelType { get; set; } = "Generic Stokvel";

    public decimal MonthlyContributionAmount { get; set; }
    public int ContributionDueDay { get; set; }
    public int GracePeriodDays { get; set; }
    public bool AllowPartialPayments { get; set; }
    public bool ChargeLatePaymentFine { get; set; }
    public decimal LatePaymentFineAmount { get; set; }

    public bool EnableDependents { get; set; }
    public int MaximumDependents { get; set; }
    public int MemberWaitingPeriodMonths { get; set; }
    public int DependentWaitingPeriodMonths { get; set; }
    public bool RequireDependentIdNumber { get; set; }

    public bool EnableClaims { get; set; }
    public bool RequireDeathCertificateForClaims { get; set; }
    public bool RequireClaimDocuments { get; set; }
    public bool BlockClaimsIfMemberInArrears { get; set; }
    public bool BlockClaimsIfMemberSuspended { get; set; }
    public decimal DefaultClaimPayoutAmount { get; set; }

    public bool EnableAttendanceTracking { get; set; }
    public int AbsenceReminderThreshold { get; set; }
    public int FormalWarningThreshold { get; set; }
    public int ExecutiveReviewThreshold { get; set; }
    public int ApologyDeadlineHoursBeforeMeeting { get; set; }
    public bool ChargeLateApologyFine { get; set; }
    public decimal LateApologyFineAmount { get; set; }
    public bool ChargeAbsenceWithoutApologyFine { get; set; }
    public decimal AbsenceWithoutApologyFineAmount { get; set; }

    public bool EnableMeetings { get; set; }
    public bool RequireMinutesApproval { get; set; }
    public decimal QuorumPercentage { get; set; }

    public bool EnableVoting { get; set; }
    public decimal DefaultVotingApprovalThreshold { get; set; }
    public bool AllowAnonymousVoting { get; set; }

    public bool EnableRotationalPayouts { get; set; }

    [MaxLength(100)]
    public string? PayoutFrequency { get; set; }

    public bool RequireTreasurerConfirmationForPayouts { get; set; }

    public bool EnableGroceryModule { get; set; }
    public bool EnableInvestmentModule { get; set; }
    public bool EnablePropertyModule { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? CreatedByMemberId { get; set; }
    public Guid? UpdatedByMemberId { get; set; }
}
