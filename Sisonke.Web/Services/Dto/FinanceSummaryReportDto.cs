namespace Sisonke.Web.Services.Dto;

public sealed class FinanceSummaryReportDto
{
    public Guid StokvelId { get; set; }
    public string StokvelName { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public int ActiveMemberCount { get; set; }
    public decimal MonthlyContributionAmount { get; set; }
    public decimal ExpectedContributions { get; set; }
    public decimal ActualContributions { get; set; }
    public decimal OutstandingContributions { get; set; }
    public decimal CollectionRatePercentage { get; set; }
    public int PaidCount { get; set; }
    public int PartiallyPaidCount { get; set; }
    public int OverdueCount { get; set; }
    public int UnpaidCount { get; set; }
    public decimal OutstandingFinesTotal { get; set; }
    public int OutstandingFinesCount { get; set; }
    public decimal ApprovedClaimsAwaitingPayoutTotal { get; set; }
    public int ApprovedClaimsAwaitingPayoutCount { get; set; }
    public DateTime GeneratedAt { get; set; }
}
