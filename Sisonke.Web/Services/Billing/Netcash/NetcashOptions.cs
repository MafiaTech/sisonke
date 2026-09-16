namespace Sisonke.Web.Services.Billing.Netcash;

/// <summary>Netcash NIWS DebiCheck configuration. Service keys must come from secret configuration.</summary>
public sealed class NetcashOptions
{
    public bool Enabled { get; set; }
    public bool TestMode { get; set; }
    public string ServiceUrl { get; set; } = "https://ws.netcash.co.za/NIWS/NIWS_NIF.svc";
    /// <summary>
    /// Debit Order service key for the Netcash merchant account owned by the Sisonke operating
    /// business. A stokvel/member beneficiary or settlement account is never configurable here.
    /// </summary>
    public string? DebitOrderServiceKey { get; set; }
    public string? DebiCheckMandateTemplateId { get; set; }
    public int RequestTimeoutSeconds { get; set; } = 180;

    public bool IsConfigured =>
        Enabled &&
        Uri.TryCreate(ServiceUrl, UriKind.Absolute, out var uri) &&
        uri.Scheme == Uri.UriSchemeHttps &&
        !string.IsNullOrWhiteSpace(DebitOrderServiceKey) &&
        !string.IsNullOrWhiteSpace(DebiCheckMandateTemplateId) &&
        RequestTimeoutSeconds >= 180;
}
