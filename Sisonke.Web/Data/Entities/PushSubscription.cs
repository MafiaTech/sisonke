using System.ComponentModel.DataAnnotations;

namespace Sisonke.Web.Data.Entities;

public class PushSubscription
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Endpoint { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string P256dh { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Auth { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
