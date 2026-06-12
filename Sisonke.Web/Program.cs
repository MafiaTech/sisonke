using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Data.Sqlite;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
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
authSettings.RequireConfirmedAccount = builder.Configuration.GetValue("Auth:RequireConfirmedAccount", false);
authSettings.SessionTimeoutMinutes   = builder.Configuration.GetValue("Auth:SessionTimeoutMinutes", 60);
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
    options.ExpireTimeSpan    = TimeSpan.FromMinutes(authSettings.SessionTimeoutMinutes);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly   = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite  = SameSiteMode.Lax;
    options.LoginPath        = "/Account/Login";
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

app.MapPost("/Account/RegisterSubmit", RegistrationSubmitEndpoint.HandleAsync)
    .DisableAntiforgery()
    .WithName("RegisterSubmit");

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

Console.WriteLine($"Resolved DefaultConnection: {connectionString}");
Console.WriteLine($"[Sisonke] SMTP configured: {!string.IsNullOrWhiteSpace(emailSettings.SmtpHost)} | RequireConfirmedAccount: {authSettings.RequireConfirmedAccount} | PublicBaseUrl: {appSettings.PublicBaseUrl ?? "(NavigationManager)"}");


app.Run();

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
            ResidentialArea = input.ResidentialArea?.Trim()
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
    [Required]
    [Display(Name = "Full Name")]
    public string? FullName { get; set; }

    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [Required]
    [MaxLength(30)]
    [Display(Name = "ID Number")]
    public string? IdNumber { get; set; }

    [Required]
    [MaxLength(30)]
    [Display(Name = "Cellphone Number")]
    public string? CellphoneNumber { get; set; }

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
