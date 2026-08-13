using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;
using Sisonke.Web.Tests.TestSupport;

namespace Sisonke.Web.Tests.Billing;

public class AdminBillingServiceTests
{
    [Fact]
    public async Task ExtendTrialAsync_WithoutReason_IsRejected()
    {
        using var harness = new AdminBillingServiceTestHarness();
        await harness.Entitlements.SeedCatalogueAsync();
        var stokvelId = await SeedSubscriptionAsync(harness, SubscriptionStatus.Trialing, s => s.TrialEndsAt = DateTime.UtcNow.AddDays(10));

        var result = await harness.Sut.ExtendTrialAsync(stokvelId, 7, "", "admin-1");

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ExtendTrialAsync_WhenTrialing_ExtendsTrialEndAndWritesEvent()
    {
        using var harness = new AdminBillingServiceTestHarness();
        await harness.Entitlements.SeedCatalogueAsync();
        var trialEnd = DateTime.UtcNow.AddDays(10);
        var stokvelId = await SeedSubscriptionAsync(harness, SubscriptionStatus.Trialing, s => s.TrialEndsAt = trialEnd);

        var result = await harness.Sut.ExtendTrialAsync(stokvelId, 14, "Goodwill extension", "admin-1");

        Assert.True(result.Success);

        await using var context = harness.Entitlements.CreateContext();
        var subscription = await context.OrganisationSubscriptions.SingleAsync(s => s.StokvelId == stokvelId);
        Assert.Equal(trialEnd.AddDays(14).Date, subscription.TrialEndsAt!.Value.Date);

        var evt = await context.SubscriptionEvents.SingleAsync(e => e.OrganisationSubscriptionId == subscription.Id);
        Assert.Equal(SubscriptionEventType.TrialExtended, evt.EventType);
        Assert.Equal("admin-1", evt.ActorUserId);
        Assert.Contains("Goodwill extension", evt.Notes);
    }

    [Fact]
    public async Task ExtendTrialAsync_WhenNotTrialing_IsRejected()
    {
        using var harness = new AdminBillingServiceTestHarness();
        await harness.Entitlements.SeedCatalogueAsync();
        var stokvelId = await SeedSubscriptionAsync(harness, SubscriptionStatus.Active);

        var result = await harness.Sut.ExtendTrialAsync(stokvelId, 7, "reason", "admin-1");

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ApplyPromotionalTrialAsync_AppliesLaunch60_ExtendingFromTrialStart()
    {
        using var harness = new AdminBillingServiceTestHarness();
        await harness.Entitlements.SeedCatalogueAsync();
        var trialStart = DateTime.UtcNow.AddDays(-5);
        var stokvelId = await SeedSubscriptionAsync(harness, SubscriptionStatus.Trialing, s =>
        {
            s.TrialStartedAt = trialStart;
            s.TrialEndsAt = trialStart.AddDays(60);
        });

        var result = await harness.Sut.ApplyPromotionalTrialAsync(stokvelId, "LAUNCH60", "Promo applied per marketing campaign", "admin-1");

        Assert.True(result.Success);

        await using var context = harness.Entitlements.CreateContext();
        var subscription = await context.OrganisationSubscriptions.SingleAsync(s => s.StokvelId == stokvelId);
        Assert.Equal(trialStart.AddDays(60).Date, subscription.TrialEndsAt!.Value.Date);

        var evt = await context.SubscriptionEvents.SingleAsync(e => e.OrganisationSubscriptionId == subscription.Id);
        Assert.Equal(SubscriptionEventType.PromotionalTrialApplied, evt.EventType);
    }

    [Fact]
    public async Task ApplyPromotionalTrialAsync_UnknownCode_IsRejected()
    {
        using var harness = new AdminBillingServiceTestHarness();
        await harness.Entitlements.SeedCatalogueAsync();
        var stokvelId = await SeedSubscriptionAsync(harness, SubscriptionStatus.Trialing, s => s.TrialEndsAt = DateTime.UtcNow.AddDays(10));

        var result = await harness.Sut.ApplyPromotionalTrialAsync(stokvelId, "DOES_NOT_EXIST", "reason", "admin-1");

        Assert.False(result.Success);
    }

    [Fact]
    public async Task WaiveInvoiceAsync_MarksInvoiceWaived_AndRejectsAlreadyPaid()
    {
        using var harness = new AdminBillingServiceTestHarness();
        await harness.Entitlements.SeedCatalogueAsync();
        var stokvelId = await SeedSubscriptionAsync(harness, SubscriptionStatus.PastDue);

        Guid invoiceId;
        Guid paidInvoiceId;
        await using (var context = harness.Entitlements.CreateContext())
        {
            var subscription = await context.OrganisationSubscriptions.SingleAsync(s => s.StokvelId == stokvelId);

            var invoice = new SubscriptionInvoice
            {
                Id = Guid.NewGuid(), OrganisationSubscriptionId = subscription.Id,
                InvoiceNumber = "SIS-INV-2026-000001", Status = SubscriptionInvoiceStatus.Issued,
                Total = 249m, SubTotal = 249m
            };
            context.SubscriptionInvoices.Add(invoice);
            invoiceId = invoice.Id;

            var paidInvoice = new SubscriptionInvoice
            {
                Id = Guid.NewGuid(), OrganisationSubscriptionId = subscription.Id,
                InvoiceNumber = "SIS-INV-2026-000002", Status = SubscriptionInvoiceStatus.Paid,
                Total = 249m, SubTotal = 249m
            };
            context.SubscriptionInvoices.Add(paidInvoice);
            paidInvoiceId = paidInvoice.Id;

            await context.SaveChangesAsync();
        }

        var waiveResult = await harness.Sut.WaiveInvoiceAsync(invoiceId, "Goodwill waiver", "admin-1");
        Assert.True(waiveResult.Success);

        var paidResult = await harness.Sut.WaiveInvoiceAsync(paidInvoiceId, "Should not work", "admin-1");
        Assert.False(paidResult.Success);

        await using var verifyContext = harness.Entitlements.CreateContext();
        var invoiceAfter = await verifyContext.SubscriptionInvoices.SingleAsync(i => i.Id == invoiceId);
        Assert.Equal(SubscriptionInvoiceStatus.Waived, invoiceAfter.Status);
    }

    [Fact]
    public async Task RetryChargeAsync_Success_ReactivatesSubscriptionFromPastDue()
    {
        using var harness = new AdminBillingServiceTestHarness();
        await harness.Entitlements.SeedCatalogueAsync();
        var stokvelId = await SeedSubscriptionAsync(harness, SubscriptionStatus.PastDue, s => s.BillingEmail = "billing@example.com");
        await SeedPaymentMethodAsync(harness, stokvelId);

        var result = await harness.Sut.RetryChargeAsync(stokvelId, "Customer confirmed funds available", "admin-1");

        Assert.True(result.Success);

        await using var context = harness.Entitlements.CreateContext();
        var subscription = await context.OrganisationSubscriptions.SingleAsync(s => s.StokvelId == stokvelId);
        Assert.Equal(SubscriptionStatus.Active, subscription.Status);
        Assert.Null(subscription.DunningStartedAt);

        var payment = await context.SubscriptionPayments.SingleAsync(p => p.OrganisationSubscriptionId == subscription.Id);
        Assert.Equal(SubscriptionPaymentStatus.Succeeded, payment.Status);
    }

    [Fact]
    public async Task RetryChargeAsync_ProviderDeclines_LeavesSubscriptionInPastDue()
    {
        using var harness = new AdminBillingServiceTestHarness();
        await harness.Entitlements.SeedCatalogueAsync();
        var stokvelId = await SeedSubscriptionAsync(harness, SubscriptionStatus.PastDue, s => s.BillingEmail = "billing@example.com");
        await SeedPaymentMethodAsync(harness, stokvelId);
        harness.BillingProvider.ChargeResultToReturn = new(false, "charge-ref", "declined", "Insufficient funds");

        var result = await harness.Sut.RetryChargeAsync(stokvelId, "Trying again", "admin-1");

        Assert.False(result.Success);

        await using var context = harness.Entitlements.CreateContext();
        var subscription = await context.OrganisationSubscriptions.SingleAsync(s => s.StokvelId == stokvelId);
        Assert.Equal(SubscriptionStatus.PastDue, subscription.Status); // unchanged
    }

    [Fact]
    public async Task MoveToEnterpriseAsync_SwitchesPlanToEnterprise()
    {
        using var harness = new AdminBillingServiceTestHarness();
        await harness.Entitlements.SeedCatalogueAsync();
        var stokvelId = await SeedSubscriptionAsync(harness, SubscriptionStatus.Active);

        var result = await harness.Sut.MoveToEnterpriseAsync(stokvelId, "Negotiated custom deal", "admin-1");

        Assert.True(result.Success);

        await using var context = harness.Entitlements.CreateContext();
        var subscription = await context.OrganisationSubscriptions
            .Include(s => s.SubscriptionPlan)
            .SingleAsync(s => s.StokvelId == stokvelId);
        Assert.Equal(Sisonke.Web.Data.PlanCodes.Enterprise, subscription.SubscriptionPlan!.Code);
    }

    [Fact]
    public async Task ReinstateAsync_OnlyWorksFromSuspended_AndMovesToActive()
    {
        using var harness = new AdminBillingServiceTestHarness();
        await harness.Entitlements.SeedCatalogueAsync();
        var suspendedStokvelId = await SeedSubscriptionAsync(harness, SubscriptionStatus.Suspended);
        var activeStokvelId = await SeedSubscriptionAsync(harness, SubscriptionStatus.Active);

        var reinstateResult = await harness.Sut.ReinstateAsync(suspendedStokvelId, "Payment dispute resolved", "admin-1");
        Assert.True(reinstateResult.Success);

        var invalidResult = await harness.Sut.ReinstateAsync(activeStokvelId, "Should not work", "admin-1");
        Assert.False(invalidResult.Success);

        await using var context = harness.Entitlements.CreateContext();
        var subscription = await context.OrganisationSubscriptions.SingleAsync(s => s.StokvelId == suspendedStokvelId);
        Assert.Equal(SubscriptionStatus.Active, subscription.Status);
    }

    private static async Task<Guid> SeedSubscriptionAsync(
        AdminBillingServiceTestHarness harness, SubscriptionStatus status, Action<OrganisationSubscription>? configure = null)
    {
        await using var context = harness.Entitlements.CreateContext();
        var stokvel = TestData.CreateStokvel(context);
        var growingPlan = await context.SubscriptionPlans.SingleAsync(p => p.Code == Sisonke.Web.Data.PlanCodes.Growing);

        var subscription = TestData.CreateOrganisationSubscription(context, stokvel, growingPlan.Id, status);
        configure?.Invoke(subscription);

        await context.SaveChangesAsync();
        return stokvel.Id;
    }

    private static async Task SeedPaymentMethodAsync(AdminBillingServiceTestHarness harness, Guid stokvelId)
    {
        await using var context = harness.Entitlements.CreateContext();
        var subscription = await context.OrganisationSubscriptions.SingleAsync(s => s.StokvelId == stokvelId);

        context.SubscriptionPaymentMethods.Add(new SubscriptionPaymentMethod
        {
            Id = Guid.NewGuid(),
            OrganisationSubscriptionId = subscription.Id,
            Provider = SubscriptionProvider.Paystack,
            ProviderAuthorizationCode = "AUTH_test",
            CardBrand = "visa",
            Last4 = "4242",
            IsDefault = true,
            IsReusable = true
        });

        await context.SaveChangesAsync();
    }
}
