using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Sisonke.Web.Data.Enums;
using Sisonke.Web.Services.Billing;
using Sisonke.Web.Tests.TestSupport;

namespace Sisonke.Web.Tests.Billing;

/// <summary>
/// Full transition-table coverage, independently hand-written (not copied from
/// SubscriptionStateMachine's own source) so a typo in the production table is caught here too.
/// </summary>
public class SubscriptionStateMachineTests
{
    private static readonly SubscriptionStatus[] AllStatuses =
    [
        SubscriptionStatus.LegacyUnsubscribed, SubscriptionStatus.PendingPaymentMethod, SubscriptionStatus.Trialing,
        SubscriptionStatus.Active, SubscriptionStatus.PastDue, SubscriptionStatus.Restricted,
        SubscriptionStatus.Suspended, SubscriptionStatus.Cancelled, SubscriptionStatus.Expired
    ];

    // Brief's transition table, transcribed independently.
    private static readonly Dictionary<SubscriptionStatus, SubscriptionStatus[]> ExpectedAllowed = new()
    {
        [SubscriptionStatus.LegacyUnsubscribed] = [SubscriptionStatus.PendingPaymentMethod],
        [SubscriptionStatus.PendingPaymentMethod] = [SubscriptionStatus.Trialing, SubscriptionStatus.Cancelled],
        [SubscriptionStatus.Trialing] =
            [SubscriptionStatus.Active, SubscriptionStatus.PastDue, SubscriptionStatus.Cancelled, SubscriptionStatus.Expired],
        [SubscriptionStatus.Active] = [SubscriptionStatus.PastDue, SubscriptionStatus.Cancelled],
        [SubscriptionStatus.PastDue] = [SubscriptionStatus.Active, SubscriptionStatus.Restricted, SubscriptionStatus.Cancelled],
        [SubscriptionStatus.Restricted] = [SubscriptionStatus.Active, SubscriptionStatus.Suspended, SubscriptionStatus.Cancelled],
        [SubscriptionStatus.Suspended] = [SubscriptionStatus.Active, SubscriptionStatus.Cancelled, SubscriptionStatus.Expired],
        [SubscriptionStatus.Cancelled] = [SubscriptionStatus.Trialing, SubscriptionStatus.Active],
        [SubscriptionStatus.Expired] = []
    };

    public static IEnumerable<object[]> AllStatusPairs()
    {
        foreach (var from in AllStatuses)
        {
            foreach (var to in AllStatuses)
            {
                if (from == to)
                {
                    continue; // self-transitions are never in the table — covered by IllegalTransition test below via a couple of explicit cases
                }

                yield return [from, to];
            }
        }
    }

    [Theory]
    [MemberData(nameof(AllStatusPairs))]
    public void CanTransition_MatchesTheBriefsTable(SubscriptionStatus from, SubscriptionStatus to)
    {
        var expected = ExpectedAllowed[from].Contains(to);

        var stateMachine = new SubscriptionStateMachine(
            Substitute(), new FakeTimeProvider(DateTimeOffset.UtcNow), NullLogger<SubscriptionStateMachine>.Instance);

        Assert.Equal(expected, stateMachine.CanTransition(from, to));
    }

    [Theory]
    [MemberData(nameof(AllStatusPairs))]
    public async Task TransitionAsync_LegalTransitions_Succeed_IllegalTransitions_Throw(SubscriptionStatus from, SubscriptionStatus to)
    {
        using var harness = new EntitlementTestHarness();
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var stateMachine = new SubscriptionStateMachine(harness.Sut, timeProvider, NullLogger<SubscriptionStateMachine>.Instance);

        await using var context = harness.CreateContext();
        var stokvel = TestData.CreateStokvel(context);
        var subscription = TestData.CreateOrganisationSubscription(context, stokvel, null, from);
        await context.SaveChangesAsync();

        var isLegal = ExpectedAllowed[from].Contains(to);

        if (isLegal)
        {
            await stateMachine.TransitionAsync(context, subscription, to, SubscriptionEventType.NotificationSent, null, "test", CancellationToken.None);
            Assert.Equal(to, subscription.Status);

            var loggedEvent = await context.SubscriptionEvents
                .Where(e => e.OrganisationSubscriptionId == subscription.Id)
                .OrderByDescending(e => e.OccurredAt)
                .FirstAsync();
            Assert.Equal(from, loggedEvent.FromStatus);
            Assert.Equal(to, loggedEvent.ToStatus);
        }
        else
        {
            await Assert.ThrowsAsync<InvalidSubscriptionTransitionException>(() =>
                stateMachine.TransitionAsync(context, subscription, to, SubscriptionEventType.NotificationSent, null, "test", CancellationToken.None));
            Assert.Equal(from, subscription.Status); // unchanged
        }
    }

    [Fact]
    public async Task SelfTransition_IsNotInTheTable_AndThrows()
    {
        using var harness = new EntitlementTestHarness();
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var stateMachine = new SubscriptionStateMachine(harness.Sut, timeProvider, NullLogger<SubscriptionStateMachine>.Instance);

        await using var context = harness.CreateContext();
        var stokvel = TestData.CreateStokvel(context);
        var subscription = TestData.CreateOrganisationSubscription(context, stokvel, null, SubscriptionStatus.Active);
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidSubscriptionTransitionException>(() =>
            stateMachine.TransitionAsync(context, subscription, SubscriptionStatus.Active, SubscriptionEventType.NotificationSent, null, "test", CancellationToken.None));
    }

    private static Sisonke.Web.Services.Entitlements.IEntitlementService Substitute()
    {
        // CanTransition doesn't touch the entitlement service — a throwing stub is enough to
        // prove that.
        return new ThrowingEntitlementService();
    }

    private sealed class ThrowingEntitlementService : Sisonke.Web.Services.Entitlements.IEntitlementService
    {
        public Task<bool> HasFeatureAsync(Guid stokvelId, string featureCode, CancellationToken ct = default) => throw new InvalidOperationException();
        public Task<int?> GetLimitAsync(Guid stokvelId, string featureCode, CancellationToken ct = default) => throw new InvalidOperationException();
        public Task<Sisonke.Web.Services.Entitlements.EntitlementDecision> AuthorizeAsync(Guid stokvelId, string featureCode, int requestedUsage = 1, CancellationToken ct = default) => throw new InvalidOperationException();
        public Task<Sisonke.Web.Services.Entitlements.EntitlementSnapshot> GetSnapshotAsync(Guid stokvelId, CancellationToken ct = default) => throw new InvalidOperationException();
        public Task InvalidateAsync(Guid stokvelId, CancellationToken ct = default) => throw new InvalidOperationException();
    }
}
