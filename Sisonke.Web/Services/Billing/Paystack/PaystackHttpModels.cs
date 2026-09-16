namespace Sisonke.Web.Services.Billing.Paystack;

// Field names use System.Text.Json's built-in SnakeCaseLower naming policy (configured on the
// shared JsonSerializerOptions in PaystackBillingProvider) rather than per-property
// [JsonPropertyName] attributes, matching Paystack's snake_case wire format.

internal sealed class PaystackResponse<T>
{
    public bool Status { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
}

internal sealed class PaystackCustomerRequest
{
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Phone { get; set; }
}

internal sealed class PaystackCustomerData
{
    public string CustomerCode { get; set; } = string.Empty;
}

internal sealed class PaystackPlanRequest
{
    public string Name { get; set; } = string.Empty;
    public long Amount { get; set; }
    public string Interval { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
}

internal sealed class PaystackPlanData
{
    public string PlanCode { get; set; } = string.Empty;
}

internal sealed class PaystackInitializeTransactionRequest
{
    public string Email { get; set; } = string.Empty;
    public long Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string? CallbackUrl { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string[] Channels { get; set; } = ["card"];
}

internal sealed class PaystackInitializeTransactionData
{
    public string AuthorizationUrl { get; set; } = string.Empty;
    public string AccessCode { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
}

internal sealed class PaystackVerifyTransactionData
{
    public long Amount { get; set; }
    public string? Currency { get; set; }
    public string? Domain { get; set; }
    public string? Channel { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string? GatewayResponse { get; set; }
    public PaystackAuthorizationData? Authorization { get; set; }
    public PaystackCustomerSummary? Customer { get; set; }
}

internal sealed class PaystackChargeAuthorizationRequest
{
    public string AuthorizationCode { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public long Amount { get; set; }
    public string Reference { get; set; } = string.Empty;
}

internal sealed class PaystackAuthorizationData
{
    public string? AuthorizationCode { get; set; }
    public bool Reusable { get; set; }
    public string? Last4 { get; set; }
    public string? ExpMonth { get; set; }
    public string? ExpYear { get; set; }
    public string? CardType { get; set; }
    public string? Bank { get; set; }
    public string? Channel { get; set; }
}

internal sealed class PaystackCustomerSummary
{
    public string? Email { get; set; }
    public string? CustomerCode { get; set; }
}

internal sealed class PaystackRefundRequest
{
    public string Transaction { get; set; } = string.Empty;
}

internal sealed class PaystackCreateSubscriptionRequest
{
    public string Customer { get; set; } = string.Empty;
    public string Plan { get; set; } = string.Empty;
    public string? Authorization { get; set; }
    public string? StartDate { get; set; }
}

internal sealed class PaystackSubscriptionData
{
    public string SubscriptionCode { get; set; } = string.Empty;
    public string EmailToken { get; set; } = string.Empty;
    public DateTime? NextPaymentDate { get; set; }
}

internal sealed class PaystackDisableSubscriptionRequest
{
    public string Code { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}

internal sealed class PaystackManagementLinkData
{
    public string Link { get; set; } = string.Empty;
}
