using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;

namespace Sisonke.Web.Tests.TestSupport;

/// <summary>
/// Real SQLite in-memory database kept alive for the test's lifetime. Used instead of EF Core's
/// InMemory provider because InMemory does not enforce unique indexes, which the DedupeKey
/// uniqueness guarantee depends on.
/// </summary>
public sealed class SqliteTestDatabase : IDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteTestDatabase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    public ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;
        return new ApplicationDbContext(options);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
