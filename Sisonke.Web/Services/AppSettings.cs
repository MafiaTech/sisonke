namespace Sisonke.Web.Services;

public class AppSettings
{
    /// <summary>
    /// Override the base URL used when building email confirmation and password reset links.
    /// Required when the app runs behind a reverse proxy or under a custom domain.
    /// Leave empty for local development — NavigationManager provides the correct URL.
    /// Azure env var: App__PublicBaseUrl
    /// Example: https://app.sisonkestokvel.co.za
    /// </summary>
    public string? PublicBaseUrl { get; set; }
}
