using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;
using Sisonke.Web.Tests.TestSupport;

namespace Sisonke.Web.Tests.Billing;

public class SubscriptionPaymentMethodPersistenceTests
{
    [Fact]
    public async Task PaymentMethodAndMandateMetadata_PersistWithoutBankOrCardCredentials()
    {
        using var db = new SqliteTestDatabase();
        await using var context = db.CreateContext();
        var stokvel = TestData.CreateStokvel(context);
        var subscription = TestData.CreateOrganisationSubscription(context, stokvel, null, SubscriptionStatus.PendingPaymentMethod);
        var createdAt = DateTime.UtcNow.AddMinutes(-2);
        var activatedAt = DateTime.UtcNow.AddMinutes(-1);

        var method = new SubscriptionPaymentMethod
        {
            Id = Guid.NewGuid(),
            OrganisationSubscriptionId = subscription.Id,
            Provider = SubscriptionProvider.Netcash,
            PaymentMethodType = SubscriptionPaymentMethodType.DebiCheck,
            MandateStatus = MandateStatus.Active,
            ProviderMandateReference = "safe-mandate-reference",
            ProviderPaymentMethodReference = "safe-payment-method-reference",
            MaskedDisplay = "Account ending 4321",
            MandateCreatedAt = createdAt,
            MandateActivatedAt = activatedAt,
            IsDefault = true,
            IsReusable = true
        };
        context.SubscriptionPaymentMethods.Add(method);

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var saved = await context.SubscriptionPaymentMethods.SingleAsync(m => m.Id == method.Id);
        Assert.Equal(SubscriptionProvider.Netcash, saved.Provider);
        Assert.Equal(SubscriptionPaymentMethodType.DebiCheck, saved.PaymentMethodType);
        Assert.Equal(MandateStatus.Active, saved.MandateStatus);
        Assert.Equal("safe-mandate-reference", saved.ProviderMandateReference);
        Assert.Equal("safe-payment-method-reference", saved.ProviderPaymentMethodReference);
        Assert.Equal("Account ending 4321", saved.MaskedDisplay);
        Assert.Equal(createdAt, saved.MandateCreatedAt);
        Assert.Equal(activatedAt, saved.MandateActivatedAt);
        Assert.Null(saved.ProviderAuthorizationCode);
    }

    [Fact]
    public async Task SubscriptionPayment_ProviderCorrelationAndPurpose_Persist()
    {
        using var db = new SqliteTestDatabase();
        await using var context = db.CreateContext();
        var stokvel = TestData.CreateStokvel(context);
        var subscription = TestData.CreateOrganisationSubscription(context, stokvel, null, SubscriptionStatus.Active);
        var processedAt = DateTime.UtcNow.AddMinutes(-1);
        var settledAt = DateTime.UtcNow;

        var payment = new SubscriptionPayment
        {
            Id = Guid.NewGuid(),
            OrganisationSubscriptionId = subscription.Id,
            Amount = 249m,
            Provider = SubscriptionProvider.Netcash,
            ChargePurpose = SubscriptionChargePurpose.RecurringSubscription,
            ProviderReference = $"charge-{Guid.NewGuid():N}",
            ProviderRequestReference = "request-correlation-123",
            ProviderTransactionId = "provider-transaction-456",
            Status = SubscriptionPaymentStatus.Succeeded,
            ProcessedAt = processedAt,
            SettledAt = settledAt
        };
        context.SubscriptionPayments.Add(payment);

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var saved = await context.SubscriptionPayments.SingleAsync(p => p.Id == payment.Id);
        Assert.Equal(SubscriptionProvider.Netcash, saved.Provider);
        Assert.Equal(SubscriptionChargePurpose.RecurringSubscription, saved.ChargePurpose);
        Assert.Equal("request-correlation-123", saved.ProviderRequestReference);
        Assert.Equal("provider-transaction-456", saved.ProviderTransactionId);
        Assert.Equal(processedAt, saved.ProcessedAt);
        Assert.Equal(settledAt, saved.SettledAt);
    }

    [Fact]
    public void BillingEntities_DoNotIntroduceSensitiveCredentialProperties()
    {
        var forbiddenPropertyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "CardNumber", "Pan", "Cvv", "Cvc", "BankAccountNumber", "OnlineBankingPassword",
            "OnlineBankingUsername", "BankingPassword", "BankingPin"
        };

        var billingEntityTypes = new[]
        {
            typeof(OrganisationSubscription),
            typeof(SubscriptionPaymentMethod),
            typeof(SubscriptionPayment)
        };

        var introducedSensitiveProperties = billingEntityTypes
            .SelectMany(type => type.GetProperties())
            .Where(property => forbiddenPropertyNames.Contains(property.Name))
            .Select(property => $"{property.DeclaringType!.Name}.{property.Name}")
            .ToList();

        Assert.Empty(introducedSensitiveProperties);
    }
}
