using System.ComponentModel.DataAnnotations;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Data.Entities;

public class NotificationMessage
{
    public Guid Id { get; set; }

    public Guid? StokvelId { get; set; }

    public Guid RecipientMemberId { get; set; }
    public Member RecipientMember { get; set; } = default!;

    [Required]
    [MaxLength(100)]
    public string EntityType { get; set; } = string.Empty;

    public Guid EntityId { get; set; }

    public NotificationChannel Channel { get; set; }

    public NotificationType Type { get; set; }

    [Required]
    [MaxLength(200)]
    public string DedupeKey { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Subject { get; set; }

    [Required]
    public string Body { get; set; } = string.Empty;

    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;

    public int AttemptCount { get; set; }

    public DateTime? LastAttemptAt { get; set; }

    [MaxLength(1000)]
    public string? LastError { get; set; }

    public DateTime? SentAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
