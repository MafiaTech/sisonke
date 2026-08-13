using System.Reflection;
using Sisonke.Web.Data.Seed;
using Sisonke.Web.Tests.TestSupport;

namespace Sisonke.Web.Tests.Data;

public class DatabaseScriptDeployerTests
{
    [Fact]
    public async Task EnsureStoredProceduresAsync_OnSqlite_IsNoOpAndDoesNotThrow()
    {
        // DashboardQueryService already tolerates missing stored procedures on non-SQL-Server
        // providers by falling back to LINQ — this asserts the deployer itself never attempts to
        // run T-SQL (CREATE OR ALTER PROCEDURE, etc.) against SQLite, which would fail outright.
        using var db = new SqliteTestDatabase();
        var dbFactory = new TestDbContextFactory(db);
        await using var context = dbFactory.CreateDbContext();

        await DatabaseScriptDeployer.EnsureStoredProceduresAsync(context);
    }

    [Fact]
    public void StoredProcedureScripts_AreEmbeddedInTheAssembly()
    {
        // Locks in the embedded-resource wiring in Sisonke.Web.csproj — if a future script is
        // added under Database/Scripts/StoredProcedures without the glob picking it up, or the
        // resource-naming convention changes, this fails loudly instead of silently deploying
        // nothing (the original bug being fixed here).
        var assembly = Assembly.Load("Sisonke.Web");
        var resourceNames = assembly.GetManifestResourceNames()
            .Where(name => name.Contains(".Database.Scripts.StoredProcedures.", StringComparison.Ordinal))
            .ToList();

        Assert.Contains(resourceNames, name => name.EndsWith("sp_GetStokvelDashboardSummary.sql", StringComparison.Ordinal));
        Assert.Contains(resourceNames, name => name.EndsWith("sp_GetMemberContributionStatus.sql", StringComparison.Ordinal));
        Assert.Contains(resourceNames, name => name.EndsWith("sp_GetOutstandingContributions.sql", StringComparison.Ordinal));
    }
}
