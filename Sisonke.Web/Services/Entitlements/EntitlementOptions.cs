using Sisonke.Web.Data;

namespace Sisonke.Web.Services.Entitlements;

/// <summary>
/// Azure env var prefix: Entitlements__. Bound once at startup and registered as a singleton,
/// matching AppSettings/EmailSettings/FeatureFlags elsewhere in Program.cs.
/// </summary>
public class EntitlementOptions
{
    /// <summary>
    /// The migration cutover date (UTC) from the subscription billing brief. Before this date,
    /// a stokvel with no subscription / Status == LegacyUnsubscribed gets legacy grace access
    /// (see LegacyEquivalentPlanCode); on or after it, they are denied with NoSubscription.
    /// Defaults to DateTime.MaxValue (grace never expires) so an unconfigured deployment fails
    /// open for existing customers rather than silently locking them out.
    /// </summary>
    public DateTime LegacyGraceCutoverUtc { get; set; } = DateTime.MaxValue;

    /// <summary>Plan code whose feature catalogue legacy stokvels are evaluated against during the grace period.</summary>
    public string LegacyEquivalentPlanCode { get; set; } = PlanCodes.Growing;

    /// <summary>How long an EntitlementSnapshot may be served from cache before being recomputed.</summary>
    public int CacheTtlMinutes { get; set; } = 5;

    /// <summary>How often EntitlementUsageRollupService persists SubscriptionUsage rollups.</summary>
    public int UsageRollupIntervalMinutes { get; set; } = 60;

    /// <summary>Minimum days between LegacyMigrationReminderJob reminders to the same organisation.</summary>
    public int LegacyMigrationReminderIntervalDays { get; set; } = 7;
}
