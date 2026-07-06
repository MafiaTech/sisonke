using System.ComponentModel.DataAnnotations;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Data.Entities;

public class StokvelLoanConfiguration
{
    public Guid Id { get; set; }

    public Guid StokvelId { get; set; }
    public Stokvel Stokvel { get; set; } = default!;

    public bool LoansEnabled { get; set; }

    public decimal MinLoanAmount { get; set; }

    public decimal MaxLoanAmount { get; set; }

    public int MaxRepaymentMonths { get; set; }

    public int DefaultRepaymentMonths { get; set; }

    public LoanInterestType LoanInterestType { get; set; }

    public decimal LoanInterestRate { get; set; }

    public LatePenaltyType LateRepaymentFineType { get; set; }

    public decimal? LateRepaymentFineAmount { get; set; }

    public int GracePeriodDays { get; set; }

    public int FreezePeriodAfterFullRepaymentDays { get; set; }

    public bool RequireChairpersonApproval { get; set; }

    public bool RequireTreasurerDisbursementConfirmation { get; set; }

    public bool SurplusBackedLoansEnabled { get; set; }

    public decimal SurplusEquityLoanMultiplier { get; set; } = 1;

    public bool EarlyPayoutLoansEnabled { get; set; }

    public decimal EarlyPayoutDiscountRatePercent { get; set; }

    public int RequiredGuarantorCount { get; set; } = 2;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(450)]
    public string? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    [MaxLength(450)]
    public string? UpdatedBy { get; set; }
}
