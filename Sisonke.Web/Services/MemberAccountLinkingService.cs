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

    private static string NormalizeEmail(string? email)
    {
        return string.IsNullOrWhiteSpace(email)
            ? string.Empty
            : email.Trim().ToUpperInvariant();
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

    public async Task<int> LinkUserToMembersByVerifiedEmailAsync(string userId, string? email)
    {
        await using var context = await dbFactory.CreateDbContextAsync();
        var normalizedEmail = NormalizeEmail(email);

        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrEmpty(normalizedEmail))
        {
            return 0;
        }

        var members = await context.Members
            .Where(member => member.EmailAddress != null)
            .ToListAsync();

        var matchingMembers = members
            .Where(member => NormalizeEmail(member.EmailAddress) == normalizedEmail)
            .ToList();

        if (!IsSafeEmailMatch(matchingMembers))
        {
            return 0;
        }

        foreach (var member in matchingMembers)
        {
            member.ApplicationUserId = userId;
        }

        await context.SaveChangesAsync();

        return matchingMembers.Count;
    }

    public async Task<int> RefreshUserMembershipLinksAsync(
        string userId,
        string? idNumber,
        string? verifiedEmail = null)
    {
        var normalizedIdNumber = NormalizeIdNumber(idNumber);
        var linkedById = await LinkUserToMembersByIdNumberAsync(userId, idNumber);
        if (linkedById > 0 || !string.IsNullOrEmpty(normalizedIdNumber))
        {
            return linkedById;
        }

        return await LinkUserToMembersByVerifiedEmailAsync(userId, verifiedEmail);
    }

    public async Task<List<Member>> GetMembershipsForUserAsync(
        string userId,
        string? idNumber = null,
        string? verifiedEmail = null)
    {
        await using var context = await dbFactory.CreateDbContextAsync();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return [];
        }

        var linkedMembers = await context.Members
            .Include(member => member.Tenant)
            .Where(member => member.ApplicationUserId == userId)
            .OrderBy(member => member.FullName)
            .ToListAsync();

        if (linkedMembers.Count > 0)
        {
            return linkedMembers;
        }

        await RefreshUserMembershipLinksAsync(userId, idNumber, verifiedEmail);

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

    private static bool IsSafeEmailMatch(List<Member> matchingMembers)
    {
        if (matchingMembers.Count == 0)
        {
            return false;
        }

        var distinctCapturedIdNumbers = matchingMembers
            .Select(member => NormalizeIdNumber(member.IdNumber))
            .Where(idNumber => !string.IsNullOrWhiteSpace(idNumber))
            .Distinct(StringComparer.Ordinal)
            .Count();

        return distinctCapturedIdNumbers <= 1;
    }
}
