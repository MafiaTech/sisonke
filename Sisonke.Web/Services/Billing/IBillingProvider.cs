namespace Sisonke.Web.Services.Billing;

/// <summary>
/// Provider-agnostic billing gateway abstraction. SubscriptionService and everything else in
/// this application depends only on this interface and the plain records below — no Paystack
/// request/response shape, field name or type may appear outside Services/Billing/Paystack (see
/// the PaystackTypesStayInPaystackFolder architecture test).
/// </summary>
public interface IBillingProvider
{
    Task<string> EnsureCustomerAsync(Guid stokvelId, string email, string name, string? phone, CancellationToken ct = default);

    Task<string> EnsurePlanAsync(string planCode, string name, long amountMinorUnits, string interval, CancellationToken ct = default);

    Task<CardAuthorisationStart> StartCardAuthorisationAsync(
        string customerCode, string email, long amountMinorUnits, string callbackUrl, CancellationToken ct = default);

    Task<VerifiedAuthorisation> VerifyAuthorisationAsync(string reference, CancellationToken ct = default);

    /// <summary>Refunds a real charge made purely to capture a reusable authorisation (see PaystackOptions.CardVerificationAmountMinorUnits).</summary>
    Task RefundTransactionAsync(string reference, CancellationToken ct = default);

    Task<ProviderSubscriptionResult> CreateSubscriptionAsync(
        string customerCode, string planCode, string authorizationCode, DateTime startDate, CancellationToken ct = default);

    Task CancelSubscriptionAsync(string subscriptionCode, string emailToken, CancellationToken ct = default);

    Task<string> GetManagementLinkAsync(string subscriptionCode, CancellationToken ct = default);

    /// <summary>
    /// Charges a stored reusable authorisation directly (Paystack's transaction/charge_authorization).
    /// Used by TrialExpiryJob (Paystack has no confirmed "get subscription status" call to check
    /// whether its own automatic first debit will fire) and DunningJob's manual retries. Reference
    /// must be a caller-generated, stable-per-attempt idempotency key.
    /// </summary>
    Task<ChargeResult> ChargeAuthorisationAsync(
        string authorizationCode, string email, long amountMinorUnits, string reference, CancellationToken ct = default);
}

public sealed record CardAuthorisationStart(string AuthorisationUrl, string Reference, string AccessCode);

public sealed record VerifiedAuthorisation(
    bool Success,
    string Reference,
    string? AuthorizationCode,
    bool Reusable,
    string? CardBrand,
    string? Last4,
    int? ExpiryMonth,
    int? ExpiryYear,
    string? Bank,
    string? CustomerEmail);

public sealed record ProviderSubscriptionResult(string SubscriptionCode, string EmailToken, DateTime? NextPaymentDate);

public sealed record ChargeResult(bool Success, string Reference, string? FailureCode, string? FailureMessage);
