using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Services.Entitlements;

public sealed class EntitlementService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    IEntitlementUsageProvider usageProvider,
    IMemoryCache cache,
    EntitlementOptions options,
    ILogger<EntitlementService> logger) : IEntitlementService
{
    public async Task<bool> HasFeatureAsync(Guid stokvelId, string featureCode, CancellationToken ct = default)
    {
        // requestedUsage: 0 — a pure existence check must not itself push a numeric feature over its cap.
        var decision = await BuildDecisionAsync(stokvelId, featureCode, requestedUsage: 0, ct);
        return decision.Allowed;
    }

    public async Task<int?> GetLimitAsync(Guid stokvelId, string featureCode, CancellationToken ct = default)
    {
        var snapshot = await GetSnapshotAsync(stokvelId, ct);
        return snapshot.Features.TryGetValue(featureCode, out var value) ? value.LimitValue : null;
    }

    public Task<EntitlementDecision> AuthorizeAsync(Guid stokvelId, string featureCode, int requestedUsage = 1, CancellationToken ct = default) =>
        BuildDecisionAsync(stokvelId, featureCode, requestedUsage, ct);

    public async Task<EntitlementSnapshot> GetSnapshotAsync(Guid stokvelId, CancellationToken ct = default)
    {
        var cacheKey = BuildCacheKey(stokvelId);

        if (cache.TryGetValue(cacheKey, out EntitlementSnapshot? cached) && cached is not null)
        {
            return cached;
        }

        var snapshot = await BuildSnapshotAsync(stokvelId, ct);
        cache.Set(cacheKey, snapshot, TimeSpan.FromMinutes(Math.Max(1, options.CacheTtlMinutes)));
        return snapshot;
    }

    public Task InvalidateAsync(Guid stokvelId, CancellationToken ct = default)
    {
        cache.Remove(BuildCacheKey(stokvelId));
        return Task.CompletedTask;
    }

    private static string BuildCacheKey(Guid stokvelId) => $"entitlement-snapshot:{stokvelId:N}";

    private async Task<EntitlementDecision> BuildDecisionAsync(Guid stokvelId, string featureCode, int requestedUsage, CancellationToken ct)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var subscription = await ResolveCurrentSubscriptionAsync(context, stokvelId, ct);

        if (subscription is null || subscription.Status == SubscriptionStatus.LegacyUnsubscribed)
        {
            return await BuildLegacyDecisionAsync(context, stokvelId, featureCode, requestedUsage, ct);
        }

        if (FeatureCodes.AlwaysAllowed.Contains(featureCode))
        {
            return AllowedDecision(featureCode, subscription.SubscriptionPlan, currentUsage: 0);
        }

        switch (subscription.Status)
        {
            case SubscriptionStatus.Suspended:
                return DeniedDecision(featureCode, subscription.SubscriptionPlan, EntitlementDenialReason.SubscriptionSuspended,
                    "This account is suspended. Contact support to reactivate your subscription.");

            case SubscriptionStatus.Restricted when FeatureCodes.WriteBlocked.Contains(featureCode):
                return DeniedDecision(featureCode, subscription.SubscriptionPlan, EntitlementDenialReason.SubscriptionRestricted,
                    "This account is restricted pending payment. Update your payment details to resume full access.");

            case SubscriptionStatus.PendingPaymentMethod when FeatureCodes.WriteBlocked.Contains(featureCode):
                return DeniedDecision(featureCode, subscription.SubscriptionPlan, EntitlementDenialReason.SubscriptionRestricted,
                    "Add a payment method to activate your subscription and unlock this feature.");

            case SubscriptionStatus.Expired when FeatureCodes.WriteBlocked.Contains(featureCode):
                return DeniedDecision(featureCode, subscription.SubscriptionPlan, EntitlementDenialReason.TrialExpired,
                    "Your trial has expired. Choose a plan to continue.");

            case SubscriptionStatus.Cancelled
                when !IsWithinCurrentPeriod(subscription) && FeatureCodes.WriteBlocked.Contains(featureCode):
                return DeniedDecision(featureCode, subscription.SubscriptionPlan, EntitlementDenialReason.SubscriptionRestricted,
                    "This subscription has been cancelled. Resubscribe to continue.");
        }

        // Trialing / Active / PastDue / Cancelled-but-in-period / Restricted-or-PendingPaymentMethod-or-
        // Expired-but-not-write-blocked all evaluate against the real plan normally.
        return await EvaluatePlanFeatureAsync(context, featureCode, subscription.SubscriptionPlan, stokvelId, requestedUsage, ct);
    }

    private async Task<EntitlementDecision> BuildLegacyDecisionAsync(
        ApplicationDbContext context, Guid stokvelId, string featureCode, int requestedUsage, CancellationToken ct)
    {
        if (FeatureCodes.AlwaysAllowed.Contains(featureCode))
        {
            return AllowedDecision(featureCode, null, currentUsage: 0);
        }

        if (DateTime.UtcNow >= options.LegacyGraceCutoverUtc)
        {
            return DeniedDecision(featureCode, null, EntitlementDenialReason.NoSubscription,
                "This organisation does not have an active subscription. Choose a plan to continue.");
        }

        var legacyPlan = await context.SubscriptionPlans
            .SingleOrDefaultAsync(p => p.Code == options.LegacyEquivalentPlanCode, ct);

        if (legacyPlan is null)
        {
            logger.LogError(
                "Legacy grace plan code {PlanCode} (EntitlementOptions.LegacyEquivalentPlanCode) was not found in the catalogue.",
                options.LegacyEquivalentPlanCode);
            return DeniedDecision(featureCode, null, EntitlementDenialReason.NoSubscription,
                "This organisation does not have an active subscription. Choose a plan to continue.");
        }

        return await EvaluatePlanFeatureAsync(context, featureCode, legacyPlan, stokvelId, requestedUsage, ct);
    }

    private async Task<EntitlementDecision> EvaluatePlanFeatureAsync(
        ApplicationDbContext context, string featureCode, SubscriptionPlan? plan, Guid stokvelId, int requestedUsage, CancellationToken ct)
    {
        if (plan is null)
        {
            return DeniedDecision(featureCode, null, EntitlementDenialReason.NoSubscription,
                "No active plan is selected for this organisation.");
        }

        var planFeature = await context.PlanFeatures
            .Include(pf => pf.FeatureDefinition)
            .FirstOrDefaultAsync(pf => pf.SubscriptionPlanId == plan.Id && pf.FeatureDefinition.Code == featureCode, ct);

        if (planFeature is null)
        {
            // Operation classifiers (Op*) have no catalogue row by design — nothing gates them once
            // the status checks in BuildDecisionAsync have already passed.
            if (featureCode.StartsWith("OP_", StringComparison.Ordinal))
            {
                return AllowedDecision(featureCode, plan, currentUsage: 0);
            }

            var upgradeTo = await FindCheapestPlanWithFeatureAsync(context, featureCode, plan.Id, requestedUsage, ct);
            return DeniedDecision(featureCode, plan, EntitlementDenialReason.FeatureNotInPlan,
                $"Your {plan.Name} plan does not include this feature.", upgradeTo);
        }

        switch (planFeature.FeatureDefinition.DataType)
        {
            case FeatureDataType.Boolean:
                if (!planFeature.IsEnabled)
                {
                    var upgradeTo = await FindCheapestPlanWithFeatureAsync(context, featureCode, plan.Id, requestedUsage, ct);
                    return DeniedDecision(featureCode, plan, EntitlementDenialReason.FeatureNotInPlan,
                        $"Your {plan.Name} plan does not include this feature.", upgradeTo);
                }

                return AllowedDecision(featureCode, plan, currentUsage: 0);

            case FeatureDataType.Enum:
                // Brief: "Enum feature: return the configured level; caller compares." The decision
                // only confirms the row exists and is enabled — GetSnapshotAsync exposes the level.
                return AllowedDecision(featureCode, plan, currentUsage: 0);

            case FeatureDataType.Numeric:
            default:
                var currentUsage = await usageProvider.GetCurrentUsageAsync(stokvelId, featureCode, ct);
                var prospectiveUsage = currentUsage + Math.Max(0, requestedUsage);

                if (planFeature.LimitValue is null || prospectiveUsage <= planFeature.LimitValue)
                {
                    return AllowedDecision(featureCode, plan, currentUsage, planFeature.LimitValue);
                }

                var upgradeToHigherLimit = await FindCheapestPlanWithFeatureAsync(context, featureCode, plan.Id, prospectiveUsage, ct);
                return new EntitlementDecision(
                    false,
                    EntitlementDenialReason.LimitExceeded,
                    featureCode,
                    plan.Code,
                    plan.Name,
                    planFeature.LimitValue,
                    currentUsage,
                    $"Your {plan.Name} plan supports up to {planFeature.LimitValue} for this feature.",
                    upgradeToHigherLimit);
        }
    }

    private static async Task<string?> FindCheapestPlanWithFeatureAsync(
        ApplicationDbContext context, string featureCode, Guid currentPlanId, int requestedUsage, CancellationToken ct)
    {
        var candidates = await context.PlanFeatures
            .Include(pf => pf.SubscriptionPlan)
            .Include(pf => pf.FeatureDefinition)
            .Where(pf =>
                pf.FeatureDefinition.Code == featureCode &&
                pf.SubscriptionPlanId != currentPlanId &&
                pf.SubscriptionPlan.Code != null)
            .ToListAsync(ct);

        return candidates
            .Where(pf => SatisfiesRequest(pf, requestedUsage))
            .OrderBy(pf => pf.SubscriptionPlan.DisplayOrder)
            .FirstOrDefault()
            ?.SubscriptionPlan.Code;
    }

    private static bool SatisfiesRequest(PlanFeature planFeature, int requestedUsage) =>
        planFeature.FeatureDefinition.DataType switch
        {
            FeatureDataType.Boolean => planFeature.IsEnabled,
            FeatureDataType.Numeric => planFeature.LimitValue is null || planFeature.LimitValue >= requestedUsage,
            _ => planFeature.IsEnabled
        };

    private async Task<EntitlementSnapshot> BuildSnapshotAsync(Guid stokvelId, CancellationToken ct)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var subscription = await ResolveCurrentSubscriptionAsync(context, stokvelId, ct);
        var status = subscription?.Status ?? SubscriptionStatus.LegacyUnsubscribed;
        var effectivePlan = subscription?.SubscriptionPlan;

        if (subscription is null || subscription.Status == SubscriptionStatus.LegacyUnsubscribed)
        {
            effectivePlan = DateTime.UtcNow < options.LegacyGraceCutoverUtc
                ? await context.SubscriptionPlans.SingleOrDefaultAsync(p => p.Code == options.LegacyEquivalentPlanCode, ct)
                : null;
        }

        var features = new Dictionary<string, FeatureValue>();

        if (effectivePlan is not null)
        {
            var planFeatures = await context.PlanFeatures
                .Include(pf => pf.FeatureDefinition)
                .Where(pf => pf.SubscriptionPlanId == effectivePlan.Id)
                .ToListAsync(ct);

            foreach (var planFeature in planFeatures)
            {
                features[planFeature.FeatureDefinition.Code] = new FeatureValue(
                    planFeature.FeatureDefinition.Code,
                    planFeature.FeatureDefinition.DataType,
                    planFeature.IsEnabled,
                    planFeature.LimitValue,
                    ExtractEnumValue(planFeature.ConfigurationJson));
            }
        }

        return new EntitlementSnapshot(
            stokvelId,
            effectivePlan?.Code,
            effectivePlan?.Name,
            status,
            subscription?.TrialEndsAt,
            subscription?.NextBillingAt,
            features);
    }

    private static async Task<OrganisationSubscription?> ResolveCurrentSubscriptionAsync(
        ApplicationDbContext context, Guid stokvelId, CancellationToken ct)
    {
        var subscriptions = await context.OrganisationSubscriptions
            .Include(s => s.SubscriptionPlan)
            .Where(s => s.StokvelId == stokvelId)
            .ToListAsync(ct);

        if (subscriptions.Count == 0)
        {
            return null;
        }

        // The filtered unique index guarantees at most one "live" (non-Cancelled/Expired) row —
        // prefer it; otherwise fall back to the most recent historical row.
        return subscriptions
            .Where(s => s.Status is not (SubscriptionStatus.Cancelled or SubscriptionStatus.Expired))
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefault()
            ?? subscriptions.OrderByDescending(s => s.CreatedAt).First();
    }

    private static bool IsWithinCurrentPeriod(OrganisationSubscription subscription) =>
        subscription.CurrentPeriodEndsAt is { } periodEnd && periodEnd > DateTime.UtcNow;

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

    private static EntitlementDecision AllowedDecision(string featureCode, SubscriptionPlan? plan, int currentUsage, int? limit = null) =>
        new(true, EntitlementDenialReason.Allowed, featureCode, plan?.Code, plan?.Name, limit, currentUsage, string.Empty, null);

    private static EntitlementDecision DeniedDecision(
        string featureCode, SubscriptionPlan? plan, EntitlementDenialReason reason, string message, string? upgradeToPlanCode = null) =>
        new(false, reason, featureCode, plan?.Code, plan?.Name, null, 0, message, upgradeToPlanCode);
}
