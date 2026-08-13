using System.Security.Claims;
using Bunit;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Sisonke.Web.Components.Shared;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Enums;
using Sisonke.Web.Services;
using Sisonke.Web.Services.Entitlements;
using Sisonke.Web.Tests.TestSupport;

namespace Sisonke.Web.Tests.Components;

public sealed class LegacyMigrationPromptTests : BunitContext
{
    private const string UserId = "user-1";

    [Fact]
    public void RemindMeLater_ClosesModal()
    {
        using var db = new SqliteTestDatabase();
        SeedLegacyOfficeBearer(db);
        ConfigureServices(db);
        JSInterop.Setup<string?>("sessionStorage.getItem", "sisonke-legacy-migration-deferred")
            .SetResult(null);
        JSInterop.SetupVoid("sessionStorage.setItem", "sisonke-legacy-migration-deferred", "true")
            .SetVoidResult();

        var component = Render<LegacyMigrationPrompt>();

        Assert.Contains("Sisonke subscription plans are now available", component.Markup);
        component.Find("button.sisonke-btn-outline").Click();

        component.WaitForAssertion(() =>
            Assert.DoesNotContain("Sisonke subscription plans are now available", component.Markup));
    }

    [Fact]
    public void SelectYourPlan_ClosesModalAndNavigatesToRegistrationWizard()
    {
        using var db = new SqliteTestDatabase();
        var stokvelId = SeedLegacyOfficeBearer(db);
        ConfigureServices(db);
        JSInterop.Setup<string?>("sessionStorage.getItem", "sisonke-legacy-migration-deferred")
            .SetResult(null);
        JSInterop.SetupVoid("sessionStorage.setItem", "sisonke-legacy-migration-deferred", "true")
            .SetVoidResult();

        var component = Render<LegacyMigrationPrompt>();

        component.Find("button.sisonke-btn-primary").Click();

        var navigationManager = Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();
        component.WaitForAssertion(() =>
            Assert.EndsWith($"/register-stokvel/{stokvelId}", navigationManager.Uri));
        component.WaitForAssertion(() =>
            Assert.DoesNotContain("Sisonke subscription plans are now available", component.Markup));
    }

    [Fact]
    public void AlreadyDeferred_DoesNotShowModal()
    {
        using var db = new SqliteTestDatabase();
        SeedLegacyOfficeBearer(db);
        ConfigureServices(db);
        JSInterop.Setup<string?>("sessionStorage.getItem", "sisonke-legacy-migration-deferred")
            .SetResult("true");

        var component = Render<LegacyMigrationPrompt>();

        Assert.DoesNotContain("Sisonke subscription plans are now available", component.Markup);
    }

    private void ConfigureServices(SqliteTestDatabase db)
    {
        Services.AddSingleton<IDbContextFactory<ApplicationDbContext>>(new TestDbContextFactory(db));
        Services.AddScoped(_ => db.CreateContext());
        Services.AddScoped<MemberAccessService>();
        Services.AddScoped<StokvelArchetypeConfigurationService>();
        Services.AddScoped<StokvelService>();
        Services.AddScoped<IEntitlementUsageProvider, EntitlementUsageProvider>();
        Services.AddScoped<IEntitlementService, EntitlementService>();
        Services.AddSingleton<IMemoryCache, MemoryCache>();
        Services.AddSingleton(new EntitlementOptions());
        Services.AddSingleton(NullLogger<EntitlementService>.Instance);
        Services.AddSingleton<ILogger<LegacyMigrationPrompt>>(NullLogger<LegacyMigrationPrompt>.Instance);
        Services.AddSingleton<AuthenticationStateProvider>(
            new TestAuthenticationStateProvider(new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, UserId)],
                authenticationType: "Test"))));
    }

    private static Guid SeedLegacyOfficeBearer(SqliteTestDatabase db)
    {
        using var context = db.CreateContext();
        var stokvel = TestData.CreateStokvel(context);
        var member = TestData.CreateStokvelMember(context, stokvel, role: SisonkeRole.Chairperson);
        member.ApplicationUserId = UserId;
        TestData.CreateOrganisationSubscription(context, stokvel, null, SubscriptionStatus.LegacyUnsubscribed);
        context.SaveChanges();
        return stokvel.Id;
    }

    private sealed class TestAuthenticationStateProvider(ClaimsPrincipal user) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
            Task.FromResult(new AuthenticationState(user));
    }
}
