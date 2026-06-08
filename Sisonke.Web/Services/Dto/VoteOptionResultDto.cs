namespace Sisonke.Web.Services.Dto;

public class VoteOptionResultDto
{
    public Guid VoteOptionId { get; set; }
    public string OptionText { get; set; } = string.Empty;
    public int VoteCount { get; set; }
    public decimal Percentage { get; set; }
}
