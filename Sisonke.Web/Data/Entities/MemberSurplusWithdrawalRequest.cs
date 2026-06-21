using System.ComponentModel.DataAnnotations;
using Sisonke.Web.Data.Enums;
namespace Sisonke.Web.Data.Entities;
public class MemberSurplusWithdrawalRequest
{
    public Guid Id { get; set; }
    public Guid StokvelId { get; set; }
    public Guid MemberId { get; set; }
    public Member Member { get; set; } = default!;
    public Guid WalletId { get; set; }
    public MemberSurplusWallet Wallet { get; set; } = default!;
    public decimal RequestedAmount { get; set; }
    public SurplusWithdrawalStatus WithdrawalStatus { get; set; } = SurplusWithdrawalStatus.PendingApproval;
    [Required, MaxLength(1000)] public string RequestReason { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
    [MaxLength(450)] public string? RequestedBy { get; set; }
    [MaxLength(450)] public string? ApprovedByChairpersonId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    [MaxLength(450)] public string? RejectedByChairpersonId { get; set; }
    public DateTime? RejectedAt { get; set; }
    [MaxLength(1000)] public string? RejectionReason { get; set; }
    [MaxLength(450)] public string? PaidByTreasurerId { get; set; }
    public DateTime? PaidAt { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    [MaxLength(200)] public string? PaymentReference { get; set; }
    [MaxLength(1000)] public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [MaxLength(450)] public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    [MaxLength(450)] public string? UpdatedBy { get; set; }
}
