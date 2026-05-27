using System.ComponentModel.DataAnnotations;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Data.Entities;

public class MeetingVote
{
    public Guid Id { get; set; }

    public Guid MeetingId { get; set; }
    public Meeting Meeting { get; set; } = default!;

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public VotingMethod VotingMethod { get; set; } = VotingMethod.SimpleMajority;

    public VoteStatus Status { get; set; } = VoteStatus.Open;

    public VoteResult Result { get; set; } = VoteResult.Pending;

    public DateTime OpenedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ClosedAt { get; set; }

    public ICollection<MeetingVoteResponse> Responses { get; set; } = new List<MeetingVoteResponse>();
}
