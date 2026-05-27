using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Data.Entities;

public class MeetingVoteResponse
{
    public Guid Id { get; set; }

    public Guid MeetingVoteId { get; set; }
    public MeetingVote MeetingVote { get; set; } = default!;

    public Guid MemberId { get; set; }
    public Member Member { get; set; } = default!;

    public VoteChoice Choice { get; set; } = VoteChoice.Abstain;

    public DateTime VotedAt { get; set; } = DateTime.UtcNow;
}
