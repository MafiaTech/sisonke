using System.ComponentModel.DataAnnotations;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Data.Entities;

public class Meeting
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = default!;

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public DateTime MeetingDate { get; set; } = DateTime.Today;

    [MaxLength(150)]
    public string? Venue { get; set; }

    public MeetingStatus Status { get; set; } = MeetingStatus.Planned;

    [MaxLength(500)]
    public string? Purpose { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<MeetingAgendaItem> AgendaItems { get; set; } = new List<MeetingAgendaItem>();
}
