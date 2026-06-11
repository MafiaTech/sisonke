using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Components;
using Sisonke.Web.Components.Account;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Seed;
using Sisonke.Web.Services;

var builder = WebApplication.CreateBuilder(args);

var dataDirectory = Path.Combine(builder.Environment.ContentRootPath, "Data");
Directory.CreateDirectory(dataDirectory);

// ── Auth, app and email settings (from config / env vars) ────────────────
var authSettings  = builder.Configuration.GetSection("Auth").Get<AuthSettings>()   ?? new AuthSettings();
var appSettings   = builder.Configuration.GetSection("App").Get<AppSettings>()     ?? new AppSettings();
var emailSettings = builder.Configuration.GetSection("Email").Get<EmailSettings>() ?? new EmailSettings();
builder.Services.AddSingleton(authSettings);
builder.Services.AddSingleton(appSettings);
builder.Services.AddSingleton(emailSettings);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

string connectionString;

if (builder.Environment.IsDevelopment() || builder.Environment.IsStaging())
{
    var configuredConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    var connectionStringBuilder = new SqliteConnectionStringBuilder(configuredConnectionString);
    var configuredDataSource = connectionStringBuilder.DataSource;

    if (string.IsNullOrWhiteSpace(configuredDataSource))
    {
        throw new InvalidOperationException("Connection string 'DefaultConnection' must include a SQLite Data Source.");
    }

    var absoluteDatabasePath = Path.IsPathRooted(configuredDataSource)
        ? Path.GetFullPath(configuredDataSource)
        : Path.GetFullPath(Path.Combine(
            string.IsNullOrWhiteSpace(Path.GetDirectoryName(configuredDataSource))
                ? dataDirectory
                : builder.Environment.ContentRootPath,
            configuredDataSource));

    connectionStringBuilder.DataSource = absoluteDatabasePath;
    connectionString = connectionStringBuilder.ConnectionString;

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite(connectionString)
               .ConfigureWarnings(w =>
                   w.Ignore(RelationalEventId.PendingModelChangesWarning)));
}
else
{
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found. Supply it via Azure App Service Configuration as ConnectionStrings__DefaultConnection.");

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString));
}

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        // Pilot: RequireConfirmedAccount = false (users can sign in before confirming email).
        // Production: set Auth__RequireConfirmedAccount = true in Azure App Service config.
        options.SignIn.RequireConfirmedAccount = authSettings.RequireConfirmedAccount;
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version2;

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
builder.Services.AddHttpClient();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    await context.Database.MigrateAsync();
    await SisonkeSeedData.SeedAsync(context);

    if (app.Environment.IsDevelopment())
    {
        await SisonkeDemoDataSeeder.SeedAsync(scope.ServiceProvider);
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

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

Console.WriteLine($"Resolved DefaultConnection: {connectionString}");
Console.WriteLine($"[Sisonke] SMTP configured: {!string.IsNullOrWhiteSpace(emailSettings.SmtpHost)} | RequireConfirmedAccount: {authSettings.RequireConfirmedAccount} | PublicBaseUrl: {appSettings.PublicBaseUrl ?? "(NavigationManager)"}");


app.Run();
