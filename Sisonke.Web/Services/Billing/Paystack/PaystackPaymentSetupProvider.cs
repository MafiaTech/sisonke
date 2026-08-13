using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Services.Billing.Paystack;

/// <summary>Adapts the existing Paystack gateway to the provider-neutral setup contract.</summary>
public sealed class PaystackPaymentSetupProvider(
    IBillingProvider billingProvider,
    PaystackOptions options,
    ILogger<PaystackPaymentSetupProvider> logger) : ISubscriptionPaymentProvider
{
    public SubscriptionProvider Provider => SubscriptionProvider.Paystack;
    public string DisplayName => "Paystack";
    public bool IsConfigured => !string.IsNullOrWhiteSpace(options.SecretKey);
    public string UnavailableMessage => "Payment setup is not configured in this environment.";

    public async Task<PaymentSetupResult> StartPaymentMethodSetupAsync(PaymentSetupRequest request, CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            return PaymentSetupResult.Unavailable(Provider, UnavailableMessage);
        }

        try
        {
            var customerReference = request.ProviderCustomerReference;
            if (string.IsNullOrWhiteSpace(customerReference))
            {
                customerReference = await billingProvider.EnsureCustomerAsync(
                    request.StokvelId, request.Email, request.Name, request.Phone, ct);
            }

            var start = await billingProvider.StartCardAuthorisationAsync(
                customerReference, request.Email, options.CardVerificationAmountMinorUnits,
                request.CallbackUrl, request.CorrelationReference, ct);

            return new(true, Provider, start.AuthorisationUrl, customerReference, start.Reference, null, null);
        }
        catch (Exception ex) when (ex is PaystackApiException or HttpRequestException or TaskCanceledException)
        {
            logger.LogError(ex, "Paystack payment setup could not be started for subscription {SubscriptionId}.", request.SubscriptionId);
            return PaymentSetupResult.Unavailable(Provider, DescribeFailure(ex));
        }
    }

    public async Task<PaymentMethodStatusResult> GetPaymentMethodStatusAsync(string providerReference, CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            return Failed(UnavailableMessage);
        }

        try
        {
            var verified = await billingProvider.VerifyAuthorisationAsync(providerReference, ct);
            if (!verified.Success || string.IsNullOrWhiteSpace(verified.AuthorizationCode))
            {
                return Failed("Payment method setup could not be verified.");
            }

            if (!verified.Reusable)
            {
                return Failed("This card cannot be used for recurring billing. Please use another payment method.");
            }

            try
            {
                await billingProvider.RefundTransactionAsync(providerReference, ct);
            }
            catch (Exception ex) when (ex is PaystackApiException or HttpRequestException or TaskCanceledException)
            {
                logger.LogError(ex, "Paystack verification refund requires attention for reference {Reference}.", providerReference);
            }

            return new(
                true, Provider, SubscriptionPaymentMethodType.Card, MandateStatus.Active,
                verified.AuthorizationCode, providerReference, true,
                string.IsNullOrWhiteSpace(verified.Last4) ? "Card on file" : $"Card ending {verified.Last4}",
                verified.CardBrand, verified.Last4, verified.ExpiryMonth, verified.ExpiryYear,
                verified.Bank, null, null);
        }
        catch (Exception ex) when (ex is PaystackApiException or HttpRequestException or TaskCanceledException)
        {
            logger.LogError(ex, "Paystack payment setup verification failed.");
            return Failed(DescribeFailure(ex));
        }
    }

    public bool IsPaymentReady(SubscriptionPaymentMethod method) =>
        method.Provider == Provider && SubscriptionPaymentReadiness.IsReady(method);

    private PaymentMethodStatusResult Failed(string message) => new(
        false, Provider, SubscriptionPaymentMethodType.Unknown, MandateStatus.Failed,
        null, null, false, null, null, null, null, null, null, "setup_failed", message);

    private static string DescribeFailure(Exception ex) =>
        ex is PaystackApiException { StatusCode: 401 or 403 }
            ? "Payment setup is not configured in this environment."
            : "Payment setup is temporarily unavailable. Please try again later.";
}
