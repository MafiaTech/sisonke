using System.Text.Json;

namespace Sisonke.Web.Services.Billing.Paystack;

/// <summary>
/// Thin, non-throwing reader over a raw Paystack webhook JSON body. Deliberately not a strongly
/// typed DTO — webhook payload shape varies significantly by event type and Paystack does not
/// document a single schema covering all of them; every accessor here returns null rather than
/// throwing when a field is absent, so an unexpected shape degrades gracefully (logged, event
/// marked Failed for retry) instead of crashing the processing loop.
/// </summary>
public sealed class PaystackWebhookPayload
{
    private readonly JsonElement _root;

    private PaystackWebhookPayload(JsonElement root) => _root = root;

    public static PaystackWebhookPayload? TryParse(string rawBody)
    {
        try
        {
            using var document = JsonDocument.Parse(rawBody);
            return new PaystackWebhookPayload(document.RootElement.Clone());
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public string? EventType => GetString(_root, "event");

    private JsonElement Data => _root.TryGetProperty("data", out var data) ? data : default;

    /// <summary>
    /// Idempotency key fallback chain: Paystack's webhook envelope has no documented dedicated
    /// delivery id (unconfirmed during this phase's API research — verify against a real sandbox
    /// payload), so this falls back to the underlying resource id, then the transaction
    /// reference, then a hash of the raw body (unique per distinct payload at minimum).
    /// </summary>
    public string ResolveProviderEventId(string rawBody)
    {
        var dataId = GetRawValue(Data, "id");
        if (!string.IsNullOrEmpty(dataId))
        {
            return $"{EventType}:{dataId}";
        }

        var reference = GetString(Data, "reference");
        if (!string.IsNullOrEmpty(reference))
        {
            return $"{EventType}:{reference}";
        }

        var hash = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(rawBody)));
        return $"{EventType}:{hash}";
    }

    public string? CustomerCode => GetString(Data, "customer", "customer_code");
    public string? SubscriptionCode => GetString(Data, "subscription_code") ?? GetString(Data, "subscription", "subscription_code");
    public string? PlanCode => GetString(Data, "plan", "plan_code");
    public string? Reference => GetString(Data, "reference");
    public string? Status => GetString(Data, "status");
    public string? EmailToken => GetString(Data, "email_token");
    public decimal? AmountMinorUnits => GetDecimal(Data, "amount");
    public DateTime? NextPaymentDate => GetDateTime(Data, "next_payment_date");
    public DateTime? PeriodStart => GetDateTime(Data, "period_start");
    public DateTime? PeriodEnd => GetDateTime(Data, "period_end");
    public string? InvoiceCode => GetRawValue(Data, "id");
    public string? TransactionReference => GetString(Data, "transaction", "reference") ?? GetString(Data, "reference");

    private static string? GetString(JsonElement element, string property)
    {
        return element.ValueKind == JsonValueKind.Object && element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static string? GetString(JsonElement element, string parentProperty, string childProperty)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(parentProperty, out var parent))
        {
            return null;
        }

        return GetString(parent, childProperty);
    }

    private static string? GetRawValue(JsonElement element, string property)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(property, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            _ => null
        };
    }

    private static decimal? GetDecimal(JsonElement element, string property)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(property, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Number => value.GetDecimal(),
            JsonValueKind.String when decimal.TryParse(value.GetString(), out var parsed) => parsed,
            _ => null
        };
    }

    private static DateTime? GetDateTime(JsonElement element, string property)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return DateTime.TryParse(value.GetString(), out var parsed) ? parsed.ToUniversalTime() : null;
    }
}
