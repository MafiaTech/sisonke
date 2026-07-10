using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Services.Notifications.Channels;

public interface INotificationChannelSender
{
    NotificationChannel Channel { get; }

    Task SendAsync(NotificationMessage message, CancellationToken ct);
}
