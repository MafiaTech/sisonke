namespace Sisonke.Web.Services;

public class AuthSettings
{
    /// <summary>
    /// Set to true in production/staging to require email confirmation before login.
    /// Set to false for pilot — users can sign in immediately; verification email is still sent.
    /// Azure env var: Auth__RequireConfirmedAccount
    /// </summary>
    public bool RequireConfirmedAccount { get; set; } = false;
}
