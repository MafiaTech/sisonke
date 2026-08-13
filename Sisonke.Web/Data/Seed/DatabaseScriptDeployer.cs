using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Sisonke.Web.Data.Seed;

/// <summary>
/// Idempotently (re-)creates the hand-written reporting stored procedures under
/// Database/Scripts/StoredProcedures on every startup — mirrors how EF migrations already
/// auto-apply, so these scripts stop being orphaned source files nobody ever runs.
///
/// SQL-Server-only: DashboardQueryService already has an equivalent IsSqlServer guard and falls
/// back to LINQ when a stored procedure is missing/fails, so skipping this entirely on SQLite
/// (dev/tests) is safe by that existing design, not a new gap.
///
/// The scripts are embedded resources (Sisonke.Web.csproj), not loose files read from disk, so
/// this works identically after publish regardless of the app's working directory/content root.
/// Every script uses `CREATE OR ALTER PROCEDURE`, so re-running this on every startup is safe —
/// no DROP/existence-check is needed.
/// </summary>
public static class DatabaseScriptDeployer
{
    private static readonly Regex GoBatchSeparator = new(@"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static async Task EnsureStoredProceduresAsync(ApplicationDbContext context, ILogger? logger = null)
    {
        if (!string.Equals(context.Database.ProviderName, "Microsoft.EntityFrameworkCore.SqlServer", StringComparison.OrdinalIgnoreCase))
        {
            logger?.LogInformation("[DatabaseScripts] Non-SQL-Server provider — skipping stored procedure deployment.");
            return;
        }

        var assembly = Assembly.GetExecutingAssembly();
        var resourceNames = assembly.GetManifestResourceNames()
            .Where(name => name.Contains(".Database.Scripts.StoredProcedures.", StringComparison.Ordinal)
                        && name.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var resourceName in resourceNames)
        {
            await using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Embedded SQL script resource '{resourceName}' could not be opened.");
            using var reader = new StreamReader(stream);
            var script = await reader.ReadToEndAsync();

            foreach (var batch in GoBatchSeparator.Split(script))
            {
                if (string.IsNullOrWhiteSpace(batch))
                {
                    continue;
                }

                await context.Database.ExecuteSqlRawAsync(batch);
            }

            logger?.LogInformation("[DatabaseScripts] Applied {Resource}.", resourceName);
        }
    }
}
