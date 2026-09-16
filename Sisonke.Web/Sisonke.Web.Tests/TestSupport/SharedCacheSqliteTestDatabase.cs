using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;

namespace Sisonke.Web.Tests.TestSupport;

/// <summary>
/// Like SqliteTestDatabase, but for tests that genuinely race two real OS threads (Task.Run, not
/// just concurrent async continuations) against the database — e.g. testing a DB-row-based
/// distributed lock's acquisition step. A single Microsoft.Data.Sqlite.SqliteConnection instance
/// is not safe to use from multiple threads at once (unlike Phase 2's IStokvelOperationLock,
/// which never touches the DB during acquisition, this DistributedJobLock's acquisition step
/// does — see the Phase 4 report). SQLite's named shared-cache in-memory mode lets every
/// CreateContext() call open its own connection while all of them see the same data, which is
/// what real production instances look like against SQL Server's connection pool anyway.
/// </summary>
public sealed class SharedCacheSqliteTestDatabase : IDisposable
{
    private readonly string _connectionString;
    private readonly SqliteConnection _keepAliveConnection;

    public SharedCacheSqliteTestDatabase()
    {
        _connectionString = $"Data Source=file:{Guid.NewGuid():N};Mode=Memory;Cache=Shared";

        // Shared-cache in-memory SQLite databases are destroyed once their last connection
        // closes — this connection just stays open for the lifetime of the test to keep the data
        // alive between/across the per-call connections CreateContext() opens.
        _keepAliveConnection = new SqliteConnection(_connectionString);
        _keepAliveConnection.Open();

        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    public ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connectionString)
            .Options;
        return new ApplicationDbContext(options);
    }

    public void Dispose() => _keepAliveConnection.Dispose();
}
