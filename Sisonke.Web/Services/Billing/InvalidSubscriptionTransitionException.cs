using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Services.Billing;

public sealed class InvalidSubscriptionTransitionException(SubscriptionStatus from, SubscriptionStatus to)
    : InvalidOperationException($"Illegal subscription status transition: {from} -> {to}.")
{
    public SubscriptionStatus From { get; } = from;
    public SubscriptionStatus To { get; } = to;
}
