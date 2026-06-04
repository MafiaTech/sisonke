namespace Sisonke.Web.Data.Entities;

public class MemberWarning
{
    public Guid Id { get; set; }

    public Guid MemberId { get; set; }
    public Member? Member { get; set; }

    public Guid StokvelId { get; set; }
    public Stokvel? Stokvel { get; set; }

    public Guid? MeetingId { get; set; }
    public Meeting? Meeting { get; set; }

    public string WarningType { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;

    public int AbsenceCount { get; set; }

    public string Status { get; set; } = "Open";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? AcknowledgedAt { get; set; }

    public Guid? CreatedByMemberId { get; set; }

    public string? Notes { get; set; }
}
