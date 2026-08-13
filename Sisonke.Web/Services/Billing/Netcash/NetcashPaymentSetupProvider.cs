using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Services.Billing.Netcash;

/// <summary>
/// Provider-neutral readiness adapter. Netcash DebiCheck initiation is handled by
/// INetcashMandateService because bank details are transient input, never generic hosted-setup state.
/// </summary>
public sealed class NetcashPaymentSetupProvider(NetcashOptions options) : ISubscriptionPaymentProvider
{
    public SubscriptionProvider Provider => SubscriptionProvider.Netcash;
    public string DisplayName => "Netcash";
    public bool IsConfigured => options.IsConfigured;
    public string UnavailableMessage => options.Enabled
        ? "Netcash DebiCheck configuration is incomplete. Please contact support."
        : "Netcash DebiCheck setup is not configured in this environment.";

    public Task<PaymentSetupResult> StartPaymentMethodSetupAsync(PaymentSetupRequest request, CancellationToken ct = default) =>
        Task.FromResult(PaymentSetupResult.Unavailable(Provider,
            "Choose DebiCheck on the Billing & Subscription page to submit a mandate."));

    public Task<PaymentMethodStatusResult> GetPaymentMethodStatusAsync(string providerReference, CancellationToken ct = default) =>
        Task.FromResult(new PaymentMethodStatusResult(
            false, Provider, SubscriptionPaymentMethodType.Unknown, MandateStatus.None,
            null, null, false, null, null, null, null, null, null,
            "provider_unavailable", UnavailableMessage));

    public bool IsPaymentReady(SubscriptionPaymentMethod method) =>
        method.Provider == Provider && SubscriptionPaymentReadiness.IsReady(method);
}
