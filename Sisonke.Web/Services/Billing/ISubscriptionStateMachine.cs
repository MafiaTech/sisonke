using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Services.Billing;

/// <summary>
/// The single authoritative place OrganisationSubscription.Status ever changes. Every caller —
/// SubscriptionService, BillingWebhookProcessor, every Phase 4 job — must go through this
/// instead of assigning subscription.Status directly.
/// </summary>
public interface ISubscriptionStateMachine
{
    bool CanTransition(SubscriptionStatus from, SubscriptionStatus to);

    /// <summary>
    /// Throws InvalidSubscriptionTransitionException for anything not in the allowed-transitions
    /// table — including a same-state "transition", which is not in the table either. Callers
    /// that only want to act when a real change is needed must check subscription.Status
    /// themselves before calling. Saves, writes the SubscriptionEvent and invalidates the
    /// entitlement cache as one unit.
    /// </summary>
    Task TransitionAsync(
        ApplicationDbContext context,
        OrganisationSubscription subscription,
        SubscriptionStatus to,
        SubscriptionEventType eventType,
        string? actorUserId,
        string reason,
        CancellationToken ct = default);
}
