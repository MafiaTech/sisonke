# Local development database

Sisonke.Web runs against SQL Server. For local development, a SQL Server 2022
container gives you the same provider/schema as Azure SQL without needing a
cloud connection.

## First-time setup

1. Start the local database container:

   ```
   docker compose -f docker-compose.dev.yml up -d
   ```

   This starts SQL Server 2022 on `localhost,14333` with a named volume
   (`sisonke-dev-sql-data`) for persistence, and a healthcheck that waits for
   SQL Server to accept connections.

2. Run the app:

   ```
   dotnet run --project Sisonke.Web
   ```

   `Sisonke.Web/appsettings.Development.json` already points
   `ConnectionStrings:DefaultConnection` at the container. On startup, the app
   runs in the `Development` environment (see `Properties/launchSettings.json`)
   and automatically applies any pending EF Core migrations to the
   `SisonkeDev` database — no manual `dotnet ef database update` needed.

## Resetting the local database

To wipe the database and start from a clean schema:

```
docker compose -f docker-compose.dev.yml down -v
docker compose -f docker-compose.dev.yml up -d
```

`down -v` removes the named volume along with the container, so the next
`up -d` starts SQL Server with an empty data directory. `dotnet run` will
then reapply all migrations from scratch.

## Notes

- The `docker-compose.dev.yml` SA password is a local-only development value
  — it only matters for this container, which listens on `localhost` and
  isn't reachable from outside your machine.
- `appsettings.Development.json` is excluded from `dotnet publish` output
  (see `Sisonke.Web.csproj`), so it never ships in a deployment package.
  Production/Azure connection strings and secrets are configured separately
  through Azure App Service settings — this file has no effect there.
- Auto-migration on startup only runs when the app starts in the
  `Development` environment (or when `Database:MigrateOnStartup` is
  explicitly set to `true`). In any other environment, the app checks for
  pending migrations and logs a warning instead of applying them or failing
  to start — schema changes in Production/Staging go through a controlled
  deployment step instead.
