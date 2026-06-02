using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;

namespace Sisonke.Web.Services;

public class MemberAccessService(ApplicationDbContext context)
{
    private static bool IsOfficeBearerRole(string? role)
    {
        return role?.Trim().Equals("Chairperson", StringComparison.OrdinalIgnoreCase) == true ||
            role?.Trim().Equals("Secretary", StringComparison.OrdinalIgnoreCase) == true ||
            role?.Trim().Equals("Treasurer", StringComparison.OrdinalIgnoreCase) == true ||
            role?.Trim().Equals("Admin", StringComparison.OrdinalIgnoreCase) == true ||
            role?.Trim().Equals("StokvelAdmin", StringComparison.OrdinalIgnoreCase) == true ||
            role?.Trim().Equals("Creator", StringComparison.OrdinalIgnoreCase) == true ||
            role?.Trim().Equals("Office Bearer", StringComparison.OrdinalIgnoreCase) == true ||
            role?.Trim().Equals("OfficeBearer", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static bool CanReviewClaimsRole(string? role)
    {
        var normalizedRole = role?.Trim();

        return normalizedRole?.Equals("Secretary", StringComparison.OrdinalIgnoreCase) == true ||
            normalizedRole?.Equals("Creator", StringComparison.OrdinalIgnoreCase) == true ||
            normalizedRole?.Equals("Admin", StringComparison.OrdinalIgnoreCase) == true ||
            normalizedRole?.Equals("StokvelAdmin", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static bool CanApproveClaimsRole(string? role)
    {
        var normalizedRole = role?.Trim();

        return normalizedRole?.Equals("Chairperson", StringComparison.OrdinalIgnoreCase) == true ||
            normalizedRole?.Equals("Creator", StringComparison.OrdinalIgnoreCase) == true ||
            normalizedRole?.Equals("Admin", StringComparison.OrdinalIgnoreCase) == true ||
            normalizedRole?.Equals("StokvelAdmin", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static bool CanManageMemberStatusRole(string? role)
    {
        var normalizedRole = role?.Trim();

        return normalizedRole?.Equals("Chairperson", StringComparison.OrdinalIgnoreCase) == true ||
            normalizedRole?.Equals("Secretary", StringComparison.OrdinalIgnoreCase) == true ||
            normalizedRole?.Equals("Creator", StringComparison.OrdinalIgnoreCase) == true ||
            normalizedRole?.Equals("Admin", StringComparison.OrdinalIgnoreCase) == true ||
            normalizedRole?.Equals("StokvelAdmin", StringComparison.OrdinalIgnoreCase) == true;
    }

    public async Task<Member?> GetLinkedMemberForUserAsync(string userId, Guid stokvelId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        var stokvel = await context.Stokvels
            .Where(existingStokvel => existingStokvel.Id == stokvelId)
            .OrderBy(existingStokvel => existingStokvel.CreatedAt)
            .ThenBy(existingStokvel => existingStokvel.Name)
            .FirstOrDefaultAsync();

        if (stokvel is null)
        {
            return null;
        }

        var linkedMembers = await context.Members
            .Where(member =>
                member.ApplicationUserId == userId &&
                member.TenantId == stokvel.TenantId)
            .OrderBy(member => member.CreatedAt)
            .ThenBy(member => member.FullName)
            .ToListAsync();

        return linkedMembers.FirstOrDefault(member => IsOfficeBearerRole(member.DefaultRole.ToString())) ??
            linkedMembers.FirstOrDefault();
    }

    public async Task<List<Member>> GetLinkedMembershipsForUserAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return [];
        }

        return await context.Members
            .Include(member => member.Tenant)
            .Where(member => member.ApplicationUserId == userId)
            .OrderBy(member => member.FullName)
            .ToListAsync();
    }

    public async Task<bool> IsOfficeBearerAsync(string userId, Guid stokvelId)
    {
        var member = await GetLinkedMemberForUserAsync(userId, stokvelId);

        return member is not null && IsOfficeBearerRole(member.DefaultRole.ToString());
    }

    public async Task<bool> IsOrdinaryMemberAsync(string userId, Guid stokvelId)
    {
        var member = await GetLinkedMemberForUserAsync(userId, stokvelId);

        return member is not null && !IsOfficeBearerRole(member.DefaultRole.ToString());
    }

    public async Task<bool> CanViewStokvelAsync(string userId, Guid stokvelId)
    {
        return await GetLinkedMemberForUserAsync(userId, stokvelId) is not null;
    }

    public async Task<bool> CanManageStokvelAsync(string userId, Guid stokvelId)
    {
        return await IsOfficeBearerAsync(userId, stokvelId);
    }

    public Task<bool> CanManageMembersAsync(string userId, Guid stokvelId)
    {
        return CanManageStokvelAsync(userId, stokvelId);
    }

    public Task<bool> CanManageConstitutionAsync(string userId, Guid stokvelId)
    {
        return CanManageStokvelAsync(userId, stokvelId);
    }

    public Task<bool> CanManageFinesAsync(string userId, Guid stokvelId)
    {
        return CanManageStokvelAsync(userId, stokvelId);
    }

    public Task<bool> CanManageContributionRulesAsync(string userId, Guid stokvelId)
    {
        return CanManageStokvelAsync(userId, stokvelId);
    }

    public Task<bool> CanApproveRequestsAsync(string userId, Guid stokvelId)
    {
        return CanManageStokvelAsync(userId, stokvelId);
    }

    public Task<bool> CanViewFullDashboardAsync(string userId, Guid stokvelId)
    {
        return CanManageStokvelAsync(userId, stokvelId);
    }

    public async Task<bool> CanViewOwnMemberProfileAsync(string userId, Guid memberId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return false;
        }

        return await context.Members
            .AnyAsync(member => member.Id == memberId && member.ApplicationUserId == userId);
    }

    public async Task<bool> CanViewMemberProfileAsync(string userId, Guid memberId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return false;
        }

        var member = await context.Members
            .Where(existingMember => existingMember.Id == memberId)
            .OrderBy(existingMember => existingMember.CreatedAt)
            .ThenBy(existingMember => existingMember.FullName)
            .FirstOrDefaultAsync();

        if (member is null)
        {
            return false;
        }

        if (member.ApplicationUserId == userId)
        {
            return true;
        }

        var stokvelId = await GetStokvelIdForMemberAsync(memberId);

        return stokvelId is not null && await CanManageStokvelAsync(userId, stokvelId.Value);
    }

    public async Task<Guid?> GetStokvelIdForMemberAsync(Guid memberId)
    {
        var member = await context.Members
            .Where(existingMember => existingMember.Id == memberId)
            .OrderBy(existingMember => existingMember.CreatedAt)
            .ThenBy(existingMember => existingMember.FullName)
            .FirstOrDefaultAsync();

        if (member is null)
        {
            return null;
        }

        var stokvel = await context.Stokvels
            .Where(existingStokvel => existingStokvel.TenantId == member.TenantId)
            .OrderBy(existingStokvel => existingStokvel.CreatedAt)
            .ThenBy(existingStokvel => existingStokvel.Name)
            .FirstOrDefaultAsync();

        return stokvel?.Id;
    }

    public async Task<bool> CanAccessMemberHomeAsync(string userId, Guid memberId)
    {
        return await CanViewMemberProfileAsync(userId, memberId);
    }

    public Task<bool> CanAccessManagementDashboardAsync(string userId, Guid stokvelId)
    {
        return CanManageStokvelAsync(userId, stokvelId);
    }

    public async Task<bool> CanReviewClaimsAsync(string userId, Guid stokvelId)
    {
        var member = await GetLinkedMemberForUserAsync(userId, stokvelId);

        return member is not null && CanReviewClaimsRole(member.DefaultRole.ToString());
    }

    public async Task<bool> CanApproveClaimsAsync(string userId, Guid stokvelId)
    {
        var member = await GetLinkedMemberForUserAsync(userId, stokvelId);

        return member is not null && CanApproveClaimsRole(member.DefaultRole.ToString());
    }

    public async Task<bool> CanManageMemberStatusAsync(string userId, Guid stokvelId)
    {
        var member = await GetLinkedMemberForUserAsync(userId, stokvelId);

        return member is not null && CanManageMemberStatusRole(member.DefaultRole.ToString());
    }
}
