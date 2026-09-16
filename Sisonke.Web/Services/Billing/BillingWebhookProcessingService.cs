using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Enums;
using Sisonke.Web.Services.Billing.Paystack;

namespace Sisonke.Web.Services.Billing;

/// <summary>
/// The "existing job runner" the brief refers to for asynchronous webhook processing — same
/// BackgroundService + PeriodicTimer shape as NotificationDispatchService. The webhook endpoint
/// only ever inserts a Received row and returns 200; this loop does the actual work.
/// </summary>
public sealed class BillingWebhookProcessingService(
    IServiceScopeFactory scopeFactory,
    ILogger<BillingWebhookProcessingService> logger) : BackgroundService
{
    private const int BatchSize = 25;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollInterval);

        do
        {
            try
            {
                await ProcessPendingAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Billing webhook processing tick failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    public async Task ProcessPendingAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        var processor = scope.ServiceProvider.GetRequiredService<BillingWebhookProcessor>();

        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var pending = await context.BillingWebhookEvents
            .Where(e => e.ProcessingStatus == WebhookProcessingStatus.Received)
            .OrderBy(e => e.ReceivedAt)
            .Take(BatchSize)
            .ToListAsync(ct);

        foreach (var webhookEvent in pending)
        {
            var payload = PaystackWebhookPayload.TryParse(webhookEvent.RawPayload);

            try
            {
                if (payload is null)
                {
                    throw new InvalidOperationException("Webhook payload could not be parsed as JSON.");
                }

                await processor.ProcessAsync(context, webhookEvent, payload, ct);
                webhookEvent.ProcessedAt = DateTime.UtcNow;
                webhookEvent.RawPayload = payload.CreateSafeDiagnosticJson();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                webhookEvent.AttemptCount++;
                webhookEvent.ErrorMessage = ex.Message;
                webhookEvent.ProcessingStatus = webhookEvent.AttemptCount >= 5
                    ? WebhookProcessingStatus.Failed
                    : WebhookProcessingStatus.Received;

                if (webhookEvent.ProcessingStatus == WebhookProcessingStatus.Failed)
                {
                    webhookEvent.ProcessedAt = DateTime.UtcNow;
                    webhookEvent.RawPayload = payload?.CreateSafeDiagnosticJson() ?? "{\"event\":\"unparseable\"}";
                }

                logger.LogError(ex, "Failed to process billing webhook event {WebhookEventId} ({EventType}).", webhookEvent.Id, webhookEvent.EventType);
            }

            await context.SaveChangesAsync(ct);
        }
    }
}
