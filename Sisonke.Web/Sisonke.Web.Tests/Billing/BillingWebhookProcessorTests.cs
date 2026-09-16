using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;
using Sisonke.Web.Services.Billing.Paystack;
using Sisonke.Web.Tests.TestSupport;

namespace Sisonke.Web.Tests.Billing;

public class BillingWebhookProcessorTests
{
    private const string ChargeSuccessBody = """
        {"event":"charge.success","data":{"id":555001,"reference":"chg_ref_1","amount":24900,"currency":"ZAR",
        "customer":{"customer_code":"CUS_test1"}}}
        """;

    [Fact]
    public async Task ChargeSuccess_DeliveredThreeTimes_ProducesOnePaymentOneReceiptOneNotification()
    {
        using var harness = new BillingWebhookTestHarness();
        var (stokvelId, recipientId) = await SeedTrialingSubscriptionAsync(harness);

        for (var attempt = 0; attempt < 3; attempt++)
        {
            await using var context = harness.Entitlements.CreateContext();
            var webhookEvent = BuildEvent(PaystackWebhookEventTypes.ChargeSuccess, ChargeSuccessBody);
            var payload = PaystackWebhookPayload.TryParse(ChargeSuccessBody)!;

            await harness.Processor.ProcessAsync(context, webhookEvent, payload, CancellationToken.None);
            await context.SaveChangesAsync();
        }

        await using var verifyContext = harness.Entitlements.CreateContext();
        var payments = await verifyContext.SubscriptionPayments.Where(p => p.ProviderReference == "chg_ref_1").ToListAsync();
        Assert.Single(payments);

        // TestData.CreateStokvelMember defaults to EmailEnabled=true and WebPushEnabled=true, so
        // NotificationEnqueuer fans one logical notification out to two channel rows — the
        // idempotency guarantee is that replay doesn't multiply *that*, not that only one row
        // ever exists.
        var notifications = await verifyContext.NotificationMessages
            .Where(n => n.RecipientMemberId == recipientId && n.Type == NotificationType.InvoiceReceiptIssued)
            .ToListAsync();
        Assert.Equal(2, notifications.Count);
        Assert.Equal(2, notifications.Select(n => n.Channel).Distinct().Count());

        var subscription = await verifyContext.OrganisationSubscriptions.SingleAsync(s => s.StokvelId == stokvelId);
        Assert.Equal(SubscriptionStatus.Active, subscription.Status);
    }

    [Fact]
    public async Task SubscriptionDisable_ArrivingBeforeChargeSuccess_DoesNotCorruptStatus()
    {
        using var harness = new BillingWebhookTestHarness();
        var (stokvelId, _) = await SeedTrialingSubscriptionAsync(harness);

        const string disableBody = """{"event":"subscription.disable","data":{"id":555002,"subscription_code":"SUB_test1"}}""";

        // Process disable FIRST (out of order relative to when the charge actually happened).
        await using (var context = harness.Entitlements.CreateContext())
        {
            var webhookEvent = BuildEvent(PaystackWebhookEventTypes.SubscriptionDisable, disableBody);
            var payload = PaystackWebhookPayload.TryParse(disableBody)!;
            await harness.Processor.ProcessAsync(context, webhookEvent, payload, CancellationToken.None);
            await context.SaveChangesAsync();
        }

        // Then the (chronologically earlier) charge.success arrives.
        var exception = await Record.ExceptionAsync(async () =>
        {
            await using var context = harness.Entitlements.CreateContext();
            var webhookEvent = BuildEvent(PaystackWebhookEventTypes.ChargeSuccess, ChargeSuccessBody);
            var payload = PaystackWebhookPayload.TryParse(ChargeSuccessBody)!;
            await harness.Processor.ProcessAsync(context, webhookEvent, payload, CancellationToken.None);
            await context.SaveChangesAsync();
        });

        Assert.Null(exception);

        await using var verifyContext = harness.Entitlements.CreateContext();
        var subscription = await verifyContext.OrganisationSubscriptions.SingleAsync(s => s.StokvelId == stokvelId);

        // The payment is still recorded (charge.success's own effect is applied regardless of
        // arrival order) but a disabled subscription does not get silently un-cancelled by a
        // late-arriving charge — status stays a valid, coherent value, not corrupted.
        Assert.Equal(SubscriptionStatus.Cancelled, subscription.Status);
        var payments = await verifyContext.SubscriptionPayments.Where(p => p.ProviderReference == "chg_ref_1").ToListAsync();
        Assert.Single(payments);
    }

    [Fact]
    public async Task UnknownEventType_IsMarkedIgnored_NotAnError()
    {
        using var harness = new BillingWebhookTestHarness();
        await using var context = harness.Entitlements.CreateContext();

        const string body = """{"event":"customeridentification.success","data":{"id":1}}""";
        var webhookEvent = BuildEvent("customeridentification.success", body);
        var payload = PaystackWebhookPayload.TryParse(body)!;

        var exception = await Record.ExceptionAsync(() => harness.Processor.ProcessAsync(context, webhookEvent, payload, CancellationToken.None));

        Assert.Null(exception);
        Assert.Equal(WebhookProcessingStatus.Ignored, webhookEvent.ProcessingStatus);
    }

    [Fact]
    public async Task PaymentSetupChargeSuccess_DoesNotCreatePaymentOrEndTrial()
    {
        using var harness = new BillingWebhookTestHarness();
        var (stokvelId, _) = await SeedTrialingSubscriptionAsync(harness);
        const string body = """
            {"event":"charge.success","data":{"id":555003,"reference":"sisonke-setup-abc123","amount":100,
            "currency":"ZAR","customer":{"customer_code":"CUS_test1"}}}
            """;

        await using (var context = harness.Entitlements.CreateContext())
        {
            await harness.Processor.ProcessAsync(context, BuildEvent(PaystackWebhookEventTypes.ChargeSuccess, body),
                PaystackWebhookPayload.TryParse(body)!, CancellationToken.None);
            await context.SaveChangesAsync();
        }

        await using var verify = harness.Entitlements.CreateContext();
        Assert.Empty(await verify.SubscriptionPayments.ToListAsync());
        Assert.Equal(SubscriptionStatus.Trialing,
            (await verify.OrganisationSubscriptions.SingleAsync(value => value.StokvelId == stokvelId)).Status);
    }

    [Theory]
    [InlineData(100, "ZAR")]
    [InlineData(24900, "USD")]
    public async Task ChargeSuccess_WithWrongAmountOrCurrency_IsRejected(decimal amount, string currency)
    {
        using var harness = new BillingWebhookTestHarness();
        var (stokvelId, _) = await SeedTrialingSubscriptionAsync(harness);
        var body = System.Text.Json.JsonSerializer.Serialize(new
        {
            @event = "charge.success",
            data = new
            {
                id = 555004,
                reference = "charge-mismatch",
                amount,
                currency,
                customer = new { customer_code = "CUS_test1" }
            }
        });

        await using (var context = harness.Entitlements.CreateContext())
        {
            await harness.Processor.ProcessAsync(context, BuildEvent(PaystackWebhookEventTypes.ChargeSuccess, body),
                PaystackWebhookPayload.TryParse(body)!, CancellationToken.None);
            await context.SaveChangesAsync();
        }

        await using var verify = harness.Entitlements.CreateContext();
        Assert.Empty(await verify.SubscriptionPayments.ToListAsync());
        Assert.Equal(SubscriptionStatus.Trialing,
            (await verify.OrganisationSubscriptions.SingleAsync(value => value.StokvelId == stokvelId)).Status);
    }

    [Fact]
    public async Task DifferentSuccessfulReferences_InSameBillingPeriod_CreateOneLogicalPayment()
    {
        using var harness = new BillingWebhookTestHarness();
        var (stokvelId, _) = await SeedTrialingSubscriptionAsync(harness);

        await ProcessChargeAsync(harness, "billing-period-charge-1", 555010);
        await ProcessChargeAsync(harness, "billing-period-charge-2", 555011);

        await using var verify = harness.Entitlements.CreateContext();
        var subscription = await verify.OrganisationSubscriptions.SingleAsync(value => value.StokvelId == stokvelId);
        Assert.Equal(SubscriptionStatus.Active, subscription.Status);
        Assert.NotNull(subscription.CurrentPeriodStartedAt);
        Assert.NotNull(subscription.CurrentPeriodEndsAt);
        Assert.Single(await verify.SubscriptionPayments.Where(value => value.OrganisationSubscriptionId == subscription.Id).ToListAsync());
    }

    private static async Task ProcessChargeAsync(BillingWebhookTestHarness harness, string reference, long id)
    {
        var body = System.Text.Json.JsonSerializer.Serialize(new
        {
            @event = "charge.success",
            data = new
            {
                id,
                reference,
                amount = 24900,
                currency = "ZAR",
                customer = new { customer_code = "CUS_test1" }
            }
        });
        await using var context = harness.Entitlements.CreateContext();
        await harness.Processor.ProcessAsync(context, BuildEvent(PaystackWebhookEventTypes.ChargeSuccess, body),
            PaystackWebhookPayload.TryParse(body)!, CancellationToken.None);
        await context.SaveChangesAsync();
    }

    private static BillingWebhookEvent BuildEvent(string eventType, string rawBody) => new()
    {
        Id = Guid.NewGuid(),
        Provider = SubscriptionProvider.Paystack,
        ProviderEventId = $"{eventType}:{Guid.NewGuid():N}",
        EventType = eventType,
        RawPayload = rawBody,
        SignatureValid = true,
        ProcessingStatus = WebhookProcessingStatus.Received,
        ReceivedAt = DateTime.UtcNow
    };

    private static async Task<(Guid StokvelId, Guid RecipientMemberId)> SeedTrialingSubscriptionAsync(BillingWebhookTestHarness harness)
    {
        await harness.Entitlements.SeedCatalogueAsync();

        await using var context = harness.Entitlements.CreateContext();
        var growingPlan = await context.SubscriptionPlans.SingleAsync(p => p.Code == PlanCodes.Growing);
        var stokvel = TestData.CreateStokvel(context);
        var member = TestData.CreateStokvelMember(context, stokvel, role: SisonkeRole.StokvelAdmin);

        var applicationUserId = $"user-{Guid.NewGuid():N}";
        member.ApplicationUserId = applicationUserId;

        var subscription = TestData.CreateOrganisationSubscription(context, stokvel, growingPlan.Id, SubscriptionStatus.Trialing);
        subscription.ProviderCustomerCode = "CUS_test1";
        subscription.ProviderSubscriptionCode = "SUB_test1";
        subscription.ProviderEmailToken = "tok_test1";
        subscription.TermsAcceptedByUserId = applicationUserId;

        await context.SaveChangesAsync();

        return (stokvel.Id, member.Id);
    }
}
