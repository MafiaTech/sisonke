using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Services.Billing;

public sealed class SubscriptionPaymentProviderResolver : ISubscriptionPaymentProviderResolver
{
    private readonly IReadOnlyDictionary<SubscriptionProvider, ISubscriptionPaymentProvider> providers;

    public SubscriptionPaymentProviderResolver(IEnumerable<ISubscriptionPaymentProvider> providers)
    {
        this.providers = providers.ToDictionary(provider => provider.Provider);
    }

    public ISubscriptionPaymentProvider GetProvider(SubscriptionProvider provider) =>
        providers.TryGetValue(provider, out var resolved)
            ? resolved
            : throw new InvalidOperationException($"No payment setup provider is registered for {provider}.");

    public bool TryGetProvider(SubscriptionProvider provider, out ISubscriptionPaymentProvider? paymentProvider) =>
        providers.TryGetValue(provider, out paymentProvider);
}
