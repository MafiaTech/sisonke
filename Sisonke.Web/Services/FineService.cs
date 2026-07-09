using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Services;

public class FineService(ApplicationDbContext context)
{
    public async Task<List<FineType>> GetFineTypesByStokvelIdAsync(Guid stokvelId)
    {
        var stokvel = await context.Stokvels
            .SingleOrDefaultAsync(existingStokvel => existingStokvel.Id == stokvelId);

        if (stokvel is null)
        {
            return [];
        }

        return await context.FineTypes
            .Where(fineType =>
                fineType.TenantId == stokvel.TenantId &&
                fineType.IsActive)
            .OrderBy(fineType => fineType.Name)
            .ToListAsync();
    }

    public async Task<List<MemberFine>> GetMemberFinesAsync(Guid memberId)
    {
        return await context.MemberFines
            .Include(memberFine => memberFine.FineType)
            .Where(memberFine => memberFine.MemberId == memberId)
            .OrderByDescending(memberFine => memberFine.FineDate)
            .ToListAsync();
    }

    public async Task<List<MemberFine>> GetOutstandingFinesByStokvelIdAsync(Guid stokvelId)
    {
        var stokvel = await context.Stokvels
            .Where(existingStokvel => existingStokvel.Id == stokvelId)
            .OrderBy(existingStokvel => existingStokvel.CreatedAt)
            .ThenBy(existingStokvel => existingStokvel.Name)
            .FirstOrDefaultAsync();

        if (stokvel is null)
        {
            return [];
        }

        return await context.MemberFines
            .Include(memberFine => memberFine.Member)
            .Include(memberFine => memberFine.FineType)
            .Where(memberFine =>
                memberFine.TenantId == stokvel.TenantId &&
                memberFine.Status == FineStatus.Unpaid)
            .OrderByDescending(memberFine => memberFine.FineDate)
            .ThenBy(memberFine => memberFine.Member.FullName)
            .ToListAsync();
    }

    public async Task<int> GetOutstandingFineCountByStokvelIdAsync(Guid stokvelId)
    {
        var stokvel = await context.Stokvels
            .Where(existingStokvel => existingStokvel.Id == stokvelId)
            .OrderBy(existingStokvel => existingStokvel.CreatedAt)
            .ThenBy(existingStokvel => existingStokvel.Name)
            .FirstOrDefaultAsync();

        if (stokvel is null)
        {
            return 0;
        }

        return await context.MemberFines
            .CountAsync(memberFine =>
                memberFine.TenantId == stokvel.TenantId &&
                memberFine.Status == FineStatus.Unpaid);
    }

    public async Task<MemberFine?> GetMemberFineByIdAsync(Guid memberFineId)
    {
        return await context.MemberFines
            .Include(memberFine => memberFine.FineType)
            .Include(memberFine => memberFine.Member)
            .SingleOrDefaultAsync(memberFine => memberFine.Id == memberFineId);
    }

    public async Task<MemberFine?> AddMemberFineAsync(
        Guid memberId,
        Guid fineTypeId,
        decimal amount,
        string reason,
        DateTime fineDate)
    {
        var member = await context.Members
            .SingleOrDefaultAsync(existingMember => existingMember.Id == memberId);

        var fineType = await context.FineTypes
            .SingleOrDefaultAsync(existingFineType => existingFineType.Id == fineTypeId);

        if (member is null || fineType is null)
        {
            return null;
        }

        if (fineType.TenantId != member.TenantId)
        {
            return null;
        }

        var memberFine = new MemberFine
        {
            Id = Guid.NewGuid(),
            TenantId = member.TenantId,
            MemberId = member.Id,
            FineTypeId = fineType.Id,
            Amount = amount,
            Reason = reason,
            FineDate = fineDate,
            Status = FineStatus.Unpaid,
            CreatedAt = DateTime.UtcNow
        };

        context.MemberFines.Add(memberFine);
        await context.SaveChangesAsync();

        return memberFine;
    }

    public async Task<MemberFine?> MarkFineAsPaidAsync(Guid memberFineId)
    {
        var memberFine = await context.MemberFines
            .SingleOrDefaultAsync(existingMemberFine => existingMemberFine.Id == memberFineId);

        if (memberFine is null)
        {
            return null;
        }

        memberFine.Status = FineStatus.Paid;
        memberFine.PaidDate = DateTime.Today;

        await context.SaveChangesAsync();

        return memberFine;
    }

    public async Task<MemberFine?> VoidFineAsync(Guid memberFineId)
    {
        var memberFine = await context.MemberFines
            .SingleOrDefaultAsync(existingMemberFine => existingMemberFine.Id == memberFineId);

        if (memberFine is null)
        {
            return null;
        }

        memberFine.Status = FineStatus.Cancelled;

        await context.SaveChangesAsync();

        return memberFine;
    }

    public async Task EnsureDefaultFineTypesForStokvelAsync(Guid stokvelId)
    {
        var stokvel = await context.Stokvels
            .SingleOrDefaultAsync(existingStokvel => existingStokvel.Id == stokvelId);

        if (stokvel is null)
        {
            return;
        }

        var tenantAlreadyHasFineTypes = await context.FineTypes
            .AnyAsync(fineType => fineType.TenantId == stokvel.TenantId);

        if (tenantAlreadyHasFineTypes)
        {
            return;
        }

        var now = DateTime.UtcNow;

        var defaults = stokvel.Type switch
        {
            StokvelType.BurialSociety => new[]
            {
                ("Late Coming Fine",       50m),
                ("Late Apology Fine",       0m),
                ("No Apology Fine",         0m),
                ("Food Contribution Fine",  0m),
                ("Misconduct Fine",         0m),
                ("Custom Fine",             0m),
            },
            StokvelType.SavingsStokvel => new[]
            {
                ("Late Payment Fine",  50m),
                ("Late Coming Fine",   50m),
                ("Late Apology Fine",   0m),
                ("No Apology Fine",     0m),
                ("Misconduct Fine",   200m),
                ("Custom Fine",         0m),
            },
            _ => new[]
            {
                ("Late Payment Fine", 50m),
                ("Late Coming Fine",  50m),
                ("Misconduct Fine",  200m),
                ("Custom Fine",        0m),
            }
        };

        var fineTypes = defaults.Select(defaultFineType =>
        {
            var (name, defaultAmount) = defaultFineType;

            return new FineType
            {
                Id = Guid.NewGuid(),
                TenantId = stokvel.TenantId,
                Name = name,
                DefaultAmount = defaultAmount,
                IsActive = true,
                CreatedAt = now
            };
        });

        context.FineTypes.AddRange(fineTypes);

        await context.SaveChangesAsync();
    }

    public async Task<decimal> GetOutstandingFinesTotalByStokvelIdAsync(Guid stokvelId)
    {
        var stokvel = await context.Stokvels
            .SingleOrDefaultAsync(existingStokvel => existingStokvel.Id == stokvelId);

        if (stokvel is null)
        {
            return 0;
        }

        return await context.MemberFines
            .Where(memberFine =>
                memberFine.TenantId == stokvel.TenantId &&
                memberFine.Status == FineStatus.Unpaid)
            .SumAsync(memberFine => memberFine.Amount);
    }
}
