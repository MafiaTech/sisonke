using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;

namespace Sisonke.Web.Services;

public static class ContributionRuleResolver
{
    public static Task<ContributionRule?> ResolveAsync(ApplicationDbContext db, Guid tenantId, DateTime asOf) =>
        db.ContributionRules.AsNoTracking()
            .Where(r => r.TenantId == tenantId && r.IsActive && r.EffectiveFrom < asOf.Date.AddDays(1))
            .OrderByDescending(r => r.EffectiveFrom).ThenByDescending(r => r.CreatedAt).ThenBy(r => r.Id)
            .FirstOrDefaultAsync();
}
