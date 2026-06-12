using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Sisonke.Web.Data;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var basePath = Directory.GetCurrentDirectory();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false)
            .AddUserSecrets<ApplicationDbContextFactory>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        var rawConnectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(rawConnectionString))
        {
            throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        var services = new ServiceCollection();
        var configuredProvider = configuration["DatabaseProvider"];
        var isSqlServer = string.Equals(configuredProvider, "SqlServer", StringComparison.OrdinalIgnoreCase)
                          || rawConnectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase);
        var isSqlite = !isSqlServer
                       && (string.Equals(configuredProvider, "Sqlite", StringComparison.OrdinalIgnoreCase)
                           || rawConnectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase));

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            if (isSqlServer)
            {
                options.UseSqlServer(rawConnectionString);
                return;
            }

            if (isSqlite)
            {
                var csb = new SqliteConnectionStringBuilder(rawConnectionString);
                var dataSource = csb.DataSource;

                if (string.IsNullOrWhiteSpace(dataSource))
                {
                    throw new InvalidOperationException("SQLite connection string must include a non-empty Data Source path.");
                }

                csb.DataSource = Path.IsPathRooted(dataSource)
                    ? Path.GetFullPath(dataSource)
                    : Path.GetFullPath(Path.Combine(
                        string.IsNullOrWhiteSpace(Path.GetDirectoryName(dataSource))
                            ? Path.Combine(basePath, "Data")
                            : basePath,
                        dataSource));

                options.UseSqlite(csb.ConnectionString);
                return;
            }

            throw new InvalidOperationException(
                "Unable to determine database provider. Set DatabaseProvider to 'Sqlite' or 'SqlServer', " +
                "or supply a DefaultConnection containing either 'Data Source=' or 'Server='.");
        });

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Stores.SchemaVersion = IdentitySchemaVersions.Version2;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        var serviceProvider = services.BuildServiceProvider();
        var scope = serviceProvider.CreateScope();

        return scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    }
}
