using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Sisonke.Web.Services;
using Sisonke.Web.Services.Billing;
using Sisonke.Web.Services.Billing.Jobs;
using Sisonke.Web.Services.Jobs;
using Sisonke.Web.Services.Notifications;
using Sisonke.Web.Tests.TestSupport;

namespace Sisonke.Web.Tests.Billing;

public sealed class DunningJobTestHarness : IDisposable
{
    public EntitlementTestHarness Entitlements { get; }
    public FakeBillingProvider BillingProvider { get; } = new();
    public FakeTimeProvider TimeProvider { get; } = new(DateTimeOffset.UtcNow);
    public IDistributedJobLock JobLock { get; }
    public ISubscriptionStateMachine StateMachine { get; }
    public SubscriptionNotificationService NotificationService { get; }
    public DunningJob Sut { get; }

    public DunningJobTestHarness() : this(new EntitlementTestHarness()) { }

    /// <summary>For tests that race two real OS threads against the database — see SharedCacheSqliteTestDatabase.</summary>
    public DunningJobTestHarness(SharedCacheSqliteTestDatabase db) : this(new EntitlementTestHarness(db)) { }

    private DunningJobTestHarness(EntitlementTestHarness entitlements)
    {
        Entitlements = entitlements;

        var memberAccessService = new MemberAccessService(Entitlements.CreateContext());
        var notificationEnqueuer = new NotificationEnqueuer(
            new NotificationEmailTemplateRenderer(new AppSettings(), NullLogger<NotificationEmailTemplateRenderer>.Instance));

        JobLock = new DistributedJobLock(Entitlements.DbFactory);
        StateMachine = new SubscriptionStateMachine(Entitlements.Sut, TimeProvider, NullLogger<SubscriptionStateMachine>.Instance);
        NotificationService = new SubscriptionNotificationService(memberAccessService, notificationEnqueuer, NullLogger<SubscriptionNotificationService>.Instance);

        Sut = new DunningJob(
            Entitlements.DbFactory,
            BillingProvider,
            JobLock,
            StateMachine,
            NotificationService,
            Entitlements.Sut,
            TimeProvider,
            NullLogger<DunningJob>.Instance);
    }

    public void Dispose() => Entitlements.Dispose();
}
