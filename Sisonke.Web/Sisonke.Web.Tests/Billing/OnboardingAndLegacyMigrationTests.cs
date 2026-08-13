using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Enums;
using Sisonke.Web.Services.Billing;
using Sisonke.Web.Tests.TestSupport;

namespace Sisonke.Web.Tests.Billing;

/// <summary>
/// Phase 5 brief's required test scenarios that weren't already covered by Phase 3/4's
/// SubscriptionServiceTests.cs: the declined-card path leaving no trial dates set, and the
/// guarantee that a legacy (or brand-new) organisation cannot reach Trialing by any path other
/// than the explicit SelectPlanAsync -> BeginCardAuthorisationAsync -> CompleteCardAuthorisationAsync
/// sequence, asserted at the service/state-machine layer rather than only in the UI.
/// </summary>
public class OnboardingAndLegacyMigrationTests
{
    [Fact]
    public async Task CompleteCardAuthorisationAsync_DeclinedVerification_LeavesPendingPaymentMethod_NoTrialDatesSet()
    {
        using var harness = new SubscriptionServiceTestHarness();
        await harness.Entitlements.SeedCatalogueAsync();
        var (stokvelId, adminUserId, _) = await SeedStokvelWithMembersAsync(harness);

        await harness.Sut.SelectPlanAsync(stokvelId, PlanCodes.Growing, "v1", adminUserId);
        await harness.Sut.BeginCardAuthorisationAsync(stokvelId, "chair@example.com", "Jane Chair", null, "https://app.sisonke/callback");

        harness.BillingProvider.VerificationToReturn = harness.BillingProvider.VerificationToReturn with { Success = false };

        var result = await harness.Sut.CompleteCardAuthorisationAsync(stokvelId, "ref-declined");

        Assert.False(result.Success);

        await using var context = harness.Entitlements.CreateContext();
        var subscription = await context.OrganisationSubscriptions.SingleAsync(s => s.StokvelId == stokvelId);
        Assert.Equal(SubscriptionStatus.PendingPaymentMethod, subscription.Status);
        Assert.Null(subscription.TrialStartedAt);
        Assert.Null(subscription.TrialEndsAt);
        Assert.False(subscription.TrialOptedIn);
        Assert.Null(subscription.ProviderSubscriptionCode);
        Assert.Empty(await context.SubscriptionPaymentMethods.ToListAsync());
    }

    [Fact]
    public async Task LegacyOrganisation_NonOfficeBearer_CannotSelectPlan_StaysLegacyUnsubscribed()
    {
        using var harness = new SubscriptionServiceTestHarness();
        await harness.Entitlements.SeedCatalogueAsync();
        var (stokvelId, _, ordinaryMemberUserId) = await SeedLegacyStokvelAsync(harness);

        var result = await harness.Sut.SelectPlanAsync(stokvelId, PlanCodes.Growing, "v1", ordinaryMemberUserId);

        Assert.False(result.Success);

        await using var context = harness.Entitlements.CreateContext();
        var subscription = await context.OrganisationSubscriptions.SingleAsync(s => s.StokvelId == stokvelId);
        Assert.Equal(SubscriptionStatus.LegacyUnsubscribed, subscription.Status);
    }

    [Fact]
    public async Task LegacyOrganisation_OfficeBearer_CompletesFullFlow_ReachesTrialing()
    {
        // This is the legacy-migration opt-in flow (Phase 5 brief §C) — SelectPlanAsync already
        // transparently handles a subscription starting in LegacyUnsubscribed, so the UI's
        // "opt in" button is just an entry point into the same sequence onboarding uses.
        using var harness = new SubscriptionServiceTestHarness();
        await harness.Entitlements.SeedCatalogueAsync();
        var (stokvelId, officeBearerUserId, _) = await SeedLegacyStokvelAsync(harness);

        var selectResult = await harness.Sut.SelectPlanAsync(stokvelId, PlanCodes.Growing, "v1", officeBearerUserId, "196.10.10.5");
        Assert.True(selectResult.Success);

        var beginResult = await harness.Sut.BeginCardAuthorisationAsync(
            stokvelId, "chair@example.com", "Jane Chair", null, "https://app.sisonke/callback");
        Assert.True(beginResult.Success);

        var completeResult = await harness.Sut.CompleteCardAuthorisationAsync(stokvelId, "ref-1");
        Assert.True(completeResult.Success);

        await using var context = harness.Entitlements.CreateContext();
        var subscription = await context.OrganisationSubscriptions.SingleAsync(s => s.StokvelId == stokvelId);
        Assert.Equal(SubscriptionStatus.Trialing, subscription.Status);
        Assert.True(subscription.TrialOptedIn);
        Assert.NotNull(subscription.TrialStartedAt);
        Assert.Equal(subscription.TrialStartedAt!.Value.AddDays(60), subscription.TrialEndsAt);
        Assert.NotNull(subscription.ProviderSubscriptionCode);
        Assert.Equal("v1", subscription.TermsVersion);
        Assert.NotNull(subscription.TermsAcceptedAt);
        Assert.Equal(officeBearerUserId, subscription.TermsAcceptedByUserId);
        Assert.Equal("196.10.10.5", subscription.TermsAcceptedIpAddress);
    }

    [Fact]
    public void StateMachine_CannotTransitionDirectlyFromLegacyUnsubscribedToTrialing()
    {
        // Structural guarantee, independent of any UI: the only route out of LegacyUnsubscribed
        // is through PendingPaymentMethod (i.e. an explicit SelectPlanAsync call), never straight
        // to Trialing. No path — manual DB edit aside — can skip authorisation.
        using var entitlements = new EntitlementTestHarness();
        var stateMachine = new SubscriptionStateMachine(
            entitlements.Sut, new Microsoft.Extensions.Time.Testing.FakeTimeProvider(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<SubscriptionStateMachine>.Instance);

        Assert.False(stateMachine.CanTransition(SubscriptionStatus.LegacyUnsubscribed, SubscriptionStatus.Trialing));
        Assert.True(stateMachine.CanTransition(SubscriptionStatus.LegacyUnsubscribed, SubscriptionStatus.PendingPaymentMethod));
        Assert.True(stateMachine.CanTransition(SubscriptionStatus.PendingPaymentMethod, SubscriptionStatus.Trialing));
    }

    private static async Task<(Guid StokvelId, string AdminUserId, string OrdinaryUserId)> SeedStokvelWithMembersAsync(SubscriptionServiceTestHarness harness)
    {
        await using var context = harness.Entitlements.CreateContext();
        var stokvel = TestData.CreateStokvel(context);

        var adminUserId = $"admin-{Guid.NewGuid():N}";
        var admin = TestData.CreateStokvelMember(context, stokvel, role: SisonkeRole.StokvelAdmin);
        admin.ApplicationUserId = adminUserId;

        var ordinaryUserId = $"member-{Guid.NewGuid():N}";
        var ordinary = TestData.CreateStokvelMember(context, stokvel, role: SisonkeRole.Member);
        ordinary.ApplicationUserId = ordinaryUserId;

        TestData.CreateOrganisationSubscription(context, stokvel, null, SubscriptionStatus.LegacyUnsubscribed);

        await context.SaveChangesAsync();
        return (stokvel.Id, adminUserId, ordinaryUserId);
    }

    /// <summary>A pre-existing organisation sitting in LegacyUnsubscribed — same shape as SeedStokvelWithMembersAsync, named to make legacy-migration tests read clearly.</summary>
    private static Task<(Guid StokvelId, string OfficeBearerUserId, string OrdinaryUserId)> SeedLegacyStokvelAsync(SubscriptionServiceTestHarness harness) =>
        SeedStokvelWithMembersAsync(harness);
}
