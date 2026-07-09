using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Services.Dto;

namespace Sisonke.Web.Services;

public class AuditLogService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    ILogger<AuditLogService> logger)
{
    public async Task RecordAsync(
        string? userId,
        Guid? stokvelId,
        string actionType,
        string entityType,
        Guid? entityId,
        string summary,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await dbFactory.CreateDbContextAsync(cancellationToken);

            context.AuditLogEntries.Add(new AuditLogEntry
            {
                UserId = string.IsNullOrWhiteSpace(userId) ? null : userId,
                StokvelId = stokvelId,
                ActionType = Trim(actionType, 100),
                EntityType = Trim(entityType, 100),
                EntityId = entityId,
                Summary = Trim(summary, 1000),
                TimestampUtc = DateTime.UtcNow
            });

            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Audit log write failed for {ActionType} on {EntityType}.", actionType, entityType);
        }
    }

    public async Task<List<AuditLogRow>> GetRecentAsync(int take = 25, CancellationToken cancellationToken = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(cancellationToken);

        return await context.AuditLogEntries
            .AsNoTracking()
            .OrderByDescending(entry => entry.TimestampUtc)
            .Take(Math.Clamp(take, 1, 100))
            .Select(entry => new AuditLogRow
            {
                TimestampUtc = entry.TimestampUtc,
                ActionType = entry.ActionType,
                EntityType = entry.EntityType,
                Summary = entry.Summary
            })
            .ToListAsync(cancellationToken);
    }

    private static string Trim(string value, int maxLength)
    {
        var trimmed = string.IsNullOrWhiteSpace(value) ? "Not specified" : value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}
