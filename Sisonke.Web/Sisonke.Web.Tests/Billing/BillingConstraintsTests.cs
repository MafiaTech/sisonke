using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;
using Sisonke.Web.Tests.TestSupport;

namespace Sisonke.Web.Tests.Billing;

public class BillingConstraintsTests
{
    [Fact]
    public async Task HistoricSubscriptionAndPaymentRows_RemainValidWithNewFieldsUnset()
    {
        using var db = new SqliteTestDatabase();
        await using var context = db.CreateContext();
        var stokvel = TestData.CreateStokvel(context);
        var subscription = TestData.CreateOrganisationSubscription(
            context, stokvel, null, SubscriptionStatus.LegacyUnsubscribed);

        context.SubscriptionPayments.Add(new SubscriptionPayment
        {
            Id = Guid.NewGuid(),
            OrganisationSubscriptionId = subscription.Id,
            Amount = 99m,
            ProviderReference = $"historic-{Guid.NewGuid():N}"
        });

        context.SubscriptionPaymentMethods.Add(new SubscriptionPaymentMethod
        {
            Id = Guid.NewGuid(),
            OrganisationSubscriptionId = subscription.Id,
            Provider = SubscriptionProvider.Paystack
        });

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var savedSubscription = await context.OrganisationSubscriptions.SingleAsync(s => s.Id == subscription.Id);
        var savedPayment = await context.SubscriptionPayments.SingleAsync();
        var savedMethod = await context.SubscriptionPaymentMethods.SingleAsync();

        Assert.False(savedSubscription.TrialOptedIn);
        Assert.Null(savedPayment.Provider);
        Assert.Null(savedPayment.ChargePurpose);
        Assert.Equal(SubscriptionPaymentMethodType.Unknown, savedMethod.PaymentMethodType);
        Assert.Equal(MandateStatus.None, savedMethod.MandateStatus);
        Assert.Null(savedMethod.ProviderMandateReference);
    }

    [Fact]
    public void SubscriptionProvider_PersistedValuesRemainStable()
    {
        Assert.Equal(0, (int)SubscriptionProvider.Unknown);
        Assert.Equal(1, (int)SubscriptionProvider.Paystack);
        Assert.Equal(2, (int)SubscriptionProvider.Netcash);
    }

    [Fact]
    public void NewBillingEnums_HaveStableExplicitPersistedValues()
    {
        Assert.Equal(1, (int)SubscriptionChargePurpose.InitialSubscription);
        Assert.Equal(2, (int)SubscriptionChargePurpose.RecurringSubscription);
        Assert.Equal(3, (int)SubscriptionChargePurpose.Retry);
        Assert.Equal(4, (int)SubscriptionChargePurpose.ManualCollection);
        Assert.Equal(5, (int)SubscriptionChargePurpose.Adjustment);

        Assert.Equal(0, (int)SubscriptionPaymentMethodType.Unknown);
        Assert.Equal(1, (int)SubscriptionPaymentMethodType.Card);
        Assert.Equal(2, (int)SubscriptionPaymentMethodType.DebitOrder);
        Assert.Equal(3, (int)SubscriptionPaymentMethodType.DebiCheck);
        Assert.Equal(4, (int)SubscriptionPaymentMethodType.BankTransfer);
        Assert.Equal(5, (int)SubscriptionPaymentMethodType.Other);

        Assert.Equal(0, (int)MandateStatus.None);
        Assert.Equal(1, (int)MandateStatus.Pending);
        Assert.Equal(2, (int)MandateStatus.Active);
        Assert.Equal(3, (int)MandateStatus.Failed);
        Assert.Equal(4, (int)MandateStatus.Revoked);
        Assert.Equal(5, (int)MandateStatus.Expired);
    }

    [Fact]
    public async Task BillingWebhookEvent_DuplicateProviderEventId_ThrowsOnSave()
    {
        using var db = new SqliteTestDatabase();
        await using var context = db.CreateContext();

        context.BillingWebhookEvents.Add(new BillingWebhookEvent
        {
            Id = Guid.NewGuid(),
            Provider = SubscriptionProvider.Paystack,
            ProviderEventId = "evt_123",
            EventType = "charge.success",
            RawPayload = "{}"
        });
        await context.SaveChangesAsync();

        context.BillingWebhookEvents.Add(new BillingWebhookEvent
        {
            Id = Guid.NewGuid(),
            Provider = SubscriptionProvider.Paystack,
            ProviderEventId = "evt_123",
            EventType = "charge.success",
            RawPayload = "{}"
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task OrganisationSubscription_SecondLiveSubscriptionForSameStokvel_ThrowsOnSave()
    {
        using var db = new SqliteTestDatabase();
        await using var context = db.CreateContext();
        var stokvel = TestData.CreateStokvel(context);
        await context.SaveChangesAsync();

        context.OrganisationSubscriptions.Add(new OrganisationSubscription
        {
            Id = Guid.NewGuid(),
            StokvelId = stokvel.Id,
            Status = SubscriptionStatus.Trialing
        });
        await context.SaveChangesAsync();

        context.OrganisationSubscriptions.Add(new OrganisationSubscription
        {
            Id = Guid.NewGuid(),
            StokvelId = stokvel.Id,
            Status = SubscriptionStatus.Active
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task OrganisationSubscription_CancelledSubscriptionDoesNotBlockANewLiveOne()
    {
        using var db = new SqliteTestDatabase();
        await using var context = db.CreateContext();
        var stokvel = TestData.CreateStokvel(context);
        await context.SaveChangesAsync();

        context.OrganisationSubscriptions.Add(new OrganisationSubscription
        {
            Id = Guid.NewGuid(),
            StokvelId = stokvel.Id,
            Status = SubscriptionStatus.Cancelled
        });
        await context.SaveChangesAsync();

        context.OrganisationSubscriptions.Add(new OrganisationSubscription
        {
            Id = Guid.NewGuid(),
            StokvelId = stokvel.Id,
            Status = SubscriptionStatus.Trialing
        });

        var exception = await Record.ExceptionAsync(() => context.SaveChangesAsync());
        Assert.Null(exception);

        var liveCount = await context.OrganisationSubscriptions
            .CountAsync(s => s.StokvelId == stokvel.Id && s.Status != SubscriptionStatus.Cancelled);
        Assert.Equal(1, liveCount);
    }
}
