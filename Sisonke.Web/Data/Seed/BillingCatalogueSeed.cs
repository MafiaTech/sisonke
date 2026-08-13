using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Data.Seed;

/// <summary>
/// Seeds the subscription plan/feature catalogue (Data/FeatureCodes.cs, Data/PlanCodes.cs) and
/// backfills a LegacyUnsubscribed OrganisationSubscription for every pre-existing stokvel.
/// Both entry points are upsert-by-natural-key and safe to call on every application startup —
/// re-running after a catalogue change updates existing rows to match the source of truth below
/// rather than skipping them. Neither method ever creates a second OrganisationSubscription for
/// a stokvel that already has one, and neither ever touches TenantSubscription/legacy plans.
/// </summary>
public static class BillingCatalogueSeed
{
    private sealed record PlanSeed(
        string Code,
        string Name,
        string? Description,
        decimal MonthlyPrice,
        decimal? AnnualPrice,
        int? MaximumMembers,
        int? MaximumAdministrators,
        int? MaximumSchemes,
        int? StorageLimitMb,
        bool IsCustomPricing,
        int DisplayOrder);

    private sealed record FeatureSeed(
        string Code,
        string Name,
        string Category,
        FeatureDataType DataType,
        string DefaultValue);

    private static readonly PlanSeed[] Plans =
    [
        new(PlanCodes.Starter, "Starter", "For a single small stokvel getting started.",
            99m, 990m, 30, 1, 1, 100, false, 1),
        new(PlanCodes.Growing, "Growing Society", "For a growing stokvel that needs rotation, loans and voting.",
            249m, 2490m, 100, 5, 1, 1000, false, 2),
        new(PlanCodes.Professional, "Professional Society", "For an established society running multiple schemes.",
            499m, 4990m, 300, 15, 5, 5000, false, 3),
        new(PlanCodes.Enterprise, "Enterprise & Federations", "Custom pricing for federations and multi-branch operations.",
            999m, null, null, null, null, null, true, 4)
    ];

    private static readonly FeatureSeed[] Features =
    [
        new(FeatureCodes.MaxMembers, "Maximum active members", "Limits", FeatureDataType.Numeric, "30"),
        new(FeatureCodes.MaxAdministrators, "Maximum administrators", "Limits", FeatureDataType.Numeric, "1"),
        new(FeatureCodes.MaxSchemes, "Maximum schemes", "Limits", FeatureDataType.Numeric, "1"),
        new(FeatureCodes.StorageMb, "Storage (MB)", "Limits", FeatureDataType.Numeric, "100"),
        new(FeatureCodes.MemberManagement, "Member management", "Core", FeatureDataType.Boolean, "true"),
        new(FeatureCodes.DependentManagement, "Dependent management", "Core", FeatureDataType.Boolean, "true"),
        new(FeatureCodes.Meetings, "Meetings", "Governance", FeatureDataType.Boolean, "true"),
        new(FeatureCodes.ContributionTracking, "Contribution tracking", "Finance", FeatureDataType.Boolean, "true"),
        new(FeatureCodes.TaskManagement, "Task management", "Core", FeatureDataType.Boolean, "true"),
        new(FeatureCodes.ClaimsBasic, "Basic claims", "Claims", FeatureDataType.Boolean, "true"),
        new(FeatureCodes.ClaimsFullWorkflow, "Full claims workflow", "Claims", FeatureDataType.Boolean, "false"),
        new(FeatureCodes.RotationalStokvel, "Rotational stokvel", "Core", FeatureDataType.Boolean, "false"),
        new(FeatureCodes.LoansAndWithdrawals, "Loans and withdrawals", "Finance", FeatureDataType.Boolean, "false"),
        new(FeatureCodes.Voting, "Voting", "Governance", FeatureDataType.Boolean, "false"),
        new(FeatureCodes.AttendanceWarnings, "Attendance warnings", "Governance", FeatureDataType.Boolean, "false"),
        new(FeatureCodes.AiMeetingMinutes, "AI meeting minutes", "Governance", FeatureDataType.Boolean, "false"),
        new(FeatureCodes.EmailNotifications, "Email notifications", "Notifications", FeatureDataType.Boolean, "true"),
        new(FeatureCodes.WhatsAppNotifications, "WhatsApp notifications", "Notifications", FeatureDataType.Boolean, "false"),
        new(FeatureCodes.DocumentStorage, "Document storage", "Core", FeatureDataType.Boolean, "false"),
        new(FeatureCodes.ExportExcelPdf, "Export to Excel/PDF", "Reporting", FeatureDataType.Boolean, "false"),
        new(FeatureCodes.StandardReporting, "Standard reporting", "Reporting", FeatureDataType.Boolean, "true"),
        new(FeatureCodes.FinanceSummariesArrears, "Finance summaries and arrears", "Finance", FeatureDataType.Boolean, "false"),
        new(FeatureCodes.AdvancedReporting, "Advanced reporting", "Reporting", FeatureDataType.Boolean, "false"),
        new(FeatureCodes.ManualPaymentRecording, "Manual payment recording", "Payments", FeatureDataType.Boolean, "true"),
        new(FeatureCodes.PaymentLinks, "Payment links", "Payments", FeatureDataType.Boolean, "false"),
        new(FeatureCodes.TreasurerReconciliationTools, "Treasurer reconciliation tools", "Payments", FeatureDataType.Boolean, "false"),
        new(FeatureCodes.OnlineMemberPayments, "Online member payments", "Payments", FeatureDataType.Enum, nameof(OnlineMemberPaymentsLevel.None)),
        new(FeatureCodes.AutomatedRecurringCollections, "Automated recurring collections", "Payments", FeatureDataType.Boolean, "false"),
        new(FeatureCodes.AutomatedReconciliation, "Automated reconciliation", "Payments", FeatureDataType.Boolean, "false"),
        new(FeatureCodes.FailedPaymentFollowup, "Failed payment follow-up", "Payments", FeatureDataType.Boolean, "false"),
        new(FeatureCodes.SettlementReports, "Settlement reports", "Payments", FeatureDataType.Boolean, "false"),
        new(FeatureCodes.ControlledPayouts, "Controlled payouts", "Finance", FeatureDataType.Boolean, "false"),
        new(FeatureCodes.MultipleContributionStructures, "Multiple contribution structures", "Finance", FeatureDataType.Boolean, "false"),
        new(FeatureCodes.SurplusWallet, "Surplus wallet", "Finance", FeatureDataType.Boolean, "false"),
        new(FeatureCodes.ConfigurableApprovalWorkflows, "Configurable approval workflows", "Governance", FeatureDataType.Boolean, "false"),
        new(FeatureCodes.ApiAccess, "API access", "Platform", FeatureDataType.Boolean, "false"),
        new(FeatureCodes.MultipleSchemes, "Multiple schemes", "Platform", FeatureDataType.Boolean, "false"),
        new(FeatureCodes.FuneralParlourManagement, "Funeral parlour management", "Platform", FeatureDataType.Boolean, "false"),
        new(FeatureCodes.ConsolidatedReporting, "Consolidated reporting", "Reporting", FeatureDataType.Boolean, "false"),
        new(FeatureCodes.CustomBranding, "Custom branding", "Platform", FeatureDataType.Boolean, "false"),
        new(FeatureCodes.CustomDomain, "Custom domain", "Platform", FeatureDataType.Boolean, "false"),
        new(FeatureCodes.RoleCustomisation, "Role customisation", "Platform", FeatureDataType.Boolean, "false"),
        new(FeatureCodes.DataMigrationSupport, "Data migration support", "Support", FeatureDataType.Boolean, "false"),
        new(FeatureCodes.WhiteLabel, "White label", "Platform", FeatureDataType.Boolean, "false"),
        new(FeatureCodes.SupportTier, "Support tier", "Support", FeatureDataType.Enum, nameof(Enums.SupportTier.Community))
    ];

    // code -> [Starter, Growing, Professional, Enterprise]
    private static readonly Dictionary<string, object?[]> PlanFeatureMatrix = new()
    {
        [FeatureCodes.MaxMembers] = [30, 100, 300, null],
        [FeatureCodes.MaxAdministrators] = [1, 5, 15, null],
        [FeatureCodes.MaxSchemes] = [1, 1, 5, null],
        [FeatureCodes.StorageMb] = [100, 1000, 5000, null],
        [FeatureCodes.MemberManagement] = [true, true, true, true],
        [FeatureCodes.DependentManagement] = [true, true, true, true],
        [FeatureCodes.Meetings] = [true, true, true, true],
        [FeatureCodes.ContributionTracking] = [true, true, true, true],
        [FeatureCodes.TaskManagement] = [true, true, true, true],
        [FeatureCodes.ClaimsBasic] = [true, true, true, true],
        [FeatureCodes.ClaimsFullWorkflow] = [false, true, true, true],
        [FeatureCodes.RotationalStokvel] = [false, true, true, true],
        [FeatureCodes.LoansAndWithdrawals] = [false, true, true, true],
        [FeatureCodes.Voting] = [false, true, true, true],
        [FeatureCodes.AttendanceWarnings] = [false, true, true, true],
        [FeatureCodes.AiMeetingMinutes] = [false, true, true, true],
        [FeatureCodes.EmailNotifications] = [true, true, true, true],
        [FeatureCodes.WhatsAppNotifications] = [false, true, true, true],
        [FeatureCodes.DocumentStorage] = [false, true, true, true],
        [FeatureCodes.ExportExcelPdf] = [false, true, true, true],
        [FeatureCodes.StandardReporting] = [true, true, true, true],
        [FeatureCodes.FinanceSummariesArrears] = [false, true, true, true],
        [FeatureCodes.AdvancedReporting] = [false, false, true, true],
        [FeatureCodes.ManualPaymentRecording] = [true, true, true, true],
        [FeatureCodes.PaymentLinks] = [false, true, true, true],
        [FeatureCodes.TreasurerReconciliationTools] = [false, true, true, true],
        [FeatureCodes.OnlineMemberPayments] =
        [
            OnlineMemberPaymentsLevel.None, OnlineMemberPaymentsLevel.Limited,
            OnlineMemberPaymentsLevel.Full, OnlineMemberPaymentsLevel.Full
        ],
        [FeatureCodes.AutomatedRecurringCollections] = [false, false, true, true],
        [FeatureCodes.AutomatedReconciliation] = [false, false, true, true],
        [FeatureCodes.FailedPaymentFollowup] = [false, false, true, true],
        [FeatureCodes.SettlementReports] = [false, false, true, true],
        [FeatureCodes.ControlledPayouts] = [false, false, true, true],
        [FeatureCodes.MultipleContributionStructures] = [false, false, true, true],
        [FeatureCodes.SurplusWallet] = [false, false, true, true],
        [FeatureCodes.ConfigurableApprovalWorkflows] = [false, false, true, true],
        [FeatureCodes.ApiAccess] = [false, false, true, true],
        [FeatureCodes.MultipleSchemes] = [false, false, false, true],
        [FeatureCodes.FuneralParlourManagement] = [false, false, false, true],
        [FeatureCodes.ConsolidatedReporting] = [false, false, false, true],
        [FeatureCodes.CustomBranding] = [false, false, false, true],
        [FeatureCodes.CustomDomain] = [false, false, false, true],
        [FeatureCodes.RoleCustomisation] = [false, false, false, true],
        [FeatureCodes.DataMigrationSupport] = [false, false, false, true],
        [FeatureCodes.WhiteLabel] = [false, false, false, true],
        [FeatureCodes.SupportTier] =
        [
            Enums.SupportTier.Community, Enums.SupportTier.Priority,
            Enums.SupportTier.PriorityWhatsApp, Enums.SupportTier.Dedicated
        ]
    };

    private const string LaunchTrialCode = "LAUNCH60";

    /// <summary>
    /// Upserts the four plans, every feature definition and every plan/feature row from the
    /// matrix above, plus the LAUNCH60 promotional trial. Never touches TenantSubscription,
    /// the legacy member-count tier plans, or any OrganisationSubscription row.
    /// </summary>
    public static async Task EnsureBillingCatalogueAsync(ApplicationDbContext context, ILogger? logger = null)
    {
        var plansByCode = await UpsertPlansAsync(context, logger);
        var featuresByCode = await UpsertFeatureDefinitionsAsync(context, logger);
        await UpsertPlanFeaturesAsync(context, logger, plansByCode, featuresByCode);
        await UpsertLaunchTrialAsync(context, logger, plansByCode);

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Gives every stokvel that does not yet have any OrganisationSubscription row a
    /// LegacyUnsubscribed one with no plan and no trial dates. Existing rows — including ones
    /// created by an earlier run of this method — are left completely untouched.
    /// </summary>
    public static async Task EnsureLegacyOrganisationSubscriptionsAsync(ApplicationDbContext context, ILogger? logger = null)
    {
        var stokvelIdsWithSubscription = await context.OrganisationSubscriptions
            .Select(s => s.StokvelId)
            .Distinct()
            .ToListAsync();

        var stokvelIdsMissingSubscription = await context.Stokvels
            .Where(s => !stokvelIdsWithSubscription.Contains(s.Id))
            .Select(s => s.Id)
            .ToListAsync();

        if (stokvelIdsMissingSubscription.Count == 0)
        {
            logger?.LogInformation("[BillingSeed] Every stokvel already has an OrganisationSubscription — skipping backfill.");
            return;
        }

        logger?.LogInformation(
            "[BillingSeed] Backfilling LegacyUnsubscribed OrganisationSubscription for {Count} stokvel(s).",
            stokvelIdsMissingSubscription.Count);

        var createdAt = DateTime.UtcNow;
        foreach (var stokvelId in stokvelIdsMissingSubscription)
        {
            context.OrganisationSubscriptions.Add(new OrganisationSubscription
            {
                Id = Guid.NewGuid(),
                StokvelId = stokvelId,
                SubscriptionPlanId = null,
                Status = SubscriptionStatus.LegacyUnsubscribed,
                TrialStartedAt = null,
                TrialEndsAt = null,
                CreatedAt = createdAt
            });
        }

        await context.SaveChangesAsync();
    }

    private static async Task<Dictionary<string, SubscriptionPlan>> UpsertPlansAsync(
        ApplicationDbContext context, ILogger? logger)
    {
        var codes = Plans.Select(p => p.Code).ToArray();
        var existing = await context.SubscriptionPlans
            .Where(p => p.Code != null && codes.Contains(p.Code))
            .ToDictionaryAsync(p => p.Code!);

        foreach (var seed in Plans)
        {
            if (!existing.TryGetValue(seed.Code, out var plan))
            {
                plan = new SubscriptionPlan { Id = Guid.NewGuid(), Code = seed.Code };
                context.SubscriptionPlans.Add(plan);
                existing[seed.Code] = plan;
                logger?.LogInformation("[BillingSeed] Creating subscription plan {Code}.", seed.Code);
            }

            plan.Name = seed.Name;
            plan.Description = seed.Description;
            plan.MonthlyPrice = seed.MonthlyPrice;
            plan.AnnualPrice = seed.AnnualPrice;
            plan.Currency = "ZAR";
            plan.MaximumMembers = seed.MaximumMembers;
            plan.MaximumAdministrators = seed.MaximumAdministrators;
            plan.MaximumSchemes = seed.MaximumSchemes;
            plan.StorageLimitMb = seed.StorageLimitMb;
            plan.IsCustomPricing = seed.IsCustomPricing;
            plan.DisplayOrder = seed.DisplayOrder;
            plan.IsActive = true;
            plan.UpdatedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();
        return existing;
    }

    private static async Task<Dictionary<string, FeatureDefinition>> UpsertFeatureDefinitionsAsync(
        ApplicationDbContext context, ILogger? logger)
    {
        var codes = Features.Select(f => f.Code).ToArray();
        var existing = await context.FeatureDefinitions
            .Where(f => codes.Contains(f.Code))
            .ToDictionaryAsync(f => f.Code);

        foreach (var seed in Features)
        {
            if (!existing.TryGetValue(seed.Code, out var feature))
            {
                feature = new FeatureDefinition { Id = Guid.NewGuid(), Code = seed.Code };
                context.FeatureDefinitions.Add(feature);
                existing[seed.Code] = feature;
                logger?.LogInformation("[BillingSeed] Creating feature definition {Code}.", seed.Code);
            }

            feature.Name = seed.Name;
            feature.Category = seed.Category;
            feature.DataType = seed.DataType;
            feature.DefaultValue = seed.DefaultValue;
            feature.IsActive = true;
        }

        await context.SaveChangesAsync();
        return existing;
    }

    private static async Task UpsertPlanFeaturesAsync(
        ApplicationDbContext context,
        ILogger? logger,
        Dictionary<string, SubscriptionPlan> plansByCode,
        Dictionary<string, FeatureDefinition> featuresByCode)
    {
        var planIds = plansByCode.Values.Select(p => p.Id).ToArray();
        var featureIds = featuresByCode.Values.Select(f => f.Id).ToArray();

        var existingPlanFeatures = await context.PlanFeatures
            .Where(pf => planIds.Contains(pf.SubscriptionPlanId) && featureIds.Contains(pf.FeatureDefinitionId))
            .ToDictionaryAsync(pf => (pf.SubscriptionPlanId, pf.FeatureDefinitionId));

        var planCodesInOrder = new[] { PlanCodes.Starter, PlanCodes.Growing, PlanCodes.Professional, PlanCodes.Enterprise };

        foreach (var (featureCode, valuesByPlan) in PlanFeatureMatrix)
        {
            var feature = featuresByCode[featureCode];

            for (var i = 0; i < planCodesInOrder.Length; i++)
            {
                var plan = plansByCode[planCodesInOrder[i]];
                var value = valuesByPlan[i];
                var key = (plan.Id, feature.Id);

                if (!existingPlanFeatures.TryGetValue(key, out var planFeature))
                {
                    planFeature = new PlanFeature { Id = Guid.NewGuid(), SubscriptionPlanId = plan.Id, FeatureDefinitionId = feature.Id };
                    context.PlanFeatures.Add(planFeature);
                    existingPlanFeatures[key] = planFeature;
                }

                ApplyPlanFeatureValue(planFeature, feature.DataType, value);
            }
        }

        logger?.LogInformation(
            "[BillingSeed] Upserted {Count} plan/feature rows.", existingPlanFeatures.Count);

        await context.SaveChangesAsync();
    }

    private static void ApplyPlanFeatureValue(PlanFeature planFeature, FeatureDataType dataType, object? value)
    {
        switch (dataType)
        {
            case FeatureDataType.Numeric:
                planFeature.IsEnabled = true;
                planFeature.LimitValue = value is int limit ? limit : null;
                planFeature.ConfigurationJson = null;
                break;

            case FeatureDataType.Enum:
                planFeature.IsEnabled = true;
                planFeature.LimitValue = null;
                planFeature.ConfigurationJson = $$"""{"value":"{{value}}"}""";
                break;

            case FeatureDataType.Boolean:
            default:
                planFeature.IsEnabled = value is true;
                planFeature.LimitValue = null;
                planFeature.ConfigurationJson = null;
                break;
        }
    }

    private static async Task UpsertLaunchTrialAsync(
        ApplicationDbContext context, ILogger? logger, Dictionary<string, SubscriptionPlan> plansByCode)
    {
        var trial = await context.PromotionalTrials
            .SingleOrDefaultAsync(t => t.Code == LaunchTrialCode);

        if (trial is null)
        {
            trial = new PromotionalTrial { Id = Guid.NewGuid(), Code = LaunchTrialCode };
            context.PromotionalTrials.Add(trial);
            logger?.LogInformation("[BillingSeed] Creating promotional trial {Code}.", LaunchTrialCode);
        }

        trial.Description = "Launch offer: 60-day free trial on any plan.";
        trial.TrialDays = 60;
        trial.AppliesToPlanId = null;
        trial.IsActive = true;

        await context.SaveChangesAsync();
    }
}
