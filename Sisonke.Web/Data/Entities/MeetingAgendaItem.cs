using System.ComponentModel.DataAnnotations;

namespace Sisonke.Web.Data.Entities;

public class MeetingAgendaItem
{
    public Guid Id { get; set; }

    public Guid MeetingId { get; set; }
    public Meeting Meeting { get; set; } = default!;

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsCompleted { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }
}
