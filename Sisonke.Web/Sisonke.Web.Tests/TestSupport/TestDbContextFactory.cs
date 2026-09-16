using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;

namespace Sisonke.Web.Tests.TestSupport;

public sealed class TestDbContextFactory : IDbContextFactory<ApplicationDbContext>
{
    private readonly Func<ApplicationDbContext> _createContext;

    public TestDbContextFactory(SqliteTestDatabase db) : this(db.CreateContext) { }

    public TestDbContextFactory(SharedCacheSqliteTestDatabase db) : this(db.CreateContext) { }

    private TestDbContextFactory(Func<ApplicationDbContext> createContext) => _createContext = createContext;

    public ApplicationDbContext CreateDbContext() => _createContext();

    public Task<ApplicationDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_createContext());
}
