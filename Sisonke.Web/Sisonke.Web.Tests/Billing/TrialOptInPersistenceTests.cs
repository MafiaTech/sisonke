using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Enums;
using Sisonke.Web.Tests.TestSupport;

namespace Sisonke.Web.Tests.Billing;

public class TrialOptInPersistenceTests
{
    [Fact]
    public async Task TrialDatesAlone_DoNotRepresentExplicitOptIn()
    {
        using var db = new SqliteTestDatabase();
        await using var context = db.CreateContext();
        var stokvel = TestData.CreateStokvel(context);
        var subscription = TestData.CreateOrganisationSubscription(context, stokvel, null, SubscriptionStatus.Trialing);
        subscription.TrialStartedAt = DateTime.UtcNow;
        subscription.TrialEndsAt = subscription.TrialStartedAt.Value.AddDays(60);

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var saved = await context.OrganisationSubscriptions.SingleAsync(s => s.Id == subscription.Id);
        Assert.False(saved.TrialOptedIn);
        Assert.NotNull(saved.TrialStartedAt);
        Assert.NotNull(saved.TrialEndsAt);
    }

    [Fact]
    public async Task CompletedAuthorisation_PersistsExplicitTrialAndVersionedConsentEvidence()
    {
        using var harness = new SubscriptionServiceTestHarness();
        await harness.Entitlements.SeedCatalogueAsync();

        Guid stokvelId;
        string officeBearerUserId;
        await using (var context = harness.Entitlements.CreateContext())
        {
            var stokvel = TestData.CreateStokvel(context);
            stokvelId = stokvel.Id;
            officeBearerUserId = $"admin-{Guid.NewGuid():N}";
            var admin = TestData.CreateStokvelMember(context, stokvel, role: SisonkeRole.StokvelAdmin);
            admin.ApplicationUserId = officeBearerUserId;
            TestData.CreateOrganisationSubscription(context, stokvel, null, SubscriptionStatus.LegacyUnsubscribed);
            await context.SaveChangesAsync();
        }

        var select = await harness.Sut.SelectPlanAsync(
            stokvelId, PlanCodes.Growing, BillingTermsVersion.Current, officeBearerUserId, "127.0.0.1");
        Assert.True(select.Success);

        await using (var pendingContext = harness.Entitlements.CreateContext())
        {
            var pending = await pendingContext.OrganisationSubscriptions.SingleAsync(s => s.StokvelId == stokvelId);
            Assert.False(pending.TrialOptedIn);
            Assert.NotNull(pending.TermsAcceptedAt);
        }

        var begin = await harness.Sut.BeginCardAuthorisationAsync(
            stokvelId, "billing@example.com", "Billing Admin", null, "https://app.sisonke/callback");
        Assert.True(begin.Success);
        var complete = await harness.Sut.CompleteCardAuthorisationAsync(stokvelId, "trial-opt-in-reference");
        Assert.True(complete.Success);

        await using var savedContext = harness.Entitlements.CreateContext();
        var saved = await savedContext.OrganisationSubscriptions.SingleAsync(s => s.StokvelId == stokvelId);
        Assert.True(saved.TrialOptedIn);
        Assert.NotNull(saved.TrialStartedAt);
        Assert.Equal(saved.TrialStartedAt!.Value.AddDays(60), saved.TrialEndsAt);
        Assert.Equal(BillingTermsVersion.Current, saved.TermsVersion);
        Assert.NotNull(saved.TermsAcceptedAt);
        Assert.Equal(officeBearerUserId, saved.TermsAcceptedByUserId);
        Assert.Equal("127.0.0.1", saved.TermsAcceptedIpAddress);
    }
}
