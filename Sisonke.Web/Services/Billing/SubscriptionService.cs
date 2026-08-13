using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;
using Sisonke.Web.Services.Billing.Paystack;
using Sisonke.Web.Services.Entitlements;

namespace Sisonke.Web.Services.Billing;

/// <summary>
/// Orchestrates the plan-selection -> card-authorisation -> trial -> upgrade/downgrade/cancel
/// lifecycle. All status changes go through ISubscriptionStateMachine (the single place
/// OrganisationSubscription.Status is ever assigned — see Phase 4 report); non-status field
/// changes (plan swap, terms, provider codes) are saved directly. Deviates from the brief's rough
/// CompleteCardAuthorisationAsync(reference) shape by requiring stokvelId explicitly — the
/// alternative (recovering the stokvel from Paystack's transaction metadata) was not verified
/// during Phase 3's API research, and every other method in this codebase identifies its target
/// stokvel explicitly rather than implicitly.
/// </summary>
public sealed class SubscriptionService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    IBillingProvider billingProvider,
    IEntitlementService entitlementService,
    IEntitlementUsageProvider usageProvider,
    ISubscriptionStateMachine stateMachine,
    MemberAccessService memberAccessService,
    SubscriptionNotificationService notificationService,
    PaystackOptions paystackOptions,
    TimeProvider timeProvider,
    ILogger<SubscriptionService> logger)
{
    private static readonly (string Code, string Description)[] NumericFeatures =
    [
        (FeatureCodes.MaxMembers, "active members"),
        (FeatureCodes.MaxAdministrators, "administrators"),
        (FeatureCodes.MaxSchemes, "schemes"),
        (FeatureCodes.StorageMb, "MB of storage used")
    ];

    public async Task<SubscriptionActionResult> SelectPlanAsync(
        Guid stokvelId, string planCode, string acceptedTermsVersion, string actorUserId,
        string? actorIpAddress = null, CancellationToken ct = default)
    {
        if (!await memberAccessService.IsOfficeBearerAsync(actorUserId, stokvelId))
        {
            return SubscriptionActionResult.Failed("Only an office bearer can select a subscription plan.");
        }

        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var plan = await context.SubscriptionPlans.SingleOrDefaultAsync(p => p.Code == planCode && p.IsActive, ct);
        if (plan is null)
        {
            return SubscriptionActionResult.Failed("The selected plan is not available.");
        }

        var subscription = await GetOrCreateSubscriptionAsync(context, stokvelId, ct);
        var now = timeProvider.GetUtcNow().UtcDateTime;

        subscription.SubscriptionPlanId = plan.Id;
        subscription.TermsAcceptedAt = now;
        subscription.TermsAcceptedByUserId = actorUserId;
        subscription.TermsVersion = acceptedTermsVersion;
        subscription.TermsAcceptedIpAddress = actorIpAddress;

        AddAuditEvent(context, subscription, SubscriptionEventType.TermsAccepted, actorUserId, $"Accepted terms version {acceptedTermsVersion}.", now);

        if (subscription.Status == SubscriptionStatus.PendingPaymentMethod)
        {
            // Re-selecting a plan before authorising a card — no status change, just persist.
            Touch(subscription, now);
            await context.SaveChangesAsync(ct);
            await entitlementService.InvalidateAsync(stokvelId, ct);
        }
        else
        {
            await context.SaveChangesAsync(ct); // persist plan/terms fields before the transition reloads them
            await stateMachine.TransitionAsync(
                context, subscription, SubscriptionStatus.PendingPaymentMethod,
                SubscriptionEventType.PlanSelected, actorUserId, $"Selected plan {planCode}.", ct);
        }

        return SubscriptionActionResult.Succeeded();
    }

    public async Task<CardAuthorisationResult> BeginCardAuthorisationAsync(
        Guid stokvelId, string email, string name, string? phone, string callbackUrl, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var subscription = await GetLiveSubscriptionAsync(context, stokvelId, ct);

        if (subscription?.SubscriptionPlan is null)
        {
            return CardAuthorisationResult.Failed("Select a plan before authorising a card.");
        }

        if (subscription.Status != SubscriptionStatus.PendingPaymentMethod)
        {
            return CardAuthorisationResult.Failed("This organisation is not currently awaiting a payment method.");
        }

        try
        {
            if (string.IsNullOrEmpty(subscription.ProviderCustomerCode))
            {
                subscription.ProviderCustomerCode = await billingProvider.EnsureCustomerAsync(stokvelId, email, name, phone, ct);
                subscription.BillingEmail = email;
                Touch(subscription, timeProvider.GetUtcNow().UtcDateTime);
                await context.SaveChangesAsync(ct);
            }

            var start = await billingProvider.StartCardAuthorisationAsync(
                subscription.ProviderCustomerCode!, email, paystackOptions.CardVerificationAmountMinorUnits, callbackUrl, ct);

            return new CardAuthorisationResult(true, null, start.AuthorisationUrl, start.Reference);
        }
        catch (Exception ex) when (IsProviderFailure(ex))
        {
            logger.LogError(ex, "Failed to start card authorisation for stokvel {StokvelId}.", stokvelId);
            return CardAuthorisationResult.Failed(DescribeProviderFailure(ex));
        }
    }

    public async Task<SubscriptionActionResult> CompleteCardAuthorisationAsync(Guid stokvelId, string reference, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var subscription = await GetLiveSubscriptionAsync(context, stokvelId, ct);

        if (subscription?.SubscriptionPlan is null || string.IsNullOrEmpty(subscription.ProviderCustomerCode))
        {
            return SubscriptionActionResult.Failed("Card authorisation was started for a subscription that no longer exists.");
        }

        VerifiedAuthorisation verification;
        try
        {
            verification = await billingProvider.VerifyAuthorisationAsync(reference, ct);
        }
        catch (Exception ex) when (IsProviderFailure(ex))
        {
            logger.LogError(ex, "Failed to verify card authorisation for stokvel {StokvelId}, reference {Reference}.", stokvelId, reference);
            return SubscriptionActionResult.Failed(DescribeProviderFailure(ex));
        }

        if (!verification.Success || verification.AuthorizationCode is null)
        {
            return SubscriptionActionResult.Failed("Card authorisation could not be verified. Please try again.");
        }

        if (!verification.Reusable)
        {
            return SubscriptionActionResult.Failed(
                "This card cannot be used for a recurring subscription. Please try a different card that supports recurring billing.");
        }

        await RefundVerificationChargeAsync(reference, stokvelId, ct);

        var plan = subscription.SubscriptionPlan;
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var trialEnds = now.AddDays(paystackOptions.TrialDays);

        ProviderSubscriptionResult subscriptionResult;
        try
        {
            if (string.IsNullOrEmpty(plan.ProviderPlanCode))
            {
                plan.ProviderPlanCode = await billingProvider.EnsurePlanAsync(
                    plan.Code!, plan.Name, ToMinorUnits(plan.MonthlyPrice), "monthly", ct);
                await context.SaveChangesAsync(ct);
            }

            subscriptionResult = await billingProvider.CreateSubscriptionAsync(
                subscription.ProviderCustomerCode!, plan.ProviderPlanCode, verification.AuthorizationCode, trialEnds, ct);
        }
        catch (Exception ex) when (IsProviderFailure(ex))
        {
            // The card IS authorised at this point (verification succeeded and was refunded) —
            // the payment method row is intentionally not added here so a retry from the same
            // callback doesn't end up with an orphaned card-on-file and no subscription. The
            // subscription stays PendingPaymentMethod; the office bearer can retry authorisation.
            logger.LogError(ex, "Card verified but Paystack subscription creation failed for stokvel {StokvelId}.", stokvelId);
            return SubscriptionActionResult.Failed(DescribeProviderFailure(ex));
        }

        context.SubscriptionPaymentMethods.Add(BuildPaymentMethod(subscription.Id, verification, now));

        subscription.ProviderSubscriptionCode = subscriptionResult.SubscriptionCode;
        subscription.ProviderEmailToken = subscriptionResult.EmailToken;
        subscription.ProviderPlanCode = plan.ProviderPlanCode;
        subscription.Provider = SubscriptionProvider.Paystack;
        subscription.TrialStartedAt = now;
        subscription.TrialEndsAt = trialEnds;
        subscription.TrialOptedIn = true;
        subscription.NextBillingAt = trialEnds;

        AddAuditEvent(context, subscription, SubscriptionEventType.PaymentMethodAuthorised, null, "Card authorised.", now);
        await context.SaveChangesAsync(ct);

        await stateMachine.TransitionAsync(
            context, subscription, SubscriptionStatus.Trialing,
            SubscriptionEventType.TrialStarted, null, $"Trial started, ends {trialEnds:yyyy-MM-dd}.", ct);

        await notificationService.SendTrialStartedAsync(context, subscription, plan, ct);

        return SubscriptionActionResult.Succeeded();
    }

    public async Task<SubscriptionActionResult> ChangePlanAsync(Guid stokvelId, string newPlanCode, string actorUserId, CancellationToken ct = default)
    {
        if (!await memberAccessService.IsOfficeBearerAsync(actorUserId, stokvelId))
        {
            return SubscriptionActionResult.Failed("Only an office bearer can change the subscription plan.");
        }

        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var subscription = await GetLiveSubscriptionAsync(context, stokvelId, ct);

        if (subscription?.SubscriptionPlan is null)
        {
            return SubscriptionActionResult.Failed("No active subscription to change.");
        }

        var oldPlan = subscription.SubscriptionPlan;
        var newPlan = await context.SubscriptionPlans.SingleOrDefaultAsync(p => p.Code == newPlanCode && p.IsActive, ct);
        if (newPlan is null)
        {
            return SubscriptionActionResult.Failed("The selected plan is not available.");
        }

        var isUpgrade = newPlan.DisplayOrder > oldPlan.DisplayOrder;
        var now = timeProvider.GetUtcNow().UtcDateTime;

        if (!isUpgrade)
        {
            var blocker = await FindDowngradeBlockerAsync(context, stokvelId, newPlan, ct);
            if (blocker is { } found)
            {
                return SubscriptionActionResult.Failed(
                    $"You have {found.Usage} {found.Description}; {newPlan.Name} supports up to {found.Limit}. " +
                    $"Remove some before downgrading, or choose a different plan.");
            }

            subscription.PendingPlanChangePlanId = newPlan.Id;
            subscription.PendingPlanChangeEffectiveAt = subscription.CurrentPeriodEndsAt ?? subscription.TrialEndsAt ?? now;
            Touch(subscription, now);

            AddAuditEvent(context, subscription, SubscriptionEventType.Downgraded, actorUserId,
                $"Downgrade to {newPlanCode} scheduled for {subscription.PendingPlanChangeEffectiveAt:yyyy-MM-dd}.", now);

            await context.SaveChangesAsync(ct);
            await entitlementService.InvalidateAsync(stokvelId, ct);
            await notificationService.SendPlanChangedAsync(
                context, subscription, oldPlan.Name, newPlan.Name, subscription.PendingPlanChangeEffectiveAt!.Value,
                "This is a downgrade — it takes effect at the end of your current billing period; no refund is issued for the current period.", ct);

            return SubscriptionActionResult.Succeeded(
                $"Downgrade to {newPlan.Name} is scheduled for the end of the current billing period ({subscription.PendingPlanChangeEffectiveAt:d MMMM yyyy}).");
        }

        // Upgrade takes effect immediately (proration policy: brief recommends immediate upgrade,
        // downgrade at period end — implemented above). Paystack-side plan swap (disable + create
        // new subscription on the new plan) is not yet wired here — no confirmed single-call
        // "change subscription plan" endpoint was found during Phase 3's API research; the local
        // entitlement-relevant state changes immediately, provider-side proration/billing sync is
        // a follow-up.
        subscription.SubscriptionPlanId = newPlan.Id;
        subscription.PendingPlanChangePlanId = null;
        subscription.PendingPlanChangeEffectiveAt = null;
        Touch(subscription, now);

        AddAuditEvent(context, subscription, SubscriptionEventType.Upgraded, actorUserId, $"Upgraded to {newPlanCode}.", now);

        await context.SaveChangesAsync(ct);
        await entitlementService.InvalidateAsync(stokvelId, ct);
        await notificationService.SendPlanChangedAsync(
            context, subscription, oldPlan.Name, newPlan.Name, now, "This is an upgrade — it is effective immediately; your next invoice reflects the new plan's price.", ct);

        return SubscriptionActionResult.Succeeded();
    }

    public async Task<SubscriptionActionResult> CancelAsync(
        Guid stokvelId, bool atPeriodEnd, string? reason, string actorUserId, CancellationToken ct = default)
    {
        if (!await memberAccessService.IsOfficeBearerAsync(actorUserId, stokvelId))
        {
            return SubscriptionActionResult.Failed("Only an office bearer can cancel the subscription.");
        }

        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var subscription = await GetLiveSubscriptionAsync(context, stokvelId, ct);
        if (subscription is null)
        {
            return SubscriptionActionResult.Failed("No active subscription to cancel.");
        }

        if (!stateMachine.CanTransition(subscription.Status, SubscriptionStatus.Cancelled))
        {
            return SubscriptionActionResult.Failed($"A subscription in {subscription.Status} status cannot be cancelled.");
        }

        if (!string.IsNullOrEmpty(subscription.ProviderSubscriptionCode) && !string.IsNullOrEmpty(subscription.ProviderEmailToken))
        {
            try
            {
                await billingProvider.CancelSubscriptionAsync(subscription.ProviderSubscriptionCode, subscription.ProviderEmailToken, ct);
            }
            catch (PaystackApiException ex)
            {
                logger.LogError(ex, "Failed to disable Paystack subscription for stokvel {StokvelId}.", stokvelId);
                return SubscriptionActionResult.Failed("Could not cancel the subscription with the payment provider. Please try again.");
            }
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        subscription.CancelAtPeriodEnd = atPeriodEnd;
        var accessUntil = subscription.CurrentPeriodEndsAt ?? subscription.TrialEndsAt ?? now;

        if (!atPeriodEnd)
        {
            subscription.CancelledAt = now;
            await context.SaveChangesAsync(ct);
            await stateMachine.TransitionAsync(
                context, subscription, SubscriptionStatus.Cancelled,
                SubscriptionEventType.Cancelled, actorUserId, reason ?? "Cancelled by office bearer.", ct);
            accessUntil = now;
        }
        else
        {
            Touch(subscription, now);
            AddAuditEvent(context, subscription, SubscriptionEventType.Cancelled, actorUserId,
                reason ?? "Cancellation scheduled for period end by office bearer.", now);
            await context.SaveChangesAsync(ct);
            await entitlementService.InvalidateAsync(stokvelId, ct);
        }

        await notificationService.SendCancellationAsync(context, subscription, now, accessUntil, ct);

        return SubscriptionActionResult.Succeeded(atPeriodEnd
            ? $"Subscription will remain active until {accessUntil:d MMMM yyyy}, then end."
            : "Subscription cancelled immediately.");
    }

    public async Task<CardAuthorisationResult> UpdatePaymentMethodAsync(
        Guid stokvelId, string email, string name, string? phone, string callbackUrl, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var subscription = await GetLiveSubscriptionAsync(context, stokvelId, ct);
        if (subscription is null)
        {
            return CardAuthorisationResult.Failed("No subscription found for this organisation.");
        }

        try
        {
            var customerCode = subscription.ProviderCustomerCode;
            if (string.IsNullOrEmpty(customerCode))
            {
                customerCode = await billingProvider.EnsureCustomerAsync(stokvelId, email, name, phone, ct);
                subscription.ProviderCustomerCode = customerCode;
                Touch(subscription, timeProvider.GetUtcNow().UtcDateTime);
                await context.SaveChangesAsync(ct);
            }

            var start = await billingProvider.StartCardAuthorisationAsync(
                customerCode, email, paystackOptions.CardVerificationAmountMinorUnits, callbackUrl, ct);

            return new CardAuthorisationResult(true, null, start.AuthorisationUrl, start.Reference);
        }
        catch (Exception ex) when (IsProviderFailure(ex))
        {
            logger.LogError(ex, "Failed to start payment-method update for stokvel {StokvelId}.", stokvelId);
            return CardAuthorisationResult.Failed(DescribeProviderFailure(ex));
        }
    }

    public async Task<SubscriptionActionResult> CompletePaymentMethodUpdateAsync(Guid stokvelId, string reference, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var subscription = await GetLiveSubscriptionAsync(context, stokvelId, ct);
        if (subscription is null)
        {
            return SubscriptionActionResult.Failed("No subscription found for this organisation.");
        }

        VerifiedAuthorisation verification;
        try
        {
            verification = await billingProvider.VerifyAuthorisationAsync(reference, ct);
        }
        catch (Exception ex) when (IsProviderFailure(ex))
        {
            logger.LogError(ex, "Failed to verify payment-method update for stokvel {StokvelId}, reference {Reference}.", stokvelId, reference);
            return SubscriptionActionResult.Failed(DescribeProviderFailure(ex));
        }

        if (!verification.Success || verification.AuthorizationCode is null)
        {
            return SubscriptionActionResult.Failed("Card authorisation could not be verified. Please try again.");
        }

        if (!verification.Reusable)
        {
            return SubscriptionActionResult.Failed("This card cannot be used for a recurring subscription. Please try a different card.");
        }

        await RefundVerificationChargeAsync(reference, stokvelId, ct);

        var existingMethods = await context.SubscriptionPaymentMethods
            .Where(m => m.OrganisationSubscriptionId == subscription.Id && m.RemovedAt == null)
            .ToListAsync(ct);
        foreach (var method in existingMethods)
        {
            method.IsDefault = false;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        context.SubscriptionPaymentMethods.Add(BuildPaymentMethod(subscription.Id, verification, now));

        var wasPastDue = subscription.Status == SubscriptionStatus.PastDue;
        Touch(subscription, now);

        AddAuditEvent(context, subscription, SubscriptionEventType.PaymentMethodAuthorised, null, "Payment method updated.", now);

        if (wasPastDue)
        {
            // Actually re-charging the outstanding invoice with the new card is DunningJob's job
            // (Phase 4) — it re-attempts on its own schedule and will pick up this new default
            // payment method; no confirmed single Paystack call for "retry immediately" was found
            // during Phase 3's API research, so this does not attempt an immediate charge itself.
            AddAuditEvent(context, subscription, SubscriptionEventType.RetryScheduled, null,
                "Payment method updated while PastDue; next dunning retry will use it.", now);
        }

        await context.SaveChangesAsync(ct);
        await entitlementService.InvalidateAsync(stokvelId, ct);

        return SubscriptionActionResult.Succeeded();
    }

    /// <summary>Paystack's hosted subscription-management page link. Kept behind this wrapper so no Paystack type/call leaks into the UI layer.</summary>
    public async Task<string?> GetManagementLinkAsync(Guid stokvelId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var subscription = await GetLiveSubscriptionAsync(context, stokvelId, ct);

        if (string.IsNullOrEmpty(subscription?.ProviderSubscriptionCode))
        {
            return null;
        }

        try
        {
            return await billingProvider.GetManagementLinkAsync(subscription.ProviderSubscriptionCode, ct);
        }
        catch (Exception ex) when (IsProviderFailure(ex))
        {
            logger.LogError(ex, "Failed to fetch Paystack management link for stokvel {StokvelId}.", stokvelId);
            return null;
        }
    }

    /// <summary>
    /// Every public method here that calls IBillingProvider must catch failures like this one —
    /// letting a Paystack outage/misconfiguration (including a missing Paystack:SecretKey, which
    /// surfaces as an HTTP 401) throw out of a Blazor event handler kills the user's circuit
    /// instead of showing an inline error. PaystackApiException covers business-level failures
    /// (4xx/5xx after Polly's retries are exhausted); HttpRequestException/TaskCanceledException
    /// cover the network/timeout case for the same reason.
    /// </summary>
    private static bool IsProviderFailure(Exception ex) =>
        ex is PaystackApiException or HttpRequestException or TaskCanceledException;

    private static string DescribeProviderFailure(Exception ex) =>
        ex is PaystackApiException { StatusCode: 401 or 403 }
            ? "The payment provider is not configured correctly. Please contact support."
            : "We could not reach the payment provider. Please try again in a moment.";

    private async Task RefundVerificationChargeAsync(string reference, Guid stokvelId, CancellationToken ct)
    {
        try
        {
            await billingProvider.RefundTransactionAsync(reference, ct);
        }
        catch (PaystackApiException ex)
        {
            // Not fatal to the authorisation flow — the card is authorised either way — but must
            // not be silent: a stuck refund is real money sitting somewhere it shouldn't.
            logger.LogError(ex, "Failed to refund card verification charge {Reference} for stokvel {StokvelId}.", reference, stokvelId);
        }
    }

    private async Task<(int Usage, int Limit, string Description)?> FindDowngradeBlockerAsync(
        ApplicationDbContext context, Guid stokvelId, SubscriptionPlan targetPlan, CancellationToken ct)
    {
        foreach (var (code, description) in NumericFeatures)
        {
            var planFeature = await context.PlanFeatures
                .Include(pf => pf.FeatureDefinition)
                .FirstOrDefaultAsync(pf => pf.SubscriptionPlanId == targetPlan.Id && pf.FeatureDefinition.Code == code, ct);

            if (planFeature?.LimitValue is not { } limit)
            {
                continue; // unlimited on the target plan, or feature not tracked there
            }

            var usage = await usageProvider.GetCurrentUsageAsync(stokvelId, code, ct);
            if (usage > limit)
            {
                return (usage, limit, description);
            }
        }

        return null;
    }

    private static SubscriptionPaymentMethod BuildPaymentMethod(Guid organisationSubscriptionId, VerifiedAuthorisation verification, DateTime now) => new()
    {
        Id = Guid.NewGuid(),
        OrganisationSubscriptionId = organisationSubscriptionId,
        Provider = SubscriptionProvider.Paystack,
        PaymentMethodType = SubscriptionPaymentMethodType.Card,
        MandateStatus = MandateStatus.Active,
        ProviderAuthorizationCode = verification.AuthorizationCode!,
        MaskedDisplay = string.IsNullOrWhiteSpace(verification.Last4) ? null : $"Card ending {verification.Last4}",
        CardBrand = verification.CardBrand,
        Last4 = verification.Last4,
        ExpiryMonth = verification.ExpiryMonth,
        ExpiryYear = verification.ExpiryYear,
        Bank = verification.Bank,
        IsDefault = true,
        IsReusable = true,
        AuthorisedAt = now,
        MandateCreatedAt = now,
        MandateActivatedAt = now
    };

    private async Task<OrganisationSubscription> GetOrCreateSubscriptionAsync(ApplicationDbContext context, Guid stokvelId, CancellationToken ct)
    {
        var existing = await GetLiveSubscriptionAsync(context, stokvelId, ct);
        if (existing is not null)
        {
            return existing;
        }

        var created = new OrganisationSubscription
        {
            Id = Guid.NewGuid(),
            StokvelId = stokvelId,
            Status = SubscriptionStatus.LegacyUnsubscribed,
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime
        };
        context.OrganisationSubscriptions.Add(created);

        return created;
    }

    private static async Task<OrganisationSubscription?> GetLiveSubscriptionAsync(ApplicationDbContext context, Guid stokvelId, CancellationToken ct)
    {
        var subscriptions = await context.OrganisationSubscriptions
            .Include(s => s.SubscriptionPlan)
            .Where(s => s.StokvelId == stokvelId)
            .ToListAsync(ct);

        if (subscriptions.Count == 0)
        {
            return null;
        }

        return subscriptions
            .Where(s => s.Status is not (SubscriptionStatus.Cancelled or SubscriptionStatus.Expired))
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefault()
            ?? subscriptions.OrderByDescending(s => s.CreatedAt).First();
    }

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

    private static long ToMinorUnits(decimal amount) => (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);
}
