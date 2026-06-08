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
        int? expectedMemberCount,
        Guid subscriptionPlanId,
        string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var plan = await context.SubscriptionPlans
            .SingleOrDefaultAsync(subscriptionPlan =>
                subscriptionPlan.Id == subscriptionPlanId &&
                subscriptionPlan.IsActive &&
                subscriptionPlan.Name != "Pilot");

        if (plan is null)
        {
            return null;
        }

        var trimmedName = name.Trim();
        var createdAt = DateTime.UtcNow;
        var stokvelCode = await GenerateStokvelCode(trimmedName);
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
            Code = stokvelCode,
            Type = type,
            Province = province,
            TownOrArea = townOrArea,
            EstablishedDate = establishedDate,
            ExpectedMemberCount = expectedMemberCount,
            Description = description,
            IsActive = true,
            IsSetupComplete = false,
            SetupCompletedAt = null,
            CreatedAt = createdAt
        };

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

        context.Stokvels.Add(stokvel);
        await context.SaveChangesAsync();

        return stokvel;
    }

    public async Task<string> GenerateStokvelCode(string stokvelName)
    {
        var baseCode = BuildCodeFromName(stokvelName);
        var code = baseCode;
        var suffix = 2;

        while (await context.Stokvels.AnyAsync(stokvel => stokvel.Code == code))
        {
            code = $"{baseCode}{suffix}";
            suffix++;
        }

        return code;
    }

    public static string NormalizeStokvelCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return string.Empty;
        }

        var normalized = new string(code
            .Where(char.IsLetterOrDigit)
            .Select(char.ToUpperInvariant)
            .ToArray());

        return normalized.Length > 6
            ? normalized[..6]
            : normalized;
    }

    public async Task<List<SubscriptionPlan>> GetPublicSubscriptionPlansAsync()
    {
        return await context.SubscriptionPlans
            .Where(subscriptionPlan =>
                subscriptionPlan.IsActive &&
                subscriptionPlan.Name != "Pilot")
            .OrderBy(subscriptionPlan => subscriptionPlan.MinMembers)
            .ToListAsync();
    }

    public async Task<SubscriptionPlan?> GetRecommendedSubscriptionPlanAsync(int? expectedMemberCount)
    {
        if (expectedMemberCount is null || expectedMemberCount < 1)
        {
            return null;
        }

        return await context.SubscriptionPlans
            .Where(subscriptionPlan =>
                subscriptionPlan.IsActive &&
                subscriptionPlan.Name != "Pilot" &&
                expectedMemberCount >= subscriptionPlan.MinMembers &&
                (subscriptionPlan.MaxMembers == null || expectedMemberCount <= subscriptionPlan.MaxMembers))
            .OrderBy(subscriptionPlan => subscriptionPlan.MinMembers)
            .FirstOrDefaultAsync();
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

    public async Task<TenantSubscription?> GetActiveSubscriptionByStokvelIdAsync(Guid stokvelId)
    {
        var stokvel = await GetStokvelByIdForLookupAsync(stokvelId);

        if (stokvel is null)
        {
            return null;
        }

        return await GetLatestActiveSubscriptionByTenantIdAsync(stokvel.TenantId);
    }

    public async Task<bool> CanAddMemberAsync(Guid stokvelId)
    {
        var stokvel = await GetStokvelByIdForLookupAsync(stokvelId);

        if (stokvel is null)
        {
            return false;
        }

        var subscription = await GetLatestActiveSubscriptionByTenantIdAsync(stokvel.TenantId);

        if (subscription?.SubscriptionPlan is null)
        {
            return false;
        }

        if (subscription.SubscriptionPlan.MaxMembers is null)
        {
            return true;
        }

        var activeMemberCount = await context.Members
            .CountAsync(member =>
                member.TenantId == stokvel.TenantId &&
                member.Status == MemberStatus.Active);

        return activeMemberCount < subscription.SubscriptionPlan.MaxMembers;
    }

    public async Task<string> GetMemberLimitMessageAsync(Guid stokvelId)
    {
        var stokvel = await GetStokvelByIdForLookupAsync(stokvelId);

        if (stokvel is null)
        {
            return "Subscription package could not be found.";
        }

        var subscription = await GetLatestActiveSubscriptionByTenantIdAsync(stokvel.TenantId);

        if (subscription?.SubscriptionPlan is null)
        {
            return "Subscription package could not be found.";
        }

        var activeMemberCount = await context.Members
            .CountAsync(member =>
                member.TenantId == stokvel.TenantId &&
                member.Status == MemberStatus.Active);

        if (subscription.SubscriptionPlan.MaxMembers is null)
        {
            return "Unlimited members allowed on this package.";
        }

        return $"{activeMemberCount} of {subscription.SubscriptionPlan.MaxMembers} members captured on the {subscription.SubscriptionPlan.Name} package.";
    }

    private async Task<Stokvel?> GetStokvelByIdForLookupAsync(Guid stokvelId)
    {
        // Duplicate-tolerant lookup for local/demo data.
        return await context.Stokvels
            .Where(existingStokvel => existingStokvel.Id == stokvelId)
            .OrderBy(existingStokvel => existingStokvel.CreatedAt)
            .ThenBy(existingStokvel => existingStokvel.Id)
            .FirstOrDefaultAsync();
    }

    private async Task<TenantSubscription?> GetLatestActiveSubscriptionByTenantIdAsync(Guid tenantId)
    {
        // Duplicate-tolerant lookup for local/demo data.
        return await context.TenantSubscriptions
            .Include(existingSubscription => existingSubscription.SubscriptionPlan)
            .Where(existingSubscription =>
                existingSubscription.TenantId == tenantId &&
                existingSubscription.Status == SubscriptionStatus.Active)
            .OrderByDescending(existingSubscription => existingSubscription.StartDate)
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

    private static string BuildCodeFromName(string stokvelName)
    {
        var words = stokvelName
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(word => new string(word.Where(char.IsLetterOrDigit).ToArray()))
            .Where(word => !string.IsNullOrWhiteSpace(word))
            .ToList();

        var singleWordCode = NormalizeStokvelCode(words.FirstOrDefault() ?? stokvelName);
        var code = words.Count > 1
            ? new string(words.Select(word => char.ToUpperInvariant(word[0])).ToArray())
            : singleWordCode.Length >= 3 ? singleWordCode[..3] : singleWordCode;

        if (code.Length > 6)
        {
            code = code[..6];
        }

        if (code.Length < 3)
        {
            var compactName = NormalizeStokvelCode(stokvelName);
            code = compactName.Length >= 3 ? compactName[..3] : compactName;
        }

        return string.IsNullOrWhiteSpace(code)
            ? "STK"
            : code;
    }
}
