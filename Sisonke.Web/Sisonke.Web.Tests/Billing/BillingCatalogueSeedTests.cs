using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Enums;
using Sisonke.Web.Data.Seed;
using Sisonke.Web.Tests.TestSupport;

namespace Sisonke.Web.Tests.Billing;

public class BillingCatalogueSeedTests
{
    [Fact]
    public async Task EnsureBillingCatalogueAsync_RunTwice_ProducesIdenticalState()
    {
        using var db = new SqliteTestDatabase();
        await using var context = db.CreateContext();

        await BillingCatalogueSeed.EnsureBillingCatalogueAsync(context);
        var plansAfterFirstRun = await Snapshot(context);

        await using var secondContext = db.CreateContext();
        await BillingCatalogueSeed.EnsureBillingCatalogueAsync(secondContext);
        var plansAfterSecondRun = await Snapshot(secondContext);

        Assert.Equal(plansAfterFirstRun.PlanCount, plansAfterSecondRun.PlanCount);
        Assert.Equal(plansAfterFirstRun.FeatureCount, plansAfterSecondRun.FeatureCount);
        Assert.Equal(plansAfterFirstRun.PlanFeatureCount, plansAfterSecondRun.PlanFeatureCount);
        Assert.Equal(plansAfterFirstRun.TrialCount, plansAfterSecondRun.TrialCount);
        Assert.Equal(4, plansAfterSecondRun.PlanCount);
    }

    [Fact]
    public async Task EnsureBillingCatalogueAsync_Professional_HasFullOnlinePaymentsAndAutomatedReconciliation()
    {
        using var db = new SqliteTestDatabase();
        await using var context = db.CreateContext();
        await BillingCatalogueSeed.EnsureBillingCatalogueAsync(context);

        var professionalPlan = await context.SubscriptionPlans.SingleAsync(p => p.Code == PlanCodes.Professional);

        var maxMembers = await GetPlanFeature(context, professionalPlan.Id, FeatureCodes.MaxMembers);
        var onlinePayments = await GetPlanFeature(context, professionalPlan.Id, FeatureCodes.OnlineMemberPayments);
        var automatedReconciliation = await GetPlanFeature(context, professionalPlan.Id, FeatureCodes.AutomatedReconciliation);

        Assert.Equal(300, maxMembers.LimitValue);
        Assert.Contains("Full", onlinePayments.ConfigurationJson);
        Assert.True(automatedReconciliation.IsEnabled);
    }

    [Fact]
    public async Task EnsureBillingCatalogueAsync_Starter_HasNoOnlinePaymentsAndMaxMembers30()
    {
        using var db = new SqliteTestDatabase();
        await using var context = db.CreateContext();
        await BillingCatalogueSeed.EnsureBillingCatalogueAsync(context);

        var starterPlan = await context.SubscriptionPlans.SingleAsync(p => p.Code == PlanCodes.Starter);

        var maxMembers = await GetPlanFeature(context, starterPlan.Id, FeatureCodes.MaxMembers);
        var onlinePayments = await GetPlanFeature(context, starterPlan.Id, FeatureCodes.OnlineMemberPayments);

        Assert.Equal(30, maxMembers.LimitValue);
        Assert.Contains("None", onlinePayments.ConfigurationJson);
    }

    [Fact]
    public async Task EnsureLegacyOrganisationSubscriptionsAsync_ExistingStokvel_GetsLegacyUnsubscribedRow()
    {
        using var db = new SqliteTestDatabase();
        await using var context = db.CreateContext();
        var stokvel = TestData.CreateStokvel(context);
        await context.SaveChangesAsync();

        await BillingCatalogueSeed.EnsureLegacyOrganisationSubscriptionsAsync(context);

        var subscription = await context.OrganisationSubscriptions.SingleAsync(s => s.StokvelId == stokvel.Id);
        Assert.Equal(SubscriptionStatus.LegacyUnsubscribed, subscription.Status);
        Assert.Null(subscription.SubscriptionPlanId);
        Assert.Null(subscription.TrialStartedAt);
    }

    [Fact]
    public async Task EnsureLegacyOrganisationSubscriptionsAsync_RunTwice_NeverOverwritesExistingSubscription()
    {
        using var db = new SqliteTestDatabase();
        await using var context = db.CreateContext();
        var stokvel = TestData.CreateStokvel(context);
        await context.SaveChangesAsync();

        await BillingCatalogueSeed.EnsureLegacyOrganisationSubscriptionsAsync(context);
        var subscription = await context.OrganisationSubscriptions.SingleAsync(s => s.StokvelId == stokvel.Id);

        // Simulate real progress: the stokvel has since picked a plan and is now trialing.
        await BillingCatalogueSeed.EnsureBillingCatalogueAsync(context);
        var growingPlan = await context.SubscriptionPlans.SingleAsync(p => p.Code == PlanCodes.Growing);
        subscription.Status = SubscriptionStatus.Trialing;
        subscription.SubscriptionPlanId = growingPlan.Id;
        subscription.TrialStartedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        await BillingCatalogueSeed.EnsureLegacyOrganisationSubscriptionsAsync(context);

        var subscriptionsForStokvel = await context.OrganisationSubscriptions
            .Where(s => s.StokvelId == stokvel.Id)
            .ToListAsync();
        var onlySubscription = Assert.Single(subscriptionsForStokvel);
        Assert.Equal(SubscriptionStatus.Trialing, onlySubscription.Status);
    }

    private static async Task<(int PlanCount, int FeatureCount, int PlanFeatureCount, int TrialCount)> Snapshot(
        ApplicationDbContext context)
    {
        return (
            await context.SubscriptionPlans.CountAsync(p => p.Code != null),
            await context.FeatureDefinitions.CountAsync(),
            await context.PlanFeatures.CountAsync(),
            await context.PromotionalTrials.CountAsync());
    }

    private static async Task<Sisonke.Web.Data.Entities.PlanFeature> GetPlanFeature(
        ApplicationDbContext context, Guid planId, string featureCode)
    {
        return await context.PlanFeatures
            .Include(pf => pf.FeatureDefinition)
            .SingleAsync(pf => pf.SubscriptionPlanId == planId && pf.FeatureDefinition.Code == featureCode);
    }
}
