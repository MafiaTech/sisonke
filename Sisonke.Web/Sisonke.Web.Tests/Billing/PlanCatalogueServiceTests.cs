using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;
using Sisonke.Web.Services.Billing;

namespace Sisonke.Web.Tests.Billing;

public class PlanCatalogueServiceTests
{
    [Fact]
    public async Task GetPublicPlansAsync_ReturnsOnlyCodeBasedActivePlans_OrderedByDisplayOrder()
    {
        using var harness = new EntitlementTestHarness();
        await harness.SeedCatalogueAsync();
        var sut = new PlanCatalogueService(harness.DbFactory);

        var entries = await sut.GetPublicPlansAsync();

        Assert.Equal(4, entries.Count);
        Assert.All(entries, e => Assert.NotNull(e.Plan.Code));
        Assert.Equal(
            [PlanCodes.Starter, PlanCodes.Growing, PlanCodes.Professional, PlanCodes.Enterprise],
            entries.Select(e => e.Plan.Code));
    }

    [Fact]
    public async Task GetPublicPlansAsync_ReflectsNewlySeededFeature_WithNoCodeChange()
    {
        // Proves the Phase 5 brief requirement: "adding a feature to the catalogue must change
        // the pricing page automatically" — this seeds a brand-new FeatureDefinition + PlanFeature
        // row directly (simulating a catalogue update) and asserts PlanCatalogueService picks it
        // up purely from the database, with no switch/if-list anywhere in the service.
        using var harness = new EntitlementTestHarness();
        await harness.SeedCatalogueAsync();
        var sut = new PlanCatalogueService(harness.DbFactory);

        await using (var context = harness.CreateContext())
        {
            var professionalPlan = await context.SubscriptionPlans.SingleAsync(p => p.Code == PlanCodes.Professional);

            var newFeature = new FeatureDefinition
            {
                Id = Guid.NewGuid(),
                Code = "TEST_NEW_FEATURE",
                Name = "Brand New Feature",
                Category = "Test",
                DataType = FeatureDataType.Boolean,
                DefaultValue = "false",
                IsActive = true
            };
            context.FeatureDefinitions.Add(newFeature);

            context.PlanFeatures.Add(new PlanFeature
            {
                Id = Guid.NewGuid(),
                SubscriptionPlanId = professionalPlan.Id,
                FeatureDefinitionId = newFeature.Id,
                IsEnabled = true
            });

            await context.SaveChangesAsync();
        }

        var entries = await sut.GetPublicPlansAsync();

        var professionalEntry = entries.Single(e => e.Plan.Code == PlanCodes.Professional);
        Assert.Contains(professionalEntry.Features, f => f.Code == "TEST_NEW_FEATURE" && f.IsEnabled);

        // Every other plan legitimately has no row for this brand-new feature yet.
        var starterEntry = entries.Single(e => e.Plan.Code == PlanCodes.Starter);
        Assert.DoesNotContain(starterEntry.Features, f => f.Code == "TEST_NEW_FEATURE");
    }

    [Fact]
    public async Task RecommendPlanForMemberCountAsync_PicksSmallestPlanThatFits()
    {
        using var harness = new EntitlementTestHarness();
        await harness.SeedCatalogueAsync();
        var sut = new PlanCatalogueService(harness.DbFactory);

        var recommended = await sut.RecommendPlanForMemberCountAsync(50);

        Assert.Equal(PlanCodes.Growing, recommended!.Code); // Starter caps at 30, Growing at 100
    }
}
