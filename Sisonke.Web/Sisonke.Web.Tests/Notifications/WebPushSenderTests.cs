using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;
using Sisonke.Web.Services.Notifications;
using Sisonke.Web.Services.Notifications.Channels;
using Sisonke.Web.Tests.TestSupport;

namespace Sisonke.Web.Tests.Notifications;

public class WebPushSenderTests
{
    // These two scenarios never reach the network (WebPushClient), so they're testable
    // without mocking the external push service — see NotificationDispatchServiceTests
    // for the dispatcher-level Cancelled behavior once a WebPushSender throws.

    [Fact]
    public async Task SendAsync_MemberWithNoLinkedAccount_ThrowsPushSubscriptionGoneException()
    {
        using var db = new SqliteTestDatabase();
        Guid memberId;
        await using (var context = db.CreateContext())
        {
            var member = TestData.CreateMember(context, applicationUserId: null);
            memberId = member.Id;
            await context.SaveChangesAsync();
        }

        var sender = new WebPushSender(new WebPushOptions(), new TestDbContextFactory(db));
        var message = BuildMessage(memberId);

        await Assert.ThrowsAsync<PushSubscriptionGoneException>(
            () => sender.SendAsync(message, CancellationToken.None));
    }

    [Fact]
    public async Task SendAsync_NoSubscriptionsRegistered_ThrowsPushSubscriptionGoneException()
    {
        using var db = new SqliteTestDatabase();
        Guid memberId;
        await using (var context = db.CreateContext())
        {
            var member = TestData.CreateMember(context, applicationUserId: "user-1");
            memberId = member.Id;
            await context.SaveChangesAsync();
        }

        var sender = new WebPushSender(new WebPushOptions(), new TestDbContextFactory(db));
        var message = BuildMessage(memberId);

        await Assert.ThrowsAsync<PushSubscriptionGoneException>(
            () => sender.SendAsync(message, CancellationToken.None));
    }

    private static NotificationMessage BuildMessage(Guid recipientMemberId) => new()
    {
        Id = Guid.NewGuid(),
        RecipientMemberId = recipientMemberId,
        EntityType = "RotationalPayout",
        EntityId = Guid.NewGuid(),
        Channel = NotificationChannel.WebPush,
        Type = NotificationType.TaskAssigned,
        DedupeKey = Guid.NewGuid().ToString(),
        Subject = "Subject",
        Body = "Body",
        Status = NotificationStatus.Pending
    };
}
