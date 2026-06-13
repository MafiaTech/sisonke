# Sisonke Migration Tool

Local-only utility for moving the downloaded pilot SQLite database into the prepared Azure SQL database.

The tool never changes the live App Service connection string and does not delete the source SQLite file.

## Configure

Use user secrets for the Azure SQL target so credentials are not committed:

```powershell
dotnet user-secrets set --project Sisonke.MigrationTool "TargetSqlServerConnection" "<azure-sql-connection-string>"
dotnet user-secrets set --project Sisonke.MigrationTool "SourceSqliteConnection" "Data Source=C:\Users\mpshe\Projects\Sisonke-Migration\sisonke-pilot.db"
```

`appsettings.json` already points `SourceSqliteConnection` at the local downloaded SQLite file:

```text
C:\Users\mpshe\Projects\Sisonke-Migration\sisonke-pilot.db
```

You can also pass the source path or connection strings on the command line. Never commit a real `TargetSqlServerConnection` value.

## Dry Run

```powershell
dotnet run --project Sisonke.MigrationTool -- dry-run
```

## Execute

```powershell
dotnet run --project Sisonke.MigrationTool -- execute
```

## Verify

```powershell
dotnet run --project Sisonke.MigrationTool -- verify
```

## Notes

- `__EFMigrationsHistory`, SQLite internals, and unknown tables are intentionally ignored.
- Existing Azure SQL rows with matching primary keys are skipped.
- IDs are preserved by copying common source and target columns, including identity columns when present.
