using System.ComponentModel.DataAnnotations;

namespace Sisonke.Web.Data.Entities;

public class VoteMotion
{
    public Guid Id { get; set; }

    public Guid StokvelId { get; set; }
    public Stokvel? Stokvel { get; set; }

    public Guid? MeetingId { get; set; }
    public Meeting? Meeting { get; set; }

    public Guid? AgendaItemId { get; set; }
    public MeetingAgendaItem? AgendaItem { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(50)]
    public string VoteType { get; set; } = "YesNo";

    [MaxLength(50)]
    public string Status { get; set; } = "Draft";

    public bool IsAnonymous { get; set; }

    public DateTime OpensAt { get; set; } = DateTime.UtcNow;

    public DateTime? ClosesAt { get; set; }

    public Guid CreatedByMemberId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ClosedAt { get; set; }

    public Guid? ClosedByMemberId { get; set; }

    [MaxLength(1000)]
    public string? ResultSummary { get; set; }

    [MaxLength(100)]
    public string? DecisionOutcome { get; set; }

    public ICollection<VoteOption> Options { get; set; } = new List<VoteOption>();
    public ICollection<MemberVote> MemberVotes { get; set; } = new List<MemberVote>();
}
