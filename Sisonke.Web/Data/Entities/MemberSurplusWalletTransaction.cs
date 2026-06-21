using System.ComponentModel.DataAnnotations;
using Sisonke.Web.Data.Enums;
namespace Sisonke.Web.Data.Entities;
public class MemberSurplusWalletTransaction
{
    public Guid Id { get; set; }
    public Guid StokvelId { get; set; }
    public Guid WalletId { get; set; }
    public MemberSurplusWallet Wallet { get; set; } = default!;
    public Guid MemberId { get; set; }
    public WalletTransactionType TransactionType { get; set; }
    public decimal Amount { get; set; }
    public decimal BalanceAfterTransaction { get; set; }
    public WalletTransactionSourceType SourceType { get; set; }
    public Guid? SourceReferenceId { get; set; }
    [MaxLength(500)] public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [MaxLength(450)] public string? CreatedBy { get; set; }
}
