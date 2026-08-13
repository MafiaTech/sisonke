namespace Sisonke.Web.Services.Billing.Jobs;

/// <summary>
/// Ticks the Phase 4 daily jobs. Each job internally acquires its own IDistributedJobLock and
/// implements its own idempotent, date-driven logic — this host just gives them a chance to run
/// periodically; running more than once a day is safe and cheap (each job finds nothing new to
/// do). 30 minutes keeps a missed day-boundary window short without hammering the database.
/// </summary>
public sealed class SubscriptionJobsHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<SubscriptionJobsHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromMinutes(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TickInterval);

        do
        {
            await RunAllAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    public async Task RunAllAsync(CancellationToken ct)
    {
        await RunJobAsync<TrialReminderJob>(job => job.RunAsync(ct));
        await RunJobAsync<TrialExpiryJob>(job => job.RunAsync(ct));
        await RunJobAsync<DunningJob>(job => job.RunAsync(ct));
        await RunJobAsync<RenewalJob>(job => job.RunAsync(ct));
        await RunJobAsync<LegacyMigrationReminderJob>(job => job.RunAsync(ct));
    }

    private async Task RunJobAsync<TJob>(Func<TJob, Task> action) where TJob : notnull
    {
        using var scope = scopeFactory.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService<TJob>();

        try
        {
            await action(job);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "{Job} tick failed.", typeof(TJob).Name);
        }
    }
}
