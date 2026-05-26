using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Services;

public class StokvelService(ApplicationDbContext context)
{
    public async Task<Stokvel?> RegisterStokvelAsync(
        string name,
        StokvelType type,
        string? province,
        string? townOrArea,
        DateTime? establishedDate,
        string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var trimmedName = name.Trim();
        var createdAt = DateTime.UtcNow;
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = trimmedName,
            Slug = await CreateUniqueSlugAsync(trimmedName),
            IsActive = true,
            CreatedAt = createdAt
        };

        var stokvel = new Stokvel
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Tenant = tenant,
            Name = trimmedName,
            Type = type,
            Province = province,
            TownOrArea = townOrArea,
            EstablishedDate = establishedDate,
            Description = description,
            IsActive = true,
            IsSetupComplete = false,
            SetupCompletedAt = null,
            CreatedAt = createdAt
        };

        var plan = await context.SubscriptionPlans
            .Where(subscriptionPlan => subscriptionPlan.IsActive)
            .OrderByDescending(subscriptionPlan => subscriptionPlan.Name == "Pilot")
            .ThenBy(subscriptionPlan => subscriptionPlan.Name)
            .FirstOrDefaultAsync();

        if (plan is not null)
        {
            context.TenantSubscriptions.Add(new TenantSubscription
            {
                TenantId = tenant.Id,
                Tenant = tenant,
                SubscriptionPlanId = plan.Id,
                SubscriptionPlan = plan,
                Status = SubscriptionStatus.Active,
                StartDate = createdAt,
                IsTrial = true
            });
        }

        context.Stokvels.Add(stokvel);
        await context.SaveChangesAsync();

        return stokvel;
    }

    public async Task<List<Stokvel>> GetAllStokvelsAsync()
    {
        return await context.Stokvels
            .Include(stokvel => stokvel.Tenant)
            .Where(stokvel => stokvel.IsActive)
            .OrderBy(stokvel => stokvel.Name)
            .ToListAsync();
    }

    public async Task<Stokvel?> GetStokvelByIdAsync(Guid stokvelId)
    {
        return await context.Stokvels
            .Include(stokvel => stokvel.Tenant)
            .SingleOrDefaultAsync(stokvel =>
                stokvel.Id == stokvelId &&
                stokvel.IsActive);
    }

    public async Task<Stokvel?> GetStokvelByTenantIdAsync(Guid tenantId)
    {
        return await context.Stokvels
            .Include(stokvel => stokvel.Tenant)
            .Where(stokvel =>
                stokvel.TenantId == tenantId &&
                stokvel.IsActive)
            .OrderBy(stokvel => stokvel.Name)
            .FirstOrDefaultAsync();
    }

    private async Task<string> CreateUniqueSlugAsync(string name)
    {
        var baseSlug = CreateSlug(name);
        var slug = baseSlug;

        while (await context.Tenants.AnyAsync(tenant => tenant.Slug == slug))
        {
            slug = $"{baseSlug}-{Random.Shared.Next(1000, 10000)}";
        }

        return slug;
    }

    private static string CreateSlug(string value)
    {
        var slug = value
            .Trim()
            .ToLowerInvariant()
            .Replace("'", string.Empty)
            .Replace(" ", "-");

        return string.IsNullOrWhiteSpace(slug)
            ? $"stokvel-{Guid.NewGuid():N}"[..20]
            : slug;
    }
}
