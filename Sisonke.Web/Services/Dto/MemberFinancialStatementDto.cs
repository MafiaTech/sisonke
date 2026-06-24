namespace Sisonke.Web.Services.Dto;

public sealed class MemberFinancialStatementDto
{
    public Guid MemberId { get; set; }
    public Guid StokvelId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public string? MemberNumber { get; set; }
    public string? CellphoneNumber { get; set; }
    public string? Email { get; set; }
    public string StokvelName { get; set; } = string.Empty;
    public bool IsRotationalStokvel { get; set; }
    public decimal TotalExpectedContributions { get; set; }
    public decimal TotalContributionPaid { get; set; }
    public decimal TotalContributionOutstanding { get; set; }
    public int OverdueContributionCount { get; set; }
    public decimal TotalOutstandingFines { get; set; }
    public int OutstandingFineCount { get; set; }
    public decimal TotalOutstandingBalance { get; set; }
    public DateTime GeneratedAt { get; set; }
    public List<ContributionArrearsLineDto> Contributions { get; set; } = [];
    public List<MemberFineStatementLineDto> Fines { get; set; } = [];
}
