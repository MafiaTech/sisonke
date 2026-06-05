using System.ComponentModel.DataAnnotations;

namespace Sisonke.Web.Data.Entities;

public class MeetingMinute
{
    public Guid Id { get; set; }

    public Guid MeetingId { get; set; }
    public Meeting? Meeting { get; set; }

    public Guid StokvelId { get; set; }
    public Stokvel? Stokvel { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public string OpeningNotes { get; set; } = string.Empty;

    public string AttendanceSummary { get; set; } = string.Empty;

    public string ApologySummary { get; set; } = string.Empty;

    public string MattersArising { get; set; } = string.Empty;

    public string DecisionsTaken { get; set; } = string.Empty;

    public string ActionItems { get; set; } = string.Empty;

    public string ClosingNotes { get; set; } = string.Empty;

    [Required]
    [MaxLength(30)]
    public string Status { get; set; } = "Draft";

    public Guid? CreatedByMemberId { get; set; }

    public Guid? UpdatedByMemberId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public Guid? ApprovedByMemberId { get; set; }
}
