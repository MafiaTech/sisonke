namespace Sisonke.Web.Services.Entitlements;

/// <summary>
/// The single authoritative entitlement engine. Every gated operation — application service,
/// API endpoint, UI helper, background job — must go through this interface. A plan-name or
/// tier string comparison anywhere outside the Entitlements folder (and its tests) is a defect.
/// </summary>
public interface IEntitlementService
{
    /// <summary>Convenience existence/allowed check. Shares the same decision path as AuthorizeAsync.</summary>
    Task<bool> HasFeatureAsync(Guid stokvelId, string featureCode, CancellationToken ct = default);

    /// <summary>The plan's configured limit for a numeric feature. Null means unlimited or not applicable.</summary>
    Task<int?> GetLimitAsync(Guid stokvelId, string featureCode, CancellationToken ct = default);

    /// <summary>
    /// The authoritative gate. For numeric features, requestedUsage is the increment this
    /// operation would add (default 1 = "one more"), and current usage is always read live —
    /// never from the cached snapshot — so a race cannot exceed the cap. IMPORTANT: this method
    /// alone does not serialize concurrent callers; a numeric-limit-gated write must wrap its
    /// AuthorizeAsync call and the subsequent write in IStokvelOperationLock.AcquireAsync(stokvelId,
    /// featureCode) or two simultaneous requests can both be authorized before either persists.
    /// </summary>
    Task<EntitlementDecision> AuthorizeAsync(Guid stokvelId, string featureCode, int requestedUsage = 1, CancellationToken ct = default);

    /// <summary>Cached (EntitlementOptions.CacheTtlMinutes) full feature snapshot for UI/API consumers.</summary>
    Task<EntitlementSnapshot> GetSnapshotAsync(Guid stokvelId, CancellationToken ct = default);

    /// <summary>Call after plan change, status change, payment success/failure, or catalogue re-seed.</summary>
    Task InvalidateAsync(Guid stokvelId, CancellationToken ct = default);
}
