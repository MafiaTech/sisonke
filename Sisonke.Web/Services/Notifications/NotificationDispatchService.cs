using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Enums;
using Sisonke.Web.Services.Notifications.Channels;

namespace Sisonke.Web.Services.Notifications;

public sealed class NotificationDispatchService(
    IServiceScopeFactory scopeFactory,
    NotificationOptions options,
    ILogger<NotificationDispatchService> logger) : BackgroundService
{
    private const int BatchSize = 50;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(1, options.DispatchIntervalSeconds)));

        do
        {
            try
            {
                await DispatchPendingAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Notification dispatch tick failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    public async Task DispatchPendingAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        var senders = scope.ServiceProvider.GetServices<INotificationChannelSender>();

        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var pending = await context.NotificationMessages
            .Where(message => message.Status == NotificationStatus.Pending)
            .OrderBy(message => message.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(ct);

        foreach (var message in pending)
        {
            var sender = senders.FirstOrDefault(candidate => candidate.Channel == message.Channel);

            if (sender is null)
            {
                logger.LogWarning("No channel sender registered for {Channel}.", message.Channel);
                continue;
            }

            try
            {
                await sender.SendAsync(message, ct);
                message.Status = NotificationStatus.Sent;
                message.SentAt = DateTime.UtcNow;
            }
            catch (ChannelDisabledException ex)
            {
                message.Status = NotificationStatus.Cancelled;
                message.LastError = ex.Message;
            }
            catch (PushSubscriptionGoneException ex)
            {
                message.Status = NotificationStatus.Cancelled;
                message.LastError = ex.Message;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                message.AttemptCount++;
                message.LastAttemptAt = DateTime.UtcNow;
                message.LastError = ex.Message;
                message.Status = message.AttemptCount >= options.MaxAttempts
                    ? NotificationStatus.Failed
                    : NotificationStatus.Pending;
            }

            await context.SaveChangesAsync(ct);
        }
    }
}
