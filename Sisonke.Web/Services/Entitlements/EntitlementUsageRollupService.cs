using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;
using Sisonke.Web.Services.Billing;
using Sisonke.Web.Services.Jobs;

namespace Sisonke.Web.Services.Entitlements;

/// <summary>
/// Periodically persists SubscriptionUsage rollups for reporting/UI display (never consulted by
/// AuthorizeAsync, which always reads live usage), and — Phase 4 — warns administrators once
/// usage crosses 80%/100% of MAX_MEMBERS, MAX_ADMINISTRATORS or STORAGE_MB. Warning dedup relies
/// on SubscriptionNotificationService/NotificationEnqueuer's own per-(trigger,day,feature,
/// threshold) dedupe key — no separate marker needed.
/// </summary>
public sealed class EntitlementUsageRollupService(
    IServiceScopeFactory scopeFactory,
    EntitlementOptions options,
    TimeProvider timeProvider,
    ILogger<EntitlementUsageRollupService> logger) : BackgroundService
{
    public const string JobName = "EntitlementUsageRollupJob";

    private static readonly (string Code, string Description)[] WarnableFeatures =
    [
        (FeatureCodes.MaxMembers, "active members"),
        (FeatureCodes.MaxAdministrators, "administrators"),
        (FeatureCodes.StorageMb, "MB of storage")
    ];

    private static readonly string[] NumericFeatureCodes =
    [
        FeatureCodes.MaxMembers,
        FeatureCodes.MaxAdministrators,
        FeatureCodes.MaxSchemes,
        FeatureCodes.StorageMb
    ];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(Math.Max(1, options.UsageRollupIntervalMinutes)));

        do
        {
            try
            {
                await RollUpAllAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Entitlement usage rollup tick failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    public async Task RollUpAllAsync(CancellationToken ct)
    {
        // IDistributedJobLock is Scoped (its DistributedJobLock implementation holds an
        // IDbContextFactory) — like every other dependency here, it must be resolved from a
        // per-tick scope rather than injected into this Singleton hosted service's constructor,
        // or ASP.NET Core's DI container fails ServiceProvider validation at startup.
        using var scope = scopeFactory.CreateScope();
        var jobLock = scope.ServiceProvider.GetRequiredService<IDistributedJobLock>();

        await using var lockHandle = await jobLock.TryAcquireAsync(JobName, TimeSpan.FromMinutes(30), ct);
        if (lockHandle is null)
        {
            logger.LogInformation("{Job} is already running on another instance — skipping this tick.", JobName);
            return;
        }

        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        var usageProvider = scope.ServiceProvider.GetRequiredService<IEntitlementUsageProvider>();
        var notificationService = scope.ServiceProvider.GetRequiredService<SubscriptionNotificationService>();

        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var liveSubscriptions = await context.OrganisationSubscriptions
            .Include(s => s.SubscriptionPlan)
            .Where(s => s.Status != SubscriptionStatus.Cancelled && s.Status != SubscriptionStatus.Expired)
            .ToListAsync(ct);

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var periodStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var periodEnd = periodStart.AddMonths(1);

        foreach (var subscription in liveSubscriptions)
        {
            foreach (var featureCode in NumericFeatureCodes)
            {
                var usedValue = await usageProvider.GetCurrentUsageAsync(subscription.StokvelId, featureCode, ct);

                var usageRow = await context.SubscriptionUsages.SingleOrDefaultAsync(
                    u => u.OrganisationSubscriptionId == subscription.Id &&
                         u.FeatureCode == featureCode &&
                         u.PeriodStart == periodStart,
                    ct);

                if (usageRow is null)
                {
                    usageRow = new SubscriptionUsage
                    {
                        Id = Guid.NewGuid(),
                        OrganisationSubscriptionId = subscription.Id,
                        FeatureCode = featureCode,
                        PeriodStart = periodStart,
                        PeriodEnd = periodEnd
                    };
                    context.SubscriptionUsages.Add(usageRow);
                }

                usageRow.UsedValue = usedValue;
                usageRow.LimitValue = await GetPlanLimitAsync(context, subscription, featureCode, ct);
                usageRow.LastCalculatedAt = now;
            }

            await context.SaveChangesAsync(ct);
            await WarnIfOverThresholdAsync(context, notificationService, subscription, usageProvider, ct);
        }
    }

    private async Task WarnIfOverThresholdAsync(
        ApplicationDbContext context, SubscriptionNotificationService notificationService, OrganisationSubscription subscription,
        IEntitlementUsageProvider usageProvider, CancellationToken ct)
    {
        if (subscription.SubscriptionPlan is null)
        {
            return;
        }

        foreach (var (code, description) in WarnableFeatures)
        {
            var limit = await GetPlanLimitAsync(context, subscription, code, ct);
            if (limit is null)
            {
                continue; // Unlimited on this plan — nothing to warn about.
            }

            var used = await usageProvider.GetCurrentUsageAsync(subscription.StokvelId, code, ct);
            var percent = limit.Value == 0 ? 100 : (int)Math.Floor(used * 100.0 / limit.Value);

            if (percent >= 100)
            {
                await notificationService.SendUsageWarningAsync(context, subscription, description, used, limit.Value, 100, ct);
            }
            else if (percent >= 80)
            {
                await notificationService.SendUsageWarningAsync(context, subscription, description, used, limit.Value, 80, ct);
            }
        }
    }

    private static async Task<int?> GetPlanLimitAsync(ApplicationDbContext context, OrganisationSubscription subscription, string featureCode, CancellationToken ct)
    {
        if (subscription.SubscriptionPlanId is null)
        {
            return null;
        }

        var planFeature = await context.PlanFeatures
            .Include(pf => pf.FeatureDefinition)
            .FirstOrDefaultAsync(pf => pf.SubscriptionPlanId == subscription.SubscriptionPlanId && pf.FeatureDefinition.Code == featureCode, ct);

        return planFeature?.LimitValue;
    }
}
