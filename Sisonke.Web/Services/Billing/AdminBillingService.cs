using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;
using Sisonke.Web.Services.Entitlements;

namespace Sisonke.Web.Services.Billing;

/// <summary>
/// Manual admin actions for the internal billing dashboards (Phase 5 brief §E) — extend trial,
/// apply a promotional trial, waive an invoice, retry a charge, move to Enterprise custom
/// pricing, reinstate a suspended subscription. Every method requires a non-empty reason and
/// writes a SubscriptionEvent with the acting admin's user id — never sets or reveals card data.
/// Authorization is enforced by the calling Razor page's [Authorize(Roles = "PlatformAdmin")],
/// matching PlatformAdmin.razor's existing pattern; this service trusts the caller-supplied
/// actorUserId for the audit trail rather than re-checking the role itself.
///
/// RetryChargeAsync is deliberately independent from DunningJob's private retry logic (Phase 4)
/// rather than refactored into a shared helper, to avoid touching already-tested job code for a
/// one-off admin action — the duplication is small and isolated.
/// </summary>
public sealed class AdminBillingService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    IBillingProvider billingProvider,
    IEntitlementService entitlementService,
    ISubscriptionStateMachine stateMachine,
    TimeProvider timeProvider,
    ILogger<AdminBillingService> logger)
{
    public async Task<SubscriptionActionResult> ExtendTrialAsync(
        Guid stokvelId, int additionalDays, string reason, string actorUserId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return SubscriptionActionResult.Failed("A reason is required.");
        }

        if (additionalDays <= 0)
        {
            return SubscriptionActionResult.Failed("Additional days must be positive.");
        }

        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var subscription = await GetSubscriptionAsync(context, stokvelId, ct);

        if (subscription is null)
        {
            return SubscriptionActionResult.Failed("No subscription found for this organisation.");
        }

        if (subscription.Status != SubscriptionStatus.Trialing)
        {
            return SubscriptionActionResult.Failed("Only a subscription currently in trial can be extended.");
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        subscription.TrialEndsAt = (subscription.TrialEndsAt ?? now).AddDays(additionalDays);
        subscription.NextBillingAt = subscription.TrialEndsAt;
        Touch(subscription, now);

        AddAuditEvent(context, subscription, SubscriptionEventType.TrialExtended, actorUserId,
            $"Trial extended by {additionalDays} day(s): {reason}", now);

        await context.SaveChangesAsync(ct);
        await entitlementService.InvalidateAsync(stokvelId, ct);

        return SubscriptionActionResult.Succeeded($"Trial extended to {subscription.TrialEndsAt:d MMMM yyyy}.");
    }

    public async Task<SubscriptionActionResult> ApplyPromotionalTrialAsync(
        Guid stokvelId, string promoCode, string reason, string actorUserId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return SubscriptionActionResult.Failed("A reason is required.");
        }

        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var promo = await context.PromotionalTrials.SingleOrDefaultAsync(p => p.Code == promoCode && p.IsActive, ct);
        if (promo is null)
        {
            return SubscriptionActionResult.Failed("Promotional trial code not found or inactive.");
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        if (promo.ValidFrom is { } from && now < from)
        {
            return SubscriptionActionResult.Failed("This promotional trial is not yet valid.");
        }

        if (promo.ValidTo is { } to && now > to)
        {
            return SubscriptionActionResult.Failed("This promotional trial has expired.");
        }

        if (promo.MaxRedemptions is { } max && promo.RedemptionCount >= max)
        {
            return SubscriptionActionResult.Failed("This promotional trial has reached its redemption limit.");
        }

        var subscription = await GetSubscriptionAsync(context, stokvelId, ct);
        if (subscription is null)
        {
            return SubscriptionActionResult.Failed("No subscription found for this organisation.");
        }

        if (subscription.Status != SubscriptionStatus.Trialing)
        {
            return SubscriptionActionResult.Failed("Promotional trials can only be applied to a subscription currently in trial.");
        }

        if (promo.AppliesToPlanId is { } planId && subscription.SubscriptionPlanId != planId)
        {
            return SubscriptionActionResult.Failed("This promotional trial does not apply to the subscription's current plan.");
        }

        subscription.TrialEndsAt = (subscription.TrialStartedAt ?? now).AddDays(promo.TrialDays);
        subscription.NextBillingAt = subscription.TrialEndsAt;
        Touch(subscription, now);
        promo.RedemptionCount++;

        AddAuditEvent(context, subscription, SubscriptionEventType.PromotionalTrialApplied, actorUserId,
            $"Applied promotional trial '{promoCode}' ({promo.TrialDays} days): {reason}", now);

        await context.SaveChangesAsync(ct);
        await entitlementService.InvalidateAsync(stokvelId, ct);

        return SubscriptionActionResult.Succeeded($"Trial now ends {subscription.TrialEndsAt:d MMMM yyyy}.");
    }

    public async Task<SubscriptionActionResult> WaiveInvoiceAsync(
        Guid invoiceId, string reason, string actorUserId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return SubscriptionActionResult.Failed("A reason is required.");
        }

        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var invoice = await context.SubscriptionInvoices
            .Include(i => i.OrganisationSubscription)
            .SingleOrDefaultAsync(i => i.Id == invoiceId, ct);

        if (invoice is null)
        {
            return SubscriptionActionResult.Failed("Invoice not found.");
        }

        if (invoice.Status is SubscriptionInvoiceStatus.Paid or SubscriptionInvoiceStatus.Waived)
        {
            return SubscriptionActionResult.Failed($"Invoice is already {invoice.Status}.");
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        invoice.Status = SubscriptionInvoiceStatus.Waived;
        invoice.UpdatedAt = now;

        AddAuditEvent(context, invoice.OrganisationSubscription, SubscriptionEventType.InvoiceWaived, actorUserId,
            $"Waived invoice {invoice.InvoiceNumber}: {reason}", now);

        await context.SaveChangesAsync(ct);

        return SubscriptionActionResult.Succeeded();
    }

    public async Task<SubscriptionActionResult> RetryChargeAsync(
        Guid stokvelId, string reason, string actorUserId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return SubscriptionActionResult.Failed("A reason is required.");
        }

        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var subscription = await GetSubscriptionAsync(context, stokvelId, ct);

        if (subscription?.SubscriptionPlan is null)
        {
            return SubscriptionActionResult.Failed("No subscription found for this organisation.");
        }

        if (subscription.Status is not (SubscriptionStatus.PastDue or SubscriptionStatus.Restricted))
        {
            return SubscriptionActionResult.Failed("A manual retry only applies to a subscription that is PastDue or Restricted.");
        }

        var defaultMethod = await context.SubscriptionPaymentMethods
            .Where(m => m.OrganisationSubscriptionId == subscription.Id && m.IsDefault && m.RemovedAt == null)
            .OrderByDescending(m => m.AuthorisedAt)
            .FirstOrDefaultAsync(ct);

        if (defaultMethod is null || string.IsNullOrEmpty(defaultMethod.ProviderAuthorizationCode) ||
            string.IsNullOrEmpty(subscription.BillingEmail))
        {
            return SubscriptionActionResult.Failed("No stored payment method to retry.");
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var reference = $"sisonke-admin-retry-{subscription.Id:N}-{now:yyyyMMddHHmmss}";
        var chargeResult = await billingProvider.ChargeAuthorisationAsync(
            defaultMethod.ProviderAuthorizationCode, subscription.BillingEmail,
            (long)Math.Round(subscription.SubscriptionPlan.MonthlyPrice * 100m, MidpointRounding.AwayFromZero), reference, ct);

        context.SubscriptionPayments.Add(new SubscriptionPayment
        {
            Id = Guid.NewGuid(),
            OrganisationSubscriptionId = subscription.Id,
            Amount = subscription.SubscriptionPlan.MonthlyPrice,
            Currency = "ZAR",
            Status = chargeResult.Success ? SubscriptionPaymentStatus.Succeeded : SubscriptionPaymentStatus.Failed,
            Provider = defaultMethod.Provider,
            ChargePurpose = SubscriptionChargePurpose.ManualCollection,
            ProviderReference = chargeResult.Reference,
            ProviderRequestReference = reference,
            AttemptNumber = 0, // out-of-band manual retry — not part of DunningJob's numbered attempt sequence
            FailureCode = chargeResult.FailureCode,
            FailureMessage = chargeResult.FailureMessage,
            ProcessedAt = now,
            PaidAt = chargeResult.Success ? now : null,
            CreatedAt = now
        });

        AddAuditEvent(context, subscription, SubscriptionEventType.ManualRetryTriggered, actorUserId,
            $"Manual retry {(chargeResult.Success ? "succeeded" : "failed")}: {reason}", now);

        if (chargeResult.Success)
        {
            subscription.LastSuccessfulPaymentAt = now;
            subscription.DunningStartedAt = null;
            subscription.GracePeriodEndsAt = null;
            await context.SaveChangesAsync(ct);

            await stateMachine.TransitionAsync(
                context, subscription, SubscriptionStatus.Active,
                SubscriptionEventType.PaymentSucceeded, actorUserId, $"Manual retry succeeded: {reason}", ct);

            return SubscriptionActionResult.Succeeded("Charge succeeded — subscription reactivated.");
        }

        await context.SaveChangesAsync(ct);
        await entitlementService.InvalidateAsync(stokvelId, ct);
        logger.LogWarning(
            "Admin manual retry failed for subscription {SubscriptionId}, requested by {ActorUserId}: {Message}",
            subscription.Id, actorUserId, chargeResult.FailureMessage);

        return SubscriptionActionResult.Failed($"Charge failed: {chargeResult.FailureMessage ?? "unknown reason"}.");
    }

    public async Task<SubscriptionActionResult> MoveToEnterpriseAsync(
        Guid stokvelId, string reason, string actorUserId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return SubscriptionActionResult.Failed("A reason is required.");
        }

        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var subscription = await GetSubscriptionAsync(context, stokvelId, ct);

        if (subscription is null)
        {
            return SubscriptionActionResult.Failed("No subscription found for this organisation.");
        }

        var enterprisePlan = await context.SubscriptionPlans
            .SingleOrDefaultAsync(p => p.Code == PlanCodes.Enterprise && p.IsActive, ct);

        if (enterprisePlan is null)
        {
            return SubscriptionActionResult.Failed("The Enterprise plan is not configured.");
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        subscription.SubscriptionPlanId = enterprisePlan.Id;
        subscription.PendingPlanChangePlanId = null;
        subscription.PendingPlanChangeEffectiveAt = null;
        Touch(subscription, now);

        AddAuditEvent(context, subscription, SubscriptionEventType.MovedToEnterprise, actorUserId,
            $"Moved to Enterprise custom pricing: {reason}", now);

        await context.SaveChangesAsync(ct);
        await entitlementService.InvalidateAsync(stokvelId, ct);

        return SubscriptionActionResult.Succeeded();
    }

    public async Task<SubscriptionActionResult> ReinstateAsync(
        Guid stokvelId, string reason, string actorUserId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return SubscriptionActionResult.Failed("A reason is required.");
        }

        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var subscription = await GetSubscriptionAsync(context, stokvelId, ct);

        if (subscription is null)
        {
            return SubscriptionActionResult.Failed("No subscription found for this organisation.");
        }

        if (subscription.Status != SubscriptionStatus.Suspended)
        {
            return SubscriptionActionResult.Failed("Only a Suspended subscription can be reinstated.");
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        subscription.DunningStartedAt = null;
        subscription.GracePeriodEndsAt = null;
        subscription.SuspendedAt = null;
        await context.SaveChangesAsync(ct);

        await stateMachine.TransitionAsync(
            context, subscription, SubscriptionStatus.Active,
            SubscriptionEventType.ReinstatedByAdmin, actorUserId, reason, ct);

        return SubscriptionActionResult.Succeeded();
    }

    private static async Task<OrganisationSubscription?> GetSubscriptionAsync(
        ApplicationDbContext context, Guid stokvelId, CancellationToken ct) =>
        await context.OrganisationSubscriptions
            .Include(s => s.SubscriptionPlan)
            .Where(s => s.StokvelId == stokvelId)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(ct);

    private static void Touch(OrganisationSubscription subscription, DateTime now)
    {
        subscription.UpdatedAt = now;
        subscription.RowVersion = Guid.NewGuid().ToByteArray();
    }

    private static void AddAuditEvent(
        ApplicationDbContext context,
        OrganisationSubscription subscription,
        SubscriptionEventType eventType,
        string? actorUserId,
        string notes,
        DateTime now)
    {
        context.SubscriptionEvents.Add(new SubscriptionEvent
        {
            Id = Guid.NewGuid(),
            OrganisationSubscriptionId = subscription.Id,
            EventType = eventType,
            ActorUserId = actorUserId,
            Notes = notes,
            OccurredAt = now
        });
    }
}
