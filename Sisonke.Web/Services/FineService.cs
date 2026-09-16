using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Services;

public class FineService(IDbContextFactory<ApplicationDbContext> dbFactory, AuditLogService auditLogService)
{
    private readonly ApplicationDbContext? transactionContext;
    // Explicit short-lived transaction/legacy callers only. DI uses the factory constructor.
    public FineService(ApplicationDbContext context, AuditLogService auditLogService) : this((IDbContextFactory<ApplicationDbContext>)null!, auditLogService)
    {
        transactionContext = context;
    }

    public async Task<List<FineType>> GetFineTypesByStokvelIdAsync(Guid stokvelId)
    {
        await using var operation = await DbContextOperation.OpenAsync(dbFactory, transactionContext);
        var context = operation.Context;

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
        await using var operation = await DbContextOperation.OpenAsync(dbFactory, transactionContext);
        var context = operation.Context;

        return await context.MemberFines.AsNoTracking()
            .Include(memberFine => memberFine.FineType)
            .Where(memberFine => memberFine.MemberId == memberId)
            .OrderByDescending(memberFine => memberFine.FineDate)
            .ToListAsync();
    }

    public async Task<List<MemberFine>> GetOutstandingFinesByStokvelIdAsync(Guid stokvelId)
    {
        await using var operation = await DbContextOperation.OpenAsync(dbFactory, transactionContext);
        var context = operation.Context;

        var stokvel = await context.Stokvels
            .Where(existingStokvel => existingStokvel.Id == stokvelId)
            .OrderBy(existingStokvel => existingStokvel.CreatedAt)
            .ThenBy(existingStokvel => existingStokvel.Name)
            .FirstOrDefaultAsync();

        if (stokvel is null)
        {
            return [];
        }

        return await context.MemberFines.AsNoTracking()
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
        await using var operation = await DbContextOperation.OpenAsync(dbFactory, transactionContext);
        var context = operation.Context;

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
        await using var operation = await DbContextOperation.OpenAsync(dbFactory, transactionContext);
        var context = operation.Context;

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
        await using var operation = await DbContextOperation.OpenAsync(dbFactory, transactionContext);
        var context = operation.Context;

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
        var stokvel = await context.Stokvels.AsNoTracking().FirstOrDefaultAsync(existingStokvel => existingStokvel.TenantId == member.TenantId);
        await auditLogService.RecordAsync(null, stokvel?.Id, "FineCreated", "MemberFine", memberFine.Id, $"Fine issued to {member.FullName} for R {amount:N2}.");

        return memberFine;
    }

    public async Task<MemberFine?> MarkFineAsPaidAsync(Guid memberFineId, string? actor = null, Guid? stokvelId = null, DateTime? paymentDate = null)
    {
        await using var operation = await DbContextOperation.OpenAsync(dbFactory, transactionContext);
        var context = operation.Context;

        await using var ownedTransaction = context.Database.CurrentTransaction is null
            ? await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable) : null;
        var memberFine = await context.MemberFines
            .SingleOrDefaultAsync(existingMemberFine => existingMemberFine.Id == memberFineId);

        if (memberFine is null)
        {
            return null;
        }

        await context.Entry(memberFine).ReloadAsync();
        var stokvel = await context.Stokvels.AsNoTracking().FirstOrDefaultAsync(s =>
            s.TenantId == memberFine.TenantId && (!stokvelId.HasValue || s.Id == stokvelId));
        if (memberFine.Status != FineStatus.Unpaid || stokvel is null) return null;
        if (actor is not null && !await new MemberAccessService(context).CanManagePaymentsAsync(actor, stokvel.Id)) return null;
        memberFine.Status = FineStatus.Paid;
        memberFine.PaidDate = paymentDate?.Date ?? DateTime.Today;

        auditLogService.Stage(context, actor, stokvel.Id, "FinePaid", "MemberFine", memberFine.Id, $"Fine marked paid for R {memberFine.Amount:N2}.");
        await context.SaveChangesAsync();
        if (ownedTransaction is not null) await ownedTransaction.CommitAsync();

        return memberFine;
    }

    public async Task<MemberFine?> VoidFineAsync(Guid memberFineId)
    {
        await using var operation = await DbContextOperation.OpenAsync(dbFactory, transactionContext);
        var context = operation.Context;

        var memberFine = await context.MemberFines
            .SingleOrDefaultAsync(existingMemberFine => existingMemberFine.Id == memberFineId);

        if (memberFine is null)
        {
            return null;
        }

        memberFine.Status = FineStatus.Cancelled;

        await context.SaveChangesAsync();
        var stokvel = await context.Stokvels.AsNoTracking().FirstOrDefaultAsync(existingStokvel => existingStokvel.TenantId == memberFine.TenantId);
        await auditLogService.RecordAsync(null, stokvel?.Id, "FineWaived", "MemberFine", memberFine.Id, $"Fine cancelled for R {memberFine.Amount:N2}.");

        return memberFine;
    }

    public async Task EnsureDefaultFineTypesForStokvelAsync(Guid stokvelId)
    {
        await using var operation = await DbContextOperation.OpenAsync(dbFactory, transactionContext);
        var context = operation.Context;

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
        await using var operation = await DbContextOperation.OpenAsync(dbFactory, transactionContext);
        var context = operation.Context;

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
