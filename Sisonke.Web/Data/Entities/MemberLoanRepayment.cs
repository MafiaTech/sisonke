using System.ComponentModel.DataAnnotations;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Data.Entities;

public class MemberLoanRepayment
{
    public Guid Id { get; set; }

    public Guid StokvelId { get; set; }

    public Guid LoanId { get; set; }
    public MemberLoan Loan { get; set; } = default!;

    public Guid MemberId { get; set; }

    public decimal ExpectedAmount { get; set; }

    public decimal PaidAmount { get; set; }

    public decimal FineAmount { get; set; }

    public DateTime DueDate { get; set; }

    public DateTime? PaymentDate { get; set; }

    public PaymentMethod? PaymentMethod { get; set; }

    [MaxLength(100)]
    public string? PaymentReference { get; set; }

    public LoanRepaymentStatus PaymentStatus { get; set; } = LoanRepaymentStatus.Pending;

    [MaxLength(500)]
    public string? Notes { get; set; }

    [MaxLength(450)]
    public string? ConfirmedByTreasurerId { get; set; }

    public DateTime? ConfirmedAt { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(450)]
    public string? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    [MaxLength(450)]
    public string? UpdatedBy { get; set; }
}
