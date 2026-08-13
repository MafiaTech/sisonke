using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Enums;
using Sisonke.Web.Services.Entitlements;
using Sisonke.Web.Tests.TestSupport;

namespace Sisonke.Web.Tests.Billing;

public class LockedFeatureDefinitionTests
{
    [Fact]
    public async Task Starter_PermitsBasicClaimAction_ButProtectsFullWorkflowAction()
    {
        using var harness = new EntitlementTestHarness();
        var stokvelId = await SeedSubscriptionAsync(harness, PlanCodes.Starter, SubscriptionStatus.Active);

        var basic = await harness.Sut.AuthorizeAsync(stokvelId, FeatureCodes.ClaimsBasic);
        var workflow = await harness.Sut.AuthorizeAsync(stokvelId, FeatureCodes.ClaimsFullWorkflow);

        Assert.True(basic.Allowed);
        Assert.False(workflow.Allowed);
        Assert.Equal(EntitlementDenialReason.FeatureNotInPlan, workflow.Reason);
    }

    [Fact]
    public async Task Growing_CanProcessConfiguredApprovals_WithoutWorkflowConfigurationEntitlement()
    {
        using var harness = new EntitlementTestHarness();
        var stokvelId = await SeedSubscriptionAsync(harness, PlanCodes.Growing, SubscriptionStatus.Active);

        var ordinaryProcessing = await harness.Sut.AuthorizeAsync(stokvelId, FeatureCodes.ClaimsFullWorkflow);
        var workflowConfiguration = await harness.Sut.AuthorizeAsync(stokvelId, FeatureCodes.ConfigurableApprovalWorkflows);

        Assert.True(ordinaryProcessing.Allowed);
        Assert.False(workflowConfiguration.Allowed);
        Assert.Equal(EntitlementDenialReason.FeatureNotInPlan, workflowConfiguration.Reason);
    }

    [Fact]
    public async Task Professional_PermitsWorkflowConfiguration()
    {
        using var harness = new EntitlementTestHarness();
        var stokvelId = await SeedSubscriptionAsync(harness, PlanCodes.Professional, SubscriptionStatus.Active);

        Assert.True(await harness.Sut.HasFeatureAsync(stokvelId, FeatureCodes.ConfigurableApprovalWorkflows));
    }

    [Fact]
    public async Task PastDue_WithinSevenDayGrace_RetainsPlanAccess()
    {
        using var harness = new EntitlementTestHarness();
        var stokvelId = await SeedSubscriptionAsync(
            harness, PlanCodes.Growing, SubscriptionStatus.PastDue,
            graceEndsAt: harness.TimeProvider.GetUtcNow().UtcDateTime.AddDays(7));

        var snapshot = await harness.Sut.GetSnapshotAsync(stokvelId);
        var decision = await harness.Sut.AuthorizeAsync(stokvelId, FeatureCodes.ClaimsBasic);

        Assert.Equal(SubscriptionAccessState.GracePeriod, snapshot.AccessState);
        Assert.True(decision.Allowed);
    }

    [Fact]
    public async Task PastDue_AfterGrace_BlocksTransactionalFeature_ButKeepsRecoveryAndReadOnlyAccess()
    {
        using var harness = new EntitlementTestHarness();
        var stokvelId = await SeedSubscriptionAsync(
            harness, PlanCodes.Growing, SubscriptionStatus.PastDue,
            graceEndsAt: harness.TimeProvider.GetUtcNow().UtcDateTime.AddDays(7));
        harness.TimeProvider.Advance(TimeSpan.FromDays(8));

        var transactional = await harness.Sut.AuthorizeAsync(stokvelId, FeatureCodes.ClaimsBasic);
        var claimWorkflow = await harness.Sut.AuthorizeAsync(stokvelId, FeatureCodes.ClaimsFullWorkflow);
        var billing = await harness.Sut.AuthorizeAsync(stokvelId, FeatureCodes.OpBillingPageAccess);
        var subscription = await harness.Sut.AuthorizeAsync(stokvelId, FeatureCodes.OpSubscriptionPageAccess);
        var readOnly = await harness.Sut.AuthorizeAsync(stokvelId, FeatureCodes.OpReadOnlyRecordViewing);

        Assert.False(transactional.Allowed);
        Assert.Equal(EntitlementDenialReason.SubscriptionRestricted, transactional.Reason);
        Assert.False(claimWorkflow.Allowed);
        Assert.True(billing.Allowed);
        Assert.True(subscription.Allowed);
        Assert.True(readOnly.Allowed);
    }

    private static async Task<Guid> SeedSubscriptionAsync(
        EntitlementTestHarness harness,
        string planCode,
        SubscriptionStatus status,
        DateTime? graceEndsAt = null)
    {
        await harness.SeedCatalogueAsync();
        await using var context = harness.CreateContext();
        var plan = await context.SubscriptionPlans.SingleAsync(p => p.Code == planCode);
        var stokvel = TestData.CreateStokvel(context);
        var subscription = TestData.CreateOrganisationSubscription(context, stokvel, plan.Id, status);
        subscription.GracePeriodEndsAt = graceEndsAt;
        await context.SaveChangesAsync();
        return stokvel.Id;
    }
}
