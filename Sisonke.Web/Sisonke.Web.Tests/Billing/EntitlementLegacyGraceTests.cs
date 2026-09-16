using Sisonke.Web.Data;
using Sisonke.Web.Data.Enums;
using Sisonke.Web.Services.Entitlements;
using Sisonke.Web.Tests.TestSupport;

namespace Sisonke.Web.Tests.Billing;

public class EntitlementLegacyGraceTests
{
    [Fact]
    public async Task NoSubscriptionRow_BeforeCutover_GetsGrowingEquivalentAccess()
    {
        var options = new EntitlementOptions { LegacyGraceCutoverUtc = DateTime.UtcNow.AddDays(30) };
        using var harness = new EntitlementTestHarness(options);
        await harness.SeedCatalogueAsync();

        await using var context = harness.CreateContext();
        var stokvel = TestData.CreateStokvel(context);
        // Deliberately no OrganisationSubscription row at all.
        await context.SaveChangesAsync();

        var decision = await harness.Sut.AuthorizeAsync(stokvel.Id, FeatureCodes.RotationalStokvel);
        var limitDecision = await harness.Sut.AuthorizeAsync(stokvel.Id, FeatureCodes.MaxMembers, requestedUsage: 1);

        Assert.True(decision.Allowed);
        Assert.Equal(EntitlementDenialReason.Allowed, decision.Reason);
        Assert.Equal(PlanCodes.Growing, decision.PlanCode);
        Assert.True(limitDecision.Allowed);
        Assert.Equal(100, limitDecision.Limit);
    }

    [Fact]
    public async Task LegacyUnsubscribedRow_BeforeCutover_GetsGrowingEquivalentAccess()
    {
        var options = new EntitlementOptions { LegacyGraceCutoverUtc = DateTime.UtcNow.AddDays(30) };
        using var harness = new EntitlementTestHarness(options);
        await harness.SeedCatalogueAsync();

        await using var context = harness.CreateContext();
        var stokvel = TestData.CreateStokvel(context);
        TestData.CreateOrganisationSubscription(context, stokvel, null, SubscriptionStatus.LegacyUnsubscribed);
        await context.SaveChangesAsync();

        var decision = await harness.Sut.AuthorizeAsync(stokvel.Id, FeatureCodes.ClaimsFullWorkflow);

        Assert.True(decision.Allowed);
        Assert.Equal(PlanCodes.Growing, decision.PlanCode);
    }

    [Fact]
    public async Task LegacyGrace_DoesNotGrantFeaturesGrowingItselfDoesNotHave()
    {
        var options = new EntitlementOptions { LegacyGraceCutoverUtc = DateTime.UtcNow.AddDays(30) };
        using var harness = new EntitlementTestHarness(options);
        await harness.SeedCatalogueAsync();

        await using var context = harness.CreateContext();
        var stokvel = TestData.CreateStokvel(context);
        await context.SaveChangesAsync();

        // ADVANCED_REPORTING is false on Growing (Professional+ only) — grace must not bypass that.
        var decision = await harness.Sut.AuthorizeAsync(stokvel.Id, FeatureCodes.AdvancedReporting);

        Assert.False(decision.Allowed);
        Assert.Equal(EntitlementDenialReason.FeatureNotInPlan, decision.Reason);
    }

    [Fact]
    public async Task NoSubscriptionRow_OnOrAfterCutover_DeniedWithNoSubscription()
    {
        var options = new EntitlementOptions { LegacyGraceCutoverUtc = DateTime.UtcNow.AddDays(-1) };
        using var harness = new EntitlementTestHarness(options);
        await harness.SeedCatalogueAsync();

        await using var context = harness.CreateContext();
        var stokvel = TestData.CreateStokvel(context);
        await context.SaveChangesAsync();

        var decision = await harness.Sut.AuthorizeAsync(stokvel.Id, FeatureCodes.RotationalStokvel);

        Assert.False(decision.Allowed);
        Assert.Equal(EntitlementDenialReason.NoSubscription, decision.Reason);
    }

    [Fact]
    public async Task NoSubscriptionRow_OnOrAfterCutover_AlwaysAllowedFeatureStillAllowed()
    {
        var options = new EntitlementOptions { LegacyGraceCutoverUtc = DateTime.UtcNow.AddDays(-1) };
        using var harness = new EntitlementTestHarness(options);
        await harness.SeedCatalogueAsync();

        await using var context = harness.CreateContext();
        var stokvel = TestData.CreateStokvel(context);
        await context.SaveChangesAsync();

        var decision = await harness.Sut.AuthorizeAsync(stokvel.Id, FeatureCodes.OpBillingPageAccess);

        Assert.True(decision.Allowed);
    }
}
