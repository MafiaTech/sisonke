using Sisonke.Web.Services.Billing;

namespace Sisonke.Web.Tests.TestSupport;

public sealed class FakeBillingProvider : IBillingProvider
{
    public VerifiedAuthorisation VerificationToReturn { get; set; } = new(
        Success: true, Reference: "ref-1", AmountMinorUnits: 100, Currency: "ZAR", Domain: "test", Channel: "card",
        AuthorizationCode: "AUTH_1", Reusable: true, CardBrand: "visa", Last4: "4242",
        ExpiryMonth: 12, ExpiryYear: 2030, Bank: "Test Bank", CustomerEmail: "chair@example.com", CustomerCode: "CUS_fake");

    public Exception? VerificationException { get; set; }

    public List<string> RefundedReferences { get; } = [];
    public List<string> CancelledSubscriptionCodes { get; } = [];
    public List<(string AuthorizationCode, long AmountMinorUnits, string Reference)> ChargeAttempts { get; } = [];
    public int CreateSubscriptionCalls { get; private set; }

    /// <summary>Dequeued in order for successive ChargeAuthorisationAsync calls; falls back to ChargeResultToReturn once empty.</summary>
    public Queue<ChargeResult> ChargeResultsQueue { get; } = new();

    public ChargeResult ChargeResultToReturn { get; set; } = new(true, "charge-ref", null, null);

    public Task<string> EnsureCustomerAsync(Guid stokvelId, string email, string name, string? phone, CancellationToken ct = default) =>
        Task.FromResult("CUS_fake");

    public Task<string> EnsurePlanAsync(string planCode, string name, long amountMinorUnits, string interval, CancellationToken ct = default) =>
        Task.FromResult($"PLN_fake_{planCode}");

    public Task<CardAuthorisationStart> StartCardAuthorisationAsync(
        string customerCode, string email, long amountMinorUnits, string callbackUrl, CancellationToken ct = default) =>
        Task.FromResult(new CardAuthorisationStart("https://checkout.paystack.com/fake", "ref-1", "access-1"));

    public Task<CardAuthorisationStart> StartCardAuthorisationAsync(
        string customerCode, string email, long amountMinorUnits, string callbackUrl, string reference, CancellationToken ct = default) =>
        Task.FromResult(new CardAuthorisationStart("https://checkout.paystack.com/fake", reference, "access-1"));

    public Task<VerifiedAuthorisation> VerifyAuthorisationAsync(string reference, CancellationToken ct = default) =>
        VerificationException is null
            ? Task.FromResult(VerificationToReturn with { Reference = VerificationToReturn.Reference == "ref-1" ? reference : VerificationToReturn.Reference })
            : Task.FromException<VerifiedAuthorisation>(VerificationException);

    public Task RefundTransactionAsync(string reference, CancellationToken ct = default)
    {
        RefundedReferences.Add(reference);
        return Task.CompletedTask;
    }

    public Task<ProviderSubscriptionResult> CreateSubscriptionAsync(
        string customerCode, string planCode, string authorizationCode, DateTime startDate, CancellationToken ct = default)
    {
        CreateSubscriptionCalls++;
        return Task.FromResult(new ProviderSubscriptionResult("SUB_fake", "tok_fake", startDate));
    }

    public Task CancelSubscriptionAsync(string subscriptionCode, string emailToken, CancellationToken ct = default)
    {
        CancelledSubscriptionCodes.Add(subscriptionCode);
        return Task.CompletedTask;
    }

    public Task<string> GetManagementLinkAsync(string subscriptionCode, CancellationToken ct = default) =>
        Task.FromResult("https://paystack.com/manage/subscriptions/SUB_fake");

    public Task<ChargeResult> ChargeAuthorisationAsync(
        string authorizationCode, string email, long amountMinorUnits, string reference, CancellationToken ct = default)
    {
        ChargeAttempts.Add((authorizationCode, amountMinorUnits, reference));

        var result = ChargeResultsQueue.Count > 0 ? ChargeResultsQueue.Dequeue() : ChargeResultToReturn;
        return Task.FromResult(result with { Reference = reference });
    }
}
