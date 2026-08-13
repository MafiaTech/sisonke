namespace Sisonke.Web.Services.Billing.Netcash;

/// <summary>
/// Phase 3 configuration boundary only. Real credential names and endpoints will be added after
/// Netcash merchant onboarding documentation is approved; no secrets belong in source control.
/// </summary>
public sealed class NetcashOptions
{
    public bool Enabled { get; set; }
    public bool TestMode { get; set; }
}
