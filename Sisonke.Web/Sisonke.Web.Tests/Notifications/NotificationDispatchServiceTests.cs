using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;
using Sisonke.Web.Services.Notifications;
using Sisonke.Web.Services.Notifications.Channels;
using Sisonke.Web.Tests.TestSupport;

namespace Sisonke.Web.Tests.Notifications;

public class NotificationDispatchServiceTests
{
    [Fact]
    public async Task DispatchPending_SenderSucceeds_MarksMessageSent()
    {
        using var db = new SqliteTestDatabase();
        var messageId = await SeedPendingMessageAsync(db, NotificationChannel.Email);

        var sender = new FakeChannelSender(NotificationChannel.Email, _ => Task.CompletedTask);
        var dispatcher = CreateDispatcher(db, [sender], new NotificationOptions());

        await dispatcher.DispatchPendingAsync(CancellationToken.None);

        await using var context = db.CreateContext();
        var message = await context.NotificationMessages.SingleAsync(m => m.Id == messageId);
        Assert.Equal(NotificationStatus.Sent, message.Status);
        Assert.NotNull(message.SentAt);
    }

    [Fact]
    public async Task DispatchPending_SenderKeepsFailing_IncrementsAttemptsThenMarksFailed()
    {
        using var db = new SqliteTestDatabase();
        var messageId = await SeedPendingMessageAsync(db, NotificationChannel.Email);

        var sender = new FakeChannelSender(NotificationChannel.Email, _ => throw new InvalidOperationException("boom"));
        var options = new NotificationOptions { MaxAttempts = 3 };
        var dispatcher = CreateDispatcher(db, [sender], options);

        for (var attempt = 1; attempt <= options.MaxAttempts; attempt++)
        {
            await dispatcher.DispatchPendingAsync(CancellationToken.None);

            await using var context = db.CreateContext();
            var message = await context.NotificationMessages.SingleAsync(m => m.Id == messageId);
            Assert.Equal(attempt, message.AttemptCount);
            Assert.Equal(
                attempt < options.MaxAttempts ? NotificationStatus.Pending : NotificationStatus.Failed,
                message.Status);
        }

        // A further tick must not touch a Failed message again.
        await dispatcher.DispatchPendingAsync(CancellationToken.None);
        await using var finalContext = db.CreateContext();
        var finalMessage = await finalContext.NotificationMessages.SingleAsync(m => m.Id == messageId);
        Assert.Equal(options.MaxAttempts, finalMessage.AttemptCount);
        Assert.Equal(NotificationStatus.Failed, finalMessage.Status);
    }

    [Fact]
    public async Task DispatchPending_WhatsAppChannelDisabled_MarksMessageCancelled()
    {
        using var db = new SqliteTestDatabase();
        var messageId = await SeedPendingMessageAsync(db, NotificationChannel.WhatsApp);

        var sender = new FakeChannelSender(
            NotificationChannel.WhatsApp,
            _ => throw new ChannelDisabledException("WhatsApp channel is disabled pending Meta template approval."));
        var dispatcher = CreateDispatcher(db, [sender], new NotificationOptions());

        await dispatcher.DispatchPendingAsync(CancellationToken.None);

        await using var context = db.CreateContext();
        var message = await context.NotificationMessages.SingleAsync(m => m.Id == messageId);
        Assert.Equal(NotificationStatus.Cancelled, message.Status);
        Assert.Equal(0, message.AttemptCount);
    }

    private static async Task<Guid> SeedPendingMessageAsync(SqliteTestDatabase db, NotificationChannel channel)
    {
        await using var context = db.CreateContext();
        var member = TestData.CreateMember(context);

        var message = new NotificationMessage
        {
            Id = Guid.NewGuid(),
            RecipientMemberId = member.Id,
            EntityType = "RotationalPayout",
            EntityId = Guid.NewGuid(),
            Channel = channel,
            Type = NotificationType.TaskAssigned,
            DedupeKey = Guid.NewGuid().ToString(),
            Subject = "Subject",
            Body = "Body",
            Status = NotificationStatus.Pending
        };
        context.NotificationMessages.Add(message);
        await context.SaveChangesAsync();

        return message.Id;
    }

    private static NotificationDispatchService CreateDispatcher(
        SqliteTestDatabase db, IEnumerable<INotificationChannelSender> senders, NotificationOptions options) =>
        new(new TestDbContextFactory(db), senders, options, NullLogger<NotificationDispatchService>.Instance);

    private sealed class FakeChannelSender(NotificationChannel channel, Func<NotificationMessage, Task> behavior)
        : INotificationChannelSender
    {
        public NotificationChannel Channel { get; } = channel;

        public Task SendAsync(NotificationMessage message, CancellationToken ct) => behavior(message);
    }

    private sealed class TestDbContextFactory(SqliteTestDatabase db) : IDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext() => db.CreateContext();

        public Task<ApplicationDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(db.CreateContext());
    }
}
