using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Services.Billing;

/// <summary>Single provider-aware readiness rule shared by entitlements, UI and provider adapters.</summary>
public static class SubscriptionPaymentReadiness
{
    public static bool IsReady(SubscriptionPaymentMethod method) => method.Provider switch
    {
        SubscriptionProvider.Paystack =>
            method.PaymentMethodType == SubscriptionPaymentMethodType.Card &&
            method.MandateStatus == MandateStatus.Active && method.IsReusable &&
            !string.IsNullOrWhiteSpace(method.ProviderPaymentMethodReference ?? method.ProviderAuthorizationCode),

        SubscriptionProvider.Netcash =>
            method.PaymentMethodType is SubscriptionPaymentMethodType.DebitOrder or SubscriptionPaymentMethodType.DebiCheck &&
            method.MandateStatus == MandateStatus.Active &&
            !string.IsNullOrWhiteSpace(method.ProviderMandateReference),

        _ => false
    };
}
