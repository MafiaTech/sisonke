using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Services.Billing;

public sealed record PaymentSetupRequest(
    Guid StokvelId,
    Guid SubscriptionId,
    string? ProviderCustomerReference,
    string Email,
    string Name,
    string? Phone,
    string CallbackUrl,
    string CorrelationReference);

public sealed record PaymentSetupResult(
    bool Success,
    SubscriptionProvider Provider,
    string? SetupUrl,
    string? ProviderCustomerReference,
    string? ProviderRequestReference,
    string? ErrorCode,
    string? Message)
{
    public static PaymentSetupResult Unavailable(SubscriptionProvider provider, string message) =>
        new(false, provider, null, null, null, "provider_unavailable", message);
}

public sealed record PaymentMethodStatusResult(
    bool Success,
    SubscriptionProvider Provider,
    SubscriptionPaymentMethodType PaymentMethodType,
    MandateStatus MandateStatus,
    string? ProviderPaymentMethodReference,
    string? ProviderMandateReference,
    bool IsReusable,
    string? MaskedDisplay,
    string? CardBrand,
    string? Last4,
    int? ExpiryMonth,
    int? ExpiryYear,
    string? Bank,
    string? ErrorCode,
    string? Message);

/// <summary>User-safe payment setup state. Provider tokens and mandate references are deliberately absent.</summary>
public sealed record PaymentSetupViewModel(
    SubscriptionProvider Provider,
    string ProviderDisplayName,
    bool ProviderAvailable,
    bool PaymentReady,
    SubscriptionPaymentMethodType PaymentMethodType,
    MandateStatus MandateStatus,
    string? MaskedDisplay,
    string? UnavailableMessage);

public sealed record PaymentSetupStartResult(bool Success, string? SetupUrl, string? Message)
{
    public static PaymentSetupStartResult Failed(string message) => new(false, null, message);
}

public sealed record PaymentSetupCompletionResult(bool Success, string? RedirectPath, string? Message)
{
    public static PaymentSetupCompletionResult Failed(string message) => new(false, null, message);
}
