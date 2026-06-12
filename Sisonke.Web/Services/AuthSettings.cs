namespace Sisonke.Web.Services;

public class AuthSettings
{
    /// <summary>
    /// Set to true in production/staging to require email confirmation before login.
    /// Set to false for pilot — users can sign in immediately; verification email is still sent.
    /// Azure env var: Auth__RequireConfirmedAccount
    /// </summary>
    public bool RequireConfirmedAccount { get; set; } = false;

    /// <summary>
    /// Idle session timeout in minutes. Sliding expiry — each request resets the clock.
    /// After this period of inactivity the auth cookie is rejected and the user must re-login.
    /// Azure env var: Auth__SessionTimeoutMinutes
    /// </summary>
    public int SessionTimeoutMinutes { get; set; } = 60;
}
