using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Services.Notifications;

/// <summary>
/// Adds NotificationMessage rows to the caller's own DbContext. Never calls SaveChangesAsync
/// itself, so the enqueued row commits atomically with the caller's business-action write.
/// </summary>
public sealed class NotificationEnqueuer
{
    public async Task EnqueueAsync(
        ApplicationDbContext context,
        NotificationType type,
        Guid recipientMemberId,
        Guid? stokvelId,
        string entityType,
        Guid entityId,
        string subject,
        string body,
        CancellationToken ct = default)
    {
        var recipient = await context.Members
            .SingleOrDefaultAsync(member => member.Id == recipientMemberId, ct);

        if (recipient is null || !recipient.EmailEnabled)
        {
            return;
        }

        var dedupeKey = BuildDedupeKey(type, NotificationChannel.Email, entityType, entityId, recipientMemberId);

        var alreadyEnqueued = await context.NotificationMessages
            .AnyAsync(message => message.DedupeKey == dedupeKey, ct);

        if (alreadyEnqueued)
        {
            return;
        }

        // Also check entries added earlier in this same unsaved unit of work — the query above
        // only sees committed rows, so two enqueue calls before a single SaveChangesAsync would
        // otherwise both pass the check and collide on the DedupeKey unique index.
        var alreadyPendingInThisContext = context.ChangeTracker.Entries<NotificationMessage>()
            .Any(entry => entry.State == EntityState.Added && entry.Entity.DedupeKey == dedupeKey);

        if (alreadyPendingInThisContext)
        {
            return;
        }

        context.NotificationMessages.Add(new NotificationMessage
        {
            Id = Guid.NewGuid(),
            StokvelId = stokvelId,
            RecipientMemberId = recipientMemberId,
            EntityType = entityType,
            EntityId = entityId,
            Channel = NotificationChannel.Email,
            Type = type,
            DedupeKey = dedupeKey,
            Subject = subject,
            Body = body,
            Status = NotificationStatus.Pending,
            CreatedAt = DateTime.UtcNow
        });
    }

    public Task EnqueueTaskAssignedAsync(
        ApplicationDbContext context, Guid recipientMemberId, Guid? stokvelId,
        string entityType, Guid entityId, string subject, string body, CancellationToken ct = default) =>
        EnqueueAsync(context, NotificationType.TaskAssigned, recipientMemberId, stokvelId, entityType, entityId, subject, body, ct);

    public Task EnqueueChairpersonApprovedAsync(
        ApplicationDbContext context, Guid recipientMemberId, Guid? stokvelId,
        string entityType, Guid entityId, string subject, string body, CancellationToken ct = default) =>
        EnqueueAsync(context, NotificationType.ChairpersonApproved, recipientMemberId, stokvelId, entityType, entityId, subject, body, ct);

    public Task EnqueueChairpersonRejectedAsync(
        ApplicationDbContext context, Guid recipientMemberId, Guid? stokvelId,
        string entityType, Guid entityId, string subject, string body, CancellationToken ct = default) =>
        EnqueueAsync(context, NotificationType.ChairpersonRejected, recipientMemberId, stokvelId, entityType, entityId, subject, body, ct);

    public static string BuildDedupeKey(
        NotificationType type, NotificationChannel channel, string entityType, Guid entityId, Guid recipientMemberId) =>
        $"{type}:{channel}:{entityType}:{entityId}:{recipientMemberId}";
}
