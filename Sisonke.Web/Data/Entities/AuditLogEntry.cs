using System.ComponentModel.DataAnnotations;

namespace Sisonke.Web.Data.Entities;

public class AuditLogEntry
{
    public Guid Id { get; set; }

    [MaxLength(450)]
    public string? UserId { get; set; }

    public Guid? StokvelId { get; set; }

    [Required]
    [MaxLength(100)]
    public string ActionType { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string EntityType { get; set; } = string.Empty;

    public Guid? EntityId { get; set; }

    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

    [Required]
    [MaxLength(1000)]
    public string Summary { get; set; } = string.Empty;
}
