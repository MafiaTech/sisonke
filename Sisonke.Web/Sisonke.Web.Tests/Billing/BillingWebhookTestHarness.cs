using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Sisonke.Web.Services;
using Sisonke.Web.Services.Billing;
using Sisonke.Web.Services.Notifications;
using Sisonke.Web.Tests.TestSupport;

namespace Sisonke.Web.Tests.Billing;

/// <summary>Bundles a real BillingWebhookProcessor over a SqliteTestDatabase-backed EntitlementTestHarness.</summary>
public sealed class BillingWebhookTestHarness : IDisposable
{
    public EntitlementTestHarness Entitlements { get; }
    public FakeTimeProvider TimeProvider { get; } = new(DateTimeOffset.UtcNow);
    public ISubscriptionStateMachine StateMachine { get; }
    public SubscriptionNotificationService NotificationService { get; }
    public BillingWebhookProcessor Processor { get; }

    public BillingWebhookTestHarness()
    {
        Entitlements = new EntitlementTestHarness();

        var invoiceNumberGenerator = new InvoiceNumberGenerator();
        var invoicePdfRenderer = new QuestPdfInvoiceRenderer(new InvoicingOptions());
        var documentStorage = new BillingDocumentStorage(new FakeWebHostEnvironment());
        var memberAccessService = new MemberAccessService(Entitlements.CreateContext());
        var notificationEnqueuer = new NotificationEnqueuer(
            new NotificationEmailTemplateRenderer(new AppSettings(), NullLogger<NotificationEmailTemplateRenderer>.Instance));

        StateMachine = new SubscriptionStateMachine(Entitlements.Sut, TimeProvider, NullLogger<SubscriptionStateMachine>.Instance);
        NotificationService = new SubscriptionNotificationService(memberAccessService, notificationEnqueuer, NullLogger<SubscriptionNotificationService>.Instance);

        Processor = new BillingWebhookProcessor(
            invoiceNumberGenerator,
            invoicePdfRenderer,
            documentStorage,
            Entitlements.Sut,
            StateMachine,
            NotificationService,
            TimeProvider,
            NullLogger<BillingWebhookProcessor>.Instance);
    }

    public void Dispose() => Entitlements.Dispose();
}
