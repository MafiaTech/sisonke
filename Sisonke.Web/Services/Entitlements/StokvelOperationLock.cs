using System.Collections.Concurrent;

namespace Sisonke.Web.Services.Entitlements;

/// <summary>
/// Register as a singleton — the whole point is one shared set of semaphores per process.
/// Semaphores are never evicted (bounded by the number of distinct stokvel/feature pairs ever
/// gated, which is small for this app's scale) — safe eviction would need reference counting to
/// avoid disposing a semaphore another thread is already waiting on.
/// </summary>
public sealed class StokvelOperationLock : IStokvelOperationLock
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphores = new();

    public async Task<IAsyncDisposable> AcquireAsync(Guid stokvelId, string featureCode, CancellationToken ct = default)
    {
        var key = $"{stokvelId:N}:{featureCode}";
        var semaphore = _semaphores.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(ct);
        return new Release(semaphore);
    }

    private sealed class Release(SemaphoreSlim semaphore) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            semaphore.Release();
            return ValueTask.CompletedTask;
        }
    }
}
