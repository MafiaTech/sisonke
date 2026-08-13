using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Services.Billing.Netcash;

/// <summary>Netcash setup boundary. It deliberately reports unavailable until the real API contract is configured.</summary>
public sealed class NetcashPaymentSetupProvider(NetcashOptions options) : ISubscriptionPaymentProvider
{
    public SubscriptionProvider Provider => SubscriptionProvider.Netcash;
    public string DisplayName => "Netcash";
    public bool IsConfigured => false;
    public string UnavailableMessage => options.Enabled
        ? "Netcash payment setup is not yet available."
        : "Netcash payment setup is not configured in this environment.";

    public Task<PaymentSetupResult> StartPaymentMethodSetupAsync(PaymentSetupRequest request, CancellationToken ct = default) =>
        Task.FromResult(PaymentSetupResult.Unavailable(Provider, UnavailableMessage));

    public Task<PaymentMethodStatusResult> GetPaymentMethodStatusAsync(string providerReference, CancellationToken ct = default) =>
        Task.FromResult(new PaymentMethodStatusResult(
            false, Provider, SubscriptionPaymentMethodType.Unknown, MandateStatus.None,
            null, null, false, null, null, null, null, null, null,
            "provider_unavailable", UnavailableMessage));

    public bool IsPaymentReady(SubscriptionPaymentMethod method) =>
        method.Provider == Provider && SubscriptionPaymentReadiness.IsReady(method);
}
