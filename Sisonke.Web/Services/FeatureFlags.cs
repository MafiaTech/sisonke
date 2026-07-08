namespace Sisonke.Web.Services;

public class FeatureFlags
{
    /// <summary>
    /// Shows the "pilot environment" banner when true. Default false everywhere;
    /// enable per environment via appsettings or an Azure App Service setting if ever needed.
    /// Azure env var: Features__ShowPilotBanner
    /// </summary>
    public bool ShowPilotBanner { get; set; } = false;
}
