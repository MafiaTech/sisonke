using System.Data;
using System.Globalization;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

var arguments = CliArguments.Parse(args);

if (arguments.ShowHelp)
{
    PrintHelp();
    return 0;
}

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .AddUserSecrets<Program>(optional: true)
    .AddEnvironmentVariables()
    .Build();

var sourceConnectionString = NormalizeSqliteConnectionString(
    arguments.Source
    ?? configuration.GetConnectionString("SourceSqliteConnection")
    ?? configuration["SourceSqliteConnection"]);

var targetConnectionString =
    arguments.Target
    ?? configuration.GetConnectionString("TargetSqlServerConnection")
    ?? configuration["TargetSqlServerConnection"];

if (string.IsNullOrWhiteSpace(sourceConnectionString))
{
    Console.Error.WriteLine("Missing source SQLite connection. Provide --source or ConnectionStrings:SourceSqliteConnection.");
    return 2;
}

if (string.IsNullOrWhiteSpace(targetConnectionString))
{
    Console.Error.WriteLine("Missing target SQL Server connection. Provide --target or ConnectionStrings:TargetSqlServerConnection.");
    return 2;
}

var runner = new MigrationRunner(sourceConnectionString, targetConnectionString);

try
{
    return arguments.Command switch
    {
        "dry-run" => await runner.DryRunAsync(),
        "execute" => await runner.ExecuteAsync(),
        "verify" => await runner.VerifyAsync(),
        _ => UnknownCommand(arguments.Command)
    };
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Migration failed: {ex.GetType().Name}: {ex.Message}");
    return 1;
}

static int UnknownCommand(string command)
{
    Console.Error.WriteLine($"Unknown command '{command}'.");
    PrintHelp();
    return 2;
}

static void PrintHelp()
{
    Console.WriteLine("""
Sisonke SQLite-to-Azure-SQL migration utility

Usage:
  dotnet run --project Sisonke.MigrationTool -- dry-run --source "<sqlite-file-or-connection-string>"
  dotnet run --project Sisonke.MigrationTool -- execute --source "<sqlite-file-or-connection-string>"
  dotnet run --project Sisonke.MigrationTool -- verify --source "<sqlite-file-or-connection-string>"

Options:
  --source   SQLite file path or connection string. Falls back to ConnectionStrings:SourceSqliteConnection.
  --target   SQL Server connection string. Falls back to ConnectionStrings:TargetSqlServerConnection.
""");
}

static string? NormalizeSqliteConnectionString(string? value)
{
    if (string.IsNullOrWhiteSpace(value))
    {
        return null;
    }

    if (value.Contains('='))
    {
        return value;
    }

    return new SqliteConnectionStringBuilder { DataSource = value }.ConnectionString;
}

internal sealed class MigrationRunner(string sourceConnectionString, string targetConnectionString)
{
    private readonly MigrationMappings mappings = new();

    private static readonly string[] TableOrder =
    [
        "AspNetRoles",
        "AspNetUsers",
        "AspNetRoleClaims",
        "AspNetUserClaims",
        "AspNetUserLogins",
        "AspNetUserRoles",
        "AspNetUserTokens",
        "Tenants",
        "SubscriptionPlans",
        "Stokvels",
        "TenantSubscriptions",
        "Members",
        "MemberWarnings",
        "NextOfKinRecords",
        "Beneficiaries",
        "MemberDependents",
        "FineTypes",
        "MemberFines",
        "ContributionRules",
        "ContributionCycles",
        "MemberContributions",
        "Payments",
        "ContributionPaymentAudits",
        "Meetings",
        "MeetingAgendaItems",
        "MeetingAttendances",
        "MeetingApologies",
        "MeetingMinutes",
        "MeetingVotes",
        "MeetingVoteResponses",
        "QuestionnaireSections",
        "QuestionnaireQuestions",
        "QuestionnaireOptions",
        "StokvelQuestionnaireAnswers",
        "ConstitutionDocuments",
        "ConstitutionWizardAnswers",
        "VoteMotions",
        "VoteOptions",
        "MemberVotes",
        "FuneralClaims",
        "FuneralClaimDocuments",
        "ClaimPayoutAudits",
        "StokvelOperatingRules"
    ];

    public async Task<int> DryRunAsync()
    {
        Console.WriteLine("Starting dry run.");
        await using var source = new SqliteConnection(sourceConnectionString);
        await using var target = new SqlConnection(targetConnectionString);
        await OpenConnectionsAsync(source, target);

        PrintTargetInfo(target);
        var plan = await BuildPlanAsync(source, target);

        Console.WriteLine();
        Console.WriteLine("Table migration plan:");
        foreach (var table in plan.Tables)
        {
            Console.WriteLine($"{table.Name,-32} source={table.SourceCount,5} target={table.TargetCount,5} existing={table.ExistingByKey,5} insert={table.InsertCount,5}");
        }

        if (plan.IgnoredSourceTables.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("Ignored source tables: " + string.Join(", ", plan.IgnoredSourceTables));
        }

        Console.WriteLine();
        Console.WriteLine($"Dry run complete. Planned inserts: {plan.Tables.Sum(t => t.InsertCount)}");
        return 0;
    }

    public async Task<int> ExecuteAsync()
    {
        Console.WriteLine("Starting execute mode.");
        await using var source = new SqliteConnection(sourceConnectionString);
        await using var target = new SqlConnection(targetConnectionString);
        await OpenConnectionsAsync(source, target);

        PrintTargetInfo(target);
        var plan = await BuildPlanAsync(source, target);

        foreach (var table in plan.Tables)
        {
            if (table.InsertCount == 0)
            {
                Console.WriteLine($"{table.Name}: no inserts needed.");
                continue;
            }

            var inserted = await CopyTableAsync(source, target, table);
            Console.WriteLine($"{table.Name}: inserted {inserted}, skipped existing {table.ExistingByKey}.");
        }

        Console.WriteLine("Execute mode complete.");
        return await VerifyAsync(source, target);
    }

    public async Task<int> VerifyAsync()
    {
        await using var source = new SqliteConnection(sourceConnectionString);
        await using var target = new SqlConnection(targetConnectionString);
        await OpenConnectionsAsync(source, target);
        return await VerifyAsync(source, target);
    }

    private static async Task OpenConnectionsAsync(SqliteConnection source, SqlConnection target)
    {
        await source.OpenAsync();
        await target.OpenAsync();
        Console.WriteLine("Opened SQLite source and Azure SQL target connections.");
    }

    private static void PrintTargetInfo(SqlConnection target)
    {
        var builder = new SqlConnectionStringBuilder(target.ConnectionString);
        Console.WriteLine($"Target: server={builder.DataSource}; database={builder.InitialCatalog}");
    }

    private async Task<MigrationPlan> BuildPlanAsync(SqliteConnection source, SqlConnection target)
    {
        mappings.Clear();
        var sourceTables = await GetSourceTablesAsync(source);
        var targetTables = await GetTargetTablesAsync(target);
        var plannedTables = new List<TablePlan>();

        foreach (var tableName in TableOrder)
        {
            if (!sourceTables.Contains(tableName) || !targetTables.Contains(tableName))
            {
                continue;
            }

            var sourceColumns = await GetSourceColumnsAsync(source, tableName);
            var targetColumns = await GetTargetColumnsAsync(target, tableName);
            var commonColumns = targetColumns
                .Where(c => !c.IsComputed && sourceColumns.ContainsKey(c.Name))
                .ToArray();

            var sourcePrimaryKey = sourceColumns.Values
                .Where(c => c.PrimaryKeyOrdinal > 0)
                .OrderBy(c => c.PrimaryKeyOrdinal)
                .Select(c => c.Name)
                .ToArray();
            var targetPrimaryKey = await GetTargetPrimaryKeyAsync(target, tableName);
            var primaryKey = sourcePrimaryKey.Where(targetPrimaryKey.Contains).ToArray();
            var uniqueIndexes = await GetTargetUniqueIndexesAsync(target, tableName, commonColumns.Select(c => c.Name).ToHashSet(StringComparer.OrdinalIgnoreCase));

            if (primaryKey.Length == 0)
            {
                Console.WriteLine($"{tableName}: skipped because no shared primary key was found.");
                continue;
            }

            var sourceCount = await CountSqliteAsync(source, tableName);
            var targetCount = await CountSqlServerAsync(target, tableName);
            var existingRows = sourceCount == 0
                ? 0
                : await CountExistingOrMappedAsync(source, target, tableName, commonColumns, primaryKey, uniqueIndexes);

            plannedTables.Add(new TablePlan(
                tableName,
                sourceCount,
                targetCount,
                existingRows,
                Math.Max(0, sourceCount - existingRows),
                commonColumns,
                primaryKey,
                uniqueIndexes));
        }

        var ignored = sourceTables
            .Except(TableOrder, StringComparer.OrdinalIgnoreCase)
            .Where(t => !t.StartsWith("sqlite_", StringComparison.OrdinalIgnoreCase))
            .Where(t => !string.Equals(t, "__EFMigrationsHistory", StringComparison.OrdinalIgnoreCase))
            .OrderBy(t => t)
            .ToArray();

        return new MigrationPlan(plannedTables, ignored);
    }

    private async Task<int> CopyTableAsync(SqliteConnection source, SqlConnection target, TablePlan table)
    {
        var inserted = 0;
        var skipped = 0;
        var columns = table.Columns.Select(c => c.Name).ToArray();
        var hasIdentityColumn = table.Columns.Any(c => c.IsIdentity);
        await using var transaction = (SqlTransaction)await target.BeginTransactionAsync();

        try
        {
            if (hasIdentityColumn)
            {
                await ExecuteSqlServerNonQueryAsync(target, transaction, $"SET IDENTITY_INSERT {QuoteSqlServer(table.Name)} ON");
            }

            await using var sourceCommand = source.CreateCommand();
            sourceCommand.CommandText = $"SELECT {string.Join(", ", columns.Select(QuoteSqlite))} FROM {QuoteSqlite(table.Name)}";

            await using var reader = await sourceCommand.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                for (var i = 0; i < columns.Length; i++)
                {
                    var value = await reader.IsDBNullAsync(i) ? null : reader.GetValue(i);
                    row[columns[i]] = value;
                }

                mappings.Apply(row);

                if (await ExistsInTargetAsync(target, transaction, table, row))
                {
                    RegisterSameIdMapping(table, row);
                    skipped++;
                    continue;
                }

                var uniqueMatch = await FindUniqueMatchAsync(target, transaction, table, row);
                if (uniqueMatch is not null)
                {
                    RegisterMappedDuplicate(table, row, uniqueMatch, "execute");
                    skipped++;
                    continue;
                }

                await InsertRowAsync(target, transaction, table, row);
                RegisterSameIdMapping(table, row);
                inserted++;
            }

            if (hasIdentityColumn)
            {
                await ExecuteSqlServerNonQueryAsync(target, transaction, $"SET IDENTITY_INSERT {QuoteSqlServer(table.Name)} OFF");
            }

            await transaction.CommitAsync();
            if (skipped > 0)
            {
                Console.WriteLine($"{table.Name}: skipped {skipped} existing or naturally duplicated rows.");
            }

            return inserted;
        }
        catch
        {
            if (hasIdentityColumn)
            {
                try
                {
                    await ExecuteSqlServerNonQueryAsync(target, transaction, $"SET IDENTITY_INSERT {QuoteSqlServer(table.Name)} OFF");
                }
                catch
                {
                    // Best effort; the transaction rollback below will close the table copy attempt.
                }
            }

            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task<int> CountExistingOrMappedAsync(
        SqliteConnection source,
        SqlConnection target,
        string tableName,
        ColumnInfo[] columns,
        string[] primaryKey,
        UniqueIndexInfo[] uniqueIndexes)
    {
        var skipped = 0;
        var table = new TablePlan(tableName, 0, 0, 0, 0, columns, primaryKey, uniqueIndexes);
        await using var sourceCommand = source.CreateCommand();
        sourceCommand.CommandText = $"SELECT {string.Join(", ", columns.Select(c => QuoteSqlite(c.Name)))} FROM {QuoteSqlite(tableName)}";
        await using var reader = await sourceCommand.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < columns.Length; i++)
            {
                row[columns[i].Name] = await reader.IsDBNullAsync(i) ? null : reader.GetValue(i);
            }

            mappings.Apply(row);

            if (await ExistsInTargetAsync(target, null, table, row))
            {
                RegisterSameIdMapping(table, row);
                skipped++;
                continue;
            }

            var uniqueMatch = await FindUniqueMatchAsync(target, null, table, row);
            if (uniqueMatch is not null)
            {
                RegisterMappedDuplicate(table, row, uniqueMatch, "dry-run");
                skipped++;
            }
        }

        return skipped;
    }

    private static async Task<bool> ExistsInTargetAsync(SqlConnection target, SqlTransaction? transaction, TablePlan table, IReadOnlyDictionary<string, object?> row)
    {
        await using var command = target.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"SELECT COUNT(1) FROM {QuoteSqlServer(table.Name)} WHERE {BuildWhereClause(table.PrimaryKey)}";
        AddKeyParameters(command, table, row);
        var count = Convert.ToInt32(await command.ExecuteScalarAsync(), CultureInfo.InvariantCulture);
        return count > 0;
    }

    private static async Task<UniqueMatch?> FindUniqueMatchAsync(SqlConnection target, SqlTransaction? transaction, TablePlan table, IReadOnlyDictionary<string, object?> row)
    {
        foreach (var index in table.UniqueIndexes)
        {
            if (index.Columns.Any(c => !row.TryGetValue(c, out var value) || value is null or DBNull))
            {
                continue;
            }

            await using var command = target.CreateCommand();
            command.Transaction = transaction;
            command.CommandText =
                $"SELECT TOP (1) {string.Join(", ", table.PrimaryKey.Select(QuoteSqlServerColumn))} " +
                $"FROM {QuoteSqlServer(table.Name)} WHERE {BuildWhereClause(index.Columns, "unique")}";

            for (var i = 0; i < index.Columns.Length; i++)
            {
                var columnName = index.Columns[i];
                var column = table.Columns.First(c => string.Equals(c.Name, columnName, StringComparison.OrdinalIgnoreCase));
                command.Parameters.AddWithValue($"@unique_{columnName}", ConvertValue(row[columnName], column) ?? DBNull.Value);
            }

            await using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                continue;
            }

            var keyValues = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < table.PrimaryKey.Length; i++)
            {
                keyValues[table.PrimaryKey[i]] = await reader.IsDBNullAsync(i) ? null : reader.GetValue(i);
            }

            return new UniqueMatch(index, keyValues);
        }

        return null;
    }

    private static async Task InsertRowAsync(SqlConnection target, SqlTransaction transaction, TablePlan table, IReadOnlyDictionary<string, object?> row)
    {
        await using var command = target.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            $"INSERT INTO {QuoteSqlServer(table.Name)} ({string.Join(", ", table.Columns.Select(c => QuoteSqlServerColumn(c.Name)))}) " +
            $"VALUES ({string.Join(", ", table.Columns.Select((_, i) => $"@p{i}"))})";

        for (var i = 0; i < table.Columns.Length; i++)
        {
            var column = table.Columns[i];
            var parameter = command.Parameters.AddWithValue($"@p{i}", ConvertValue(row[column.Name], column) ?? DBNull.Value);
            parameter.IsNullable = column.IsNullable;
        }

        await command.ExecuteNonQueryAsync();
    }

    private static void AddKeyParameters(SqlCommand command, TablePlan table, IReadOnlyDictionary<string, object?> row)
    {
        foreach (var key in table.PrimaryKey)
        {
            var column = table.Columns.First(c => string.Equals(c.Name, key, StringComparison.OrdinalIgnoreCase));
            command.Parameters.AddWithValue($"@key_{key}", ConvertValue(row[key], column) ?? DBNull.Value);
        }
    }

    private static string BuildWhereClause(IEnumerable<string> primaryKey) =>
        string.Join(" AND ", primaryKey.Select(k => $"{QuoteSqlServerColumn(k)} = @key_{k}"));

    private static string BuildWhereClause(IEnumerable<string> columns, string prefix) =>
        string.Join(" AND ", columns.Select(c => $"{QuoteSqlServerColumn(c)} = @{prefix}_{c}"));

    private void RegisterSameIdMapping(TablePlan table, IReadOnlyDictionary<string, object?> row)
    {
        if (table.PrimaryKey.Length != 1 || !row.TryGetValue(table.PrimaryKey[0], out var id) || id is null or DBNull)
        {
            return;
        }

        RegisterIdMapping(table.Name, id, id);
    }

    private void RegisterMappedDuplicate(TablePlan table, IReadOnlyDictionary<string, object?> sourceRow, UniqueMatch match, string mode)
    {
        if (table.PrimaryKey.Length != 1 ||
            !sourceRow.TryGetValue(table.PrimaryKey[0], out var sourceId) ||
            sourceId is null or DBNull ||
            !match.PrimaryKeyValues.TryGetValue(table.PrimaryKey[0], out var targetId) ||
            targetId is null or DBNull)
        {
            return;
        }

        RegisterIdMapping(table.Name, sourceId, targetId);

        if (string.Equals(table.Name, "Tenants", StringComparison.OrdinalIgnoreCase) &&
            sourceRow.TryGetValue("Slug", out var slug))
        {
            Console.WriteLine($"Tenant slug already exists: {slug}. Mapping source tenant {sourceId} to target tenant {targetId}.");
            return;
        }

        if (string.Equals(table.Name, "AspNetUsers", StringComparison.OrdinalIgnoreCase) &&
            sourceRow.TryGetValue("Email", out var email))
        {
            Console.WriteLine($"User already exists by {string.Join("/", match.Index.Columns)} ({MaskEmail(Convert.ToString(email, CultureInfo.InvariantCulture))}). Mapping source user {sourceId} to target user {targetId}.");
            return;
        }

        Console.WriteLine($"{table.Name}: {mode} found existing row by unique index {match.Index.Name}. Mapping source id {sourceId} to target id {targetId}.");
    }

    private void RegisterIdMapping(string tableName, object sourceId, object targetId)
    {
        var source = Convert.ToString(sourceId, CultureInfo.InvariantCulture);
        var target = Convert.ToString(targetId, CultureInfo.InvariantCulture);
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target))
        {
            return;
        }

        if (string.Equals(tableName, "Tenants", StringComparison.OrdinalIgnoreCase))
        {
            mappings.TenantIds[source] = target;
        }
        else if (string.Equals(tableName, "AspNetUsers", StringComparison.OrdinalIgnoreCase))
        {
            mappings.UserIds[source] = target;
        }
        else if (string.Equals(tableName, "AspNetRoles", StringComparison.OrdinalIgnoreCase))
        {
            mappings.RoleIds[source] = target;
        }
        else if (string.Equals(tableName, "Stokvels", StringComparison.OrdinalIgnoreCase))
        {
            mappings.StokvelIds[source] = target;
        }
        else if (string.Equals(tableName, "Members", StringComparison.OrdinalIgnoreCase))
        {
            mappings.MemberIds[source] = target;
        }
        else if (string.Equals(tableName, "MemberDependents", StringComparison.OrdinalIgnoreCase))
        {
            mappings.DependentIds[source] = target;
        }
        else if (string.Equals(tableName, "Meetings", StringComparison.OrdinalIgnoreCase))
        {
            mappings.MeetingIds[source] = target;
        }
        else if (string.Equals(tableName, "MeetingVotes", StringComparison.OrdinalIgnoreCase))
        {
            mappings.MeetingVoteIds[source] = target;
        }
        else if (string.Equals(tableName, "VoteMotions", StringComparison.OrdinalIgnoreCase))
        {
            mappings.VoteMotionIds[source] = target;
        }
        else if (string.Equals(tableName, "VoteOptions", StringComparison.OrdinalIgnoreCase))
        {
            mappings.VoteOptionIds[source] = target;
        }
        else if (string.Equals(tableName, "MemberContributions", StringComparison.OrdinalIgnoreCase))
        {
            mappings.MemberContributionIds[source] = target;
        }
        else if (string.Equals(tableName, "Payments", StringComparison.OrdinalIgnoreCase))
        {
            mappings.PaymentIds[source] = target;
        }
        else if (string.Equals(tableName, "FuneralClaims", StringComparison.OrdinalIgnoreCase))
        {
            mappings.FuneralClaimIds[source] = target;
        }
        else if (string.Equals(tableName, "QuestionnaireQuestions", StringComparison.OrdinalIgnoreCase))
        {
            mappings.QuestionnaireQuestionIds[source] = target;
        }
    }

    private static object? ConvertValue(object? value, ColumnInfo column)
    {
        if (value is null or DBNull)
        {
            return DBNull.Value;
        }

        var type = column.StoreType.ToLowerInvariant();
        var text = Convert.ToString(value, CultureInfo.InvariantCulture);

        if (type == "uniqueidentifier")
        {
            return value is Guid guid ? guid : Guid.Parse(text!);
        }

        if (type == "bit")
        {
            return value is bool boolValue ? boolValue : Convert.ToInt64(value, CultureInfo.InvariantCulture) != 0;
        }

        if (type is "tinyint" or "smallint" or "int" or "bigint")
        {
            return Convert.ChangeType(value, Type.GetTypeCode(type switch
            {
                "tinyint" => typeof(byte),
                "smallint" => typeof(short),
                "int" => typeof(int),
                _ => typeof(long)
            }), CultureInfo.InvariantCulture);
        }

        if (type is "decimal" or "numeric" or "money" or "smallmoney")
        {
            return Convert.ToDecimal(value, CultureInfo.InvariantCulture);
        }

        if (type is "float")
        {
            return Convert.ToDouble(value, CultureInfo.InvariantCulture);
        }

        if (type is "real")
        {
            return Convert.ToSingle(value, CultureInfo.InvariantCulture);
        }

        if (type.Contains("datetimeoffset", StringComparison.OrdinalIgnoreCase))
        {
            return value is DateTimeOffset offset ? offset : DateTimeOffset.Parse(text!, CultureInfo.InvariantCulture);
        }

        if (type.Contains("date", StringComparison.OrdinalIgnoreCase) || type.Contains("time", StringComparison.OrdinalIgnoreCase))
        {
            return value is DateTime date ? date : DateTime.Parse(text!, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        }

        return value;
    }

    private static async Task<int> VerifyAsync(SqliteConnection source, SqlConnection target)
    {
        Console.WriteLine();
        Console.WriteLine("Verification:");

        foreach (var tableName in TableOrder)
        {
            var sourceExists = await SourceTableExistsAsync(source, tableName);
            var targetExists = await TargetTableExistsAsync(target, tableName);
            if (!sourceExists || !targetExists)
            {
                continue;
            }

            var sourceCount = await CountSqliteAsync(source, tableName);
            var targetCount = await CountSqlServerAsync(target, tableName);
            Console.WriteLine($"{tableName,-32} source={sourceCount,5} target={targetCount,5}");
        }

        Console.WriteLine();
        Console.WriteLine("Key checks:");
        Console.WriteLine($"Users: {await CountSqlServerAsync(target, "AspNetUsers")}");
        Console.WriteLine($"Stokvels: {await CountSqlServerAsync(target, "Stokvels")}");
        Console.WriteLine($"Members: {await CountSqlServerAsync(target, "Members")}");
        Console.WriteLine($"Members linked to users: {await CountSqlServerWhereAsync(target, "Members", "ApplicationUserId IS NOT NULL")}");
        Console.WriteLine($"Stokvel/member tenant links: {await CountSqlServerJoinAsync(target)}");

        var sampleUsers = await GetSampleUsersAsync(target);
        if (sampleUsers.Count > 0)
        {
            Console.WriteLine("Sample users: " + string.Join(", ", sampleUsers.Select(u => $"{u.Id}:{MaskEmail(u.Email)}")));
        }

        Console.WriteLine("Verification complete.");
        return 0;
    }

    private static async Task<IReadOnlySet<string>> GetSourceTablesAsync(SqliteConnection source)
    {
        var tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var command = source.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table'";
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tables.Add(reader.GetString(0));
        }

        return tables;
    }

    private static async Task<IReadOnlySet<string>> GetTargetTablesAsync(SqlConnection target)
    {
        var tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var command = target.CreateCommand();
        command.CommandText = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_TYPE = 'BASE TABLE'";
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tables.Add(reader.GetString(0));
        }

        return tables;
    }

    private static async Task<Dictionary<string, SourceColumnInfo>> GetSourceColumnsAsync(SqliteConnection source, string tableName)
    {
        var columns = new Dictionary<string, SourceColumnInfo>(StringComparer.OrdinalIgnoreCase);
        await using var command = source.CreateCommand();
        command.CommandText = $"PRAGMA table_info({QuoteSqlite(tableName)})";
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var name = reader.GetString(1);
            var primaryKeyOrdinal = reader.GetInt32(5);
            columns[name] = new SourceColumnInfo(name, primaryKeyOrdinal);
        }

        return columns;
    }

    private static async Task<ColumnInfo[]> GetTargetColumnsAsync(SqlConnection target, string tableName)
    {
        var columns = new List<ColumnInfo>();
        await using var command = target.CreateCommand();
        command.CommandText = """
SELECT c.name, ty.name, c.is_nullable, c.is_identity, c.is_computed
FROM sys.columns c
INNER JOIN sys.types ty ON c.user_type_id = ty.user_type_id
WHERE c.object_id = OBJECT_ID(@table)
ORDER BY c.column_id
""";
        command.Parameters.AddWithValue("@table", $"dbo.{tableName}");
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            columns.Add(new ColumnInfo(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetBoolean(2),
                reader.GetBoolean(3),
                reader.GetBoolean(4)));
        }

        return columns.ToArray();
    }

    private static async Task<string[]> GetTargetPrimaryKeyAsync(SqlConnection target, string tableName)
    {
        var keys = new List<string>();
        await using var command = target.CreateCommand();
        command.CommandText = """
SELECT c.name
FROM sys.indexes i
INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
WHERE i.is_primary_key = 1 AND i.object_id = OBJECT_ID(@table)
ORDER BY ic.key_ordinal
""";
        command.Parameters.AddWithValue("@table", $"dbo.{tableName}");
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            keys.Add(reader.GetString(0));
        }

        return keys.ToArray();
    }

    private static async Task<UniqueIndexInfo[]> GetTargetUniqueIndexesAsync(SqlConnection target, string tableName, IReadOnlySet<string> availableColumns)
    {
        var indexes = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        await using var command = target.CreateCommand();
        command.CommandText = """
SELECT i.name, c.name
FROM sys.indexes i
INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
WHERE i.is_unique = 1
  AND i.is_primary_key = 0
  AND i.object_id = OBJECT_ID(@table)
  AND ic.is_included_column = 0
ORDER BY i.name, ic.key_ordinal
""";
        command.Parameters.AddWithValue("@table", $"dbo.{tableName}");
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var indexName = reader.GetString(0);
            var columnName = reader.GetString(1);
            if (!indexes.TryGetValue(indexName, out var columns))
            {
                columns = [];
                indexes[indexName] = columns;
            }

            columns.Add(columnName);
        }

        return indexes
            .Select(i => new UniqueIndexInfo(i.Key, i.Value.ToArray()))
            .Where(i => i.Columns.All(availableColumns.Contains))
            .ToArray();
    }

    private static async Task<int> CountSqliteAsync(SqliteConnection connection, string tableName)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(1) FROM {QuoteSqlite(tableName)}";
        return Convert.ToInt32(await command.ExecuteScalarAsync(), CultureInfo.InvariantCulture);
    }

    private static async Task<int> CountSqlServerAsync(SqlConnection connection, string tableName)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(1) FROM {QuoteSqlServer(tableName)}";
        return Convert.ToInt32(await command.ExecuteScalarAsync(), CultureInfo.InvariantCulture);
    }

    private static async Task<int> CountSqlServerWhereAsync(SqlConnection connection, string tableName, string whereClause)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(1) FROM {QuoteSqlServer(tableName)} WHERE {whereClause}";
        return Convert.ToInt32(await command.ExecuteScalarAsync(), CultureInfo.InvariantCulture);
    }

    private static async Task<int> CountSqlServerJoinAsync(SqlConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
SELECT COUNT(1)
FROM [dbo].[Members] m
INNER JOIN [dbo].[Stokvels] s ON s.[TenantId] = m.[TenantId]
""";
        return Convert.ToInt32(await command.ExecuteScalarAsync(), CultureInfo.InvariantCulture);
    }

    private static async Task<bool> SourceTableExistsAsync(SqliteConnection source, string tableName)
    {
        await using var command = source.CreateCommand();
        command.CommandText = "SELECT COUNT(1) FROM sqlite_master WHERE type = 'table' AND name = $table";
        command.Parameters.AddWithValue("$table", tableName);
        return Convert.ToInt32(await command.ExecuteScalarAsync(), CultureInfo.InvariantCulture) > 0;
    }

    private static async Task<bool> TargetTableExistsAsync(SqlConnection target, string tableName)
    {
        await using var command = target.CreateCommand();
        command.CommandText = "SELECT COUNT(1) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = @table";
        command.Parameters.AddWithValue("@table", tableName);
        return Convert.ToInt32(await command.ExecuteScalarAsync(), CultureInfo.InvariantCulture) > 0;
    }

    private static async Task<List<UserPreview>> GetSampleUsersAsync(SqlConnection target)
    {
        var users = new List<UserPreview>();
        await using var command = target.CreateCommand();
        command.CommandText = "SELECT TOP (5) [Id], [Email] FROM [dbo].[AspNetUsers] ORDER BY [Email]";
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            users.Add(new UserPreview(reader.GetString(0), reader.IsDBNull(1) ? null : reader.GetString(1)));
        }

        return users;
    }

    private static async Task ExecuteSqlServerNonQueryAsync(SqlConnection target, SqlTransaction transaction, string sql)
    {
        await using var command = target.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    private static string QuoteSqlite(string identifier) => "\"" + identifier.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";

    private static string QuoteSqlServer(string tableName) => "[dbo].[" + tableName.Replace("]", "]]", StringComparison.Ordinal) + "]";

    private static string QuoteSqlServerColumn(string columnName) => "[" + columnName.Replace("]", "]]", StringComparison.Ordinal) + "]";

    private static string MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            return "(no email)";
        }

        var parts = email.Split('@', 2);
        var local = parts[0];
        var maskedLocal = local.Length <= 1 ? "*" : local[0] + "***";
        return $"{maskedLocal}@{parts[1]}";
    }
}

internal sealed record CliArguments(string Command, string? Source, string? Target, bool ShowHelp)
{
    public static CliArguments Parse(string[] args)
    {
        if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
        {
            return new CliArguments("help", null, null, true);
        }

        var command = args[0].Trim().ToLowerInvariant();
        string? source = null;
        string? target = null;

        for (var i = 1; i < args.Length; i++)
        {
            if (args[i] == "--source" && i + 1 < args.Length)
            {
                source = args[++i];
            }
            else if (args[i] == "--target" && i + 1 < args.Length)
            {
                target = args[++i];
            }
        }

        return new CliArguments(command, source, target, false);
    }
}

internal sealed record MigrationPlan(IReadOnlyList<TablePlan> Tables, IReadOnlyList<string> IgnoredSourceTables);

internal sealed record TablePlan(
    string Name,
    int SourceCount,
    int TargetCount,
    int ExistingByKey,
    int InsertCount,
    ColumnInfo[] Columns,
    string[] PrimaryKey,
    UniqueIndexInfo[] UniqueIndexes);

internal sealed record ColumnInfo(string Name, string StoreType, bool IsNullable, bool IsIdentity, bool IsComputed);

internal sealed record SourceColumnInfo(string Name, int PrimaryKeyOrdinal);

internal sealed record UserPreview(string Id, string? Email);

internal sealed record UniqueIndexInfo(string Name, string[] Columns);

internal sealed record UniqueMatch(UniqueIndexInfo Index, IReadOnlyDictionary<string, object?> PrimaryKeyValues);

internal sealed class MigrationMappings
{
    public Dictionary<string, string> TenantIds { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> UserIds { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> RoleIds { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> StokvelIds { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> MemberIds { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> DependentIds { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> MeetingIds { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> MeetingVoteIds { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> VoteMotionIds { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> VoteOptionIds { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> MemberContributionIds { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> PaymentIds { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> FuneralClaimIds { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> QuestionnaireQuestionIds { get; } = new(StringComparer.OrdinalIgnoreCase);

    public void Clear()
    {
        TenantIds.Clear();
        UserIds.Clear();
        RoleIds.Clear();
        StokvelIds.Clear();
        MemberIds.Clear();
        DependentIds.Clear();
        MeetingIds.Clear();
        MeetingVoteIds.Clear();
        VoteMotionIds.Clear();
        VoteOptionIds.Clear();
        MemberContributionIds.Clear();
        PaymentIds.Clear();
        FuneralClaimIds.Clear();
        QuestionnaireQuestionIds.Clear();
    }

    public void Apply(IDictionary<string, object?> row)
    {
        Apply(row, "TenantId", TenantIds);
        Apply(row, "ApplicationUserId", UserIds);
        Apply(row, "UserId", UserIds);
        Apply(row, "RoleId", RoleIds);
        Apply(row, "StokvelId", StokvelIds);
        Apply(row, "MemberId", MemberIds);
        Apply(row, "CapturedByMemberId", MemberIds);
        Apply(row, "DependentId", DependentIds);
        Apply(row, "MeetingId", MeetingIds);
        Apply(row, "MeetingVoteId", MeetingVoteIds);
        Apply(row, "VoteMotionId", VoteMotionIds);
        Apply(row, "VoteOptionId", VoteOptionIds);
        Apply(row, "MemberContributionId", MemberContributionIds);
        Apply(row, "ContributionPaymentId", PaymentIds);
        Apply(row, "PaymentId", PaymentIds);
        Apply(row, "FuneralClaimId", FuneralClaimIds);
        Apply(row, "QuestionnaireQuestionId", QuestionnaireQuestionIds);
    }

    private static void Apply(IDictionary<string, object?> row, string columnName, IReadOnlyDictionary<string, string> map)
    {
        if (!row.TryGetValue(columnName, out var value) || value is null or DBNull)
        {
            return;
        }

        var source = Convert.ToString(value, CultureInfo.InvariantCulture);
        if (!string.IsNullOrWhiteSpace(source) && map.TryGetValue(source, out var target))
        {
            row[columnName] = target;
        }
    }
}
