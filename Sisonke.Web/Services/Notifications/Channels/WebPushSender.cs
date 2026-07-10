using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;
using WebPush;
using WebPushSubscription = WebPush.PushSubscription;

namespace Sisonke.Web.Services.Notifications.Channels;

public sealed class WebPushSender(
    WebPushOptions options,
    IDbContextFactory<ApplicationDbContext> dbFactory) : INotificationChannelSender
{
    public NotificationChannel Channel => NotificationChannel.WebPush;

    public async Task SendAsync(NotificationMessage message, CancellationToken ct)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var recipient = await context.Members
            .SingleOrDefaultAsync(member => member.Id == message.RecipientMemberId, ct);

        if (string.IsNullOrWhiteSpace(recipient?.ApplicationUserId))
        {
            throw new PushSubscriptionGoneException(
                $"Notification {message.Id} recipient {message.RecipientMemberId} has no linked login account.");
        }

        var subscriptions = await context.PushSubscriptions
            .Where(subscription => subscription.UserId == recipient.ApplicationUserId)
            .ToListAsync(ct);

        if (subscriptions.Count == 0)
        {
            throw new PushSubscriptionGoneException(
                $"Notification {message.Id} recipient {message.RecipientMemberId} has no push subscriptions registered.");
        }

        var payload = JsonSerializer.Serialize(new
        {
            title = message.Subject ?? string.Empty,
            body = message.Body,
            url = $"/{message.EntityType.ToLowerInvariant()}/{message.EntityId}"
        });

        var vapidDetails = new VapidDetails(options.Subject, options.PublicKey, options.PrivateKey);
        var client = new WebPushClient();

        var anySucceeded = false;
        var deadSubscriptionIds = new List<Guid>();

        foreach (var subscription in subscriptions)
        {
            try
            {
                await client.SendNotificationAsync(
                    new WebPushSubscription(subscription.Endpoint, subscription.P256dh, subscription.Auth),
                    payload,
                    vapidDetails,
                    ct);
                anySucceeded = true;
            }
            catch (WebPushException ex) when (ex.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone)
            {
                deadSubscriptionIds.Add(subscription.Id);
            }
        }

        if (deadSubscriptionIds.Count > 0)
        {
            context.PushSubscriptions.RemoveRange(
                context.PushSubscriptions.Where(subscription => deadSubscriptionIds.Contains(subscription.Id)));
            await context.SaveChangesAsync(ct);
        }

        if (!anySucceeded)
        {
            throw new PushSubscriptionGoneException(
                $"Notification {message.Id} recipient {message.RecipientMemberId} has no live push subscriptions left.");
        }
    }
}
