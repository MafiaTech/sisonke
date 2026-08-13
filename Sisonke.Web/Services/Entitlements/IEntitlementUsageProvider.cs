namespace Sisonke.Web.Services.Entitlements;

/// <summary>
/// Resolves live usage for a numeric limit feature. Always reads current database state — never
/// cached — so EntitlementService.AuthorizeAsync can make a race-safe decision.
/// </summary>
public interface IEntitlementUsageProvider
{
    /// <summary>Returns 0 for a feature code this provider does not know how to count.</summary>
    Task<int> GetCurrentUsageAsync(Guid stokvelId, string featureCode, CancellationToken ct = default);
}
