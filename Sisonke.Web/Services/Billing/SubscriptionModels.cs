namespace Sisonke.Web.Services.Billing;

public sealed record SubscriptionActionResult(bool Success, string? Message = null)
{
    public static SubscriptionActionResult Succeeded(string? message = null) => new(true, message);
    public static SubscriptionActionResult Failed(string message) => new(false, message);
}

public sealed record CardAuthorisationResult(bool Success, string? Message, string? AuthorisationUrl, string? Reference)
{
    public static CardAuthorisationResult Failed(string message) => new(false, message, null, null);
}
