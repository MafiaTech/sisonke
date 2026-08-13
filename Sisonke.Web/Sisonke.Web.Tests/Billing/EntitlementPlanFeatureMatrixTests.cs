using Sisonke.Web.Data;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Tests.Billing;

/// <summary>
/// Table-driven check of the seeded catalogue against the brief's plan/feature matrix, across
/// all four plans and every feature code — independent of BillingCatalogueSeed's own source so a
/// typo in the seeder's data table is caught here too.
/// </summary>
public class EntitlementPlanFeatureMatrixTests
{
    // code -> [Starter, Growing, Professional, Enterprise]. Numeric rows carry the limit
    // (null = unlimited); enum rows carry the level name; everything else is a plain bool.
    public static readonly TheoryData<string, string, object?, object?, object?, object?> Matrix = new()
    {
        { FeatureCodes.MaxMembers, "Numeric", 30, 100, 300, null },
        { FeatureCodes.MaxAdministrators, "Numeric", 1, 5, 15, null },
        { FeatureCodes.MaxSchemes, "Numeric", 1, 1, 5, null },
        { FeatureCodes.StorageMb, "Numeric", 100, 1000, 5000, null },
        { FeatureCodes.MemberManagement, "Boolean", true, true, true, true },
        { FeatureCodes.DependentManagement, "Boolean", true, true, true, true },
        { FeatureCodes.Meetings, "Boolean", true, true, true, true },
        { FeatureCodes.ContributionTracking, "Boolean", true, true, true, true },
        { FeatureCodes.TaskManagement, "Boolean", true, true, true, true },
        { FeatureCodes.ClaimsBasic, "Boolean", true, true, true, true },
        { FeatureCodes.ClaimsFullWorkflow, "Boolean", false, true, true, true },
        { FeatureCodes.RotationalStokvel, "Boolean", false, true, true, true },
        { FeatureCodes.LoansAndWithdrawals, "Boolean", false, true, true, true },
        { FeatureCodes.Voting, "Boolean", false, true, true, true },
        { FeatureCodes.AttendanceWarnings, "Boolean", false, true, true, true },
        { FeatureCodes.AiMeetingMinutes, "Boolean", false, true, true, true },
        { FeatureCodes.EmailNotifications, "Boolean", true, true, true, true },
        { FeatureCodes.WhatsAppNotifications, "Boolean", false, true, true, true },
        { FeatureCodes.DocumentStorage, "Boolean", false, true, true, true },
        { FeatureCodes.ExportExcelPdf, "Boolean", false, true, true, true },
        { FeatureCodes.StandardReporting, "Boolean", true, true, true, true },
        { FeatureCodes.FinanceSummariesArrears, "Boolean", false, true, true, true },
        { FeatureCodes.AdvancedReporting, "Boolean", false, false, true, true },
        { FeatureCodes.ManualPaymentRecording, "Boolean", true, true, true, true },
        { FeatureCodes.PaymentLinks, "Boolean", false, true, true, true },
        { FeatureCodes.TreasurerReconciliationTools, "Boolean", false, true, true, true },
        { FeatureCodes.OnlineMemberPayments, "Enum", "None", "Limited", "Full", "Full" },
        { FeatureCodes.AutomatedRecurringCollections, "Boolean", false, false, true, true },
        { FeatureCodes.AutomatedReconciliation, "Boolean", false, false, true, true },
        { FeatureCodes.FailedPaymentFollowup, "Boolean", false, false, true, true },
        { FeatureCodes.SettlementReports, "Boolean", false, false, true, true },
        { FeatureCodes.ControlledPayouts, "Boolean", false, false, true, true },
        { FeatureCodes.MultipleContributionStructures, "Boolean", false, false, true, true },
        { FeatureCodes.SurplusWallet, "Boolean", false, false, true, true },
        { FeatureCodes.ConfigurableApprovalWorkflows, "Boolean", false, false, true, true },
        { FeatureCodes.ApiAccess, "Boolean", false, false, true, true },
        { FeatureCodes.MultipleSchemes, "Boolean", false, false, false, true },
        { FeatureCodes.FuneralParlourManagement, "Boolean", false, false, false, true },
        { FeatureCodes.ConsolidatedReporting, "Boolean", false, false, false, true },
        { FeatureCodes.CustomBranding, "Boolean", false, false, false, true },
        { FeatureCodes.CustomDomain, "Boolean", false, false, false, true },
        { FeatureCodes.RoleCustomisation, "Boolean", false, false, false, true },
        { FeatureCodes.DataMigrationSupport, "Boolean", false, false, false, true },
        { FeatureCodes.WhiteLabel, "Boolean", false, false, false, true },
        { FeatureCodes.SupportTier, "Enum", "Community", "Priority", "PriorityWhatsApp", "Dedicated" }
    };

    [Theory]
    [MemberData(nameof(Matrix))]
    public async Task SeededCatalogue_MatchesBriefMatrix(
        string featureCode, string dataType, object? starter, object? growing, object? professional, object? enterprise)
    {
        using var harness = new EntitlementTestHarness();
        await harness.SeedCatalogueAsync();

        await AssertPlanFeature(harness, PlanCodes.Starter, featureCode, dataType, starter);
        await AssertPlanFeature(harness, PlanCodes.Growing, featureCode, dataType, growing);
        await AssertPlanFeature(harness, PlanCodes.Professional, featureCode, dataType, professional);
        await AssertPlanFeature(harness, PlanCodes.Enterprise, featureCode, dataType, enterprise);
    }

    private static async Task AssertPlanFeature(
        EntitlementTestHarness harness, string planCode, string featureCode, string dataType, object? expected)
    {
        await using var context = harness.CreateContext();
        var plan = context.SubscriptionPlans.Single(p => p.Code == planCode);
        var stokvel = TestSupport.TestData.CreateStokvel(context);
        TestSupport.TestData.CreateOrganisationSubscription(context, stokvel, plan.Id, SubscriptionStatus.Active);
        await context.SaveChangesAsync();

        switch (dataType)
        {
            case "Numeric":
                var limit = await harness.Sut.GetLimitAsync(stokvel.Id, featureCode);
                Assert.Equal(expected, limit);
                break;

            case "Boolean":
                var hasFeature = await harness.Sut.HasFeatureAsync(stokvel.Id, featureCode);
                Assert.Equal(expected, hasFeature);
                break;

            case "Enum":
                var snapshot = await harness.Sut.GetSnapshotAsync(stokvel.Id);
                Assert.Equal(expected, snapshot.Features[featureCode].EnumValue);
                break;
        }
    }
}
