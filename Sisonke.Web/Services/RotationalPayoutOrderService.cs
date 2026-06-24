using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Services;

public class RotationalPayoutOrderService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    IWebHostEnvironment environment,
    ILogger<RotationalPayoutOrderService> logger)
{
    public async Task<RotationalManagementSummary?> GetRotationalManagementSummaryAsync(
        Guid stokvelId,
        string? currentUserId)
    {
        var stopwatch = Stopwatch.StartNew();
        await using var context = await dbFactory.CreateDbContextAsync();

        var stokvel = await context.Stokvels
            .AsNoTracking()
            .Where(s => s.Id == stokvelId && !s.IsDeleted)
            .Select(s => new Stokvel
            {
                Id = s.Id,
                TenantId = s.TenantId,
                Name = s.Name,
                Type = s.Type,
                Archetype = s.Archetype,
                EnableRotation = s.EnableRotation,
                IsActive = s.IsActive
            })
            .SingleOrDefaultAsync();

        if (stokvel is null)
        {
            LogDevelopmentTiming("Rotational Management", stokvelId, stopwatch.ElapsedMilliseconds);
            return null;
        }

        var configuration = await context.RotationalStokvelConfigurations
            .AsNoTracking()
            .Where(c => c.StokvelId == stokvelId && c.IsActive)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync();

        var order = await context.RotationalPayoutOrders
            .AsNoTracking()
            .Where(item => item.StokvelId == stokvelId && item.IsActive)
            .OrderBy(item => item.Position)
            .Select(item => new RotationalPayoutOrderItem(
                item.Id,
                item.MemberId,
                item.Member.FullName,
                item.Member.MemberNumber,
                item.Member.Status,
                item.Member.GovernanceStatus,
                item.Position,
                item.HasReceivedPayout,
                item.LastPayoutDate))
            .ToListAsync();

        var eligibleMemberCount = await context.Members
            .AsNoTracking()
            .CountAsync(member =>
                member.TenantId == stokvel.TenantId &&
                member.Status == MemberStatus.Active &&
                member.GovernanceStatus == MemberGovernanceStatus.Active &&
                !member.IsDeceased &&
                member.SuspendedAt == null &&
                member.ExpelledAt == null);

        Member? linkedMember = null;
        if (!string.IsNullOrWhiteSpace(currentUserId))
        {
            linkedMember = await context.Members
                .AsNoTracking()
                .Where(member =>
                    member.TenantId == stokvel.TenantId &&
                    member.ApplicationUserId == currentUserId)
                .OrderByDescending(member =>
                    member.DefaultRole == SisonkeRole.Creator ||
                    member.DefaultRole == SisonkeRole.StokvelAdmin ||
                    member.DefaultRole == SisonkeRole.Chairperson ||
                    member.DefaultRole == SisonkeRole.Secretary ||
                    member.DefaultRole == SisonkeRole.Treasurer)
                .ThenBy(member => member.CreatedAt)
                .FirstOrDefaultAsync();
        }

        var canManage = linkedMember is not null &&
            (linkedMember.DefaultRole == SisonkeRole.Creator ||
             linkedMember.DefaultRole == SisonkeRole.StokvelAdmin ||
             linkedMember.DefaultRole == SisonkeRole.Chairperson ||
             linkedMember.DefaultRole == SisonkeRole.Secretary ||
             linkedMember.DefaultRole == SisonkeRole.Treasurer);
        var nextRecipient = order.FirstOrDefault(item => !item.HasReceivedPayout);
        var currentUserPosition = linkedMember is null
            ? null
            : order.FirstOrDefault(item => item.MemberId == linkedMember.Id)?.Position;

        LogDevelopmentTiming("Rotational Management", stokvelId, stopwatch.ElapsedMilliseconds);

        return new RotationalManagementSummary(
            stokvel,
            configuration,
            order,
            nextRecipient,
            linkedMember,
            currentUserPosition,
            eligibleMemberCount,
            order.Count,
            configuration?.PayoutAmount ?? 0,
            order.Count(item => item.HasReceivedPayout),
            order.Count,
            canManage);
    }

    private void LogDevelopmentTiming(string operation, Guid stokvelId, long elapsedMilliseconds)
    {
        if (environment.IsDevelopment())
        {
            logger.LogInformation("[Performance] {Operation} load for stokvel {StokvelId} completed in {ElapsedMilliseconds} ms.",
                operation, stokvelId, elapsedMilliseconds);
        }
    }

    public async Task<List<RotationalPayoutOrderItem>> GetActiveRotationOrderAsync(Guid stokvelId)
    {
        await using var context = await dbFactory.CreateDbContextAsync();

        return await context.RotationalPayoutOrders
            .AsNoTracking()
            .Include(order => order.Member)
            .Where(order => order.StokvelId == stokvelId && order.IsActive)
            .OrderBy(order => order.Position)
            .Select(order => new RotationalPayoutOrderItem(
                order.Id,
                order.MemberId,
                order.Member.FullName,
                order.Member.MemberNumber,
                order.Member.Status,
                order.Member.GovernanceStatus,
                order.Position,
                order.HasReceivedPayout,
                order.LastPayoutDate))
            .ToListAsync();
    }

    public async Task<List<RotationalPayoutOrderItem>> GetEligibleMembersAsync(Guid stokvelId)
    {
        await using var context = await dbFactory.CreateDbContextAsync();
        var stokvel = await GetRotationalStokvelAsync(context, stokvelId);

        if (stokvel is null)
        {
            return [];
        }

        return await context.Members
            .AsNoTracking()
            .Where(member =>
                member.TenantId == stokvel.TenantId &&
                member.Status == MemberStatus.Active &&
                member.GovernanceStatus == MemberGovernanceStatus.Active &&
                !member.IsDeceased &&
                member.SuspendedAt == null &&
                member.ExpelledAt == null)
            .OrderBy(member => member.JoiningDate)
            .ThenBy(member => member.FullName)
            .Select(member => new RotationalPayoutOrderItem(
                null,
                member.Id,
                member.FullName,
                member.MemberNumber,
                member.Status,
                member.GovernanceStatus,
                0,
                false,
                null))
            .ToListAsync();
    }

    public async Task<int> GetEligibleMemberCountAsync(Guid stokvelId)
    {
        await using var context = await dbFactory.CreateDbContextAsync();
        var stokvel = await GetRotationalStokvelAsync(context, stokvelId);

        if (stokvel is null)
        {
            return 0;
        }

        return await context.Members
            .AsNoTracking()
            .CountAsync(member =>
                member.TenantId == stokvel.TenantId &&
                member.Status == MemberStatus.Active &&
                member.GovernanceStatus == MemberGovernanceStatus.Active &&
                !member.IsDeceased &&
                member.SuspendedAt == null &&
                member.ExpelledAt == null);
    }

    public async Task<(List<RotationalPayoutOrderItem> Items, List<string> Errors)> PreviewRotationOrderAsync(
        Guid stokvelId,
        RotationOrderMethod method)
    {
        if (method == RotationOrderMethod.Manual)
        {
            return ([], ["Manual rotation order does not support preview generation."]);
        }

        await using var context = await dbFactory.CreateDbContextAsync();
        var eligibleMembers = await GetEligibleMembersForSaveAsync(context, stokvelId);
        var countErrors = ValidateEligibleMemberCount(eligibleMembers);

        if (countErrors.Count > 0)
        {
            return ([], countErrors);
        }

        var ordered = method switch
        {
            RotationOrderMethod.ByJoiningDate => eligibleMembers
                .OrderBy(member => member.JoiningDate)
                .ThenBy(member => member.FullName)
                .ToList(),
            RotationOrderMethod.Random => eligibleMembers
                .OrderBy(_ => Random.Shared.Next())
                .ToList(),
            _ => eligibleMembers
        };

        var items = ordered
            .Select((member, idx) => new RotationalPayoutOrderItem(
                null,
                member.Id,
                member.FullName,
                member.MemberNumber,
                member.Status,
                member.GovernanceStatus,
                idx + 1,
                false,
                null))
            .ToList();

        return (items, []);
    }

    public async Task<RotationOrderSaveResult> GenerateRotationOrderAsync(
        Guid stokvelId,
        RotationOrderMethod method,
        string currentUserId)
    {
        if (method == RotationOrderMethod.Manual)
        {
            return RotationOrderSaveResult.Failed(["Manual rotation order requires an explicit ordered member list."]);
        }

        await using var context = await dbFactory.CreateDbContextAsync();
        if (!await CanManageRotationOrderAsync(context, stokvelId, currentUserId))
        {
            return RotationOrderSaveResult.Failed(["Only office bearers can manage rotation order."]);
        }

        var eligibleMembers = await GetEligibleMembersForSaveAsync(context, stokvelId);
        var validationErrors = ValidateEligibleMemberCount(eligibleMembers);

        if (validationErrors.Count > 0)
        {
            return RotationOrderSaveResult.Failed(validationErrors);
        }

        var orderedMemberIds = method switch
        {
            RotationOrderMethod.ByJoiningDate => eligibleMembers
                .OrderBy(member => member.JoiningDate)
                .ThenBy(member => member.FullName)
                .Select(member => member.Id)
                .ToList(),
            RotationOrderMethod.Random => eligibleMembers
                .OrderBy(_ => Random.Shared.Next())
                .Select(member => member.Id)
                .ToList(),
            _ => []
        };

        return await ReplaceActiveOrderAsync(context, stokvelId, orderedMemberIds, currentUserId);
    }

    public async Task<RotationOrderSaveResult> SaveManualRotationOrderAsync(
        Guid stokvelId,
        IReadOnlyList<Guid> orderedMemberIds,
        string currentUserId)
    {
        await using var context = await dbFactory.CreateDbContextAsync();
        if (!await CanManageRotationOrderAsync(context, stokvelId, currentUserId))
        {
            return RotationOrderSaveResult.Failed(["Only office bearers can manage rotation order."]);
        }

        return await ReplaceActiveOrderAsync(context, stokvelId, orderedMemberIds, currentUserId);
    }

    public Task<RotationOrderSaveResult> ReorderRotationOrderAsync(
        Guid stokvelId,
        IReadOnlyList<Guid> orderedMemberIds,
        string currentUserId) =>
        SaveManualRotationOrderAsync(stokvelId, orderedMemberIds, currentUserId);

    public async Task<bool> DeactivateRotationOrderAsync(Guid stokvelId, string currentUserId)
    {
        await using var context = await dbFactory.CreateDbContextAsync();
        var stokvel = await GetRotationalStokvelAsync(context, stokvelId);

        if (stokvel is null || !await CanManageRotationOrderAsync(context, stokvelId, currentUserId))
        {
            return false;
        }

        await DeactivateActiveOrdersAsync(context, stokvelId, currentUserId);
        await context.SaveChangesAsync();

        return true;
    }

    public async Task<List<string>> ValidateRotationOrderAsync(Guid stokvelId)
    {
        await using var context = await dbFactory.CreateDbContextAsync();
        var activeOrder = await context.RotationalPayoutOrders
            .AsNoTracking()
            .Where(order => order.StokvelId == stokvelId && order.IsActive)
            .OrderBy(order => order.Position)
            .ToListAsync();

        var errors = await ValidateOrderedMemberIdsAsync(context, stokvelId, activeOrder.Select(order => order.MemberId).ToList());

        if (activeOrder.Select(order => order.Position).Distinct().Count() != activeOrder.Count)
        {
            errors.Add("Duplicate positions are not allowed in the active rotation order.");
        }

        if (activeOrder.Any(order => order.Position < 1))
        {
            errors.Add("Rotation order positions must start from 1.");
        }

        return errors.Distinct().ToList();
    }

    public async Task<RotationalPayoutOrderSummary> GetSummaryAsync(Guid stokvelId, Guid? memberId = null)
    {
        var order = await GetActiveRotationOrderAsync(stokvelId);
        var next = order.FirstOrDefault(item => !item.HasReceivedPayout);
        var memberEntry = memberId.HasValue
            ? order.FirstOrDefault(item => item.MemberId == memberId.Value)
            : null;

        return new RotationalPayoutOrderSummary(
            order.Count > 0,
            order.Count,
            next?.MemberName,
            memberEntry?.Position,
            memberEntry?.HasReceivedPayout);
    }

    private static async Task<RotationOrderSaveResult> ReplaceActiveOrderAsync(
        ApplicationDbContext context,
        Guid stokvelId,
        IReadOnlyList<Guid> orderedMemberIds,
        string currentUserId)
    {
        var validationErrors = await ValidateOrderedMemberIdsAsync(context, stokvelId, orderedMemberIds);

        if (validationErrors.Count > 0)
        {
            return RotationOrderSaveResult.Failed(validationErrors);
        }

        await DeactivateActiveOrdersAsync(context, stokvelId, currentUserId);
        await context.SaveChangesAsync();

        var now = DateTime.UtcNow;
        for (var index = 0; index < orderedMemberIds.Count; index++)
        {
            context.RotationalPayoutOrders.Add(new RotationalPayoutOrder
            {
                Id = Guid.NewGuid(),
                StokvelId = stokvelId,
                MemberId = orderedMemberIds[index],
                Position = index + 1,
                HasReceivedPayout = false,
                LastPayoutDate = null,
                IsActive = true,
                CreatedAt = now,
                CreatedBy = currentUserId
            });
        }

        await context.SaveChangesAsync();

        return RotationOrderSaveResult.Succeeded();
    }

    private static async Task DeactivateActiveOrdersAsync(
        ApplicationDbContext context,
        Guid stokvelId,
        string currentUserId)
    {
        var activeOrders = await context.RotationalPayoutOrders
            .Where(order => order.StokvelId == stokvelId && order.IsActive)
            .ToListAsync();

        foreach (var order in activeOrders)
        {
            order.IsActive = false;
            order.UpdatedAt = DateTime.UtcNow;
            order.UpdatedBy = currentUserId;
        }
    }

    private static async Task<List<string>> ValidateOrderedMemberIdsAsync(
        ApplicationDbContext context,
        Guid stokvelId,
        IReadOnlyList<Guid> orderedMemberIds)
    {
        var errors = new List<string>();
        var stokvel = await GetRotationalStokvelAsync(context, stokvelId);

        if (stokvel is null)
        {
            return ["Cannot save rotation order because the selected stokvel is not rotational."];
        }

        if (orderedMemberIds.Count == 0)
        {
            errors.Add("Rotation order cannot be empty.");
        }

        if (orderedMemberIds.Distinct().Count() != orderedMemberIds.Count)
        {
            errors.Add("Duplicate members are not allowed in the rotation order.");
        }

        var eligibleMembers = await GetEligibleMembersForSaveAsync(context, stokvelId);
        errors.AddRange(ValidateEligibleMemberCount(eligibleMembers));

        var eligibleIds = eligibleMembers.Select(member => member.Id).ToHashSet();
        var invalidMemberIds = orderedMemberIds.Where(memberId => !eligibleIds.Contains(memberId)).Distinct().ToList();

        if (invalidMemberIds.Count > 0)
        {
            errors.Add("Suspended, expelled, inactive, or non-member records cannot be included in the rotation order.");
        }

        var missingMembers = eligibleIds.Except(orderedMemberIds).ToList();
        if (missingMembers.Count > 0)
        {
            errors.Add("Every active eligible member must appear exactly once in the rotation order.");
        }

        return errors.Distinct().ToList();
    }

    private static List<string> ValidateEligibleMemberCount(IReadOnlyCollection<EligibleRotationMember> eligibleMembers)
    {
        return eligibleMembers.Count < 2
            ? ["At least two active eligible members are required to create a rotation order."]
            : [];
    }

    private static async Task<Stokvel?> GetRotationalStokvelAsync(ApplicationDbContext context, Guid stokvelId)
    {
        return await context.Stokvels
            .AsNoTracking()
            .Where(stokvel => stokvel.Id == stokvelId && stokvel.IsActive && stokvel.EnableRotation)
            .FirstOrDefaultAsync();
    }

    private static async Task<List<EligibleRotationMember>> GetEligibleMembersForSaveAsync(ApplicationDbContext context, Guid stokvelId)
    {
        var stokvel = await GetRotationalStokvelAsync(context, stokvelId);

        if (stokvel is null)
        {
            return [];
        }

        return await context.Members
            .AsNoTracking()
            .Where(member =>
                member.TenantId == stokvel.TenantId &&
                member.Status == MemberStatus.Active &&
                member.GovernanceStatus == MemberGovernanceStatus.Active &&
                !member.IsDeceased &&
                member.SuspendedAt == null &&
                member.ExpelledAt == null)
            .OrderBy(member => member.JoiningDate)
            .ThenBy(member => member.FullName)
            .Select(member => new EligibleRotationMember(
                member.Id,
                member.FullName,
                member.MemberNumber,
                member.Status,
                member.GovernanceStatus,
                member.JoiningDate))
            .ToListAsync();
    }

    private sealed record EligibleRotationMember(
        Guid Id,
        string FullName,
        string MemberNumber,
        MemberStatus Status,
        MemberGovernanceStatus GovernanceStatus,
        DateTime JoiningDate);

    private static async Task<bool> CanManageRotationOrderAsync(
        ApplicationDbContext context,
        Guid stokvelId,
        string currentUserId)
    {
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            return false;
        }

        var stokvel = await GetRotationalStokvelAsync(context, stokvelId);

        if (stokvel is null)
        {
            return false;
        }

        return await context.Members.AnyAsync(member =>
            member.TenantId == stokvel.TenantId &&
            member.ApplicationUserId == currentUserId &&
            (member.DefaultRole == SisonkeRole.Creator ||
                member.DefaultRole == SisonkeRole.StokvelAdmin ||
                member.DefaultRole == SisonkeRole.Chairperson ||
                member.DefaultRole == SisonkeRole.Secretary ||
                member.DefaultRole == SisonkeRole.Treasurer));
    }
}

public sealed record RotationalPayoutOrderItem(
    Guid? OrderId,
    Guid MemberId,
    string MemberName,
    string MemberNumber,
    MemberStatus MemberStatus,
    MemberGovernanceStatus GovernanceStatus,
    int Position,
    bool HasReceivedPayout,
    DateTime? LastPayoutDate);

public sealed record RotationalPayoutOrderSummary(
    bool IsConfigured,
    int MemberCount,
    string? NextPayoutMemberName,
    int? CurrentMemberPosition,
    bool? CurrentMemberHasReceivedPayout);

public sealed record RotationalManagementSummary(
    Stokvel Stokvel,
    RotationalStokvelConfiguration? ActiveConfiguration,
    List<RotationalPayoutOrderItem> ActiveOrder,
    RotationalPayoutOrderItem? NextRecipient,
    Member? LinkedMember,
    int? CurrentUserPosition,
    int EligibleMemberCount,
    int MembersInRotation,
    decimal PayoutAmount,
    int CompletedPayoutCount,
    int TotalPayoutCount,
    bool CanManage);

public sealed record RotationOrderSaveResult(bool Success, List<string> Errors)
{
    public static RotationOrderSaveResult Succeeded() => new(true, []);

    public static RotationOrderSaveResult Failed(List<string> errors) => new(false, errors);
}
