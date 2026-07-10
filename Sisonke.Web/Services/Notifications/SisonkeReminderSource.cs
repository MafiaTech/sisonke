using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Services.Notifications;

/// <summary>
/// Reminder source backed by real task/meeting entities. Recipient resolution reuses the
/// same tenant-scoped visibility rule the rest of the app uses (see MemberAccessService /
/// MeetingService.GetMeetingsByStokvelIdAsync, which are tenant-scoped, not per-member-targeted).
/// </summary>
public sealed class SisonkeReminderSource(NotificationEnqueuer enqueuer) : IReminderSource
{
    public async Task EnqueuePaymentRemindersAsync(ApplicationDbContext context, CancellationToken ct)
    {
        var openPayouts = await context.RotationalPayouts
            .Where(payout => payout.IsActive && payout.PayoutStatus == RotationalPayoutStatus.Approved)
            .ToListAsync(ct);

        foreach (var payout in openPayouts)
        {
            var stokvel = await context.Stokvels
                .SingleOrDefaultAsync(s => s.Id == payout.StokvelId, ct);

            if (stokvel is null)
            {
                continue;
            }

            var treasurers = await context.Members
                .Where(member => member.TenantId == stokvel.TenantId &&
                    member.DefaultRole == SisonkeRole.Treasurer &&
                    member.Status == MemberStatus.Active)
                .ToListAsync(ct);

            foreach (var treasurer in treasurers)
            {
                await enqueuer.EnqueueAsync(
                    context,
                    NotificationType.PaymentReminder,
                    treasurer.Id,
                    stokvel.Id,
                    nameof(RotationalPayout),
                    payout.Id,
                    "Payout awaiting your payment",
                    $"A rotational payout of {payout.PayoutAmount:C} for {stokvel.Name} has been approved and is awaiting payment.",
                    ct);
            }
        }
    }

    public async Task EnqueueMeetingRemindersAsync(ApplicationDbContext context, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var windowEnd = now.AddHours(24);

        var upcomingMeetings = await context.Meetings
            .Where(meeting => meeting.Status != MeetingStatus.Cancelled &&
                meeting.MeetingDate >= now && meeting.MeetingDate <= windowEnd)
            .ToListAsync(ct);

        foreach (var meeting in upcomingMeetings)
        {
            var stokvel = await context.Stokvels
                .SingleOrDefaultAsync(s => s.TenantId == meeting.TenantId, ct);

            var members = await context.Members
                .Where(member => member.TenantId == meeting.TenantId && member.Status == MemberStatus.Active)
                .ToListAsync(ct);

            foreach (var member in members)
            {
                await enqueuer.EnqueueAsync(
                    context,
                    NotificationType.MeetingReminder,
                    member.Id,
                    stokvel?.Id,
                    nameof(Meeting),
                    meeting.Id,
                    $"Reminder: {meeting.Title} is coming up",
                    $"{meeting.Title} is scheduled for {meeting.MeetingDate:dd MMM yyyy HH:mm}.",
                    ct);
            }
        }
    }
}
