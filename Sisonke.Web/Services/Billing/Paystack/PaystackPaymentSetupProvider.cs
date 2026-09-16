using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Services.Billing.Paystack;

/// <summary>Adapts the existing Paystack gateway to the provider-neutral setup contract.</summary>
public sealed class PaystackPaymentSetupProvider(
    IBillingProvider billingProvider,
    PaystackOptions options,
    SubscriptionPaymentOptions subscriptionPaymentOptions,
    ILogger<PaystackPaymentSetupProvider> logger) : ISubscriptionPaymentProvider
{
    public SubscriptionProvider Provider => SubscriptionProvider.Paystack;
    public string DisplayName => "Paystack";
    public bool IsConfigured => options.IsEnabled && !string.IsNullOrWhiteSpace(options.SecretKey) &&
        (!subscriptionPaymentOptions.TestMode || options.SecretKey.StartsWith("sk_test_", StringComparison.Ordinal));
    public string UnavailableMessage => subscriptionPaymentOptions.TestMode &&
        !string.IsNullOrWhiteSpace(options.SecretKey) &&
        !options.SecretKey.StartsWith("sk_test_", StringComparison.Ordinal)
            ? "Payment setup requires a Paystack test key in this environment."
            : "Payment setup is not configured in this environment.";

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

    public async Task<PaymentMethodStatusResult> GetPaymentMethodStatusAsync(
        PaymentMethodStatusRequest request, CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            return Failed(UnavailableMessage);
        }

        try
        {
            var verified = await billingProvider.VerifyAuthorisationAsync(request.ProviderReference, ct);
            if (!verified.Success || string.IsNullOrWhiteSpace(verified.AuthorizationCode))
            {
                return Failed("Payment method setup could not be verified.");
            }

            if (!verified.Reusable)
            {
                return Failed("This card cannot be used for recurring billing. Please use another payment method.");
            }

            if (!string.Equals(verified.Reference, request.ProviderReference, StringComparison.Ordinal) ||
                verified.AmountMinorUnits != options.CardVerificationAmountMinorUnits ||
                !string.Equals(verified.Currency, options.Currency, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(verified.Channel, "card", StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrWhiteSpace(request.ExpectedBillingEmail) &&
                 !string.Equals(verified.CustomerEmail, request.ExpectedBillingEmail, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(request.ExpectedCustomerReference) &&
                 !string.Equals(verified.CustomerCode, request.ExpectedCustomerReference, StringComparison.Ordinal)))
            {
                logger.LogWarning("Paystack payment setup verification did not match the server-side setup expectations.");
                return Failed("Payment method setup details did not match the original request.", "verification_mismatch");
            }

            if (subscriptionPaymentOptions.TestMode &&
                !string.Equals(verified.Domain, "test", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning("Paystack returned a non-test transaction while subscription payment test mode is enabled.");
                return Failed("Payment method setup was not completed in Paystack test mode.", "not_test_mode");
            }

            try
            {
                await billingProvider.RefundTransactionAsync(request.ProviderReference, ct);
            }
            catch (Exception ex) when (ex is PaystackApiException or HttpRequestException or TaskCanceledException)
            {
                logger.LogError(ex, "Paystack verification refund requires attention for the verified setup transaction.");
            }

            return new(
                true, Provider, SubscriptionPaymentMethodType.Card, MandateStatus.Active,
                verified.AuthorizationCode, request.ProviderReference, true,
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

    private PaymentMethodStatusResult Failed(string message, string errorCode = "setup_failed") => new(
        false, Provider, SubscriptionPaymentMethodType.Unknown, MandateStatus.Failed,
        null, null, false, null, null, null, null, null, null, errorCode, message);

    private static string DescribeFailure(Exception ex) =>
        ex is PaystackApiException { StatusCode: 401 or 403 }
            ? "Payment setup is not configured in this environment."
            : "Payment setup is temporarily unavailable. Please try again later.";
}
