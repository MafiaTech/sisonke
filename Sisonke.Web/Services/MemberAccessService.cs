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
            role?.Trim().Equals("Creator", StringComparison.OrdinalIgnoreCase) == true ||
            role?.Trim().Equals("Office Bearer", StringComparison.OrdinalIgnoreCase) == true ||
            role?.Trim().Equals("OfficeBearer", StringComparison.OrdinalIgnoreCase) == true;
    }

    public async Task<Member?> GetLinkedMemberForUserAsync(string userId, Guid stokvelId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        var stokvel = await context.Stokvels
            .SingleOrDefaultAsync(existingStokvel => existingStokvel.Id == stokvelId);

        if (stokvel is null)
        {
            return null;
        }

        return await context.Members
            .SingleOrDefaultAsync(member =>
                member.ApplicationUserId == userId &&
                member.TenantId == stokvel.TenantId);
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
            .SingleOrDefaultAsync(existingMember => existingMember.Id == memberId);

        if (member is null)
        {
            return false;
        }

        if (member.ApplicationUserId == userId)
        {
            return true;
        }

        var stokvel = await context.Stokvels
            .SingleOrDefaultAsync(existingStokvel => existingStokvel.TenantId == member.TenantId);

        if (stokvel is null)
        {
            return false;
        }

        return await CanManageStokvelAsync(userId, stokvel.Id);
    }
}
