using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Sisonke.Web.Services.Billing;
using Sisonke.Web.Tests.TestSupport;

namespace Sisonke.Web.Tests.Billing;

public sealed class AdminBillingServiceTestHarness : IDisposable
{
    public EntitlementTestHarness Entitlements { get; }
    public FakeBillingProvider BillingProvider { get; } = new();
    public FakeTimeProvider TimeProvider { get; } = new(DateTimeOffset.UtcNow);
    public ISubscriptionStateMachine StateMachine { get; }
    public AdminBillingService Sut { get; }

    public AdminBillingServiceTestHarness()
    {
        Entitlements = new EntitlementTestHarness();
        StateMachine = new SubscriptionStateMachine(Entitlements.Sut, TimeProvider, NullLogger<SubscriptionStateMachine>.Instance);

        Sut = new AdminBillingService(
            Entitlements.DbFactory,
            BillingProvider,
            Entitlements.Sut,
            StateMachine,
            TimeProvider,
            NullLogger<AdminBillingService>.Instance);
    }

    public void Dispose() => Entitlements.Dispose();
}
