using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Services;

public class StokvelService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    StokvelArchetypeConfigurationService archetypeConfigurationService)
{
    public async Task<Stokvel?> RegisterStokvelAsync(
        string name,
        StokvelType type,
        StokvelArchetype archetype,
        string? province,
        string? townOrArea,
        DateTime? establishedDate,
        int? expectedMemberCount,
        Guid subscriptionPlanId,
        string? description,
        StokvelBankingDetails? bankingDetails = null,
        string? currentUserId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        await using var context = await dbFactory.CreateDbContextAsync();

        var plan = await context.SubscriptionPlans
            .SingleOrDefaultAsync(subscriptionPlan =>
                subscriptionPlan.Id == subscriptionPlanId &&
                subscriptionPlan.IsActive &&
                subscriptionPlan.Name != "Pilot");

        if (plan is null)
        {
            return null;
        }

        if (HasBankingDetails(bankingDetails) && !IsValidBankingDetails(bankingDetails!))
        {
            return null;
        }

        var trimmedName = name.Trim();
        var createdAt = DateTime.UtcNow;
        var stokvelCode = await GenerateStokvelCode(context, trimmedName);
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = trimmedName,
            Slug = await CreateUniqueSlugAsync(context, trimmedName),
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
            Archetype = archetype,
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

        archetypeConfigurationService.ApplyDefaults(stokvel, archetype);

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

        if (HasBankingDetails(bankingDetails))
        {
            context.StokvelBankingDetails.Add(new StokvelBankingDetails
            {
                Id = Guid.NewGuid(),
                StokvelId = stokvel.Id,
                Stokvel = stokvel,
                BankName = bankingDetails!.BankName.Trim(),
                AccountHolderName = bankingDetails.AccountHolderName.Trim(),
                AccountNumber = bankingDetails.AccountNumber.Trim(),
                AccountType = bankingDetails.AccountType,
                BranchCode = NullIfWhiteSpace(bankingDetails.BranchCode),
                BranchName = NullIfWhiteSpace(bankingDetails.BranchName),
                PaymentReferenceFormat = NullIfWhiteSpace(bankingDetails.PaymentReferenceFormat),
                Notes = NullIfWhiteSpace(bankingDetails.Notes),
                IsPrimary = true,
                IsActive = true,
                CreatedAt = createdAt,
                CreatedBy = currentUserId
            });
        }

        await context.SaveChangesAsync();

        return stokvel;
    }

    public async Task<string> GenerateStokvelCode(string stokvelName)
    {
        await using var context = await dbFactory.CreateDbContextAsync();
        return await GenerateStokvelCode(context, stokvelName);
    }

    private static async Task<string> GenerateStokvelCode(ApplicationDbContext context, string stokvelName)
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
        await using var context = await dbFactory.CreateDbContextAsync();

        return await context.SubscriptionPlans
            .Where(subscriptionPlan =>
                subscriptionPlan.IsActive &&
                subscriptionPlan.Name != "Pilot")
            .OrderBy(subscriptionPlan => subscriptionPlan.MinMembers)
            .ToListAsync();
    }

    public async Task<SubscriptionPlan?> GetRecommendedSubscriptionPlanAsync(int? expectedMemberCount)
    {
        await using var context = await dbFactory.CreateDbContextAsync();

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
        await using var context = await dbFactory.CreateDbContextAsync();

        return await context.Stokvels
            .Include(stokvel => stokvel.Tenant)
            .Where(stokvel => stokvel.IsActive && !stokvel.IsDeleted)
            .OrderBy(stokvel => stokvel.Name)
            .ToListAsync();
    }

    public async Task<Stokvel?> GetStokvelByIdAsync(Guid stokvelId)
    {
        await using var context = await dbFactory.CreateDbContextAsync();

        return await context.Stokvels
            .Include(stokvel => stokvel.Tenant)
            .SingleOrDefaultAsync(stokvel =>
                stokvel.Id == stokvelId &&
                stokvel.IsActive &&
                !stokvel.IsDeleted);
    }

    public async Task<Stokvel?> GetStokvelForSettingsAsync(Guid stokvelId)
    {
        await using var context = await dbFactory.CreateDbContextAsync();

        return await context.Stokvels
            .Include(stokvel => stokvel.Tenant)
            .SingleOrDefaultAsync(stokvel => stokvel.Id == stokvelId);
    }

    public async Task<(bool Success, string? Error)> UpdateStokvelDetailsAsync(
        Guid stokvelId,
        string name,
        StokvelArchetype archetype,
        string? description,
        string? province,
        string? townOrArea,
        DateTime? establishedDate,
        int? expectedMemberCount,
        string currentUserId)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return (false, "Stokvel name is required.");
        }

        await using var context = await dbFactory.CreateDbContextAsync();

        var stokvel = await context.Stokvels
            .SingleOrDefaultAsync(s => s.Id == stokvelId && s.IsActive && !s.IsDeleted);

        if (stokvel is null)
        {
            return (false, "Stokvel not found.");
        }

        var trimmedName = name.Trim();

        var nameConflict = await context.Stokvels
            .AnyAsync(s =>
                s.Id != stokvelId &&
                s.TenantId == stokvel.TenantId &&
                s.Name == trimmedName &&
                !s.IsDeleted);

        if (nameConflict)
        {
            return (false, "A stokvel with that name already exists.");
        }

        stokvel.Name = trimmedName;
        stokvel.Type = MapArchetypeToStokvelType(archetype);
        archetypeConfigurationService.ApplyDefaults(stokvel, archetype);
        stokvel.Description = description?.Trim();
        stokvel.Province = province?.Trim();
        stokvel.TownOrArea = townOrArea?.Trim();
        stokvel.EstablishedDate = establishedDate;
        stokvel.ExpectedMemberCount = expectedMemberCount;
        stokvel.UpdatedAt = DateTime.UtcNow;
        stokvel.UpdatedBy = currentUserId;

        await context.SaveChangesAsync();

        return (true, null);
    }

    public static StokvelType MapArchetypeToStokvelType(StokvelArchetype archetype) =>
        archetype switch
        {
            StokvelArchetype.BurialSociety => StokvelType.BurialSociety,
            StokvelArchetype.Rotational => StokvelType.RotationalStokvel,
            StokvelArchetype.Grocery => StokvelType.GroceryStokvel,
            StokvelArchetype.InvestmentClub => StokvelType.InvestmentStokvel,
            StokvelArchetype.SavingsClub => StokvelType.SavingsStokvel,
            StokvelArchetype.Education => StokvelType.SavingsStokvel,
            StokvelArchetype.Borrowing => StokvelType.LoanStokvel,
            StokvelArchetype.Travel => StokvelType.SavingsStokvel,
            StokvelArchetype.SocialClub => StokvelType.SocialClub,
            _ => StokvelType.BurialSociety
        };

    public async Task<(bool Success, string? Error)> SoftDeleteStokvelAsync(
        Guid stokvelId,
        string currentUserId,
        string deleteReason,
        string confirmationName)
    {
        if (string.IsNullOrWhiteSpace(deleteReason))
        {
            return (false, "A reason for deletion is required.");
        }

        await using var context = await dbFactory.CreateDbContextAsync();

        var stokvel = await context.Stokvels
            .SingleOrDefaultAsync(s => s.Id == stokvelId && !s.IsDeleted);

        if (stokvel is null)
        {
            return (false, "Stokvel not found.");
        }

        if (!string.Equals(confirmationName.Trim(), stokvel.Name, StringComparison.Ordinal))
        {
            return (false, "The stokvel name you entered does not match. Deletion cancelled.");
        }

        stokvel.IsDeleted = true;
        stokvel.IsActive = false;
        stokvel.DeletedAt = DateTime.UtcNow;
        stokvel.DeletedBy = currentUserId;
        stokvel.DeleteReason = deleteReason.Trim();

        await context.SaveChangesAsync();

        return (true, null);
    }

    public async Task<Stokvel?> GetStokvelByTenantIdAsync(Guid tenantId)
    {
        await using var context = await dbFactory.CreateDbContextAsync();

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
        await using var context = await dbFactory.CreateDbContextAsync();

        var stokvel = await GetStokvelByIdForLookupAsync(context, stokvelId);

        if (stokvel is null)
        {
            return null;
        }

        return await GetLatestActiveSubscriptionByTenantIdAsync(context, stokvel.TenantId);
    }

    public async Task<bool> CanAddMemberAsync(Guid stokvelId)
    {
        await using var context = await dbFactory.CreateDbContextAsync();

        var stokvel = await GetStokvelByIdForLookupAsync(context, stokvelId);

        if (stokvel is null)
        {
            return false;
        }

        var subscription = await GetLatestActiveSubscriptionByTenantIdAsync(context, stokvel.TenantId);

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
        await using var context = await dbFactory.CreateDbContextAsync();

        var stokvel = await GetStokvelByIdForLookupAsync(context, stokvelId);

        if (stokvel is null)
        {
            return "Subscription package could not be found.";
        }

        var subscription = await GetLatestActiveSubscriptionByTenantIdAsync(context, stokvel.TenantId);

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

    private static async Task<Stokvel?> GetStokvelByIdForLookupAsync(ApplicationDbContext context, Guid stokvelId)
    {
        // Duplicate-tolerant lookup for local/demo data.
        return await context.Stokvels
            .Where(existingStokvel => existingStokvel.Id == stokvelId)
            .OrderBy(existingStokvel => existingStokvel.CreatedAt)
            .ThenBy(existingStokvel => existingStokvel.Id)
            .FirstOrDefaultAsync();
    }

    private static async Task<TenantSubscription?> GetLatestActiveSubscriptionByTenantIdAsync(ApplicationDbContext context, Guid tenantId)
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

    private static async Task<string> CreateUniqueSlugAsync(ApplicationDbContext context, string name)
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

    private static bool HasBankingDetails(StokvelBankingDetails? bankingDetails) =>
        bankingDetails is not null &&
        (!string.IsNullOrWhiteSpace(bankingDetails.BankName) ||
         !string.IsNullOrWhiteSpace(bankingDetails.AccountHolderName) ||
         !string.IsNullOrWhiteSpace(bankingDetails.AccountNumber) ||
         !string.IsNullOrWhiteSpace(bankingDetails.BranchCode) ||
         !string.IsNullOrWhiteSpace(bankingDetails.BranchName) ||
         !string.IsNullOrWhiteSpace(bankingDetails.PaymentReferenceFormat) ||
         !string.IsNullOrWhiteSpace(bankingDetails.Notes));

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool IsValidBankingDetails(StokvelBankingDetails bankingDetails) =>
        !string.IsNullOrWhiteSpace(bankingDetails.BankName) &&
        !string.IsNullOrWhiteSpace(bankingDetails.AccountHolderName) &&
        !string.IsNullOrWhiteSpace(bankingDetails.AccountNumber) &&
        bankingDetails.AccountNumber.All(char.IsDigit) &&
        (string.IsNullOrWhiteSpace(bankingDetails.BranchCode) || bankingDetails.BranchCode.All(char.IsDigit)) &&
        Enum.IsDefined(bankingDetails.AccountType);
}
