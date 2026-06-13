using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;

namespace Sisonke.Web.Services;

public class MemberAccountLinkingService(IDbContextFactory<ApplicationDbContext> dbFactory)
{
    private static string NormalizeIdNumber(string? idNumber)
    {
        if (string.IsNullOrWhiteSpace(idNumber))
        {
            return string.Empty;
        }

        return idNumber
            .Trim()
            .Replace(" ", string.Empty)
            .Replace("-", string.Empty);
    }

    public async Task<List<Member>> FindMembersByIdNumberAsync(string? idNumber)
    {
        await using var context = await dbFactory.CreateDbContextAsync();
        var normalizedIdNumber = NormalizeIdNumber(idNumber);

        if (string.IsNullOrEmpty(normalizedIdNumber))
        {
            return [];
        }

        var members = await context.Members
            .Include(member => member.Tenant)
            .Where(member => member.IdNumber != null)
            .OrderBy(member => member.FullName)
            .ToListAsync();

        return members
            .Where(member => NormalizeIdNumber(member.IdNumber) == normalizedIdNumber)
            .ToList();
    }

    public async Task<int> LinkUserToMembersByIdNumberAsync(string userId, string? idNumber)
    {
        await using var context = await dbFactory.CreateDbContextAsync();
        var normalizedIdNumber = NormalizeIdNumber(idNumber);

        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrEmpty(normalizedIdNumber))
        {
            return 0;
        }

        var members = await context.Members
            .Where(member => member.IdNumber != null)
            .ToListAsync();

        var matchingMembers = members
            .Where(member => NormalizeIdNumber(member.IdNumber) == normalizedIdNumber)
            .ToList();

        foreach (var member in matchingMembers)
        {
            member.ApplicationUserId = userId;
        }

        await context.SaveChangesAsync();

        return matchingMembers.Count;
    }

    public async Task<int> RefreshUserMembershipLinksAsync(string userId, string? idNumber)
    {
        return await LinkUserToMembersByIdNumberAsync(userId, idNumber);
    }

    public async Task<List<Member>> GetMembershipsForUserAsync(string userId)
    {
        await using var context = await dbFactory.CreateDbContextAsync();
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

    public async Task<bool> IsMemberLinkedToUserAsync(Guid memberId, string userId)
    {
        await using var context = await dbFactory.CreateDbContextAsync();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return false;
        }

        return await context.Members
            .AnyAsync(member => member.Id == memberId && member.ApplicationUserId == userId);
    }
}
