using Sisonke.Web.Data;
using Sisonke.Web.Data.Enums;
using Sisonke.Web.Services.Entitlements;
using Sisonke.Web.Tests.TestSupport;

namespace Sisonke.Web.Tests.Billing;

public class EntitlementLimitTests
{
    [Theory]
    [InlineData(29, true)]  // 29 active members + 1 requested = 30, at the STARTER cap — allowed
    [InlineData(30, false)] // 30 active members + 1 requested = 31, over the STARTER cap — denied
    public async Task Starter_AtOrUnderCap_AllowsNextMember_OverCap_Denies(int existingActiveMembers, bool expectedAllowed)
    {
        using var harness = new EntitlementTestHarness();
        await harness.SeedCatalogueAsync();

        await using var context = harness.CreateContext();
        var starterPlan = context.SubscriptionPlans.Single(p => p.Code == PlanCodes.Starter);
        var stokvel = TestData.CreateStokvel(context);
        TestData.CreateOrganisationSubscription(context, stokvel, starterPlan.Id, SubscriptionStatus.Active);

        for (var i = 0; i < existingActiveMembers; i++)
        {
            TestData.CreateStokvelMember(context, stokvel);
        }

        await context.SaveChangesAsync();

        var decision = await harness.Sut.AuthorizeAsync(stokvel.Id, FeatureCodes.MaxMembers, requestedUsage: 1);

        Assert.Equal(expectedAllowed, decision.Allowed);
        Assert.Equal(existingActiveMembers, decision.CurrentUsage);
        Assert.Equal(30, decision.Limit);

        if (!expectedAllowed)
        {
            Assert.Equal(EntitlementDenialReason.LimitExceeded, decision.Reason);
            Assert.Equal(PlanCodes.Growing, decision.UpgradeToPlanCode);
        }
    }

    [Fact]
    public async Task Starter_AlreadyOverCapAt31_StillDenied()
    {
        // The 31/30 case from the brief: already over the cap (e.g. after a manual override or
        // a plan downgrade) — must stay denied, not just "at the boundary".
        using var harness = new EntitlementTestHarness();
        await harness.SeedCatalogueAsync();

        await using var context = harness.CreateContext();
        var starterPlan = context.SubscriptionPlans.Single(p => p.Code == PlanCodes.Starter);
        var stokvel = TestData.CreateStokvel(context);
        TestData.CreateOrganisationSubscription(context, stokvel, starterPlan.Id, SubscriptionStatus.Active);

        for (var i = 0; i < 31; i++)
        {
            TestData.CreateStokvelMember(context, stokvel);
        }

        await context.SaveChangesAsync();

        var decision = await harness.Sut.AuthorizeAsync(stokvel.Id, FeatureCodes.MaxMembers, requestedUsage: 1);

        Assert.False(decision.Allowed);
        Assert.Equal(31, decision.CurrentUsage);
        Assert.Equal(30, decision.Limit);
    }

    [Fact]
    public async Task Enterprise_UnlimitedMembers_AlwaysAllowed()
    {
        using var harness = new EntitlementTestHarness();
        await harness.SeedCatalogueAsync();

        await using var context = harness.CreateContext();
        var enterprisePlan = context.SubscriptionPlans.Single(p => p.Code == PlanCodes.Enterprise);
        var stokvel = TestData.CreateStokvel(context);
        TestData.CreateOrganisationSubscription(context, stokvel, enterprisePlan.Id, SubscriptionStatus.Active);

        for (var i = 0; i < 5000; i++)
        {
            TestData.CreateStokvelMember(context, stokvel);
        }

        await context.SaveChangesAsync();

        var decision = await harness.Sut.AuthorizeAsync(stokvel.Id, FeatureCodes.MaxMembers, requestedUsage: 1);

        Assert.True(decision.Allowed);
        Assert.Null(decision.Limit);
    }
}
