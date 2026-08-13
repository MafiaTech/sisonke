using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Services.Billing;

/// <summary>Provider-neutral hosted payment-method setup boundary. It never accepts raw payment credentials.</summary>
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
