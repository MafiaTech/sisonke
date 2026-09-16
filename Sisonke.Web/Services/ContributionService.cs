using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Services;

public class ContributionService(IDbContextFactory<ApplicationDbContext> dbFactory)
{
    private readonly ApplicationDbContext? transactionContext;
    // Explicit short-lived transaction/legacy callers only. DI uses the factory constructor.
    public ContributionService(ApplicationDbContext context) : this((IDbContextFactory<ApplicationDbContext>)null!)
    {
        transactionContext = context;
    }

    public async Task<ContributionRule?> GetActiveContributionRuleByStokvelIdAsync(Guid stokvelId)
    {
        await using var operation = await DbContextOperation.OpenAsync(dbFactory, transactionContext);
        var context = operation.Context;

        var stokvel = await context.Stokvels
            .SingleOrDefaultAsync(existingStokvel => existingStokvel.Id == stokvelId);

        if (stokvel is null)
        {
            return null;
        }

        return await ContributionRuleResolver.ResolveAsync(context, stokvel.TenantId, DateTime.Today);
    }

    public async Task<ContributionRule?> SaveContributionRuleAsync(Guid stokvelId, ContributionRule rule)
    {
        await using var operation = await DbContextOperation.OpenAsync(dbFactory, transactionContext);
        var context = operation.Context;

        if (rule.Amount < 0 || rule.DueDayOfMonth is < 1 or > 31) return null;
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
        await using var operation = await DbContextOperation.OpenAsync(dbFactory, transactionContext);
        var context = operation.Context;

        var stokvel = await context.Stokvels
            .SingleOrDefaultAsync(existingStokvel => existingStokvel.Id == stokvelId);

        if (stokvel is null)
        {
            return 0;
        }

        var rule = await ContributionRuleResolver.ResolveAsync(context, stokvel.TenantId, DateTime.Today);

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
