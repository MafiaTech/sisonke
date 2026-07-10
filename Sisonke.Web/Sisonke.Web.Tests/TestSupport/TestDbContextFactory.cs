using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;

namespace Sisonke.Web.Tests.TestSupport;

public sealed class TestDbContextFactory(SqliteTestDatabase db) : IDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext() => db.CreateContext();

    public Task<ApplicationDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(db.CreateContext());
}
