using System.ComponentModel.DataAnnotations;

namespace Sisonke.Web.Data.Entities;

public class MemberVote
{
    public Guid Id { get; set; }

    public Guid VoteMotionId { get; set; }
    public VoteMotion? VoteMotion { get; set; }

    public Guid MemberId { get; set; }
    public Member? Member { get; set; }

    public Guid VoteOptionId { get; set; }
    public VoteOption? VoteOption { get; set; }

    public DateTime VotedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(1000)]
    public string? Notes { get; set; }
}
