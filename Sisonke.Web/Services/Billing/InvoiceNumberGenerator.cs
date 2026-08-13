using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;

namespace Sisonke.Web.Services.Billing;

/// <summary>
/// Gapless per-year sequence (SIS-INV-{year}-{number:D6}) backed by InvoiceNumberCounter. Not a
/// database SEQUENCE or EF HiLo — SQLite (used in dev/tests) supports neither. Concurrency
/// safety instead comes from an in-process lock, same tradeoff as
/// Entitlements/IStokvelOperationLock: correct for this app's current single-instance
/// deployment; would need a distributed lock if ever scaled out to multiple instances.
/// </summary>
public sealed class InvoiceNumberGenerator : IInvoiceNumberGenerator
{
    private readonly SemaphoreSlim _lock = new(1, 1);

    public async Task<string> NextAsync(ApplicationDbContext context, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var year = DateTime.UtcNow.Year;

            var counter = await context.InvoiceNumberCounters.SingleOrDefaultAsync(c => c.Year == year, ct);
            if (counter is null)
            {
                counter = new InvoiceNumberCounter { Year = year, NextNumber = 1 };
                context.InvoiceNumberCounters.Add(counter);
            }

            var allocated = counter.NextNumber;
            counter.NextNumber++;

            return $"SIS-INV-{year}-{allocated:D6}";
        }
        finally
        {
            _lock.Release();
        }
    }
}
