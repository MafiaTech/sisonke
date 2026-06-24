using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;

namespace Sisonke.Web.Services;

public class RotationalConfigurationService(IDbContextFactory<ApplicationDbContext> dbFactory)
{
    public async Task<RotationalStokvelConfiguration?> GetActiveConfigurationAsync(Guid stokvelId)
    {
        await using var context = await dbFactory.CreateDbContextAsync();
        return await context.RotationalStokvelConfigurations
            .Where(c => c.StokvelId == stokvelId && c.IsActive)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<RotationalStokvelConfiguration?> GetConfigurationByIdAsync(Guid id)
    {
        await using var context = await dbFactory.CreateDbContextAsync();
        return await context.RotationalStokvelConfigurations
            .SingleOrDefaultAsync(c => c.Id == id);
    }

    public async Task<RotationalStokvelConfiguration> SaveConfigurationAsync(
        Guid stokvelId,
        RotationalStokvelConfiguration model,
        string currentUserId)
    {
        await using var context = await dbFactory.CreateDbContextAsync();

        var existing = await context.RotationalStokvelConfigurations
            .Where(c => c.StokvelId == stokvelId && c.IsActive)
            .ToListAsync();

        foreach (var cfg in existing)
        {
            cfg.IsActive = false;
            cfg.UpdatedAt = DateTime.UtcNow;
            cfg.UpdatedBy = currentUserId;
        }

        var newConfig = new RotationalStokvelConfiguration
        {
            Id = Guid.NewGuid(),
            StokvelId = stokvelId,
            ContributionAmount = model.ContributionAmount,
            ContributionFrequency = model.ContributionFrequency,
            ContributionDueDay = model.ContributionDueDay,
            PayoutAmount = model.PayoutAmount,
            PayoutFrequency = model.PayoutFrequency,
            RotationStartDate = model.RotationStartDate,
            RotationOrderMethod = model.RotationOrderMethod,
            AllowPayoutTurnSwap = model.AllowPayoutTurnSwap,
            LatePenaltyType = model.LatePenaltyType,
            LatePenaltyAmount = model.LatePenaltyAmount,
            GracePeriodDays = model.GracePeriodDays,
            MinimumBalanceBeforePayout = model.MinimumBalanceBeforePayout,
            MissedContributionBlocksPayout = model.MissedContributionBlocksPayout,
            TreasurerConfirmationRequired = model.TreasurerConfirmationRequired,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = currentUserId
        };

        context.RotationalStokvelConfigurations.Add(newConfig);
        await context.SaveChangesAsync();
        return newConfig;
    }

    public async Task<RotationalStokvelConfiguration?> UpdateConfigurationAsync(
        Guid id,
        RotationalStokvelConfiguration model,
        string currentUserId)
    {
        await using var context = await dbFactory.CreateDbContextAsync();

        var config = await context.RotationalStokvelConfigurations
            .SingleOrDefaultAsync(c => c.Id == id);

        if (config is null) return null;

        config.ContributionAmount = model.ContributionAmount;
        config.ContributionFrequency = model.ContributionFrequency;
        config.ContributionDueDay = model.ContributionDueDay;
        config.PayoutAmount = model.PayoutAmount;
        config.PayoutFrequency = model.PayoutFrequency;
        config.RotationStartDate = model.RotationStartDate;
        config.RotationOrderMethod = model.RotationOrderMethod;
        config.AllowPayoutTurnSwap = model.AllowPayoutTurnSwap;
        config.LatePenaltyType = model.LatePenaltyType;
        config.LatePenaltyAmount = model.LatePenaltyAmount;
        config.GracePeriodDays = model.GracePeriodDays;
        config.MinimumBalanceBeforePayout = model.MinimumBalanceBeforePayout;
        config.MissedContributionBlocksPayout = model.MissedContributionBlocksPayout;
        config.TreasurerConfirmationRequired = model.TreasurerConfirmationRequired;
        config.UpdatedAt = DateTime.UtcNow;
        config.UpdatedBy = currentUserId;

        await context.SaveChangesAsync();
        return config;
    }

    public async Task DeactivateOldConfigurationsAsync(Guid stokvelId, string currentUserId)
    {
        await using var context = await dbFactory.CreateDbContextAsync();

        var activeConfigs = await context.RotationalStokvelConfigurations
            .Where(c => c.StokvelId == stokvelId && c.IsActive)
            .ToListAsync();

        foreach (var cfg in activeConfigs)
        {
            cfg.IsActive = false;
            cfg.UpdatedAt = DateTime.UtcNow;
            cfg.UpdatedBy = currentUserId;
        }

        await context.SaveChangesAsync();
    }
}
