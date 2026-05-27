using System.ComponentModel.DataAnnotations;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Data.Entities;

public class MeetingAttendance
{
    public Guid Id { get; set; }

    public Guid MeetingId { get; set; }
    public Meeting Meeting { get; set; } = default!;

    public Guid MemberId { get; set; }
    public Member Member { get; set; } = default!;

    public AttendanceStatus Status { get; set; } = AttendanceStatus.Absent;

    public bool IsLate { get; set; }

    public bool LeftEarly { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime MarkedAt { get; set; } = DateTime.UtcNow;
}
