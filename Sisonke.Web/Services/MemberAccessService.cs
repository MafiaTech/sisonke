using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;

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

    private static bool CanMakeDisciplinaryDecisionRole(string? role)
    {
        var normalizedRole = role?.Trim();

        return normalizedRole?.Equals("Chairperson", StringComparison.OrdinalIgnoreCase) == true ||
            normalizedRole?.Equals("Creator", StringComparison.OrdinalIgnoreCase) == true ||
            normalizedRole?.Equals("Admin", StringComparison.OrdinalIgnoreCase) == true ||
            normalizedRole?.Equals("StokvelAdmin", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static bool CanManageMeetingsRole(string? role)
    {
        var normalizedRole = role?.Trim();

        return normalizedRole?.Equals("Secretary", StringComparison.OrdinalIgnoreCase) == true ||
            normalizedRole?.Equals("Chairperson", StringComparison.OrdinalIgnoreCase) == true ||
            normalizedRole?.Equals("Creator", StringComparison.OrdinalIgnoreCase) == true ||
            normalizedRole?.Equals("Admin", StringComparison.OrdinalIgnoreCase) == true ||
            normalizedRole?.Equals("StokvelAdmin", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static bool CanManageMinutesRole(string? role)
    {
        var normalizedRole = role?.Trim();

        return normalizedRole?.Equals("Secretary", StringComparison.OrdinalIgnoreCase) == true ||
            normalizedRole?.Equals("Chairperson", StringComparison.OrdinalIgnoreCase) == true ||
            normalizedRole?.Equals("Creator", StringComparison.OrdinalIgnoreCase) == true ||
            normalizedRole?.Equals("Admin", StringComparison.OrdinalIgnoreCase) == true ||
            normalizedRole?.Equals("StokvelAdmin", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static bool CanManageVotesRole(string? role)
    {
        var normalizedRole = role?.Trim();

        return normalizedRole?.Equals("Chairperson", StringComparison.OrdinalIgnoreCase) == true ||
            normalizedRole?.Equals("Secretary", StringComparison.OrdinalIgnoreCase) == true ||
            normalizedRole?.Equals("Creator", StringComparison.OrdinalIgnoreCase) == true ||
            normalizedRole?.Equals("Admin", StringComparison.OrdinalIgnoreCase) == true ||
            normalizedRole?.Equals("StokvelAdmin", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static bool CanApproveMinutesRole(string? role)
    {
        var normalizedRole = role?.Trim();

        return normalizedRole?.Equals("Chairperson", StringComparison.OrdinalIgnoreCase) == true ||
            normalizedRole?.Equals("Creator", StringComparison.OrdinalIgnoreCase) == true ||
            normalizedRole?.Equals("Admin", StringComparison.OrdinalIgnoreCase) == true ||
            normalizedRole?.Equals("StokvelAdmin", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static bool CanViewSecretaryTasksRole(string? role)
    {
        var normalizedRole = role?.Trim();

        return normalizedRole?.Equals("Secretary", StringComparison.OrdinalIgnoreCase) == true ||
            normalizedRole?.Equals("Chairperson", StringComparison.OrdinalIgnoreCase) == true ||
            normalizedRole?.Equals("Creator", StringComparison.OrdinalIgnoreCase) == true ||
            normalizedRole?.Equals("Admin", StringComparison.OrdinalIgnoreCase) == true ||
            normalizedRole?.Equals("StokvelAdmin", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static bool CanViewChairpersonTasksRole(string? role)
    {
        var normalizedRole = role?.Trim();

        return normalizedRole?.Equals("Chairperson", StringComparison.OrdinalIgnoreCase) == true ||
            normalizedRole?.Equals("Creator", StringComparison.OrdinalIgnoreCase) == true ||
            normalizedRole?.Equals("Admin", StringComparison.OrdinalIgnoreCase) == true ||
            normalizedRole?.Equals("StokvelAdmin", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static bool CanViewTreasurerTasksRole(string? role)
    {
        var normalizedRole = role?.Trim();

        return normalizedRole?.Equals("Treasurer", StringComparison.OrdinalIgnoreCase) == true ||
            normalizedRole?.Equals("Creator", StringComparison.OrdinalIgnoreCase) == true ||
            normalizedRole?.Equals("Admin", StringComparison.OrdinalIgnoreCase) == true ||
            normalizedRole?.Equals("StokvelAdmin", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static bool CanManagePaymentsRole(string? role)
    {
        var normalizedRole = role?.Trim();

        return normalizedRole?.Equals("Treasurer", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static bool CanViewFinancialsRole(string? role)
    {
        var normalizedRole = role?.Trim();

        return normalizedRole?.Equals("Treasurer", StringComparison.OrdinalIgnoreCase) == true ||
            normalizedRole?.Equals("Chairperson", StringComparison.OrdinalIgnoreCase) == true ||
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

    public async Task<bool> CanManageOwnDependentsAsync(string userId, Guid memberId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return false;

        var member = await context.Members
            .FirstOrDefaultAsync(m => m.Id == memberId && m.ApplicationUserId == userId);

        if (member is null)
            return false;

        if (member.GovernanceStatus == MemberGovernanceStatus.Expelled || member.IsDeceased)
            return false;

        var stokvel = await context.Stokvels
            .FirstOrDefaultAsync(s => s.TenantId == member.TenantId);

        return stokvel is not null &&
            (stokvel.Archetype == StokvelArchetype.BurialSociety || stokvel.EnableDependents);
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

    public async Task<bool> CanMakeDisciplinaryDecisionAsync(string userId, Guid stokvelId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return false;
        }

        var stokvel = await context.Stokvels
            .Where(existingStokvel => existingStokvel.Id == stokvelId)
            .OrderBy(existingStokvel => existingStokvel.CreatedAt)
            .ThenBy(existingStokvel => existingStokvel.Name)
            .FirstOrDefaultAsync();

        if (stokvel is null)
        {
            return false;
        }

        var member = await context.Members
            .Where(existingMember =>
                existingMember.ApplicationUserId == userId &&
                existingMember.TenantId == stokvel.TenantId &&
                (existingMember.DefaultRole == SisonkeRole.Chairperson ||
                    existingMember.DefaultRole == SisonkeRole.Creator ||
                    existingMember.DefaultRole == SisonkeRole.StokvelAdmin))
            .OrderBy(existingMember => existingMember.CreatedAt)
            .ThenBy(existingMember => existingMember.FullName)
            .FirstOrDefaultAsync();

        return member is not null && CanMakeDisciplinaryDecisionRole(member.DefaultRole.ToString());
    }

    public async Task<bool> CanManageMeetingsAsync(string userId, Guid stokvelId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return false;
        }

        var member = await GetLinkedMemberForUserAsync(userId, stokvelId);

        return member is not null && CanManageMeetingsRole(member.DefaultRole.ToString());
    }

    public async Task<bool> CanManageMinutesAsync(string userId, Guid stokvelId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return false;
        }

        var stokvel = await context.Stokvels
            .Where(existingStokvel => existingStokvel.Id == stokvelId)
            .OrderBy(existingStokvel => existingStokvel.CreatedAt)
            .ThenBy(existingStokvel => existingStokvel.Name)
            .FirstOrDefaultAsync();

        if (stokvel is null)
        {
            return false;
        }

        var member = await context.Members
            .Where(existingMember =>
                existingMember.ApplicationUserId == userId &&
                existingMember.TenantId == stokvel.TenantId &&
                (existingMember.DefaultRole == SisonkeRole.Secretary ||
                    existingMember.DefaultRole == SisonkeRole.Chairperson ||
                    existingMember.DefaultRole == SisonkeRole.Creator ||
                    existingMember.DefaultRole == SisonkeRole.StokvelAdmin))
            .OrderBy(existingMember => existingMember.CreatedAt)
            .ThenBy(existingMember => existingMember.FullName)
            .FirstOrDefaultAsync();

        return member is not null && CanManageMinutesRole(member.DefaultRole.ToString());
    }

    public async Task<bool> CanApproveMinutesAsync(string userId, Guid stokvelId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return false;
        }

        var stokvel = await context.Stokvels
            .Where(existingStokvel => existingStokvel.Id == stokvelId)
            .OrderBy(existingStokvel => existingStokvel.CreatedAt)
            .ThenBy(existingStokvel => existingStokvel.Name)
            .FirstOrDefaultAsync();

        if (stokvel is null)
        {
            return false;
        }

        var member = await context.Members
            .Where(existingMember =>
                existingMember.ApplicationUserId == userId &&
                existingMember.TenantId == stokvel.TenantId &&
                (existingMember.DefaultRole == SisonkeRole.Chairperson ||
                    existingMember.DefaultRole == SisonkeRole.Creator ||
                    existingMember.DefaultRole == SisonkeRole.StokvelAdmin))
            .OrderBy(existingMember => existingMember.CreatedAt)
            .ThenBy(existingMember => existingMember.FullName)
            .FirstOrDefaultAsync();

        return member is not null && CanApproveMinutesRole(member.DefaultRole.ToString());
    }

    public async Task<bool> CanViewApprovedMinutesAsync(string userId, Guid stokvelId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return false;
        }

        var stokvel = await context.Stokvels
            .Where(existingStokvel => existingStokvel.Id == stokvelId)
            .OrderBy(existingStokvel => existingStokvel.CreatedAt)
            .ThenBy(existingStokvel => existingStokvel.Name)
            .FirstOrDefaultAsync();

        if (stokvel is null)
        {
            return false;
        }

        return await context.Members
            .Where(member =>
                member.ApplicationUserId == userId &&
                member.TenantId == stokvel.TenantId &&
                member.Status == MemberStatus.Active)
            .OrderBy(member => member.CreatedAt)
            .ThenBy(member => member.FullName)
            .FirstOrDefaultAsync() is not null;
    }

    public async Task<bool> CanManageAttendanceAsync(string userId, Guid stokvelId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return false;
        }

        var member = await GetLinkedMemberForUserAsync(userId, stokvelId);

        return member is not null && CanManageMeetingsRole(member.DefaultRole.ToString());
    }

    public async Task<bool> CanManageVotesAsync(string userId, Guid stokvelId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return false;
        }

        var member = await GetLinkedMemberForUserAsync(userId, stokvelId);

        return member is not null && CanManageVotesRole(member.DefaultRole.ToString());
    }

    public async Task<bool> CanViewSecretaryTasksAsync(string userId, Guid stokvelId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return false;
        }

        var member = await GetLinkedMemberForUserAsync(userId, stokvelId);

        return member is not null && CanViewSecretaryTasksRole(member.DefaultRole.ToString());
    }

    public async Task<bool> CanViewChairpersonTasksAsync(string userId, Guid stokvelId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return false;
        }

        var member = await GetLinkedMemberForUserAsync(userId, stokvelId);

        return member is not null && CanViewChairpersonTasksRole(member.DefaultRole.ToString());
    }

    public async Task<bool> CanViewTreasurerTasksAsync(string userId, Guid stokvelId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return false;
        }

        var member = await GetLinkedMemberForUserAsync(userId, stokvelId);

        return member is not null && CanViewTreasurerTasksRole(member.DefaultRole.ToString());
    }

    public async Task<bool> CanManagePaymentsAsync(string userId, Guid stokvelId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return false;
        }

        var member = await GetLinkedMemberForUserAsync(userId, stokvelId);

        return member is not null && CanManagePaymentsRole(member.DefaultRole.ToString());
    }

    public async Task<bool> CanViewFinancialsAsync(string userId, Guid stokvelId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return false;
        }

        var member = await GetLinkedMemberForUserAsync(userId, stokvelId);

        return member is not null && CanViewFinancialsRole(member.DefaultRole.ToString());
    }
}
