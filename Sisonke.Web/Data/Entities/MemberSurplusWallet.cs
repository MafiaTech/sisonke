using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sisonke.Web.Data.Entities;

public class MemberSurplusWallet
{
    public Guid Id { get; set; }

    public Guid StokvelId { get; set; }

    public Guid MemberId { get; set; }
    public Member Member { get; set; } = default!;

    public decimal AvailableBalance { get; set; }

    public decimal CoreSavingsBalance { get; set; }

    public decimal SurplusEquityBalance { get; set; }

    public decimal LockedSurplusEquityBalance { get; set; }

    [NotMapped]
    public decimal AvailableSurplusEquity => Math.Max(0, SurplusEquityBalance - LockedSurplusEquityBalance);

    public decimal TotalCredits { get; set; }

    public decimal TotalWithdrawals { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(450)]
    public string? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    [MaxLength(450)]
    public string? UpdatedBy { get; set; }
}
