using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Services.Billing;

public interface ISubscriptionPaymentSetupService
{
    Task<PaymentSetupViewModel> GetStatusAsync(Guid stokvelId, string actorUserId, CancellationToken ct = default);
    Task<PaymentSetupStartResult> StartAsync(
        Guid stokvelId, string actorUserId, string email, string name, string? phone,
        string callbackBaseUrl, string redirectPath, CancellationToken ct = default);
    Task<PaymentSetupCompletionResult> CompleteAsync(
        string protectedState, string providerReference, string actorUserId, CancellationToken ct = default);
}

/// <summary>Authorizes, correlates and persists hosted payment-method setup without collecting subscription fees.</summary>
public sealed class SubscriptionPaymentSetupService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    MemberAccessService memberAccessService,
    ISubscriptionPaymentProviderResolver providerResolver,
    IPaymentSetupStateProtector stateProtector,
    SubscriptionPaymentOptions options,
    TimeProvider timeProvider,
    ILogger<SubscriptionPaymentSetupService> logger) : ISubscriptionPaymentSetupService
{
    public async Task<PaymentSetupViewModel> GetStatusAsync(Guid stokvelId, string actorUserId, CancellationToken ct = default)
    {
        if (!await memberAccessService.CanViewStokvelAsync(actorUserId, stokvelId))
        {
            return new(SubscriptionProvider.Unknown, "Unavailable", false, false,
                SubscriptionPaymentMethodType.Unknown, MandateStatus.None, null,
                "Payment setup details are unavailable.");
        }

        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var subscription = await context.OrganisationSubscriptions
            .AsNoTracking()
            .Include(value => value.PaymentMethods)
            .Where(value => value.StokvelId == stokvelId && value.Status != SubscriptionStatus.Cancelled)
            .OrderByDescending(value => value.CreatedAt)
            .FirstOrDefaultAsync(ct);

        var providerId = subscription?.Provider ?? options.DefaultProvider;
        if (!providerResolver.TryGetProvider(providerId, out var provider) || provider is null)
        {
            return new(providerId, providerId.ToString(), false, false,
                SubscriptionPaymentMethodType.Unknown, MandateStatus.None, null,
                "Payment setup is not available for the selected provider.");
        }

        var method = subscription?.PaymentMethods
            .Where(value => value.IsDefault && value.RemovedAt is null)
            .OrderByDescending(value => value.AuthorisedAt)
            .FirstOrDefault();

        return new(
            providerId, provider.DisplayName, provider.IsConfigured,
            method is not null && provider.IsPaymentReady(method),
            method?.PaymentMethodType ?? SubscriptionPaymentMethodType.Unknown,
            method?.MandateStatus ?? MandateStatus.None,
            method?.MaskedDisplay,
            provider.IsConfigured ? null : provider.UnavailableMessage);
    }

    public async Task<PaymentSetupStartResult> StartAsync(
        Guid stokvelId, string actorUserId, string email, string name, string? phone,
        string callbackBaseUrl, string redirectPath, CancellationToken ct = default)
    {
        if (!await memberAccessService.IsOfficeBearerAsync(actorUserId, stokvelId))
        {
            return PaymentSetupStartResult.Failed("Only an office bearer can set up a subscription payment method.");
        }

        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var subscription = await context.OrganisationSubscriptions
            .Include(value => value.SubscriptionPlan)
            .Where(value => value.StokvelId == stokvelId && value.Status != SubscriptionStatus.Cancelled)
            .OrderByDescending(value => value.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (subscription?.SubscriptionPlan is null)
        {
            return PaymentSetupStartResult.Failed("Select a subscription plan before setting up a payment method.");
        }

        var providerId = subscription.Provider ?? options.DefaultProvider;
        if (!providerResolver.TryGetProvider(providerId, out var provider) || provider is null || !provider.IsConfigured)
        {
            var message = provider?.UnavailableMessage ?? "Payment setup is not available for the selected provider.";
            return PaymentSetupStartResult.Failed(message);
        }

        var correlationReference = $"sisonke-setup-{Guid.NewGuid():N}";
        var safeRedirect = NormalizeRedirectPath(redirectPath);
        var state = stateProtector.Protect(new(
            stokvelId, subscription.Id, actorUserId, providerId, correlationReference, safeRedirect));
        var callbackUrl = AppendQuery(callbackBaseUrl, "state", state);

        var result = await provider.StartPaymentMethodSetupAsync(new(
            stokvelId, subscription.Id, subscription.ProviderCustomerCode,
            email, name, phone, callbackUrl, correlationReference), ct);

        if (!result.Success || string.IsNullOrWhiteSpace(result.SetupUrl))
        {
            return PaymentSetupStartResult.Failed(result.Message ?? "Payment setup is currently unavailable.");
        }

        subscription.Provider = providerId;
        subscription.ProviderCustomerCode = result.ProviderCustomerReference ?? subscription.ProviderCustomerCode;
        subscription.BillingEmail = email;
        subscription.UpdatedAt = timeProvider.GetUtcNow().UtcDateTime;
        subscription.RowVersion = Guid.NewGuid().ToByteArray();
        await context.SaveChangesAsync(ct);

        return new(true, result.SetupUrl, null);
    }

    public async Task<PaymentSetupCompletionResult> CompleteAsync(
        string protectedState, string providerReference, string actorUserId, CancellationToken ct = default)
    {
        if (!stateProtector.TryUnprotect(protectedState, out var state) || state is null ||
            !string.Equals(state.ActorUserId, actorUserId, StringComparison.Ordinal) ||
            !string.Equals(state.CorrelationReference, providerReference, StringComparison.Ordinal))
        {
            return PaymentSetupCompletionResult.Failed("Payment setup could not be correlated to this session.");
        }

        if (!await memberAccessService.IsOfficeBearerAsync(actorUserId, state.StokvelId))
        {
            return PaymentSetupCompletionResult.Failed("You are not authorized to update this stokvel's payment method.");
        }

        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var subscription = await context.OrganisationSubscriptions
            .Include(value => value.PaymentMethods)
            .SingleOrDefaultAsync(value => value.Id == state.SubscriptionId && value.StokvelId == state.StokvelId, ct);
        if (subscription is null || subscription.Provider != state.Provider)
        {
            return PaymentSetupCompletionResult.Failed("The subscription payment setup request is no longer valid.");
        }

        if (subscription.PaymentMethods.Any(method =>
                method.Provider == state.Provider && method.ProviderMandateReference == providerReference && method.RemovedAt is null))
        {
            return new(true, state.RedirectPath, "Payment method is already set up.");
        }

        if (!providerResolver.TryGetProvider(state.Provider, out var provider) || provider is null)
        {
            return PaymentSetupCompletionResult.Failed("The selected payment provider is unavailable.");
        }

        var status = await provider.GetPaymentMethodStatusAsync(new(
            providerReference, subscription.ProviderCustomerCode, subscription.BillingEmail), ct);
        if (!status.Success || string.IsNullOrWhiteSpace(status.ProviderPaymentMethodReference))
        {
            return PaymentSetupCompletionResult.Failed(status.Message ?? "Payment method setup could not be verified.");
        }

        var duplicateMethod = subscription.PaymentMethods.FirstOrDefault(method =>
            method.Provider == state.Provider &&
            method.ProviderPaymentMethodReference == status.ProviderPaymentMethodReference && method.RemovedAt is null);
        if (duplicateMethod is not null)
        {
            return new(true, state.RedirectPath, "Payment method is already set up.");
        }

        foreach (var method in subscription.PaymentMethods.Where(method => method.RemovedAt is null))
        {
            method.IsDefault = false;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var paymentMethod = new SubscriptionPaymentMethod
        {
            Id = Guid.NewGuid(),
            OrganisationSubscriptionId = subscription.Id,
            Provider = state.Provider,
            PaymentMethodType = status.PaymentMethodType,
            MandateStatus = status.MandateStatus,
            ProviderAuthorizationCode = state.Provider == SubscriptionProvider.Paystack ? status.ProviderPaymentMethodReference : null,
            ProviderPaymentMethodReference = status.ProviderPaymentMethodReference,
            ProviderMandateReference = status.ProviderMandateReference,
            MaskedDisplay = status.MaskedDisplay,
            CardBrand = status.CardBrand,
            Last4 = status.Last4,
            ExpiryMonth = status.ExpiryMonth,
            ExpiryYear = status.ExpiryYear,
            Bank = status.Bank,
            IsDefault = true,
            IsReusable = status.IsReusable,
            AuthorisedAt = now,
            MandateCreatedAt = now,
            MandateActivatedAt = status.MandateStatus == MandateStatus.Active ? now : null,
            CreatedAt = now
        };
        context.SubscriptionPaymentMethods.Add(paymentMethod);
        context.Entry(subscription).State = EntityState.Unchanged;

        await context.SaveChangesAsync(ct);
        logger.LogInformation("Payment setup completed for subscription {SubscriptionId} using {Provider}.", subscription.Id, state.Provider);

        return new(true, state.RedirectPath, "Payment method set up successfully.");
    }

    private static string NormalizeRedirectPath(string redirectPath) =>
        Uri.TryCreate(redirectPath, UriKind.Relative, out _) && redirectPath.StartsWith('/') && !redirectPath.StartsWith("//")
            ? redirectPath
            : "/subscription";

    private static string AppendQuery(string url, string name, string value) =>
        $"{url}{(url.Contains('?') ? '&' : '?')}{Uri.EscapeDataString(name)}={Uri.EscapeDataString(value)}";
}
