using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;
using Sisonke.Web.Services.Entitlements;

namespace Sisonke.Web.Services.Billing;

public sealed class SubscriptionStateMachine(
    IEntitlementService entitlementService,
    TimeProvider timeProvider,
    ILogger<SubscriptionStateMachine> logger) : ISubscriptionStateMachine
{
    private static readonly Dictionary<SubscriptionStatus, SubscriptionStatus[]> AllowedTransitions = new()
    {
        [SubscriptionStatus.LegacyUnsubscribed] = [SubscriptionStatus.PendingPaymentMethod],
        [SubscriptionStatus.PendingPaymentMethod] = [SubscriptionStatus.Trialing, SubscriptionStatus.Cancelled],
        [SubscriptionStatus.Trialing] =
            [SubscriptionStatus.Active, SubscriptionStatus.PastDue, SubscriptionStatus.Cancelled, SubscriptionStatus.Expired],
        [SubscriptionStatus.Active] = [SubscriptionStatus.PastDue, SubscriptionStatus.Cancelled],
        [SubscriptionStatus.PastDue] = [SubscriptionStatus.Active, SubscriptionStatus.Restricted, SubscriptionStatus.Cancelled],
        [SubscriptionStatus.Restricted] = [SubscriptionStatus.Active, SubscriptionStatus.Suspended, SubscriptionStatus.Cancelled],
        [SubscriptionStatus.Suspended] = [SubscriptionStatus.Active, SubscriptionStatus.Cancelled, SubscriptionStatus.Expired],
        [SubscriptionStatus.Cancelled] = [SubscriptionStatus.Trialing, SubscriptionStatus.Active],
        [SubscriptionStatus.Expired] = []
    };

    public bool CanTransition(SubscriptionStatus from, SubscriptionStatus to) =>
        AllowedTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);

    public async Task TransitionAsync(
        ApplicationDbContext context,
        OrganisationSubscription subscription,
        SubscriptionStatus to,
        SubscriptionEventType eventType,
        string? actorUserId,
        string reason,
        CancellationToken ct = default)
    {
        var from = subscription.Status;

        if (!CanTransition(from, to))
        {
            logger.LogError(
                "Illegal subscription status transition attempted: {From} -> {To} for subscription {SubscriptionId}.",
                from, to, subscription.Id);
            throw new InvalidSubscriptionTransitionException(from, to);
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        subscription.Status = to;
        subscription.UpdatedAt = now;
        subscription.RowVersion = Guid.NewGuid().ToByteArray();

        context.SubscriptionEvents.Add(new SubscriptionEvent
        {
            Id = Guid.NewGuid(),
            OrganisationSubscriptionId = subscription.Id,
            EventType = eventType,
            FromStatus = from,
            ToStatus = to,
            ActorUserId = actorUserId,
            Notes = reason,
            OccurredAt = now
        });

        await context.SaveChangesAsync(ct);
        await entitlementService.InvalidateAsync(subscription.StokvelId, ct);
    }
}
