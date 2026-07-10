using Sisonke.Web.Data;

namespace Sisonke.Web.Services.Notifications;

public interface IReminderSource
{
    Task EnqueuePaymentRemindersAsync(ApplicationDbContext context, CancellationToken ct);

    Task EnqueueMeetingRemindersAsync(ApplicationDbContext context, CancellationToken ct);
}
