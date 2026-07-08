namespace Sisonke.Web.Services;

public class FeatureFlags
{
    /// <summary>
    /// Shows the "pilot environment" banner when true. Default false everywhere;
    /// enable per environment via appsettings or an Azure App Service setting if ever needed.
    /// Azure env var: Features__ShowPilotBanner
    /// </summary>
    public bool ShowPilotBanner { get; set; } = false;

    /// <summary>
    /// Allows the emergency local Identity email/password login at /Account/LocalLogin when true.
    /// Default false so Entra External ID remains the normal login flow.
    /// Azure env var: Features__UseLocalLoginFallback
    /// </summary>
    public bool UseLocalLoginFallback { get; set; } = false;
}
