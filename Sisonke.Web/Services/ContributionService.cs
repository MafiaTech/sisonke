using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Services;

public class ContributionService(ApplicationDbContext context)
{
    public async Task<ContributionRule?> GetActiveContributionRuleByStokvelIdAsync(Guid stokvelId)
    {
        var stokvel = await context.Stokvels
            .SingleOrDefaultAsync(existingStokvel => existingStokvel.Id == stokvelId);

        if (stokvel is null)
        {
            return null;
        }

        return await context.Set<ContributionRule>()
            .SingleOrDefaultAsync(rule =>
                rule.TenantId == stokvel.TenantId &&
                rule.IsActive);
    }

    public async Task<ContributionRule?> SaveContributionRuleAsync(Guid stokvelId, ContributionRule rule)
    {
        var stokvel = await context.Stokvels
            .SingleOrDefaultAsync(existingStokvel => existingStokvel.Id == stokvelId);

        if (stokvel is null)
        {
            return null;
        }

        var existingActiveRules = await context.Set<ContributionRule>()
            .Where(existingRule =>
                existingRule.TenantId == stokvel.TenantId &&
                existingRule.IsActive)
            .ToListAsync();

        foreach (var existingRule in existingActiveRules)
        {
            existingRule.IsActive = false;
        }

        if (rule.Id == Guid.Empty)
        {
            rule.Id = Guid.NewGuid();
        }

        rule.TenantId = stokvel.TenantId;
        rule.IsActive = true;
        rule.CreatedAt = DateTime.UtcNow;

        context.Set<ContributionRule>().Add(rule);
        await context.SaveChangesAsync();

        return rule;
    }

    public async Task<decimal> GetExpectedMonthlyContributionsByStokvelIdAsync(Guid stokvelId)
    {
        var stokvel = await context.Stokvels
            .SingleOrDefaultAsync(existingStokvel => existingStokvel.Id == stokvelId);

        if (stokvel is null)
        {
            return 0;
        }

        var rule = await context.Set<ContributionRule>()
            .SingleOrDefaultAsync(contributionRule =>
                contributionRule.TenantId == stokvel.TenantId &&
                contributionRule.IsActive);

        if (rule is null)
        {
            return 0;
        }

        var activeMemberCount = await context.Members
            .CountAsync(member =>
                member.TenantId == stokvel.TenantId &&
                member.Status == MemberStatus.Active);

        return activeMemberCount * rule.Amount;
    }
}
