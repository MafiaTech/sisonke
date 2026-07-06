using System.ComponentModel.DataAnnotations;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Data.Entities;

public class StokvelReserveTransaction
{
    public Guid Id { get; set; }

    public Guid StokvelId { get; set; }
    public Stokvel Stokvel { get; set; } = default!;

    public Guid? MemberLoanId { get; set; }
    public MemberLoan? MemberLoan { get; set; }

    public decimal Amount { get; set; }

    public StokvelReserveTransactionType TransactionType { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(450)]
    public string? CreatedByUserId { get; set; }
}
