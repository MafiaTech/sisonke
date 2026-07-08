using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Data.Sqlite;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Components;
using Sisonke.Web.Components.Account;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Seed;
using Sisonke.Web.Helpers;
using Sisonke.Web.Services;

var builder = WebApplication.CreateBuilder(args);

var dataDirectory = Path.Combine(builder.Environment.ContentRootPath, "Data");
Directory.CreateDirectory(dataDirectory);

// ── Auth, app and email settings (from config / env vars) ────────────────
var authSettings = builder.Configuration.GetSection("Auth").Get<AuthSettings>() ?? new AuthSettings();
authSettings.RequireConfirmedAccount = builder.Configuration.GetValue("Auth:RequireConfirmedAccount", false);
authSettings.SessionTimeoutMinutes = builder.Configuration.GetValue("Auth:SessionTimeoutMinutes", 60);
var appSettings = builder.Configuration.GetSection("App").Get<AppSettings>() ?? new AppSettings();
var emailSettings = builder.Configuration.GetSection("Email").Get<EmailSettings>() ?? new EmailSettings();
var featureFlags = builder.Configuration.GetSection("Features").Get<FeatureFlags>() ?? new FeatureFlags();
builder.Services.AddSingleton(authSettings);
builder.Services.AddSingleton(appSettings);
builder.Services.AddSingleton(emailSettings);
builder.Services.AddSingleton(featureFlags);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

var authenticationBuilder = builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultChallengeScheme = "MicrosoftEntraExternalId";
    options.DefaultSignInScheme = IdentityConstants.ApplicationScheme;
});

authenticationBuilder.AddIdentityCookies();

authenticationBuilder.AddOpenIdConnect("MicrosoftEntraExternalId", options =>
{
    var clientId = builder.Configuration["AzureAd:ClientId"];
    if (string.IsNullOrWhiteSpace(clientId))
    {
        throw new InvalidOperationException("AzureAd:ClientId is not configured.");
    }

    var clientSecret = builder.Configuration["AzureAd:ClientSecret"];
    if (string.IsNullOrWhiteSpace(clientSecret))
    {
        throw new InvalidOperationException("AzureAd:ClientSecret is not configured.");
    }

    var metadataAddress = builder.Configuration["AzureAd:MetadataAddress"]?.Trim();
    string? authority = null;
    if (!string.IsNullOrWhiteSpace(metadataAddress))
    {
        if (!metadataAddress.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("AzureAd:MetadataAddress must start with https://.");
        }

        options.MetadataAddress = metadataAddress;
    }
    else
    {
        var instance = builder.Configuration["AzureAd:Instance"]?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(instance))
        {
            throw new InvalidOperationException("AzureAd:Instance is not configured. For Entra External ID / CIAM, use https://<external-tenant-subdomain>.ciamlogin.com/.");
        }

        if (!instance.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("AzureAd:Instance must start with https://.");
        }

        var domain = builder.Configuration["AzureAd:Domain"];
        var tenantId = builder.Configuration["AzureAd:TenantId"];
        var tenantSegment = !string.IsNullOrWhiteSpace(domain) ? domain.Trim() : tenantId?.Trim();
        if (string.IsNullOrWhiteSpace(tenantSegment))
        {
            throw new InvalidOperationException("Either AzureAd:Domain or AzureAd:TenantId must be configured.");
        }

        authority = $"{instance}/{tenantSegment}/v2.0";
        options.Authority = authority;
    }

    if (builder.Environment.IsDevelopment())
    {
        if (!string.IsNullOrWhiteSpace(metadataAddress))
        {
            Console.WriteLine($"[Sisonke] Entra External ID metadata address: {metadataAddress}");
        }
        else
        {
            Console.WriteLine($"[Sisonke] Entra External ID authority: {authority}");
            Console.WriteLine($"[Sisonke] Entra External ID metadata address pattern: {authority}/.well-known/openid-configuration");
        }
    }

    options.ClientId = clientId;
    options.ClientSecret = clientSecret;
    var callbackPath = builder.Configuration["AzureAd:CallbackPath"];
    options.CallbackPath = string.IsNullOrWhiteSpace(callbackPath) ? "/signin-oidc" : callbackPath;
    options.ResponseType = "code";
    options.SaveTokens = true;
    options.SignInScheme = IdentityConstants.ApplicationScheme;

    options.Scope.Clear();
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");

    options.Events = new OpenIdConnectEvents
    {
        OnRedirectToIdentityProvider = context =>
        {
            if (builder.Environment.IsDevelopment())
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("Sisonke.EntraExternalId");
                logger.LogInformation(
                    "Redirecting to Entra External ID. RedirectUri={RedirectUri}; CallbackPath={CallbackPath}; Scheme={Scheme}",
                    context.ProtocolMessage.RedirectUri,
                    context.Options.CallbackPath,
                    context.Scheme.Name);
            }

            return Task.CompletedTask;
        },
        OnRemoteFailure = context =>
        {
            if (builder.Environment.IsDevelopment())
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("Sisonke.EntraExternalId");
                logger.LogWarning("Entra External ID remote failure. Error={Error}", context.Failure?.Message ?? "(no exception message)");
            }

            return Task.CompletedTask;
        }
    };
});

string connectionString;

var rawConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' not found. " +
        "SQLite example: 'Data Source=Data/app.db'. " +
        "Azure SQL example: 'Server=tcp:sql-sisonke-dev.database.windows.net,1433;Initial Catalog=sqldb-sisonke-dev;...'");

// Provider can be forced for EF tooling/Azure via DatabaseProvider=SqlServer.
// Otherwise infer from the connection string so local SQLite pilot still works.
var configuredProvider = builder.Configuration["DatabaseProvider"];
var isSqlServer = string.Equals(configuredProvider, "SqlServer", StringComparison.OrdinalIgnoreCase)
                  || rawConnectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase);
var isSqlite = !isSqlServer
               && (string.Equals(configuredProvider, "Sqlite", StringComparison.OrdinalIgnoreCase)
                   || rawConnectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase));

if (!isSqlite && !isSqlServer)
{
    throw new InvalidOperationException(
        "Unable to determine database provider. Set DatabaseProvider to 'Sqlite' or 'SqlServer', " +
        "or supply a DefaultConnection containing either 'Data Source=' or 'Server='.");
}

// Extract server and database name for startup diagnostics — no credentials are logged.
string dbDiagServer;
string dbDiagName;
try
{
    var diagCsb = new System.Data.Common.DbConnectionStringBuilder { ConnectionString = rawConnectionString };
    if (isSqlite)
    {
        dbDiagServer = "localhost (SQLite)";
        dbDiagName = diagCsb.ContainsKey("Data Source") ? diagCsb["Data Source"]?.ToString() ?? "(unknown)" : "(unknown)";
    }
    else
    {
        dbDiagServer = diagCsb.ContainsKey("Server") ? diagCsb["Server"]?.ToString() ?? "(unknown)" : "(unknown)";
        dbDiagName = diagCsb.ContainsKey("Initial Catalog") ? diagCsb["Initial Catalog"]?.ToString() ?? "(unknown)" :
                       diagCsb.ContainsKey("Database") ? diagCsb["Database"]?.ToString() ?? "(unknown)" : "(unknown)";
    }
}
catch
{
    dbDiagServer = "(unknown)";
    dbDiagName = "(unknown)";
}

if (isSqlite)
{
    // Resolve relative path to absolute so the file lands in a predictable location.
    var csb = new SqliteConnectionStringBuilder(rawConnectionString);
    var dataSource = csb.DataSource;

    if (string.IsNullOrWhiteSpace(dataSource))
        throw new InvalidOperationException("SQLite connection string must include a non-empty Data Source path.");

    var absolutePath = Path.IsPathRooted(dataSource)
        ? Path.GetFullPath(dataSource)
        : Path.GetFullPath(Path.Combine(
            string.IsNullOrWhiteSpace(Path.GetDirectoryName(dataSource))
                ? dataDirectory
                : builder.Environment.ContentRootPath,
            dataSource));

    csb.DataSource = absolutePath;
    connectionString = csb.ConnectionString;

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite(connectionString)
               .ConfigureWarnings(w =>
                   w.Ignore(RelationalEventId.PendingModelChangesWarning)));
    builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
        options.UseSqlite(connectionString)
               .ConfigureWarnings(w =>
                   w.Ignore(RelationalEventId.PendingModelChangesWarning)),
        ServiceLifetime.Scoped);
}
else
{
    connectionString = rawConnectionString;

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString));
    builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString),
        ServiceLifetime.Scoped);
}

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        // Pilot: RequireConfirmedAccount = false (users can sign in before confirming email).
        // Production: set Auth__RequireConfirmedAccount = true in Azure App Service config.
        options.SignIn.RequireConfirmedAccount = authSettings.RequireConfirmedAccount;
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version2;

        // ── User name / email policy ─────────────────────────────────────
        options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
        options.User.RequireUniqueEmail = true;

        // ── Password policy ──────────────────────────────────────────────
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = false;

        // ── Account lockout ───────────────────────────────────────────────
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

// ── Email sender ─────────────────────────────────────────────────────────
// SisonkeEmailSender sends real email when SMTP is configured.
// When SMTP is not configured: logs to console in Development, logs warning in Production.
// To enable real email, set Email__SmtpHost (and other Email__ keys) in Azure App Service config.
builder.Services.AddSingleton<SisonkeEmailSender>();
builder.Services.AddSingleton<IEmailSender<ApplicationUser>>(sp => sp.GetRequiredService<SisonkeEmailSender>());

// ── Session / application cookie ─────────────────────────────────────────
// SlidingExpiration resets the timeout on each authenticated request.
// OnRedirectToLogin detects a timed-out non-persistent session (cookie still present
// in the request but server ticket has expired) and tells Login.razor to show
// a friendly "session expired" message.
builder.Services.ConfigureApplicationCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromMinutes(authSettings.SessionTimeoutMinutes);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Events.OnRedirectToLogin = context =>
    {
        var uri = context.RedirectUri;
        // If the auth cookie is in the request but the server rejected it (ticket expired),
        // the user had an active session that timed out — show the session-expired message.
        if (context.Request.Cookies.ContainsKey(".AspNetCore.Identity.Application"))
        {
            uri = QueryHelpers.AddQueryString(uri, "sessionExpired", "1");
        }
        context.Response.Redirect(uri);
        return Task.CompletedTask;
    };
});

builder.Services.AddScoped<StokvelService>();
builder.Services.AddScoped<MemberService>();
builder.Services.AddScoped<FineService>();
builder.Services.AddScoped<ContributionService>();
builder.Services.AddScoped<ContributionPaymentService>();
builder.Services.AddScoped<FinanceReportService>();
builder.Services.AddScoped<QuestionnaireService>();
builder.Services.AddScoped<OperatingRuleService>();
builder.Services.AddScoped<ConstitutionService>();
builder.Services.AddScoped<StokvelOperatingRulesService>();
builder.Services.AddScoped<MeetingService>();
builder.Services.AddScoped<MeetingApologyService>();
builder.Services.AddScoped<MeetingMinuteService>();
builder.Services.AddScoped<MemberWarningService>();
builder.Services.AddScoped<MemberGovernanceTimelineService>();
builder.Services.AddScoped<VotingService>();
builder.Services.AddScoped<FuneralClaimService>();
builder.Services.AddScoped<ClaimEligibilityService>();
builder.Services.AddScoped<MemberAccountLinkingService>();
builder.Services.AddScoped<MemberAccessService>();
builder.Services.AddScoped<StokvelArchetypeConfigurationService>();
builder.Services.AddScoped<RotationalStokvelService>();
builder.Services.AddScoped<RotationalConfigurationService>();
builder.Services.AddScoped<RotationalPayoutOrderService>();
builder.Services.AddScoped<RotationalContributionCycleService>();
builder.Services.AddScoped<RotationalContributionPaymentService>();
builder.Services.AddScoped<RotationalPayoutService>();
builder.Services.AddScoped<RotationalTaskService>();
builder.Services.AddScoped<StokvelBankingDetailsService>();
builder.Services.AddScoped<LoansWalletService>();
builder.Services.AddScoped<DashboardQueryService>();
builder.Services.AddHttpClient();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var startupLogger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
        .CreateLogger("Sisonke.Startup");

    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    // SeedData:Enabled defaults to FALSE so live/migrated databases are never seeded accidentally.
    // Set SeedData__Enabled=true explicitly in development or staging when demo data is needed.
    var seedDataEnabled = builder.Configuration.GetValue("SeedData:Enabled", false);

    startupLogger.LogInformation(
        "[Startup] Environment: {Environment} | DB provider: {Provider} | DB server: {DbServer} | DB name: {DbName} | SeedData: {SeedEnabled} | AdminSeed: {AdminSeedEnabled}",
        builder.Environment.EnvironmentName,
        isSqlite ? "SQLite" : "SQL Server",
        dbDiagServer,
        dbDiagName,
        seedDataEnabled,
        builder.Configuration.GetValue("AdminSeed:Enabled", false));

    // Auto-migrate in Development by default. Elsewhere, only migrate when explicitly
    // opted in via Database:MigrateOnStartup — otherwise log pending migrations as a
    // warning instead of failing startup, since schema changes should be applied through
    // a controlled deployment step in Production.
    var migrateOnStartup = builder.Configuration.GetValue(
        "Database:MigrateOnStartup",
        builder.Environment.IsDevelopment());

    if (migrateOnStartup)
    {
        try
        {
            startupLogger.LogInformation("[Startup] Applying pending migrations...");
            await context.Database.MigrateAsync();
            startupLogger.LogInformation("[Startup] Migrations applied successfully.");
        }
        catch (Exception ex)
        {
            startupLogger.LogCritical(ex, "[Startup] Database migration failed. The application cannot start safely.");
            throw;
        }
    }
    else
    {
        try
        {
            var pendingMigrations = (await context.Database.GetPendingMigrationsAsync()).ToList();
            if (pendingMigrations.Count > 0)
            {
                startupLogger.LogWarning(
                    "[Startup] {Count} pending migration(s) found but Database:MigrateOnStartup is false — schema was NOT updated automatically. Apply migrations via a controlled deployment step. Pending: {Migrations}",
                    pendingMigrations.Count,
                    string.Join(", ", pendingMigrations));
            }
            else
            {
                startupLogger.LogInformation("[Startup] Database schema is up to date. No pending migrations.");
            }
        }
        catch (Exception ex)
        {
            startupLogger.LogWarning(ex, "[Startup] Could not check for pending migrations. Continuing startup.");
        }
    }

    if (builder.Configuration.GetValue("AdminSeed:Enabled", false))
    {
        try
        {
            startupLogger.LogInformation("[Startup] Running admin user seed...");
            await SeedAdminUserAsync(scope.ServiceProvider, builder.Configuration);
            startupLogger.LogInformation("[Startup] Admin user seed completed.");
        }
        catch (Exception ex)
        {
            startupLogger.LogError(ex, "[Startup] Admin user seed failed.");
            throw;
        }
    }

    if (seedDataEnabled)
    {
        try
        {
            startupLogger.LogInformation("[Startup] Running base seed data...");
            var seedLogger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
                .CreateLogger("SisonkeSeedData");
            await SisonkeSeedData.SeedAsync(context, seedLogger);
            startupLogger.LogInformation("[Startup] Base seed data completed.");
        }
        catch (Exception ex)
        {
            startupLogger.LogError(ex, "[Startup] Base seed data failed.");
            throw;
        }

        if (app.Environment.IsDevelopment())
        {
            try
            {
                startupLogger.LogInformation("[Startup] Running demo data seeder (Development only)...");
                await SisonkeDemoDataSeeder.SeedAsync(scope.ServiceProvider);
                startupLogger.LogInformation("[Startup] Demo data seeder completed.");
            }
            catch (Exception ex)
            {
                startupLogger.LogError(ex, "[Startup] Demo data seeder failed.");
                throw;
            }
        }
    }
    else
    {
        startupLogger.LogInformation("[Startup] Seed data is disabled (SeedData:Enabled=false). Skipping.");

        // Development safety nets: ensure subscription plans and questionnaire questions exist
        // so the Register wizard and Quick Setup wizard work on a fresh local database.
        // Skipped when SeedData:Enabled=true because SeedAsync already covers these.
        if (builder.Environment.IsDevelopment())
        {
            var devSeedLogger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
                .CreateLogger("SisonkeSeedData");

            try
            {
                startupLogger.LogInformation("[Startup] Checking dev subscription plans...");
                await SisonkeSeedData.EnsureDevSubscriptionPlansAsync(context, devSeedLogger);
                startupLogger.LogInformation("[Startup] Dev subscription plans check complete.");
            }
            catch (Exception ex)
            {
                startupLogger.LogError(ex, "[Startup] Dev subscription plan seed failed. Registration may show 'No packages configured'.");
            }

            try
            {
                startupLogger.LogInformation("[Startup] Checking dev questionnaire...");
                await SisonkeSeedData.EnsureDevQuestionnaireAsync(context, devSeedLogger);
                startupLogger.LogInformation("[Startup] Dev questionnaire check complete.");
            }
            catch (Exception ex)
            {
                startupLogger.LogError(ex, "[Startup] Dev questionnaire seed failed. Quick Setup wizard may show 'no questions'.");
            }

            try
            {
                startupLogger.LogInformation("[Startup] Checking dev Stokvel soft-delete columns...");
                await SisonkeSeedData.EnsureDevStokvelColumnsAsync(context, devSeedLogger);
                startupLogger.LogInformation("[Startup] Dev Stokvel columns check complete.");
            }
            catch (Exception ex)
            {
                startupLogger.LogError(ex, "[Startup] Dev Stokvel column migration failed. Stokvel Settings may not work.");
            }
        }
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapGet("/health", async (
    ApplicationDbContext context,
    IWebHostEnvironment environment) =>
{
    var timestampUtc = DateTimeOffset.UtcNow;

    if (!await context.Database.CanConnectAsync())
    {
        return Results.Json(
            new
            {
                status = "Unhealthy",
                database = "Unavailable",
                message = "Database connection failed",
                timestampUtc
            },
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    return Results.Json(new
    {
        status = "Healthy",
        app = "Sisonke.Web",
        environment = environment.EnvironmentName,
        databaseProvider = isSqlite ? "SQLite" : "SqlServer",
        database = "Connected",
        timestampUtc
    });
});

app.MapPost("/Account/RegisterSubmit", RegistrationSubmitEndpoint.HandleAsync)
    .DisableAntiforgery()
    .WithName("RegisterSubmit");

app.MapGet("/Auth/Login", (
    HttpContext context,
    [FromQuery] string? returnUrl) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        return Results.LocalRedirect(GetSafeLocalReturnUrl(returnUrl, "/my-memberships"));
    }

    return Results.Challenge(
        new AuthenticationProperties { RedirectUri = GetSafeLocalReturnUrl(returnUrl, "/my-memberships") },
        ["MicrosoftEntraExternalId"]);
}).AllowAnonymous();

app.MapGet("/Account/Login", (
    HttpContext context,
    [FromQuery] string? returnUrl) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        return Results.LocalRedirect(GetSafeLocalReturnUrl(returnUrl, "/my-memberships"));
    }

    return Results.Challenge(
        new AuthenticationProperties { RedirectUri = GetSafeLocalReturnUrl(returnUrl, "/my-memberships") },
        ["MicrosoftEntraExternalId"]);
}).AllowAnonymous();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

Console.WriteLine($"[Sisonke] Environment: {builder.Environment.EnvironmentName} | DB provider: {(isSqlite ? "SQLite" : "SQL Server")} | DB server: {dbDiagServer} | DB name: {dbDiagName} | Connection: {MaskConnectionString(connectionString)}");
Console.WriteLine($"[Sisonke] SMTP configured: {!string.IsNullOrWhiteSpace(emailSettings.SmtpHost)} | RequireConfirmedAccount: {authSettings.RequireConfirmedAccount} | SessionTimeout: {authSettings.SessionTimeoutMinutes}m | PublicBaseUrl: {appSettings.PublicBaseUrl ?? "(NavigationManager)"}");


app.Run();

static string MaskConnectionString(string value)
{
    if (string.IsNullOrWhiteSpace(value))
    {
        return "(empty)";
    }

    try
    {
        var builder = new System.Data.Common.DbConnectionStringBuilder
        {
            ConnectionString = value
        };

        foreach (var key in new[] { "Password", "Pwd", "User ID", "UID" })
        {
            if (builder.ContainsKey(key))
            {
                builder[key] = "***";
            }
        }

        return builder.ConnectionString;
    }
    catch
    {
        return "(configured)";
    }
}

static string GetSafeLocalReturnUrl(string? returnUrl, string fallback)
{
    if (string.IsNullOrWhiteSpace(returnUrl))
    {
        return fallback;
    }

    return Uri.IsWellFormedUriString(returnUrl, UriKind.Relative)
        ? returnUrl
        : fallback;
}

static async Task SeedAdminUserAsync(IServiceProvider serviceProvider, IConfiguration configuration)
{
    var logger = serviceProvider.GetRequiredService<ILoggerFactory>()
        .CreateLogger("AdminSeed");
    var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var email = configuration["AdminSeed:Email"]?.Trim();
    var password = configuration["AdminSeed:Password"];
    const string roleName = "PlatformAdmin";

    if (string.IsNullOrWhiteSpace(email))
    {
        throw new InvalidOperationException("AdminSeed is enabled but AdminSeed:Email is not configured.");
    }

    if (string.IsNullOrWhiteSpace(password))
    {
        throw new InvalidOperationException("AdminSeed is enabled but AdminSeed:Password is not configured.");
    }

    if (!await roleManager.RoleExistsAsync(roleName))
    {
        logger.LogInformation("Creating admin seed role {RoleName}.", roleName);
        var roleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
        if (!roleResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Could not create admin seed role {roleName}: {string.Join(", ", roleResult.Errors.Select(error => error.Description))}");
        }
    }

    var user = await userManager.FindByEmailAsync(email);
    if (user is null)
    {
        logger.LogInformation("Creating admin seed user {Email}.", email);
        user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = "Pilot Admin"
        };

        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Could not create admin seed user {email}: {string.Join(", ", createResult.Errors.Select(error => error.Description))}");
        }
    }
    else
    {
        logger.LogInformation("Admin seed user {Email} already exists; not overwriting user fields.", email);
    }

    if (!await userManager.IsInRoleAsync(user, roleName))
    {
        logger.LogInformation("Adding admin seed user {Email} to role {RoleName}.", email, roleName);
        var roleAddResult = await userManager.AddToRoleAsync(user, roleName);
        if (!roleAddResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Could not add admin seed user {email} to role {roleName}: {string.Join(", ", roleAddResult.Errors.Select(error => error.Description))}");
        }
    }
}

internal static class RegistrationSubmitEndpoint
{
    public static async Task<IResult> HandleAsync(
        [FromForm] RegisterSubmitInput input,
        HttpContext httpContext,
        UserManager<ApplicationUser> userManager,
        IUserStore<ApplicationUser> userStore,
        SignInManager<ApplicationUser> signInManager,
        IEmailSender<ApplicationUser> emailSender,
        MemberAccountLinkingService memberAccountLinkingService,
        AuthSettings authSettings,
        AppSettings appSettings,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("RegisterSubmit");
        var returnUrl = GetSafeLocalReturnUrl(input.ReturnUrl);
        var email = input.Email?.Trim() ?? string.Empty;

        logger.LogInformation("[RegisterSubmit] Starting HTTP registration for {Email}", email);

        var validationErrors = ValidateInput(input);
        if (validationErrors.Count > 0)
        {
            logger.LogWarning("[RegisterSubmit] Input validation failed for {Email}: {Errors}", email, string.Join(" | ", validationErrors));
            return RedirectToRegister(string.Join(" ", validationErrors), input.ReturnUrl);
        }

        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser is not null)
        {
            logger.LogWarning("[RegisterSubmit] Duplicate email registration attempt for {Email}", email);
            return RedirectToRegister("duplicate", input.ReturnUrl);
        }

        if (!userManager.SupportsUserEmail)
        {
            logger.LogError("[RegisterSubmit] User store does not support email for {Email}", email);
            return RedirectToRegister("Registration is temporarily unavailable. Please try again.", input.ReturnUrl);
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = input.FullName?.Trim(),
            IdNumber = input.IdNumber?.Trim(),
            CellphoneNumber = input.CellphoneNumber?.Trim(),
            ResidentialArea = input.ResidentialArea?.Trim(),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "SelfRegistration"
        };

        await userStore.SetUserNameAsync(user, email, CancellationToken.None);
        await ((IUserEmailStore<ApplicationUser>)userStore).SetEmailAsync(user, email, CancellationToken.None);

        logger.LogInformation("[RegisterSubmit] Calling UserManager.CreateAsync for {Email}", email);
        var result = await userManager.CreateAsync(user, input.Password);

        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(error => error.Description).ToList();
            var errorCodes = result.Errors.Select(error => error.Code).ToList();
            logger.LogWarning("[RegisterSubmit] UserManager.CreateAsync failed for {Email}: {Errors}", email, string.Join(" | ", errors));

            if (errorCodes.Any(code =>
                    string.Equals(code, "DuplicateUserName", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(code, "DuplicateEmail", StringComparison.OrdinalIgnoreCase)))
            {
                return RedirectToRegister("duplicate", input.ReturnUrl);
            }

            return RedirectToRegister(string.Join(" ", errors), input.ReturnUrl);
        }

        logger.LogInformation("[RegisterSubmit] User created successfully for {Email}", email);

        var userId = await userManager.GetUserIdAsync(user);
        try
        {
            logger.LogInformation("[RegisterSubmit] Linking created user {UserId} to members by ID number for {Email}", userId, email);
            var linkedCount = await memberAccountLinkingService.LinkUserToMembersByIdNumberAsync(userId, input.IdNumber);
            logger.LogInformation("[RegisterSubmit] Linked {Count} member record(s) for {Email}", linkedCount, email);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[RegisterSubmit] Member account linking failed for {Email}; continuing registration", email);
        }

        try
        {
            logger.LogInformation("[RegisterSubmit] Generating email confirmation token for {Email}", email);
            var code = await userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var callbackUrl = BuildEmailUrl(httpContext, appSettings, "Account/ConfirmEmail",
                new Dictionary<string, string?>
                {
                    ["userId"] = userId,
                    ["code"] = code
                });

            logger.LogInformation("[RegisterSubmit] Sending verification email to {Email}", email);
            await emailSender.SendConfirmationLinkAsync(user, email, HtmlEncoder.Default.Encode(callbackUrl));
            logger.LogInformation("[RegisterSubmit] Verification email send completed for {Email}", email);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[RegisterSubmit] Email token generation or verification email sending failed for {Email}; continuing when confirmation is not required", email);
            if (authSettings.RequireConfirmedAccount)
            {
                return RedirectToRegister("Account created, but we could not send the verification email. Please contact support.", input.ReturnUrl);
            }
        }

        if (authSettings.RequireConfirmedAccount)
        {
            logger.LogInformation("[RegisterSubmit] Account created for {Email}; confirmation required so redirecting to login", email);
            return Results.LocalRedirect("/Account/Login");
        }

        logger.LogInformation("[RegisterSubmit] Signing in {Email} during normal HTTP POST", email);
        await signInManager.SignInAsync(user, isPersistent: false);
        logger.LogInformation(
            "[RegisterSubmit] Signed in {Email}; auth cookie should be queued: {HasSetCookie}",
            email,
            httpContext.Response.Headers.SetCookie.Count > 0);

        logger.LogInformation("[RegisterSubmit] Redirecting {Email} to {Url}", email, returnUrl);
        return Results.LocalRedirect(returnUrl);
    }

    private static List<string> ValidateInput(RegisterSubmitInput input)
    {
        input.Email = input.Email?.Trim();
        input.FullName = input.FullName?.Trim();
        input.IdNumber = input.IdNumber?.Trim();
        input.CellphoneNumber = input.CellphoneNumber?.Trim();
        input.ResidentialArea = input.ResidentialArea?.Trim();

        var results = new List<ValidationResult>();
        var context = new ValidationContext(input);
        Validator.TryValidateObject(input, context, results, validateAllProperties: true);

        return results
            .Select(result => result.ErrorMessage)
            .Where(message => !string.IsNullOrWhiteSpace(message))
            .Cast<string>()
            .ToList();
    }

    private static string GetSafeLocalReturnUrl(string? returnUrl)
    {
        return !string.IsNullOrWhiteSpace(returnUrl) &&
            Uri.IsWellFormedUriString(returnUrl, UriKind.Relative)
            ? returnUrl
            : "/my-workspace";
    }

    private static IResult RedirectToRegister(string error, string? returnUrl)
    {
        var query = new Dictionary<string, string?>
        {
            ["registrationError"] = error,
            ["ReturnUrl"] = GetSafeLocalReturnUrl(returnUrl)
        };

        return Results.LocalRedirect(QueryHelpers.AddQueryString("/Account/Register", query));
    }

    private static string BuildEmailUrl(
        HttpContext httpContext,
        AppSettings appSettings,
        string path,
        Dictionary<string, string?> parameters)
    {
        var baseUri = !string.IsNullOrWhiteSpace(appSettings.PublicBaseUrl)
            ? $"{appSettings.PublicBaseUrl.TrimEnd('/')}/{path}"
            : $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/{path}";

        return QueryHelpers.AddQueryString(baseUri, parameters);
    }
}

internal sealed class RegisterSubmitInput
{
    [Required(ErrorMessage = "Full name is required.")]
    [Display(Name = "Full Name")]
    public string? FullName { get; set; }

    [Required(ErrorMessage = "Email address is required.")]
    [SaEmailWithDomain]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "ID number is required.")]
    [SaIdNumber]
    [Display(Name = "ID Number")]
    public string? IdNumber { get; set; }

    [Required(ErrorMessage = "Cellphone number is required.")]
    [SaCellphone]
    [Display(Name = "Cellphone Number")]
    public string? CellphoneNumber { get; set; }

    [Required(ErrorMessage = "Residential area is required.")]
    [Display(Name = "Residential Area")]
    public string? ResidentialArea { get; set; }

    [Required]
    [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 8)]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = "";

    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = "";

    public string? ReturnUrl { get; set; }
}
