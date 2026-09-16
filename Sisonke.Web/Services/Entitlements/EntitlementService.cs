using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;
using Sisonke.Web.Services.Billing;

namespace Sisonke.Web.Services.Entitlements;

public sealed class EntitlementService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    IEntitlementUsageProvider usageProvider,
    IMemoryCache cache,
    EntitlementOptions options,
    ILogger<EntitlementService> logger,
    TimeProvider? timeProvider = null) : IEntitlementService
{
    private readonly TimeProvider clock = timeProvider ?? TimeProvider.System;

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

        if (cache.TryGetValue(cacheKey, out EntitlementSnapshot? cached) && cached is not null &&
            !(cached.IsTrial && cached.TrialEndsAt is { } cachedTrialEnd && clock.GetUtcNow().UtcDateTime >= cachedTrialEnd))
        {
            return cached;
        }

        var snapshot = await BuildSnapshotAsync(stokvelId, ct);
        var cacheDuration = TimeSpan.FromMinutes(Math.Max(1, options.CacheTtlMinutes));
        if (snapshot.IsTrial && snapshot.TrialEndsAt is { } trialEnd)
        {
            var untilExpiry = trialEnd - clock.GetUtcNow().UtcDateTime;
            if (untilExpiry > TimeSpan.Zero && untilExpiry < cacheDuration)
            {
                cacheDuration = untilExpiry;
            }
        }

        cache.Set(cacheKey, snapshot, cacheDuration);
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

        var access = EvaluateAccess(subscription, clock.GetUtcNow().UtcDateTime);
        if (!access.CanUsePaidFeatures)
        {
            var reason = access.State switch
            {
                SubscriptionAccessState.TrialExpired => EntitlementDenialReason.TrialExpired,
                SubscriptionAccessState.Suspended => EntitlementDenialReason.SubscriptionSuspended,
                _ => EntitlementDenialReason.SubscriptionRestricted
            };
            var message = access.State switch
            {
                SubscriptionAccessState.TrialExpired => "Your trial has expired. Set up payment to continue using plan features.",
                SubscriptionAccessState.PaymentSetupRequired => "Start your trial or add a payment method to unlock plan features.",
                SubscriptionAccessState.Suspended => "This account is suspended. Contact support to reactivate your subscription.",
                SubscriptionAccessState.Cancelled => "This subscription has been cancelled. Resubscribe to continue.",
                _ => "This account does not currently have access to paid features."
            };
            return DeniedDecision(featureCode, subscription.SubscriptionPlan, reason, message);
        }

        switch (subscription.Status)
        {
            case SubscriptionStatus.Suspended:
                return DeniedDecision(featureCode, subscription.SubscriptionPlan, EntitlementDenialReason.SubscriptionSuspended,
                    "This account is suspended. Contact support to reactivate your subscription.");

            case SubscriptionStatus.Restricted when FeatureCodes.WriteBlocked.Contains(featureCode):
                return DeniedDecision(featureCode, subscription.SubscriptionPlan, EntitlementDenialReason.SubscriptionRestricted,
                    "This account is restricted pending payment. Update your payment details to resume full access.");

            case SubscriptionStatus.PastDue
                when access.State == SubscriptionAccessState.PastDue && FeatureCodes.WriteBlocked.Contains(featureCode):
                return DeniedDecision(featureCode, subscription.SubscriptionPlan, EntitlementDenialReason.SubscriptionRestricted,
                    "The seven-day payment grace period has ended. Update payment details to resume transactional features.");

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

        if (clock.GetUtcNow().UtcDateTime >= options.LegacyGraceCutoverUtc)
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
            effectivePlan = clock.GetUtcNow().UtcDateTime < options.LegacyGraceCutoverUtc
                ? await context.SubscriptionPlans.SingleOrDefaultAsync(p => p.Code == options.LegacyEquivalentPlanCode, ct)
                : null;
        }

        var now = clock.GetUtcNow().UtcDateTime;
        var access = subscription is null || subscription.Status == SubscriptionStatus.LegacyUnsubscribed
            ? new EvaluatedAccess(SubscriptionAccessState.Legacy, false, false, false, false)
            : EvaluateAccess(subscription, now);

        if (!access.CanUsePaidFeatures && subscription?.Status != SubscriptionStatus.LegacyUnsubscribed)
        {
            effectivePlan = subscription?.SubscriptionPlan;
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
                    planFeature.IsEnabled && access.CanUsePaidFeatures,
                    planFeature.LimitValue,
                    ExtractEnumValue(planFeature.ConfigurationJson));
            }
        }

        return new EntitlementSnapshot(
            stokvelId,
            effectivePlan?.Code,
            effectivePlan?.Name,
            status,
            access.State,
            access.IsTrial,
            subscription?.TrialStartedAt,
            subscription?.TrialEndsAt,
            CalculateTrialDaysRemaining(subscription?.TrialEndsAt, now),
            access.IsExpired,
            access.CanUsePaidFeatures,
            access.PaymentSetupRequired,
            subscription?.NextBillingAt,
            features);
    }

    private static async Task<OrganisationSubscription?> ResolveCurrentSubscriptionAsync(
        ApplicationDbContext context, Guid stokvelId, CancellationToken ct)
    {
        var subscriptions = await context.OrganisationSubscriptions
            .Include(s => s.SubscriptionPlan)
            .Include(s => s.PaymentMethods)
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

    private static EvaluatedAccess EvaluateAccess(OrganisationSubscription subscription, DateTime now)
    {
        var hasPaymentMethod = subscription.PaymentMethods.Any(method =>
            method.RemovedAt == null && method.IsDefault && SubscriptionPaymentReadiness.IsReady(method));
        var hasExplicitActiveTrial = subscription.Status == SubscriptionStatus.Trialing &&
            subscription.TrialOptedIn &&
            subscription.TrialStartedAt is { } trialStart &&
            subscription.TrialEndsAt is { } trialEnd &&
            trialStart <= now && now < trialEnd;

        if (subscription.Status == SubscriptionStatus.Trialing)
        {
            return hasExplicitActiveTrial
                ? new(SubscriptionAccessState.TrialActive, true, true, false, !hasPaymentMethod)
                : new(SubscriptionAccessState.TrialExpired, false, false, true, !hasPaymentMethod);
        }

        return subscription.Status switch
        {
            SubscriptionStatus.Active => new(SubscriptionAccessState.Active, false, true, false, false),
            SubscriptionStatus.PastDue when subscription.GracePeriodEndsAt is { } graceEnd && now < graceEnd
                => new(SubscriptionAccessState.GracePeriod, false, true, false, false),
            SubscriptionStatus.PastDue => new(SubscriptionAccessState.PastDue, false, true, false, false),
            SubscriptionStatus.Restricted => new(SubscriptionAccessState.Restricted, false, true, false, false),
            SubscriptionStatus.Suspended => new(SubscriptionAccessState.Suspended, false, false, true, false),
            SubscriptionStatus.Cancelled when subscription.CurrentPeriodEndsAt is { } periodEnd && now < periodEnd
                => new(SubscriptionAccessState.Cancelled, false, true, false, false),
            SubscriptionStatus.Cancelled => new(SubscriptionAccessState.Cancelled, false, false, true, false),
            SubscriptionStatus.Expired => new(SubscriptionAccessState.Expired, false, false, true, !hasPaymentMethod),
            SubscriptionStatus.PendingPaymentMethod => new(SubscriptionAccessState.PaymentSetupRequired, false, false, true, !hasPaymentMethod),
            _ => new(SubscriptionAccessState.Legacy, false, false, false, false)
        };
    }

    private static int CalculateTrialDaysRemaining(DateTime? trialEndsAt, DateTime now)
    {
        if (trialEndsAt is null || now >= trialEndsAt.Value)
        {
            return 0;
        }

        return (int)Math.Ceiling((trialEndsAt.Value - now).TotalDays);
    }

    private sealed record EvaluatedAccess(
        SubscriptionAccessState State,
        bool IsTrial,
        bool CanUsePaidFeatures,
        bool IsExpired,
        bool PaymentSetupRequired);

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
