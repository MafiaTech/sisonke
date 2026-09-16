using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Services.Billing;

/// <summary>One catalogue plan plus its feature rows, resolved from PlanFeature/FeatureDefinition — never hardcoded per-plan feature lists (Phase 5 brief: "generated from the seeded catalogue, not hard-coded").</summary>
public sealed record PlanCatalogueEntry(SubscriptionPlan Plan, IReadOnlyList<PlanFeatureView> Features);

public sealed record PlanFeatureView(
    string Code,
    string Name,
    string Category,
    FeatureDataType DataType,
    bool IsEnabled,
    int? LimitValue,
    string? EnumValue);

/// <summary>
/// Single source for the Code-based plan catalogue (STARTER/GROWING/PROFESSIONAL/ENTERPRISE),
/// shared by the onboarding wizard's plan-selection step, the public pricing page, and the
/// customer billing page's upgrade/downgrade picker — replaces the legacy MinMembers-ordered,
/// hardcoded-feature-list query that used to live in RegisterStokvel.razor.
/// </summary>
public sealed class PlanCatalogueService(IDbContextFactory<ApplicationDbContext> dbFactory)
{
    public async Task<IReadOnlyList<PlanCatalogueEntry>> GetPublicPlansAsync(CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var plans = await context.SubscriptionPlans
            .AsNoTracking()
            .Where(p => p.IsActive && p.Code != null)
            .OrderBy(p => p.DisplayOrder)
            .ToListAsync(ct);

        var planIds = plans.Select(p => p.Id).ToList();

        var planFeatures = await context.PlanFeatures
            .AsNoTracking()
            .Include(pf => pf.FeatureDefinition)
            .Where(pf => planIds.Contains(pf.SubscriptionPlanId) && pf.FeatureDefinition.IsActive)
            .ToListAsync(ct);

        return plans
            .Select(plan => new PlanCatalogueEntry(
                plan,
                planFeatures
                    .Where(pf => pf.SubscriptionPlanId == plan.Id)
                    .OrderBy(pf => pf.FeatureDefinition.Category)
                    .ThenBy(pf => pf.FeatureDefinition.Name)
                    .Select(pf => new PlanFeatureView(
                        pf.FeatureDefinition.Code,
                        pf.FeatureDefinition.Name,
                        pf.FeatureDefinition.Category,
                        pf.FeatureDefinition.DataType,
                        pf.IsEnabled,
                        pf.LimitValue,
                        ExtractEnumValue(pf.ConfigurationJson)))
                    .ToList()))
            .ToList();
    }

    public async Task<SubscriptionPlan?> GetActivePlanAsync(Guid planId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        return await context.SubscriptionPlans
            .AsNoTracking()
            .SingleOrDefaultAsync(plan => plan.Id == planId && plan.IsActive && plan.Code != null, ct);
    }

    /// <summary>The smallest active plan whose MaximumMembers comfortably fits the expected count (null MaximumMembers = unlimited, always fits).</summary>
    public async Task<SubscriptionPlan?> RecommendPlanForMemberCountAsync(int? expectedMemberCount, CancellationToken ct = default)
    {
        if (expectedMemberCount is null || expectedMemberCount < 1)
        {
            return null;
        }

        await using var context = await dbFactory.CreateDbContextAsync(ct);

        return await context.SubscriptionPlans
            .AsNoTracking()
            .Where(p => p.IsActive && p.Code != null &&
                (p.MaximumMembers == null || expectedMemberCount <= p.MaximumMembers))
            .OrderBy(p => p.DisplayOrder)
            .FirstOrDefaultAsync(ct);
    }

    private static string? ExtractEnumValue(string? configurationJson)
    {
        if (string.IsNullOrWhiteSpace(configurationJson))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(configurationJson);
            return document.RootElement.TryGetProperty("value", out var value) ? value.GetString() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
