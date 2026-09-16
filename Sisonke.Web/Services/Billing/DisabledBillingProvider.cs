namespace Sisonke.Web.Services.Billing;

/// <summary>No network calls or successful payment results when the billing gateway is disabled.</summary>
public sealed class DisabledBillingProvider : IBillingProvider
{
    private static HttpRequestException Unavailable() => new("Subscription payment processing is disabled in this environment.");
    public Task<string> EnsureCustomerAsync(Guid stokvelId, string email, string name, string? phone, CancellationToken ct = default) => Task.FromException<string>(Unavailable());
    public Task<string> EnsurePlanAsync(string planCode, string name, long amountMinorUnits, string interval, CancellationToken ct = default) => Task.FromException<string>(Unavailable());
    public Task<CardAuthorisationStart> StartCardAuthorisationAsync(string customerCode, string email, long amountMinorUnits, string callbackUrl, CancellationToken ct = default) => Task.FromException<CardAuthorisationStart>(Unavailable());
    public Task<CardAuthorisationStart> StartCardAuthorisationAsync(string customerCode, string email, long amountMinorUnits, string callbackUrl, string reference, CancellationToken ct = default) => Task.FromException<CardAuthorisationStart>(Unavailable());
    public Task<VerifiedAuthorisation> VerifyAuthorisationAsync(string reference, CancellationToken ct = default) => Task.FromException<VerifiedAuthorisation>(Unavailable());
    public Task RefundTransactionAsync(string reference, CancellationToken ct = default) => Task.FromException(Unavailable());
    public Task<ProviderSubscriptionResult> CreateSubscriptionAsync(string customerCode, string planCode, string authorizationCode, DateTime startDate, CancellationToken ct = default) => Task.FromException<ProviderSubscriptionResult>(Unavailable());
    public Task CancelSubscriptionAsync(string subscriptionCode, string emailToken, CancellationToken ct = default) => Task.FromException(Unavailable());
    public Task<string> GetManagementLinkAsync(string subscriptionCode, CancellationToken ct = default) => Task.FromException<string>(Unavailable());
    public Task<ChargeResult> ChargeAuthorisationAsync(string authorizationCode, string email, long amountMinorUnits, string reference, CancellationToken ct = default) => Task.FromException<ChargeResult>(Unavailable());
}
