using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Services.Billing;

/// <summary>
/// Provider-neutral setup boundary exclusively for fees owed to Sisonke for its SaaS subscription.
/// It must never be used for contributions, loans, member payments, benefits, payouts, wallets or
/// any other stokvel funds. It never accepts raw payment credentials or a client-selected amount.
/// See docs/subscription-payment-boundary.md.
/// </summary>
public interface ISubscriptionPaymentProvider
{
    SubscriptionProvider Provider { get; }
    string DisplayName { get; }
    bool IsConfigured { get; }
    string UnavailableMessage { get; }

    Task<PaymentSetupResult> StartPaymentMethodSetupAsync(PaymentSetupRequest request, CancellationToken ct = default);
    Task<PaymentMethodStatusResult> GetPaymentMethodStatusAsync(string providerReference, CancellationToken ct = default);
    bool IsPaymentReady(SubscriptionPaymentMethod method);
}

public interface ISubscriptionPaymentProviderResolver
{
    ISubscriptionPaymentProvider GetProvider(SubscriptionProvider provider);
    bool TryGetProvider(SubscriptionProvider provider, out ISubscriptionPaymentProvider? paymentProvider);
}
