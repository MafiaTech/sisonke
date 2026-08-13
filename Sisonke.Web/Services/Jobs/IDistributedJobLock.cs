namespace Sisonke.Web.Services.Jobs;

/// <summary>
/// Portable mutual exclusion for scheduled jobs across multiple app instances — a DB-row lock
/// (works identically on SQL Server and SQLite), not sp_getapplock or a Hangfire-specific
/// attribute (this app has no job scheduler; see Phase 4 report for why). Only prevents two
/// instances literally overlapping on the same job at the same moment — the "safe to re-run",
/// "safe after a missed window" and "once per day" properties come from each job's own
/// idempotent, date-driven logic (markers, DunningStartedAt day-math, etc.), not from this lock.
/// </summary>
public interface IDistributedJobLock
{
    /// <summary>Returns null if another instance currently holds the lock. Dispose the handle to release early; otherwise it self-expires after leaseDuration.</summary>
    Task<IAsyncDisposable?> TryAcquireAsync(string jobName, TimeSpan leaseDuration, CancellationToken ct = default);
}
