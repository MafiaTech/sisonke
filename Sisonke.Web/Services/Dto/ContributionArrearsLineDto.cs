namespace Sisonke.Web.Services.Dto;

public sealed class ContributionArrearsLineDto
{
    public Guid ContributionId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthYear { get; set; } = string.Empty;
    public decimal ExpectedAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal Balance { get; set; }
    public DateTime? DueDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Reference { get; set; }
}
