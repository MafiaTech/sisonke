namespace Sisonke.Web.Services.Entitlements;

/// <summary>
/// In-process serialization for numeric-limit-gated writes (e.g. adding a member). Keyed on
/// (stokvelId, featureCode) so unrelated stokvels/features never block each other.
/// AuthorizeAsync alone reads live usage but does not serialize callers — two concurrent
/// requests can both read "29 of 30" before either writes. Wrap the check-then-write critical
/// section in AcquireAsync to make that atomic.
/// This is single-process only — fine for Sisonke's current single-instance deployment, but if
/// the app is ever scaled out to multiple instances this must move to a distributed lock
/// (e.g. SQL Server sp_getapplock) or the guarantee no longer holds across instances.
/// </summary>
public interface IStokvelOperationLock
{
    Task<IAsyncDisposable> AcquireAsync(Guid stokvelId, string featureCode, CancellationToken ct = default);
}
