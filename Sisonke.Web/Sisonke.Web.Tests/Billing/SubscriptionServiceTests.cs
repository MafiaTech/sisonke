using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Enums;
using Sisonke.Web.Services.Billing;
using Sisonke.Web.Tests.TestSupport;

namespace Sisonke.Web.Tests.Billing;

public class SubscriptionServiceTests
{
    [Fact]
    public async Task SelectPlanAsync_NonOfficeBearer_IsRejected()
    {
        using var harness = new SubscriptionServiceTestHarness();
        await harness.Entitlements.SeedCatalogueAsync();
        var (stokvelId, _, ordinaryUserId) = await SeedStokvelWithMembersAsync(harness);

        var result = await harness.Sut.SelectPlanAsync(stokvelId, PlanCodes.Growing, "v1", ordinaryUserId);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task SelectPlanAsync_OfficeBearer_SetsPendingPaymentMethod_AndDoesNotStartTrial()
    {
        using var harness = new SubscriptionServiceTestHarness();
        await harness.Entitlements.SeedCatalogueAsync();
        var (stokvelId, adminUserId, _) = await SeedStokvelWithMembersAsync(harness);

        var result = await harness.Sut.SelectPlanAsync(stokvelId, PlanCodes.Growing, "v1", adminUserId);

        Assert.True(result.Success);

        await using var context = harness.Entitlements.CreateContext();
        var subscription = await context.OrganisationSubscriptions.SingleAsync(s => s.StokvelId == stokvelId);
        Assert.Equal(SubscriptionStatus.PendingPaymentMethod, subscription.Status);
        Assert.Null(subscription.TrialStartedAt);
        Assert.Equal("v1", subscription.TermsVersion);
    }

    [Fact]
    public async Task CompleteCardAuthorisationAsync_NonReusableAuthorisation_IsRejected()
    {
        using var harness = new SubscriptionServiceTestHarness();
        await harness.Entitlements.SeedCatalogueAsync();
        var (stokvelId, adminUserId, _) = await SeedStokvelWithMembersAsync(harness);
        await harness.Sut.SelectPlanAsync(stokvelId, PlanCodes.Growing, "v1", adminUserId);
        await harness.Sut.BeginCardAuthorisationAsync(stokvelId, "chair@example.com", "Jane Chair", null, "https://app.sisonke/callback");

        harness.BillingProvider.VerificationToReturn = harness.BillingProvider.VerificationToReturn with { Reusable = false };

        var result = await harness.Sut.CompleteCardAuthorisationAsync(stokvelId, "ref-1");

        Assert.False(result.Success);
        Assert.Contains("recurring", result.Message, StringComparison.OrdinalIgnoreCase);

        await using var context = harness.Entitlements.CreateContext();
        var subscription = await context.OrganisationSubscriptions.SingleAsync(s => s.StokvelId == stokvelId);
        Assert.Equal(SubscriptionStatus.PendingPaymentMethod, subscription.Status); // never advanced to Trialing
        Assert.Empty(await context.SubscriptionPaymentMethods.ToListAsync());
    }

    [Fact]
    public async Task CompleteCardAuthorisationAsync_ReusableAuthorisation_StartsTrialAndRefundsVerificationCharge()
    {
        using var harness = new SubscriptionServiceTestHarness();
        await harness.Entitlements.SeedCatalogueAsync();
        var (stokvelId, adminUserId, _) = await SeedStokvelWithMembersAsync(harness);
        await harness.Sut.SelectPlanAsync(stokvelId, PlanCodes.Growing, "v1", adminUserId);
        await harness.Sut.BeginCardAuthorisationAsync(stokvelId, "chair@example.com", "Jane Chair", null, "https://app.sisonke/callback");

        var result = await harness.Sut.CompleteCardAuthorisationAsync(stokvelId, "ref-1");

        Assert.True(result.Success);
        Assert.Contains("ref-1", harness.BillingProvider.RefundedReferences);

        await using var context = harness.Entitlements.CreateContext();
        var subscription = await context.OrganisationSubscriptions.SingleAsync(s => s.StokvelId == stokvelId);
        Assert.Equal(SubscriptionStatus.Trialing, subscription.Status);
        Assert.NotNull(subscription.TrialStartedAt);
        Assert.NotNull(subscription.TrialEndsAt);
        Assert.Equal(subscription.TrialEndsAt, subscription.NextBillingAt);
        Assert.Single(await context.SubscriptionPaymentMethods.ToListAsync());
    }

    [Fact]
    public async Task ChangePlanAsync_DowngradeOverCapacity_IsBlockedWithSpecificMessage()
    {
        using var harness = new SubscriptionServiceTestHarness();
        await harness.Entitlements.SeedCatalogueAsync();
        var (stokvelId, adminUserId, _) = await SeedStokvelWithMembersAsync(harness);

        await using (var context = harness.Entitlements.CreateContext())
        {
            var professionalPlan = await context.SubscriptionPlans.SingleAsync(p => p.Code == PlanCodes.Professional);
            var stokvel = await context.Stokvels.SingleAsync(s => s.Id == stokvelId);
            var subscription = await context.OrganisationSubscriptions.SingleAsync(s => s.StokvelId == stokvelId);
            subscription.SubscriptionPlanId = professionalPlan.Id;
            subscription.Status = SubscriptionStatus.Active;

            // SeedStokvelWithMembersAsync already created 2 active members (admin + ordinary);
            // 140 more brings the total to exactly 142. Professional allows up to 300 members;
            // Growing (the downgrade target) allows 100.
            for (var i = 0; i < 140; i++)
            {
                TestData.CreateStokvelMember(context, stokvel);
            }

            await context.SaveChangesAsync();
        }

        var result = await harness.Sut.ChangePlanAsync(stokvelId, PlanCodes.Growing, adminUserId);

        Assert.False(result.Success);
        Assert.Contains("142", result.Message);
        Assert.Contains("active members", result.Message);
        Assert.Contains("100", result.Message);

        await using var verifyContext = harness.Entitlements.CreateContext();
        var subscriptionAfter = await verifyContext.OrganisationSubscriptions
            .Include(s => s.SubscriptionPlan)
            .SingleAsync(s => s.StokvelId == stokvelId);
        Assert.Equal(PlanCodes.Professional, subscriptionAfter.SubscriptionPlan!.Code); // unchanged
    }

    [Fact]
    public async Task ChangePlanAsync_DowngradeWithinCapacity_IsScheduledForPeriodEnd_NotImmediate()
    {
        using var harness = new SubscriptionServiceTestHarness();
        await harness.Entitlements.SeedCatalogueAsync();
        var (stokvelId, adminUserId, _) = await SeedStokvelWithMembersAsync(harness);

        await using (var context = harness.Entitlements.CreateContext())
        {
            var professionalPlan = await context.SubscriptionPlans.SingleAsync(p => p.Code == PlanCodes.Professional);
            var subscription = await context.OrganisationSubscriptions.SingleAsync(s => s.StokvelId == stokvelId);
            subscription.SubscriptionPlanId = professionalPlan.Id;
            subscription.Status = SubscriptionStatus.Active;
            subscription.CurrentPeriodEndsAt = DateTime.UtcNow.AddDays(10);
            await context.SaveChangesAsync();
        }

        var result = await harness.Sut.ChangePlanAsync(stokvelId, PlanCodes.Growing, adminUserId);

        Assert.True(result.Success);

        await using var verifyContext = harness.Entitlements.CreateContext();
        var subscriptionAfter = await verifyContext.OrganisationSubscriptions
            .Include(s => s.SubscriptionPlan)
            .Include(s => s.PendingPlanChangePlan)
            .SingleAsync(s => s.StokvelId == stokvelId);

        Assert.Equal(PlanCodes.Professional, subscriptionAfter.SubscriptionPlan!.Code); // not changed yet
        Assert.Equal(PlanCodes.Growing, subscriptionAfter.PendingPlanChangePlan!.Code);
        Assert.NotNull(subscriptionAfter.PendingPlanChangeEffectiveAt);
    }

    [Fact]
    public async Task ChangePlanAsync_Upgrade_TakesEffectImmediately()
    {
        using var harness = new SubscriptionServiceTestHarness();
        await harness.Entitlements.SeedCatalogueAsync();
        var (stokvelId, adminUserId, _) = await SeedStokvelWithMembersAsync(harness);

        await using (var context = harness.Entitlements.CreateContext())
        {
            var starterPlan = await context.SubscriptionPlans.SingleAsync(p => p.Code == PlanCodes.Starter);
            var subscription = await context.OrganisationSubscriptions.SingleAsync(s => s.StokvelId == stokvelId);
            subscription.SubscriptionPlanId = starterPlan.Id;
            subscription.Status = SubscriptionStatus.Active;
            await context.SaveChangesAsync();
        }

        var result = await harness.Sut.ChangePlanAsync(stokvelId, PlanCodes.Growing, adminUserId);

        Assert.True(result.Success);

        await using var verifyContext = harness.Entitlements.CreateContext();
        var subscriptionAfter = await verifyContext.OrganisationSubscriptions
            .Include(s => s.SubscriptionPlan)
            .SingleAsync(s => s.StokvelId == stokvelId);
        Assert.Equal(PlanCodes.Growing, subscriptionAfter.SubscriptionPlan!.Code);
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
}
