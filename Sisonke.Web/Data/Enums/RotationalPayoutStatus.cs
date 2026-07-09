namespace Sisonke.Web.Data.Enums;

public enum RotationalPayoutStatus
{
    Scheduled = 0,
    ReadyForApproval = 1,
    Approved = 2,
    Rejected = 3,
    Paid = 4,
    Cancelled = 5,
    ReturnedToSecretary = 6,
    PendingSecretaryReview = 7,
    PendingChairpersonApproval = 8
}
