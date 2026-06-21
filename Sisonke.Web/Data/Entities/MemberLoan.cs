using System.ComponentModel.DataAnnotations;
using Sisonke.Web.Data.Enums;
namespace Sisonke.Web.Data.Entities;
public class MemberLoan
{
    public Guid Id { get; set; }
    public Guid StokvelId { get; set; }
    public Stokvel Stokvel { get; set; } = default!;
    public Guid MemberId { get; set; }
    public Member Member { get; set; } = default!;
    public decimal RequestedAmount { get; set; }
    public decimal? ApprovedAmount { get; set; }
    public int RepaymentMonths { get; set; }
    public decimal MonthlyRepaymentAmount { get; set; }
    public decimal TotalRepayableAmount { get; set; }
    public decimal OutstandingBalance { get; set; }
    public MemberLoanStatus LoanStatus { get; set; } = MemberLoanStatus.PendingApproval;
    [Required, MaxLength(1000)] public string RequestReason { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
    [MaxLength(450)] public string? RequestedBy { get; set; }
    [MaxLength(450)] public string? ApprovedByChairpersonId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    [MaxLength(450)] public string? RejectedByChairpersonId { get; set; }
    public DateTime? RejectedAt { get; set; }
    [MaxLength(1000)] public string? RejectionReason { get; set; }
    [MaxLength(450)] public string? DisbursedByTreasurerId { get; set; }
    public DateTime? DisbursedAt { get; set; }
    [MaxLength(200)] public string? DisbursementReference { get; set; }
    public PaymentMethod? DisbursementMethod { get; set; }
    public DateTime? DueStartDate { get; set; }
    public DateTime? ExpectedFinalPaymentDate { get; set; }
    public DateTime? FullyRepaidAt { get; set; }
    public DateTime? NextEligibleLoanDate { get; set; }
    [MaxLength(1000)] public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [MaxLength(450)] public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    [MaxLength(450)] public string? UpdatedBy { get; set; }
    public ICollection<MemberLoanRepayment> Repayments { get; set; } = new List<MemberLoanRepayment>();
}
