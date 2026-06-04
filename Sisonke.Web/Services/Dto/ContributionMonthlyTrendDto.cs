namespace Sisonke.Web.Services.Dto;

public class ContributionMonthlyTrendDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public decimal ExpectedContributions { get; set; }
    public decimal ActualContributions { get; set; }
    public decimal OutstandingContributions { get; set; }
    public decimal CollectionRatePercentage { get; set; }
    public int PaidCount { get; set; }
    public int PartiallyPaidCount { get; set; }
    public int OverdueCount { get; set; }
    public int UnpaidCount { get; set; }
}
