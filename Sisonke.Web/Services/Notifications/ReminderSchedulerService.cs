using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;

namespace Sisonke.Web.Services.Notifications;

public sealed class ReminderSchedulerService(
    IServiceScopeFactory scopeFactory,
    NotificationOptions options,
    ILogger<ReminderSchedulerService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(Math.Max(1, options.ReminderIntervalMinutes)));

        do
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
                var reminderSource = scope.ServiceProvider.GetRequiredService<IReminderSource>();

                await using var context = await dbFactory.CreateDbContextAsync(stoppingToken);
                await reminderSource.EnqueuePaymentRemindersAsync(context, stoppingToken);
                await reminderSource.EnqueueMeetingRemindersAsync(context, stoppingToken);
                await context.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Reminder scheduler tick failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
