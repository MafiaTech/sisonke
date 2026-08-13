using Sisonke.Web.Data;
using Sisonke.Web.Data.Enums;
using Sisonke.Web.Services.Entitlements;
using Sisonke.Web.Tests.TestSupport;

namespace Sisonke.Web.Tests.Billing;

public class EntitlementStatusTests
{
    public static IEnumerable<object[]> WriteBlockedFeatureCodes =>
        FeatureCodes.WriteBlocked.Select(code => new object[] { code });

    public static IEnumerable<object[]> AlwaysAllowedFeatureCodes =>
        FeatureCodes.AlwaysAllowed.Select(code => new object[] { code });

    [Theory]
    [MemberData(nameof(WriteBlockedFeatureCodes))]
    public async Task Restricted_BlocksEachWriteBlockedOperation(string featureCode)
    {
        var decision = await AuthorizeUnderStatus(SubscriptionStatus.Restricted, featureCode);

        Assert.False(decision.Allowed);
        Assert.Equal(EntitlementDenialReason.SubscriptionRestricted, decision.Reason);
    }

    [Theory]
    [MemberData(nameof(AlwaysAllowedFeatureCodes))]
    public async Task Restricted_AllowsEachAlwaysAllowedOperation(string featureCode)
    {
        var decision = await AuthorizeUnderStatus(SubscriptionStatus.Restricted, featureCode);

        Assert.True(decision.Allowed);
    }

    [Theory]
    [MemberData(nameof(WriteBlockedFeatureCodes))]
    public async Task Suspended_BlocksEachWriteBlockedOperation(string featureCode)
    {
        var decision = await AuthorizeUnderStatus(SubscriptionStatus.Suspended, featureCode);

        Assert.False(decision.Allowed);
        Assert.Equal(EntitlementDenialReason.SubscriptionSuspended, decision.Reason);
    }

    [Theory]
    [MemberData(nameof(AlwaysAllowedFeatureCodes))]
    public async Task Suspended_AllowsEachAlwaysAllowedOperation(string featureCode)
    {
        var decision = await AuthorizeUnderStatus(SubscriptionStatus.Suspended, featureCode);

        Assert.True(decision.Allowed);
    }

    [Fact]
    public async Task Suspended_BlocksNonWriteBlockedCatalogueFeatureToo()
    {
        // Suspended denies *everything* except the always-allowed set — unlike Restricted, which
        // only blocks writes and otherwise evaluates the plan normally.
        var decision = await AuthorizeUnderStatus(SubscriptionStatus.Suspended, FeatureCodes.StandardReporting);

        Assert.False(decision.Allowed);
        Assert.Equal(EntitlementDenialReason.SubscriptionSuspended, decision.Reason);
    }

    [Fact]
    public async Task Restricted_AllowsNonWriteBlockedCatalogueFeatureViaNormalPlanEvaluation()
    {
        var decision = await AuthorizeUnderStatus(SubscriptionStatus.Restricted, FeatureCodes.StandardReporting);

        Assert.True(decision.Allowed);
    }

    private static async Task<EntitlementDecision> AuthorizeUnderStatus(SubscriptionStatus status, string featureCode)
    {
        using var harness = new EntitlementTestHarness();
        await harness.SeedCatalogueAsync();

        await using var context = harness.CreateContext();
        var plan = context.SubscriptionPlans.Single(p => p.Code == PlanCodes.Growing);
        var stokvel = TestData.CreateStokvel(context);
        TestData.CreateOrganisationSubscription(context, stokvel, plan.Id, status);
        await context.SaveChangesAsync();

        return await harness.Sut.AuthorizeAsync(stokvel.Id, featureCode);
    }
}
