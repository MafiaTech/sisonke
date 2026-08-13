using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Seed;
using Sisonke.Web.Services.Entitlements;
using Sisonke.Web.Tests.TestSupport;

namespace Sisonke.Web.Tests.Billing;

/// <summary>Bundles a SqliteTestDatabase-backed EntitlementService with its real (non-faked) dependencies.</summary>
public sealed class EntitlementTestHarness : IDisposable
{
    private readonly IDisposable _db;

    public TestDbContextFactory DbFactory { get; }
    public EntitlementOptions Options { get; }
    public EntitlementUsageProvider UsageProvider { get; }
    public IStokvelOperationLock OperationLock { get; } = new StokvelOperationLock();
    public EntitlementService Sut { get; }

    public EntitlementTestHarness(EntitlementOptions? options = null)
    {
        var db = new SqliteTestDatabase();
        _db = db;
        DbFactory = new TestDbContextFactory(db);
        Options = options ?? new EntitlementOptions();
        UsageProvider = new EntitlementUsageProvider(DbFactory);
        var cache = new MemoryCache(new MemoryCacheOptions());
        Sut = new EntitlementService(DbFactory, UsageProvider, cache, Options, NullLogger<EntitlementService>.Instance);
    }

    /// <summary>
    /// For tests that race two real OS threads (Task.Run) against the database — see
    /// SharedCacheSqliteTestDatabase's doc comment for why a plain SqliteTestDatabase can't be
    /// used safely for that.
    /// </summary>
    public EntitlementTestHarness(SharedCacheSqliteTestDatabase db, EntitlementOptions? options = null)
    {
        _db = db;
        DbFactory = new TestDbContextFactory(db);
        Options = options ?? new EntitlementOptions();
        UsageProvider = new EntitlementUsageProvider(DbFactory);
        var cache = new MemoryCache(new MemoryCacheOptions());
        Sut = new EntitlementService(DbFactory, UsageProvider, cache, Options, NullLogger<EntitlementService>.Instance);
    }

    public async Task SeedCatalogueAsync()
    {
        await using var context = DbFactory.CreateDbContext();
        await BillingCatalogueSeed.EnsureBillingCatalogueAsync(context);
    }

    public ApplicationDbContext CreateContext() => DbFactory.CreateDbContext();

    public void Dispose() => _db.Dispose();
}
