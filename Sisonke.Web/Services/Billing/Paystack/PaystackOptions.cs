namespace Sisonke.Web.Services.Billing.Paystack;

/// <summary>
/// Azure env var prefix: Paystack__. SecretKey and WebhookSecret must come from user-secrets
/// (local dev — this project already has a UserSecretsId) / Azure App Service configuration /
/// Key Vault — never appsettings.json, never committed. PublicKey is safe to expose to the
/// browser but is not currently used server-side (card entry happens on Paystack's hosted page).
/// </summary>
public class PaystackOptions
{
    public string BaseUrl { get; set; } = "https://api.paystack.co";

    public string PublicKey { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;

    public string WebhookSecret { get; set; } = string.Empty;

    public string Currency { get; set; } = "ZAR";

    public int TrialDays { get; set; } = 60;

    /// <summary>
    /// Minor-unit (cents) amount charged to capture a reusable card authorisation and
    /// immediately refunded — Paystack has no documented zero-amount verification hold, so a
    /// small real charge is the standard mechanism. Confirmed with product 2026-08-03: charge
    /// and refund immediately, do not apply as trial credit. Default: R1.00.
    /// </summary>
    public long CardVerificationAmountMinorUnits { get; set; } = 100;
}
