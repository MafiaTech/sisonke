namespace Sisonke.Web.Data.Enums;

public enum SurplusWithdrawalStatus
{
    Submitted = 1,
    PendingApproval = 2,
    Approved = 3,
    PaymentPending = 4,
    Paid = 5,
    Rejected = 6,
    Cancelled = 7,
    PendingSecretaryReview = 8,
    AwaitingChairpersonApproval = 9,
    AwaitingTreasurerPayout = 10
}
