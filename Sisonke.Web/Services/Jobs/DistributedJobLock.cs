using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;

namespace Sisonke.Web.Services.Jobs;

public sealed class DistributedJobLock(IDbContextFactory<ApplicationDbContext> dbFactory) : IDistributedJobLock
{
    public async Task<IAsyncDisposable?> TryAcquireAsync(string jobName, TimeSpan leaseDuration, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var exists = await context.JobExecutionLocks.AnyAsync(l => l.JobName == jobName, ct);
        if (!exists)
        {
            context.JobExecutionLocks.Add(new JobExecutionLock { JobName = jobName });
            try
            {
                await context.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                // Another instance inserted the row first — fine, proceed to the atomic acquire below.
            }
        }

        var token = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var expiresAt = now.Add(leaseDuration);

        var rowsAffected = await context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE JobExecutionLocks SET LockToken = {token}, LockedAt = {now}, LockExpiresAt = {expiresAt} WHERE JobName = {jobName} AND (LockToken IS NULL OR LockExpiresAt < {now})",
            ct);

        if (rowsAffected == 0)
        {
            return null;
        }

        return new LockHandle(dbFactory, jobName, token);
    }

    private sealed class LockHandle(IDbContextFactory<ApplicationDbContext> dbFactory, string jobName, Guid token) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            await using var context = await dbFactory.CreateDbContextAsync();

            // Only clear the lock if we still hold it — if our lease already expired and someone
            // else acquired it, releasing here would steal their lock out from under them.
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE JobExecutionLocks SET LockToken = NULL, LockExpiresAt = NULL WHERE JobName = {jobName} AND LockToken = {token}");
        }
    }
}
