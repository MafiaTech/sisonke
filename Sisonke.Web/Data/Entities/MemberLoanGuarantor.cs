using System.ComponentModel.DataAnnotations;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Data.Entities;

public class MemberLoanGuarantor
{
    public Guid Id { get; set; }

    public Guid LoanId { get; set; }
    public MemberLoan Loan { get; set; } = default!;

    public Guid GuarantorMemberId { get; set; }
    public Member GuarantorMember { get; set; } = default!;

    public MemberLoanGuarantorStatus Status { get; set; } = MemberLoanGuarantorStatus.Pending;

    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    public DateTime? RespondedAt { get; set; }

    [MaxLength(450)]
    public string? RequestedByUserId { get; set; }

    [MaxLength(450)]
    public string? RespondedByUserId { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    [MaxLength(1000)]
    public string? Reason { get; set; }
}
