using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Services.Notifications.Channels;

/// <summary>
/// No live WhatsApp integration yet — Meta message templates are not approved. Every send
/// throws ChannelDisabledException while WhatsAppOptions.Enabled is false (CLAUDE.md rule 6).
/// </summary>
public sealed class WhatsAppChannelSender(WhatsAppOptions options) : INotificationChannelSender
{
    public NotificationChannel Channel => NotificationChannel.WhatsApp;

    public Task SendAsync(NotificationMessage message, CancellationToken ct)
    {
        if (!options.Enabled)
        {
            throw new ChannelDisabledException("WhatsApp channel is disabled pending Meta template approval.");
        }

        throw new NotImplementedException("WhatsApp sending is not implemented yet.");
    }
}
