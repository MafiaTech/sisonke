namespace Sisonke.Web.Services.Dto;

public class VoteMotionSummaryDto
{
    public Guid VoteMotionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string VoteType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsAnonymous { get; set; }
    public DateTime OpensAt { get; set; }
    public DateTime? ClosesAt { get; set; }
    public int EligibleMemberCount { get; set; }
    public int TotalVotesCast { get; set; }
    public bool CurrentUserHasVoted { get; set; }
    public List<VoteOptionResultDto> Results { get; set; } = [];
}
