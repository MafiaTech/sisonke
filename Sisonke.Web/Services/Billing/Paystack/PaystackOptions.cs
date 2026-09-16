namespace Sisonke.Web.Services.Billing.Paystack;

/// <summary>
/// Azure env var prefix: Paystack__. SecretKey must come from user-secrets
/// (local dev — this project already has a UserSecretsId) / Azure App Service configuration /
/// Key Vault — never appsettings.json, never committed. PublicKey is safe to expose to the
/// browser but is not currently used server-side (card entry happens on Paystack's hosted page).
/// </summary>
public class PaystackOptions
{
    /// <summary>
    /// Explicit provider switch (Paystack__Enabled). When omitted, existing credentials
    /// retain legacy enablement; an unconfigured installation leaves Paystack disabled.
    /// </summary>
    public bool? Enabled { get; set; }

    public bool IsEnabled => Enabled ??
        (!string.IsNullOrWhiteSpace(SecretKey) || !string.IsNullOrWhiteSpace(PublicKey));

    public string BaseUrl { get; set; } = "https://api.paystack.co";

    public string PublicKey { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Legacy, unused configuration property retained so an existing setting does not become a
    /// breaking configuration change. Paystack webhooks are signed with SecretKey, not a
    /// separately issued webhook secret.
    /// </summary>
    public string WebhookSecret { get; set; } = string.Empty;

    public string Currency { get; set; } = "ZAR";

    public int TrialDays { get; set; } = 60;

    /// <summary>
    /// Minor-unit (cents) amount charged to capture a reusable card authorisation and
    /// immediately refunded. Paystack's recurring-charge documentation recommends ZAR 1.00 as
    /// the minimum first card transaction used to obtain a reusable authorization. This is setup,
    /// never a subscription fee or trial credit. Default: R1.00.
    /// </summary>
    public long CardVerificationAmountMinorUnits { get; set; } = 100;
}
