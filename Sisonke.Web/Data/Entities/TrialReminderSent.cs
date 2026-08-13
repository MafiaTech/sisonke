namespace Sisonke.Web.Data.Entities;

/// <summary>
/// One row per (OrganisationSubscription, days-remaining bucket) — the idempotency marker
/// TrialReminderJob checks before sending, so a re-run (same day or after a missed window)
/// never double-sends a bucket's reminder.
/// </summary>
public class TrialReminderSent
{
    public Guid Id { get; set; }

    public Guid OrganisationSubscriptionId { get; set; }
    public OrganisationSubscription OrganisationSubscription { get; set; } = default!;

    /// <summary>One of 30, 14, 7, 3, 1.</summary>
    public int Bucket { get; set; }

    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}
