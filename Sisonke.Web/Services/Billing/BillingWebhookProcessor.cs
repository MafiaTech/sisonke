using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;
using Sisonke.Web.Services.Billing.Paystack;
using Sisonke.Web.Services.Entitlements;

namespace Sisonke.Web.Services.Billing;

/// <summary>
/// Per-event-type webhook handlers. Every handler is idempotent and re-runnable — a replayed
/// event must not double-issue an invoice, double-record a payment or double-send a
/// notification — achieved by looking up existing rows by provider-assigned identifiers
/// (ProviderReference, ProviderInvoiceCode, dedupe keys) before inserting. All status changes go
/// through ISubscriptionStateMachine.
///
/// Out-of-order delivery note: handlers apply "last processed wins" for status — each handler
/// only asserts what its own event unambiguously means (disable -> Cancelled, charge.success ->
/// Active + record payment) rather than trying to reason about relative event timing, which
/// Paystack does not guarantee. If the state machine says a transition implied by a stale/
/// out-of-order event is no longer legal from the subscription's current status (e.g. a stray
/// disable arriving for an already-Expired subscription), the handler logs and skips rather than
/// throwing — a webhook handler must never fail the whole event over a transition that turned out
/// to be moot.
/// </summary>
public sealed class BillingWebhookProcessor(
    IInvoiceNumberGenerator invoiceNumberGenerator,
    IInvoicePdfRenderer invoicePdfRenderer,
    BillingDocumentStorage documentStorage,
    IEntitlementService entitlementService,
    ISubscriptionStateMachine stateMachine,
    SubscriptionNotificationService notificationService,
    TimeProvider timeProvider,
    ILogger<BillingWebhookProcessor> logger)
{
    public async Task ProcessAsync(ApplicationDbContext context, BillingWebhookEvent webhookEvent, PaystackWebhookPayload payload, CancellationToken ct)
    {
        switch (webhookEvent.EventType)
        {
            case PaystackWebhookEventTypes.ChargeSuccess:
                await HandleChargeSuccessAsync(context, payload, ct);
                break;

            case PaystackWebhookEventTypes.InvoiceCreate:
                await HandleInvoiceCreateAsync(context, payload, ct);
                break;

            case PaystackWebhookEventTypes.InvoiceUpdate:
                await HandleInvoiceUpdateAsync(context, payload, ct);
                break;

            case PaystackWebhookEventTypes.InvoicePaymentFailed:
                await HandleInvoicePaymentFailedAsync(context, payload, ct);
                break;

            case PaystackWebhookEventTypes.SubscriptionCreate:
                await HandleSubscriptionCreateAsync(context, payload, ct);
                break;

            case PaystackWebhookEventTypes.SubscriptionDisable:
                await HandleSubscriptionDisableAsync(context, payload, ct);
                break;

            case PaystackWebhookEventTypes.RefundPending:
            case PaystackWebhookEventTypes.RefundProcessed:
            case PaystackWebhookEventTypes.RefundFailed:
                await HandleRefundAsync(context, webhookEvent.EventType, payload, ct);
                break;

            default:
                webhookEvent.ProcessingStatus = WebhookProcessingStatus.Ignored;
                logger.LogInformation("Ignoring unhandled Paystack webhook event type {EventType}.", webhookEvent.EventType);
                return;
        }

        webhookEvent.ProcessingStatus = WebhookProcessingStatus.Processed;
    }

    private async Task HandleChargeSuccessAsync(ApplicationDbContext context, PaystackWebhookPayload payload, CancellationToken ct)
    {
        var subscription = await FindSubscriptionAsync(context, payload, ct);
        if (subscription is null)
        {
            logger.LogInformation("charge.success for an unrecognised customer/subscription — ignoring (likely a member-contribution payment, not a subscription payment).");
            return;
        }

        if (payload.Reference is null)
        {
            logger.LogWarning("charge.success payload had no reference for subscription {SubscriptionId}.", subscription.Id);
            return;
        }

        var existingPayment = await context.SubscriptionPayments
            .SingleOrDefaultAsync(p => p.ProviderReference == payload.Reference, ct);

        if (existingPayment is not null)
        {
            // Pure replay of an already-processed charge — nothing left to do.
            return;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var invoice = await FindLatestOpenInvoiceAsync(context, subscription.Id, ct);
        var wasInDunning = subscription.DunningStartedAt is not null;

        var payment = new SubscriptionPayment
        {
            Id = Guid.NewGuid(),
            OrganisationSubscriptionId = subscription.Id,
            SubscriptionInvoiceId = invoice?.Id,
            Amount = (payload.AmountMinorUnits ?? 0) / 100m,
            Currency = "ZAR",
            Status = SubscriptionPaymentStatus.Succeeded,
            Provider = SubscriptionProvider.Paystack,
            ChargePurpose = subscription.Status == SubscriptionStatus.Trialing
                ? SubscriptionChargePurpose.InitialSubscription
                : wasInDunning
                    ? SubscriptionChargePurpose.Retry
                    : SubscriptionChargePurpose.RecurringSubscription,
            ProviderReference = payload.Reference,
            ProcessedAt = now,
            PaidAt = now,
            CreatedAt = now
        };
        context.SubscriptionPayments.Add(payment);

        if (invoice is not null)
        {
            invoice.Status = SubscriptionInvoiceStatus.Paid;
            invoice.PaidAt = now;
            invoice.UpdatedAt = now;
        }

        // Any successful charge clears dunning, regardless of how it got here.
        subscription.LastSuccessfulPaymentAt = now;
        subscription.DunningStartedAt = null;
        subscription.GracePeriodEndsAt = null;

        var plan = subscription.SubscriptionPlan;

        // Deliberately narrower than "whatever CanTransition allows" — Cancelled -> Active is a
        // legal transition in the table, but only as an explicit reactivation with a *new*
        // authorisation (brief: "reactivation, new authorisation required"), never as a side
        // effect of a stray/out-of-order charge.success replay landing on an already-cancelled
        // subscription. A cancelled subscription that gets a late charge stays cancelled.
        var chargeSuccessEligibleStatuses = subscription.Status is
            SubscriptionStatus.Trialing or SubscriptionStatus.PastDue or SubscriptionStatus.Restricted;

        if (chargeSuccessEligibleStatuses)
        {
            await context.SaveChangesAsync(ct); // persist the payment/invoice/cleared-dunning fields first
            await stateMachine.TransitionAsync(
                context, subscription, SubscriptionStatus.Active,
                SubscriptionEventType.PaymentSucceeded, null, $"Charge succeeded ({payload.Reference}).", ct);
        }
        else
        {
            await context.SaveChangesAsync(ct);
            await entitlementService.InvalidateAsync(subscription.StokvelId, ct);
        }

        await GenerateAndStoreReceiptAsync(context, subscription, payload.Reference, ct);

        if (plan is not null)
        {
            await notificationService.SendDebitSucceededAsync(context, subscription, plan, payment, ct);

            if (wasInDunning)
            {
                await notificationService.SendPaymentRecoveredAsync(context, subscription, ct);
            }
        }
    }

    private async Task HandleInvoiceCreateAsync(ApplicationDbContext context, PaystackWebhookPayload payload, CancellationToken ct)
    {
        var subscription = await FindSubscriptionAsync(context, payload, ct);
        if (subscription?.SubscriptionPlan is null || payload.InvoiceCode is null)
        {
            return;
        }

        var existing = await context.SubscriptionInvoices.SingleOrDefaultAsync(i => i.ProviderInvoiceCode == payload.InvoiceCode, ct);
        if (existing is not null)
        {
            return; // Idempotent replay.
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var plan = subscription.SubscriptionPlan;
        var periodStart = payload.PeriodStart ?? now;
        var periodEnd = payload.PeriodEnd ?? periodStart.AddMonths(1);
        var subTotal = plan.MonthlyPrice;
        var vatAmount = 0m; // InvoicingOptions.VatRegistered gate applied when rendering/issuing — see BillingWebhookProcessingService wiring.
        var total = subTotal + vatAmount;

        var invoice = new SubscriptionInvoice
        {
            Id = Guid.NewGuid(),
            OrganisationSubscriptionId = subscription.Id,
            InvoiceNumber = await invoiceNumberGenerator.NextAsync(context, ct),
            Status = SubscriptionInvoiceStatus.Issued,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            SubTotal = subTotal,
            VatAmount = vatAmount,
            Total = total,
            Currency = "ZAR",
            IssuedAt = now,
            ProviderInvoiceCode = payload.InvoiceCode,
            CreatedAt = now
        };
        context.SubscriptionInvoices.Add(invoice);

        context.SubscriptionInvoiceLines.Add(new SubscriptionInvoiceLine
        {
            Id = Guid.NewGuid(),
            SubscriptionInvoiceId = invoice.Id,
            Description = $"{plan.Name} subscription ({periodStart:yyyy-MM-dd} to {periodEnd:yyyy-MM-dd})",
            Quantity = 1,
            UnitPrice = plan.MonthlyPrice,
            LineTotal = plan.MonthlyPrice,
            SortOrder = 0
        });

        await context.SaveChangesAsync(ct);
    }

    private async Task HandleInvoiceUpdateAsync(ApplicationDbContext context, PaystackWebhookPayload payload, CancellationToken ct)
    {
        if (payload.InvoiceCode is null)
        {
            return;
        }

        var invoice = await context.SubscriptionInvoices.SingleOrDefaultAsync(i => i.ProviderInvoiceCode == payload.InvoiceCode, ct);
        if (invoice is null)
        {
            logger.LogInformation("invoice.update for unknown ProviderInvoiceCode {InvoiceCode} — ignoring.", payload.InvoiceCode);
            return;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;

        if (string.Equals(payload.Status, "success", StringComparison.OrdinalIgnoreCase))
        {
            invoice.Status = SubscriptionInvoiceStatus.Paid;
            invoice.PaidAt ??= now;
        }

        invoice.UpdatedAt = now;
        await context.SaveChangesAsync(ct);
    }

    private async Task HandleInvoicePaymentFailedAsync(ApplicationDbContext context, PaystackWebhookPayload payload, CancellationToken ct)
    {
        var subscription = await FindSubscriptionAsync(context, payload, ct);
        if (subscription is null)
        {
            return;
        }

        if (subscription.Status == SubscriptionStatus.PastDue)
        {
            // Already applied by an earlier delivery of this same event — replay, not a new failure.
            return;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;

        if (payload.InvoiceCode is not null)
        {
            var invoice = await context.SubscriptionInvoices.SingleOrDefaultAsync(i => i.ProviderInvoiceCode == payload.InvoiceCode, ct);
            if (invoice is not null)
            {
                invoice.Status = SubscriptionInvoiceStatus.Failed;
                invoice.UpdatedAt = now;
            }
        }

        if (!stateMachine.CanTransition(subscription.Status, SubscriptionStatus.PastDue))
        {
            logger.LogInformation(
                "invoice.payment_failed received but subscription {SubscriptionId} is in {Status}, which cannot move to PastDue — ignoring.",
                subscription.Id, subscription.Status);
            await context.SaveChangesAsync(ct);
            return;
        }

        subscription.DunningStartedAt ??= now; // day 0 of dunning — DunningJob takes it from here
        subscription.GracePeriodEndsAt = now.AddDays(7);

        context.SubscriptionPayments.Add(new SubscriptionPayment
        {
            Id = Guid.NewGuid(),
            OrganisationSubscriptionId = subscription.Id,
            Amount = subscription.SubscriptionPlan?.MonthlyPrice ?? 0m,
            Currency = "ZAR",
            Status = SubscriptionPaymentStatus.Failed,
            Provider = SubscriptionProvider.Paystack,
            ChargePurpose = subscription.Status == SubscriptionStatus.Trialing
                ? SubscriptionChargePurpose.InitialSubscription
                : SubscriptionChargePurpose.RecurringSubscription,
            ProviderReference = payload.Reference ?? $"invoice-failed-{payload.InvoiceCode}-{now:yyyyMMddHHmmss}",
            AttemptNumber = 1,
            FailureCode = "invoice_payment_failed",
            FailureMessage = "Invoice payment failed at the provider.",
            ProcessedAt = now,
            CreatedAt = now
        });

        await context.SaveChangesAsync(ct);
        await stateMachine.TransitionAsync(
            context, subscription, SubscriptionStatus.PastDue,
            SubscriptionEventType.PaymentFailed, null, "Invoice payment failed.", ct);

        if (subscription.SubscriptionPlan is not null)
        {
            await notificationService.SendDebitFailedAsync(
                context, subscription, subscription.SubscriptionPlan, "The card was declined by your bank.", now.AddDays(1), ct);
        }
    }

    private async Task HandleSubscriptionCreateAsync(ApplicationDbContext context, PaystackWebhookPayload payload, CancellationToken ct)
    {
        var subscription = await FindSubscriptionAsync(context, payload, ct);
        if (subscription is null)
        {
            return;
        }

        // Defensive sync only — CompleteCardAuthorisationAsync already sets these synchronously;
        // this just keeps NextBillingAt aligned if Paystack's value differs.
        if (payload.NextPaymentDate is { } nextPaymentDate)
        {
            subscription.NextBillingAt = nextPaymentDate;
            subscription.UpdatedAt = timeProvider.GetUtcNow().UtcDateTime;
            await context.SaveChangesAsync(ct);
        }
    }

    private async Task HandleSubscriptionDisableAsync(ApplicationDbContext context, PaystackWebhookPayload payload, CancellationToken ct)
    {
        var subscription = await FindSubscriptionAsync(context, payload, ct);
        if (subscription is null)
        {
            return;
        }

        if (!stateMachine.CanTransition(subscription.Status, SubscriptionStatus.Cancelled))
        {
            // Already Cancelled (idempotent replay) or in a status that can no longer move to
            // Cancelled (e.g. a stray disable arriving for an already-Expired subscription) —
            // either way, nothing to do; this must not fail the whole webhook event.
            logger.LogInformation(
                "subscription.disable received but subscription {SubscriptionId} is in {Status} — ignoring.",
                subscription.Id, subscription.Status);
            return;
        }

        subscription.CancelledAt = timeProvider.GetUtcNow().UtcDateTime;
        await context.SaveChangesAsync(ct);

        await stateMachine.TransitionAsync(
            context, subscription, SubscriptionStatus.Cancelled,
            SubscriptionEventType.Cancelled, null, "Subscription disabled at provider.", ct);
    }

    private async Task HandleRefundAsync(ApplicationDbContext context, string eventType, PaystackWebhookPayload payload, CancellationToken ct)
    {
        if (payload.TransactionReference is null)
        {
            return;
        }

        var payment = await context.SubscriptionPayments.SingleOrDefaultAsync(p => p.ProviderReference == payload.TransactionReference, ct);
        if (payment is null)
        {
            // Most refunds are the card-verification charge, which is deliberately never recorded
            // as a SubscriptionPayment (it is not the customer's subscription money) — nothing to update.
            logger.LogInformation("Refund event {EventType} for reference {Reference} has no matching SubscriptionPayment — likely a verification-charge refund.", eventType, payload.TransactionReference);
            return;
        }

        if (eventType == PaystackWebhookEventTypes.RefundProcessed)
        {
            payment.Status = SubscriptionPaymentStatus.Refunded;
            await context.SaveChangesAsync(ct);
        }
    }

    private static async Task<OrganisationSubscription?> FindSubscriptionAsync(ApplicationDbContext context, PaystackWebhookPayload payload, CancellationToken ct)
    {
        IQueryable<OrganisationSubscription> query = context.OrganisationSubscriptions.Include(s => s.SubscriptionPlan);

        if (payload.SubscriptionCode is { } subscriptionCode)
        {
            var bySubscriptionCode = await query.SingleOrDefaultAsync(s => s.ProviderSubscriptionCode == subscriptionCode, ct);
            if (bySubscriptionCode is not null)
            {
                return bySubscriptionCode;
            }
        }

        if (payload.CustomerCode is { } customerCode)
        {
            return await query.FirstOrDefaultAsync(s => s.ProviderCustomerCode == customerCode, ct);
        }

        return null;
    }

    private static async Task<SubscriptionInvoice?> FindLatestOpenInvoiceAsync(ApplicationDbContext context, Guid subscriptionId, CancellationToken ct) =>
        await context.SubscriptionInvoices
            .Where(i => i.OrganisationSubscriptionId == subscriptionId && i.Status == SubscriptionInvoiceStatus.Issued)
            .OrderByDescending(i => i.CreatedAt)
            .FirstOrDefaultAsync(ct);

    private async Task GenerateAndStoreReceiptAsync(ApplicationDbContext context, OrganisationSubscription subscription, string reference, CancellationToken ct)
    {
        try
        {
            var payment = await context.SubscriptionPayments.SingleOrDefaultAsync(p => p.ProviderReference == reference, ct);
            var stokvel = await context.Stokvels.AsNoTracking().SingleOrDefaultAsync(s => s.Id == subscription.StokvelId, ct);
            if (payment is null || stokvel is null)
            {
                return;
            }

            SubscriptionInvoice? invoice = payment.SubscriptionInvoiceId is { } invoiceId
                ? await context.SubscriptionInvoices.SingleOrDefaultAsync(i => i.Id == invoiceId, ct)
                : null;

            var pdfBytes = invoicePdfRenderer.RenderReceipt(payment, invoice, stokvel);
            await documentStorage.SaveAsync("subscription-receipts", subscription.StokvelId, $"{reference}.pdf", pdfBytes, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // A receipt PDF failure must never fail the whole webhook processing — the payment
            // itself is already recorded.
            logger.LogError(ex, "Failed to generate/store receipt PDF for payment {Reference}.", reference);
        }
    }
}
