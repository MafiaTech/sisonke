using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data.Enums;
using Sisonke.Web.Services.Notifications;
using Sisonke.Web.Tests.TestSupport;

namespace Sisonke.Web.Tests.Notifications;

public class NotificationEnqueuerTests
{
    private static readonly Guid EntityId = Guid.NewGuid();
    private const string EntityType = "RotationalPayout";

    [Theory]
    [InlineData(NotificationType.TaskAssigned)]
    [InlineData(NotificationType.ChairpersonApproved)]
    [InlineData(NotificationType.ChairpersonRejected)]
    [InlineData(NotificationType.PaymentReminder)]
    [InlineData(NotificationType.MeetingReminder)]
    public async Task Enqueue_CreatesEmailRow_WithExpectedDedupeKey(NotificationType type)
    {
        using var db = new SqliteTestDatabase();
        await using var context = db.CreateContext();
        var member = TestData.CreateMember(context);
        await context.SaveChangesAsync();

        var enqueuer = new NotificationEnqueuer();
        await enqueuer.EnqueueAsync(
            context, type, member.Id, stokvelId: null, EntityType, EntityId,
            subject: "Subject", body: "Body");
        await context.SaveChangesAsync();

        var messages = await context.NotificationMessages.ToListAsync();
        var message = Assert.Single(messages);
        Assert.Equal(NotificationChannel.Email, message.Channel);
        Assert.Equal(
            $"{type}:{NotificationChannel.Email}:{EntityType}:{EntityId}:{member.Id}",
            message.DedupeKey);
    }

    [Fact]
    public async Task Enqueue_DuplicateDedupeKey_CreatesNoSecondRow()
    {
        using var db = new SqliteTestDatabase();
        await using var context = db.CreateContext();
        var member = TestData.CreateMember(context);
        await context.SaveChangesAsync();

        var enqueuer = new NotificationEnqueuer();
        await enqueuer.EnqueueAsync(
            context, NotificationType.TaskAssigned, member.Id, stokvelId: null, EntityType, EntityId,
            subject: "Subject", body: "Body");
        await enqueuer.EnqueueAsync(
            context, NotificationType.TaskAssigned, member.Id, stokvelId: null, EntityType, EntityId,
            subject: "Subject", body: "Body");
        await context.SaveChangesAsync();

        Assert.Equal(1, await context.NotificationMessages.CountAsync());
    }

    [Fact]
    public async Task Enqueue_WithoutSaveChanges_LeavesNoRowsForOtherContexts()
    {
        using var db = new SqliteTestDatabase();

        Guid memberId;
        await using (var setupContext = db.CreateContext())
        {
            var member = TestData.CreateMember(setupContext);
            memberId = member.Id;
            await setupContext.SaveChangesAsync();
        }

        await using (var enqueueContext = db.CreateContext())
        {
            var enqueuer = new NotificationEnqueuer();
            await enqueuer.EnqueueAsync(
                enqueueContext, NotificationType.TaskAssigned, memberId, stokvelId: null, EntityType, EntityId,
                subject: "Subject", body: "Body");

            // Deliberately discard without calling SaveChangesAsync — simulates a rolled-back
            // unit of work. The enqueuer must never commit on its own.
        }

        await using var verifyContext = db.CreateContext();
        Assert.Equal(0, await verifyContext.NotificationMessages.CountAsync());
    }

    [Fact]
    public async Task Enqueue_MemberWithEmailDisabled_EnqueuesNothing()
    {
        using var db = new SqliteTestDatabase();
        await using var context = db.CreateContext();
        var member = TestData.CreateMember(context, emailEnabled: false);
        await context.SaveChangesAsync();

        var enqueuer = new NotificationEnqueuer();
        await enqueuer.EnqueueAsync(
            context, NotificationType.TaskAssigned, member.Id, stokvelId: null, EntityType, EntityId,
            subject: "Subject", body: "Body");
        await context.SaveChangesAsync();

        Assert.Equal(0, await context.NotificationMessages.CountAsync());
    }
}
