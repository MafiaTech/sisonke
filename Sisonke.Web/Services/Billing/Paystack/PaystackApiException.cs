namespace Sisonke.Web.Services.Billing.Paystack;

/// <summary>Thrown for a non-2xx response or a 2xx response with status:false. Message is safe to log — never includes the secret key.</summary>
public sealed class PaystackApiException(string message, int? statusCode = null) : Exception(message)
{
    public int? StatusCode { get; } = statusCode;
}
