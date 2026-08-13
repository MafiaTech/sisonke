using System.ComponentModel.DataAnnotations;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Data.Entities;

/// <summary>
/// One row per received Paystack webhook call. ProviderEventId is the idempotency key — every
/// webhook handler must check for an existing row with the same ProviderEventId before applying
/// any state transition, and every payload's HMAC-SHA512 signature must be verified before
/// SignatureValid is set true.
/// </summary>
public class BillingWebhookEvent
{
    public Guid Id { get; set; }

    public SubscriptionProvider Provider { get; set; }

    [Required]
    [MaxLength(150)]
    public string ProviderEventId { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string EventType { get; set; } = string.Empty;

    [Required]
    public string RawPayload { get; set; } = string.Empty;

    public bool SignatureValid { get; set; }

    public DateTime? ProcessedAt { get; set; }

    public WebhookProcessingStatus ProcessingStatus { get; set; } = WebhookProcessingStatus.Received;

    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }

    public int AttemptCount { get; set; }

    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
}
