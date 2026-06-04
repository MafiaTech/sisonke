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

var configuredConnectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
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
var connectionString = connectionStringBuilder.ConnectionString;

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();
builder.Services.AddScoped<StokvelService>();
builder.Services.AddScoped<MemberService>();
builder.Services.AddScoped<FineService>();
builder.Services.AddScoped<ContributionService>();
builder.Services.AddScoped<ContributionPaymentService>();
builder.Services.AddScoped<FinanceReportService>();
builder.Services.AddScoped<QuestionnaireService>();
builder.Services.AddScoped<OperatingRuleService>();
builder.Services.AddScoped<ConstitutionService>();
builder.Services.AddScoped<MeetingService>();
builder.Services.AddScoped<MeetingApologyService>();
builder.Services.AddScoped<MemberWarningService>();
builder.Services.AddScoped<VotingService>();
builder.Services.AddScoped<FuneralClaimService>();
builder.Services.AddScoped<MemberAccountLinkingService>();
builder.Services.AddScoped<MemberAccessService>();

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
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
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

app.Run();
