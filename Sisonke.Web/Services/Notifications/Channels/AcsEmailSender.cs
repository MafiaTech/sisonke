using Azure;
using Azure.Communication.Email;
using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Services.Notifications.Channels;

public sealed class AcsEmailSender(
    AcsEmailOptions options,
    IDbContextFactory<ApplicationDbContext> dbFactory) : INotificationChannelSender
{
    public NotificationChannel Channel => NotificationChannel.Email;

    public async Task SendAsync(NotificationMessage message, CancellationToken ct)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var recipient = await context.Members
            .SingleOrDefaultAsync(member => member.Id == message.RecipientMemberId, ct);

        if (string.IsNullOrWhiteSpace(recipient?.EmailAddress))
        {
            throw new InvalidOperationException(
                $"Notification {message.Id} recipient {message.RecipientMemberId} has no email address on file.");
        }

        var emailClient = new EmailClient(options.ConnectionString);
        var emailMessage = new EmailMessage(
            senderAddress: options.SenderAddress,
            content: new EmailContent(message.Subject ?? string.Empty) { Html = message.Body },
            recipients: new EmailRecipients([new EmailAddress(recipient.EmailAddress)]));

        // WaitUntil.Started: fire the send and move on — do not block the dispatcher on
        // ACS delivery polling. Transient failures are handled by the dispatcher's own retry/backoff.
        await emailClient.SendAsync(WaitUntil.Started, emailMessage, ct);
    }
}
