namespace Sisonke.Web.Services.Billing;

/// <summary>
/// Azure env var prefix: Invoicing__. VatNumber/CompanyName are business facts, not secrets — safe
/// in appsettings.json, but keep them centrally configurable rather than hard-coded.
/// COMPLIANCE FLAG: the "Tax Invoice" label, VAT wording and layout must be reviewed by a South
/// African commercial attorney / registered tax practitioner before this is used for a real
/// invoice — see brief rule 10. Nothing here should be treated as final legal/SARS-compliant copy.
/// </summary>
public class InvoicingOptions
{
    public bool VatRegistered { get; set; }

    public decimal VatRatePercent { get; set; } = 15m;

    public string? VatNumber { get; set; }

    public string CompanyName { get; set; } = "Sisonke (PEO Capital Holdings)";

    public string? CompanyRegistrationNumber { get; set; }

    public string? CompanyAddress { get; set; }
}
