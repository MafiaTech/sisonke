using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;
using Sisonke.Web.Services.Entitlements;

namespace Sisonke.Web.Services.Billing.Netcash;

public interface INetcashMandateService
{
    bool IsConfigured { get; }
    string AvailabilityMessage { get; }
    Task<NetcashMandateActionResult> SubmitAsync(Guid stokvelId, string actorUserId, NetcashDebiCheckMandateRequest request, CancellationToken ct = default);
    Task<NetcashMandateActionResult> RefreshAsync(Guid stokvelId, string actorUserId, CancellationToken ct = default);
    Task<NetcashMandateActionResult> CancelAsync(Guid stokvelId, string actorUserId, CancellationToken ct = default);
}

/// <summary>
/// Authorizes tenant-scoped mandates solely for fees owed to Sisonke for its SaaS subscription,
/// with the amount derived from SubscriptionPlan.MonthlyPrice. This service is not a general
/// debit-order or stokvel money-movement API. It persists only provider references and masked metadata.
/// Full account and identity data exist only in the transient request passed to Netcash.
/// </summary>
public sealed class NetcashMandateService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    MemberAccessService memberAccessService,
    IStokvelOperationLock operationLock,
    INetcashClient netcashClient,
    NetcashOptions options,
    TimeProvider timeProvider,
    ILogger<NetcashMandateService> logger) : INetcashMandateService
{
    private const string OperationKey = "subscription-netcash-mandate";

    public bool IsConfigured => netcashClient.IsConfigured;
    public string AvailabilityMessage => options.Enabled
        ? "Netcash DebiCheck configuration is incomplete. Please contact support."
        : "Netcash DebiCheck setup is not configured in this environment.";

    public async Task<NetcashMandateActionResult> SubmitAsync(
        Guid stokvelId, string actorUserId, NetcashDebiCheckMandateRequest request, CancellationToken ct = default)
    {
        if (!await memberAccessService.IsOfficeBearerAsync(actorUserId, stokvelId))
        {
            return Failed("Only an office bearer can set up a DebiCheck mandate.");
        }

        var validationError = Validate(request);
        if (validationError is not null) return Failed(validationError);
        if (!IsConfigured) return Failed(AvailabilityMessage);

        await using var guard = await operationLock.AcquireAsync(stokvelId, OperationKey, ct);
        // Re-authorize after waiting for the lock so a removed office bearer cannot continue.
        if (!await memberAccessService.IsOfficeBearerAsync(actorUserId, stokvelId))
        {
            return Failed("You are no longer authorized to update this stokvel's mandate.");
        }

        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var subscription = await context.OrganisationSubscriptions
            .Include(value => value.SubscriptionPlan)
            .Include(value => value.PaymentMethods)
            .Where(value => value.StokvelId == stokvelId && value.Status != SubscriptionStatus.Cancelled && value.Status != SubscriptionStatus.Expired)
            .OrderByDescending(value => value.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (subscription?.SubscriptionPlan is null)
        {
            return Failed("Select a subscription plan before setting up DebiCheck.");
        }

        var existing = subscription.PaymentMethods
            .Where(value => value.Provider == SubscriptionProvider.Netcash && value.IsDefault && value.RemovedAt is null)
            .OrderByDescending(value => value.CreatedAt)
            .FirstOrDefault();
        if (existing is { MandateStatus: MandateStatus.Pending or MandateStatus.Active } &&
            !string.IsNullOrWhiteSpace(existing.ProviderMandateReference))
        {
            var ready = SubscriptionPaymentReadiness.IsReady(existing);
            return new(true, existing.MandateStatus, ready,
                ready ? "The DebiCheck mandate is already active." : "A DebiCheck mandate is already awaiting bank authentication.");
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var firstCollectionAt = subscription.TrialEndsAt ?? subscription.NextBillingAt ?? now.AddDays(1);
        if (firstCollectionAt <= now) firstCollectionAt = now.AddDays(1);
        var accountReference = BuildAccountReference(subscription.Id);

        var providerResult = await netcashClient.AuthenticateAsync(new(
            accountReference,
            request.AccountHolderName.Trim(), request.IsSouthAfricanId, request.DebtorIdentification.Trim(),
            request.BankAccountName.Trim(), request.BranchCode.Trim(), request.BankAccountNumber.Trim(),
            request.BankAccountType, NormalizeMobile(request.MobileNumber), request.EmailAddress.Trim(),
            subscription.SubscriptionPlan.MonthlyPrice, DateOnly.FromDateTime(firstCollectionAt),
            Math.Clamp(firstCollectionAt.Day, 1, 31)), ct);

        var status = providerResult.Success
            ? NetcashMandateStatusMapper.Map(providerResult.Status)
            : MandateStatus.Failed;

        foreach (var method in subscription.PaymentMethods.Where(value => value.IsDefault && value.RemovedAt is null))
        {
            method.IsDefault = false;
            method.RemovedAt = now;
        }

        var mandate = new SubscriptionPaymentMethod
        {
            Id = Guid.NewGuid(),
            OrganisationSubscriptionId = subscription.Id,
            Provider = SubscriptionProvider.Netcash,
            PaymentMethodType = SubscriptionPaymentMethodType.DebiCheck,
            MandateStatus = status,
            ProviderMandateReference = providerResult.ContractReference,
            MaskedDisplay = MaskAccount(request.BankAccountNumber),
            CardholderName = request.AccountHolderName.Trim(),
            IsDefault = true,
            IsReusable = status == MandateStatus.Active,
            AuthorisedAt = now,
            MandateCreatedAt = now,
            MandateActivatedAt = status == MandateStatus.Active ? now : null,
            CreatedAt = now
        };
        context.SubscriptionPaymentMethods.Add(mandate);

        subscription.Provider = SubscriptionProvider.Netcash;
        subscription.ProviderCustomerCode = accountReference;
        subscription.BillingEmail = request.EmailAddress.Trim();
        subscription.TermsVersion = request.TermsVersion.Trim();
        subscription.TermsAcceptedAt = now;
        subscription.TermsAcceptedByUserId = actorUserId;
        subscription.UpdatedAt = now;
        subscription.RowVersion = Guid.NewGuid().ToByteArray();
        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Netcash DebiCheck authentication result {ResultCode} persisted for subscription {SubscriptionId} with status {MandateStatus}.",
            providerResult.ErrorCode ?? "none", subscription.Id, status);

        if (!providerResult.Success)
        {
            return new(false, status, false, providerResult.Message ?? "Netcash rejected the DebiCheck request.");
        }

        var paymentReady = SubscriptionPaymentReadiness.IsReady(mandate);
        return new(true, status, paymentReady, providerResult.Message ??
            (paymentReady ? "The DebiCheck mandate is active." : "Approve the DebiCheck request through your bank, then check its status here."));
    }

    public async Task<NetcashMandateActionResult> RefreshAsync(
        Guid stokvelId, string actorUserId, CancellationToken ct = default)
    {
        if (!await memberAccessService.IsOfficeBearerAsync(actorUserId, stokvelId))
            return Failed("Only an office bearer can refresh a DebiCheck mandate.");
        if (!IsConfigured) return Failed(AvailabilityMessage);

        await using var guard = await operationLock.AcquireAsync(stokvelId, OperationKey, ct);
        if (!await memberAccessService.IsOfficeBearerAsync(actorUserId, stokvelId))
            return Failed("You are no longer authorized to update this stokvel's mandate.");
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var subscription = await LoadSubscriptionAsync(context, stokvelId, ct);
        var mandate = GetCurrentMandate(subscription);
        if (mandate is null || string.IsNullOrWhiteSpace(mandate.ProviderMandateReference))
            return Failed("No Netcash mandate is available to refresh.");

        var result = await netcashClient.GetAuthenticationStatusAsync(mandate.ProviderMandateReference, ct);
        if (!result.Success)
            return new(false, mandate.MandateStatus, SubscriptionPaymentReadiness.IsReady(mandate),
                result.Message ?? "The mandate status could not be refreshed.");
        if (!string.Equals(result.ContractReference, mandate.ProviderMandateReference, StringComparison.Ordinal))
        {
            logger.LogWarning("Netcash trace correlation mismatch for subscription {SubscriptionId}.", subscription!.Id);
            return new(false, mandate.MandateStatus, SubscriptionPaymentReadiness.IsReady(mandate),
                "Netcash returned a status that could not be correlated to this mandate.");
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var status = NetcashMandateStatusMapper.Map(result.Status);
        mandate.MandateStatus = status;
        mandate.IsReusable = status == MandateStatus.Active;
        mandate.MandateActivatedAt ??= status == MandateStatus.Active ? now : null;
        subscription!.UpdatedAt = now;
        subscription.RowVersion = Guid.NewGuid().ToByteArray();
        await context.SaveChangesAsync(ct);

        return new(true, status, SubscriptionPaymentReadiness.IsReady(mandate), result.Message ?? "Mandate status refreshed.");
    }

    public async Task<NetcashMandateActionResult> CancelAsync(
        Guid stokvelId, string actorUserId, CancellationToken ct = default)
    {
        if (!await memberAccessService.IsOfficeBearerAsync(actorUserId, stokvelId))
            return Failed("Only an office bearer can cancel a DebiCheck mandate.");
        if (!IsConfigured) return Failed(AvailabilityMessage);

        await using var guard = await operationLock.AcquireAsync(stokvelId, OperationKey, ct);
        if (!await memberAccessService.IsOfficeBearerAsync(actorUserId, stokvelId))
            return Failed("You are no longer authorized to update this stokvel's mandate.");
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var subscription = await LoadSubscriptionAsync(context, stokvelId, ct);
        var mandate = GetCurrentMandate(subscription);
        if (mandate is null || string.IsNullOrWhiteSpace(mandate.ProviderMandateReference))
            return Failed("No active Netcash mandate is available to cancel.");
        if (mandate.MandateStatus is MandateStatus.Revoked or MandateStatus.Expired)
            return new(true, mandate.MandateStatus, false, "The mandate is already inactive.");
        if (mandate.MandateStatus != MandateStatus.Active)
            return new(false, mandate.MandateStatus, false, "Only an accepted DebiCheck mandate can be cancelled at Netcash.");

        var result = await netcashClient.CancelAuthenticationAsync(mandate.ProviderMandateReference, ct);
        if (!result.Success)
            return new(false, mandate.MandateStatus, SubscriptionPaymentReadiness.IsReady(mandate),
                result.Message ?? "The mandate could not be cancelled.");

        // Netcash code 000 means submitted to the bank, not completed. Trace remains authoritative.
        mandate.MandateStatus = MandateStatus.Pending;
        mandate.IsReusable = false;
        var now = timeProvider.GetUtcNow().UtcDateTime;
        subscription!.UpdatedAt = now;
        subscription.RowVersion = Guid.NewGuid().ToByteArray();
        await context.SaveChangesAsync(ct);
        return new(true, MandateStatus.Pending, false, "Mandate cancellation was submitted. Check status to confirm revocation.");
    }

    public static string BuildAccountReference(Guid subscriptionId) => $"SK{subscriptionId:N}"[..22];

    private static async Task<OrganisationSubscription?> LoadSubscriptionAsync(
        ApplicationDbContext context, Guid stokvelId, CancellationToken ct) =>
        await context.OrganisationSubscriptions.Include(value => value.PaymentMethods)
            .Where(value => value.StokvelId == stokvelId && value.Status != SubscriptionStatus.Cancelled)
            .OrderByDescending(value => value.CreatedAt).FirstOrDefaultAsync(ct);

    private static SubscriptionPaymentMethod? GetCurrentMandate(OrganisationSubscription? subscription) =>
        subscription?.PaymentMethods
            .Where(value => value.Provider == SubscriptionProvider.Netcash && value.IsDefault && value.RemovedAt is null)
            .OrderByDescending(value => value.CreatedAt).FirstOrDefault();

    private static NetcashMandateActionResult Failed(string message) =>
        new(false, MandateStatus.None, false, message);

    private static string? Validate(NetcashDebiCheckMandateRequest request)
    {
        if (!request.ConsentAccepted) return "Explicit DebiCheck mandate consent is required.";
        if (!string.Equals(request.TermsVersion, BillingTermsVersion.Current, StringComparison.Ordinal))
            return "The DebiCheck consent terms changed. Review and accept the current version.";
        if (string.IsNullOrWhiteSpace(request.AccountHolderName) || request.AccountHolderName.Trim().Length > 50)
            return "Enter the account holder name (maximum 50 characters).";
        if (string.IsNullOrWhiteSpace(request.BankAccountName) || request.BankAccountName.Trim().Length > 30)
            return "Enter the name registered on the bank account (maximum 30 characters).";
        if (string.IsNullOrWhiteSpace(request.DebtorIdentification) || request.DebtorIdentification.Trim().Length > 20)
            return "Enter a valid identity or passport number.";
        if (!Regex.IsMatch(request.BranchCode?.Trim() ?? string.Empty, "^[0-9]{6}$"))
            return "Enter a valid 6-digit branch code.";
        if (!Regex.IsMatch(request.BankAccountNumber?.Trim() ?? string.Empty, "^[0-9]{4,16}$"))
            return "Enter a valid bank account number (4 to 16 digits).";
        if (!Enum.IsDefined(request.BankAccountType)) return "Select a valid bank account type.";
        if (!Regex.IsMatch(NormalizeMobile(request.MobileNumber), "^[0-9]{10,11}$"))
            return "Enter a valid mobile number.";
        if (!System.Net.Mail.MailAddress.TryCreate(request.EmailAddress?.Trim(), out _))
            return "Enter a valid email address.";
        return null;
    }

    private static string NormalizeMobile(string value) => Regex.Replace(value ?? string.Empty, "[^0-9]", string.Empty);

    private static string MaskAccount(string accountNumber)
    {
        var digits = accountNumber.Trim();
        var last4 = digits.Length >= 4 ? digits[^4..] : digits;
        return $"Bank account ending {last4}";
    }
}
