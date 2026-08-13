using System.ComponentModel.DataAnnotations;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Data.Entities;

/// <summary>
/// Append-only audit trail of subscription state transitions. No update or delete mappings —
/// rows are written once by whichever service performs the transition and never modified.
/// </summary>
public class SubscriptionEvent
{
    public Guid Id { get; set; }

    public Guid OrganisationSubscriptionId { get; set; }
    public OrganisationSubscription OrganisationSubscription { get; set; } = default!;

    public SubscriptionEventType EventType { get; set; }

    public SubscriptionStatus? FromStatus { get; set; }

    public SubscriptionStatus? ToStatus { get; set; }

    /// <summary>Null = system-initiated (background job, webhook), not a user action.</summary>
    [MaxLength(450)]
    public string? ActorUserId { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public string? PayloadJson { get; set; }

    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
