using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Sisonke.Web.Services;
using Sisonke.Web.Services.Billing;
using Sisonke.Web.Services.Billing.Paystack;
using Sisonke.Web.Services.Jobs;
using Sisonke.Web.Services.Notifications;
using Sisonke.Web.Tests.TestSupport;

namespace Sisonke.Web.Tests.Billing;

public sealed class SubscriptionServiceTestHarness : IDisposable
{
    public EntitlementTestHarness Entitlements { get; }
    public FakeBillingProvider BillingProvider { get; } = new();
    public PaystackOptions PaystackOptions { get; } = new();
    public FakeTimeProvider TimeProvider { get; }
    public IDistributedJobLock JobLock { get; }
    public ISubscriptionStateMachine StateMachine { get; }
    public SubscriptionNotificationService NotificationService { get; }
    public SubscriptionService Sut { get; }

    public SubscriptionServiceTestHarness()
    {
        TimeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        Entitlements = new EntitlementTestHarness(timeProvider: TimeProvider);

        var memberAccessService = new MemberAccessService(Entitlements.CreateContext());
        var notificationEnqueuer = new NotificationEnqueuer(
            new NotificationEmailTemplateRenderer(new AppSettings(), NullLogger<NotificationEmailTemplateRenderer>.Instance));

        JobLock = new DistributedJobLock(Entitlements.DbFactory);
        StateMachine = new SubscriptionStateMachine(Entitlements.Sut, TimeProvider, NullLogger<SubscriptionStateMachine>.Instance);
        NotificationService = new SubscriptionNotificationService(memberAccessService, notificationEnqueuer, NullLogger<SubscriptionNotificationService>.Instance);

        Sut = new SubscriptionService(
            Entitlements.DbFactory,
            BillingProvider,
            Entitlements.Sut,
            Entitlements.UsageProvider,
            StateMachine,
            memberAccessService,
            NotificationService,
            PaystackOptions,
            TimeProvider,
            NullLogger<SubscriptionService>.Instance);
    }

    public void Dispose() => Entitlements.Dispose();
}
