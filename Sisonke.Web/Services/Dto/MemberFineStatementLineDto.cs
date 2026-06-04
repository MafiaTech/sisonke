namespace Sisonke.Web.Services.Dto;

public sealed class MemberFineStatementLineDto
{
    public Guid FineId { get; set; }
    public string FineType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal Balance { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? IssuedDate { get; set; }
    public string? Reason { get; set; }
}
