using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Enums;
using Sisonke.Web.Services.Entitlements;
using Sisonke.Web.Tests.TestSupport;

namespace Sisonke.Web.Tests.Billing;

public class TrialLifecycleAndEntitlementTests
{
    [Fact]
    public async Task ExplicitActivation_StartsExactlySixtyDays_AndPersistsConsent()
    {
        using var harness = new SubscriptionServiceTestHarness();
        await harness.Entitlements.SeedCatalogueAsync();
        var (stokvelId, adminId) = await SeedLegacyStokvelAsync(harness);
        var startedAt = harness.TimeProvider.GetUtcNow();

        Assert.True((await harness.Sut.SelectPlanAsync(stokvelId, PlanCodes.Growing, "selection-v1", adminId)).Success);
        var result = await harness.Sut.ActivateTrialAsync(stokvelId, BillingTermsVersion.Current, adminId, "127.0.0.1");

        Assert.True(result.Success);
        await using var context = harness.Entitlements.CreateContext();
        var subscription = await context.OrganisationSubscriptions.SingleAsync(s => s.StokvelId == stokvelId);
        Assert.True(subscription.TrialOptedIn);
        Assert.Equal(startedAt.UtcDateTime, subscription.TrialStartedAt);
        Assert.Equal(startedAt.UtcDateTime.AddDays(60), subscription.TrialEndsAt);
        Assert.Equal(BillingTermsVersion.Current, subscription.TermsVersion);
        Assert.Equal(startedAt.UtcDateTime, subscription.TermsAcceptedAt);
        Assert.Equal(adminId, subscription.TermsAcceptedByUserId);
        Assert.Equal(SubscriptionStatus.Trialing, subscription.Status);
        Assert.Null(subscription.Provider);
    }

    [Fact]
    public async Task RepeatedActivation_IsIdempotent_AndDoesNotExtendTrial()
    {
        using var harness = new SubscriptionServiceTestHarness();
        await harness.Entitlements.SeedCatalogueAsync();
        var (stokvelId, adminId) = await SeedLegacyStokvelAsync(harness);
        await harness.Sut.SelectPlanAsync(stokvelId, PlanCodes.Growing, BillingTermsVersion.Current, adminId);
        await harness.Sut.ActivateTrialAsync(stokvelId, BillingTermsVersion.Current, adminId);

        DateTime firstEnd;
        await using (var context = harness.Entitlements.CreateContext())
        {
            firstEnd = (await context.OrganisationSubscriptions.SingleAsync(s => s.StokvelId == stokvelId)).TrialEndsAt!.Value;
        }

        harness.TimeProvider.Advance(TimeSpan.FromDays(10));
        var second = await harness.Sut.ActivateTrialAsync(stokvelId, BillingTermsVersion.Current, adminId);

        Assert.True(second.Success);
        await using var verify = harness.Entitlements.CreateContext();
        Assert.Equal(firstEnd, (await verify.OrganisationSubscriptions.SingleAsync(s => s.StokvelId == stokvelId)).TrialEndsAt);
    }

    [Fact]
    public async Task ActiveTrial_GetsSelectedPlanFeatures_ThenExpiryRemovesThem()
    {
        using var harness = new SubscriptionServiceTestHarness();
        await harness.Entitlements.SeedCatalogueAsync();
        var (stokvelId, adminId) = await SeedLegacyStokvelAsync(harness);
        await harness.Sut.SelectPlanAsync(stokvelId, PlanCodes.Growing, BillingTermsVersion.Current, adminId);
        await harness.Sut.ActivateTrialAsync(stokvelId, BillingTermsVersion.Current, adminId);

        var duringTrial = await harness.Entitlements.Sut.AuthorizeAsync(stokvelId, FeatureCodes.RotationalStokvel);
        var snapshot = await harness.Entitlements.Sut.GetSnapshotAsync(stokvelId);
        Assert.True(duringTrial.Allowed);
        Assert.Equal(SubscriptionAccessState.TrialActive, snapshot.AccessState);
        Assert.True(snapshot.IsTrial);
        Assert.True(snapshot.CanUsePaidFeatures);
        Assert.Equal(60, snapshot.TrialDaysRemaining);

        harness.TimeProvider.Advance(TimeSpan.FromDays(60));
        var expired = await harness.Entitlements.Sut.AuthorizeAsync(stokvelId, FeatureCodes.RotationalStokvel);
        var expiredSnapshot = await harness.Entitlements.Sut.GetSnapshotAsync(stokvelId);
        Assert.False(expired.Allowed);
        Assert.Equal(EntitlementDenialReason.TrialExpired, expired.Reason);
        Assert.Equal(SubscriptionAccessState.TrialExpired, expiredSnapshot.AccessState);
        Assert.False(expiredSnapshot.CanUsePaidFeatures);
        Assert.Equal(0, expiredSnapshot.TrialDaysRemaining);
        Assert.False(expiredSnapshot.Features[FeatureCodes.RotationalStokvel].IsEnabled);
    }

    [Fact]
    public async Task TrialDatesWithoutExplicitOptIn_DoNotGrantPlanFeatures()
    {
        using var harness = new EntitlementTestHarness();
        await harness.SeedCatalogueAsync();
        await using var context = harness.CreateContext();
        var plan = await context.SubscriptionPlans.SingleAsync(p => p.Code == PlanCodes.Growing);
        var stokvel = TestData.CreateStokvel(context);
        var subscription = TestData.CreateOrganisationSubscription(context, stokvel, plan.Id, SubscriptionStatus.Trialing);
        subscription.TrialStartedAt = harness.TimeProvider.GetUtcNow().UtcDateTime;
        subscription.TrialEndsAt = subscription.TrialStartedAt.Value.AddDays(60);
        subscription.TrialOptedIn = false;
        await context.SaveChangesAsync();

        var result = await harness.Sut.AuthorizeAsync(stokvel.Id, FeatureCodes.RotationalStokvel);
        Assert.False(result.Allowed);
        Assert.Equal(EntitlementDenialReason.TrialExpired, result.Reason);
    }

    private static async Task<(Guid StokvelId, string AdminId)> SeedLegacyStokvelAsync(SubscriptionServiceTestHarness harness)
    {
        await using var context = harness.Entitlements.CreateContext();
        var stokvel = TestData.CreateStokvel(context);
        var adminId = $"admin-{Guid.NewGuid():N}";
        var admin = TestData.CreateStokvelMember(context, stokvel, role: SisonkeRole.StokvelAdmin);
        admin.ApplicationUserId = adminId;
        TestData.CreateOrganisationSubscription(context, stokvel, null, SubscriptionStatus.LegacyUnsubscribed);
        await context.SaveChangesAsync();
        return (stokvel.Id, adminId);
    }
}
