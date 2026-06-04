using System.ComponentModel.DataAnnotations;

namespace Sisonke.Web.Data.Entities;

public class MeetingApology
{
    public Guid Id { get; set; }

    public Guid MeetingId { get; set; }

    public Meeting? Meeting { get; set; }

    public Guid MemberId { get; set; }

    public Member? Member { get; set; }

    [Required]
    [MaxLength(100)]
    public string ApologyType { get; set; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string Reason { get; set; } = string.Empty;

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [MaxLength(30)]
    public string Status { get; set; } = "Submitted";

    [MaxLength(1000)]
    public string? ResponseNote { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public Guid? ReviewedByMemberId { get; set; }
}
