namespace Sisonke.Web.Services.Dto;

public class ContributionMonthSummaryDto
{
    public int ActiveMemberCount { get; set; }
    public decimal MonthlyContributionAmount { get; set; }
    public decimal ExpectedContributions { get; set; }
    public decimal ActualContributions { get; set; }
    public decimal OutstandingContributions { get; set; }
    public decimal CollectionRatePercentage { get; set; }
    public int TotalMembers { get; set; }
    public int RecordsGenerated { get; set; }
    public int PaidCount { get; set; }
    public int PartiallyPaidCount { get; set; }
    public int OverdueCount { get; set; }
    public int UnpaidCount { get; set; }
    public int WaivedCount { get; set; }
    public decimal TotalExpected { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalOutstanding { get; set; }
}
