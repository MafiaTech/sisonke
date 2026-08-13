using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;

namespace Sisonke.Web.Services.Billing.Paystack;

/// <summary>
/// The only class in Sisonke.Web that talks to Paystack's HTTP API or knows its request/response
/// shapes. HttpClient is registered as a typed client in Program.cs with the Polly retry policy,
/// base address and Authorization header already configured — see the DI registration comment
/// there for the idempotency reasoning behind retrying POSTs.
/// </summary>
public sealed class PaystackBillingProvider(HttpClient httpClient, ILogger<PaystackBillingProvider> logger) : IBillingProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    public async Task<string> EnsureCustomerAsync(Guid stokvelId, string email, string name, string? phone, CancellationToken ct = default)
    {
        var nameParts = name.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);

        var request = new PaystackCustomerRequest
        {
            Email = email,
            FirstName = nameParts.Length > 0 ? nameParts[0] : name,
            LastName = nameParts.Length > 1 ? nameParts[1] : string.Empty,
            Phone = phone
        };

        var data = await PostAsync<PaystackCustomerRequest, PaystackCustomerData>("customer", request, ct);
        return data.CustomerCode;
    }

    public async Task<string> EnsurePlanAsync(string planCode, string name, long amountMinorUnits, string interval, CancellationToken ct = default)
    {
        var request = new PaystackPlanRequest
        {
            Name = name,
            Amount = amountMinorUnits,
            Interval = interval,
            Currency = "ZAR"
        };

        var data = await PostAsync<PaystackPlanRequest, PaystackPlanData>("plan", request, ct);
        return data.PlanCode;
    }

    public async Task<CardAuthorisationStart> StartCardAuthorisationAsync(
        string customerCode, string email, long amountMinorUnits, string callbackUrl, CancellationToken ct = default)
        => await StartCardAuthorisationAsync(
            customerCode, email, amountMinorUnits, callbackUrl, $"sisonke-auth-{Guid.NewGuid():N}", ct);

    public async Task<CardAuthorisationStart> StartCardAuthorisationAsync(
        string customerCode, string email, long amountMinorUnits, string callbackUrl, string reference, CancellationToken ct = default)
    {
        // Client-generated reference makes this call idempotent under Polly retry — a retried
        // POST after a timeout carries the same reference, so Paystack rejects a duplicate
        // rather than initializing (and, downstream, charging) twice.
        var request = new PaystackInitializeTransactionRequest
        {
            Email = email,
            Amount = amountMinorUnits,
            Currency = "ZAR",
            CallbackUrl = callbackUrl,
            Reference = reference
        };

        var data = await PostAsync<PaystackInitializeTransactionRequest, PaystackInitializeTransactionData>(
            "transaction/initialize", request, ct);

        return new CardAuthorisationStart(data.AuthorizationUrl, data.Reference, data.AccessCode);
    }

    public async Task<VerifiedAuthorisation> VerifyAuthorisationAsync(string reference, CancellationToken ct = default)
    {
        var data = await GetAsync<PaystackVerifyTransactionData>($"transaction/verify/{Uri.EscapeDataString(reference)}", ct);

        var authorization = data.Authorization;
        int.TryParse(authorization?.ExpMonth, out var expMonth);
        int.TryParse(authorization?.ExpYear, out var expYear);

        return new VerifiedAuthorisation(
            Success: string.Equals(data.Status, "success", StringComparison.OrdinalIgnoreCase),
            Reference: data.Reference,
            AuthorizationCode: authorization?.AuthorizationCode,
            Reusable: authorization?.Reusable ?? false,
            CardBrand: authorization?.CardType,
            Last4: authorization?.Last4,
            ExpiryMonth: expMonth == 0 ? null : expMonth,
            ExpiryYear: expYear == 0 ? null : expYear,
            Bank: authorization?.Bank,
            CustomerEmail: data.Customer?.Email);
    }

    public async Task RefundTransactionAsync(string reference, CancellationToken ct = default)
    {
        var request = new PaystackRefundRequest { Transaction = reference };
        await PostVoidAsync("refund", request, ct);
    }

    public async Task<ProviderSubscriptionResult> CreateSubscriptionAsync(
        string customerCode, string planCode, string authorizationCode, DateTime startDate, CancellationToken ct = default)
    {
        var request = new PaystackCreateSubscriptionRequest
        {
            Customer = customerCode,
            Plan = planCode,
            Authorization = authorizationCode,
            StartDate = startDate.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture)
        };

        var data = await PostAsync<PaystackCreateSubscriptionRequest, PaystackSubscriptionData>("subscription", request, ct);
        return new ProviderSubscriptionResult(data.SubscriptionCode, data.EmailToken, data.NextPaymentDate);
    }

    public async Task CancelSubscriptionAsync(string subscriptionCode, string emailToken, CancellationToken ct = default)
    {
        var request = new PaystackDisableSubscriptionRequest { Code = subscriptionCode, Token = emailToken };
        await PostVoidAsync("subscription/disable", request, ct);
    }

    public async Task<ChargeResult> ChargeAuthorisationAsync(
        string authorizationCode, string email, long amountMinorUnits, string reference, CancellationToken ct = default)
    {
        var request = new PaystackChargeAuthorizationRequest
        {
            AuthorizationCode = authorizationCode,
            Email = email,
            Amount = amountMinorUnits,
            Reference = reference
        };

        try
        {
            var data = await PostAsync<PaystackChargeAuthorizationRequest, PaystackVerifyTransactionData>(
                "transaction/charge_authorization", request, ct);

            var success = string.Equals(data.Status, "success", StringComparison.OrdinalIgnoreCase);
            return new ChargeResult(
                success, reference,
                FailureCode: success ? null : data.Status,
                FailureMessage: success ? null : data.GatewayResponse);
        }
        catch (PaystackApiException ex)
        {
            return new ChargeResult(false, reference, FailureCode: "provider_error", FailureMessage: ex.Message);
        }
    }

    public async Task<string> GetManagementLinkAsync(string subscriptionCode, CancellationToken ct = default)
    {
        var data = await GetAsync<PaystackManagementLinkData>($"subscription/{Uri.EscapeDataString(subscriptionCode)}/manage/link", ct);
        return data.Link;
    }

    private async Task<TResponse> PostAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken ct)
    {
        logger.LogInformation("Paystack POST {Path}", path);

        using var response = await httpClient.PostAsJsonAsync(path, body, JsonOptions, ct);
        var envelope = await EnsureSuccessAsync<TResponse>(path, response, ct);

        if (envelope.Data is null)
        {
            throw new PaystackApiException($"Paystack response for {path} had no data payload.", (int)response.StatusCode);
        }

        return envelope.Data;
    }

    private async Task PostVoidAsync<TRequest>(string path, TRequest body, CancellationToken ct)
    {
        logger.LogInformation("Paystack POST {Path}", path);

        using var response = await httpClient.PostAsJsonAsync(path, body, JsonOptions, ct);
        await EnsureSuccessAsync<JsonElement?>(path, response, ct);
    }

    private async Task<TResponse> GetAsync<TResponse>(string path, CancellationToken ct)
    {
        logger.LogInformation("Paystack GET {Path}", path);

        using var response = await httpClient.GetAsync(path, ct);
        var envelope = await EnsureSuccessAsync<TResponse>(path, response, ct);

        if (envelope.Data is null)
        {
            throw new PaystackApiException($"Paystack response for {path} had no data payload.", (int)response.StatusCode);
        }

        return envelope.Data;
    }

    private async Task<PaystackResponse<TResponse>> EnsureSuccessAsync<TResponse>(string path, HttpResponseMessage response, CancellationToken ct)
    {
        // Never log the response body — it can contain card metadata (last4/bin/bank) even
        // though we don't persist most of it, and error bodies can echo request fields back.
        var envelope = await response.Content.ReadFromJsonAsync<PaystackResponse<TResponse>>(JsonOptions, ct);

        if (!response.IsSuccessStatusCode || envelope is null || !envelope.Status)
        {
            logger.LogWarning(
                "Paystack call to {Path} failed with HTTP {StatusCode}: {Message}",
                path, (int)response.StatusCode, envelope?.Message ?? "(no message)");
            throw new PaystackApiException(envelope?.Message ?? $"Paystack request to {path} failed.", (int)response.StatusCode);
        }

        return envelope;
    }
}
