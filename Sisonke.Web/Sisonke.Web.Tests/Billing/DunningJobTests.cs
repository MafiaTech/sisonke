using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;
using Sisonke.Web.Services.Billing;
using Sisonke.Web.Tests.TestSupport;

namespace Sisonke.Web.Tests.Billing;

/// <summary>
/// Time-controlled dunning walk: day 0 is pre-seeded (already handled at the point of failure —
/// see BillingWebhookProcessor/TrialExpiryJob), DunningJob owns days 1 onward. TimeProvider is
/// advanced explicitly between RunAsync calls rather than relying on wall-clock time.
/// </summary>
public class DunningJobTests
{
    [Fact]
    public async Task Day1_RetryFails_StaysPastDue_RecordsAttemptTwo()
    {
        using var harness = new DunningJobTestHarness();
        var (stokvelId, subscriptionId) = await SeedPastDueSubscriptionAsync(harness);
        harness.BillingProvider.ChargeResultToReturn = new ChargeResult(false, "ref", "insufficient_funds", "Insufficient funds.");

        harness.TimeProvider.Advance(TimeSpan.FromDays(1));
        await harness.Sut.RunAsync();

        await using var context = harness.Entitlements.CreateContext();
        var subscription = await context.OrganisationSubscriptions.SingleAsync(s => s.Id == subscriptionId);
        Assert.Equal(SubscriptionStatus.PastDue, subscription.Status);

        var payments = await context.SubscriptionPayments.Where(p => p.OrganisationSubscriptionId == subscriptionId).ToListAsync();
        Assert.Equal(2, payments.Count); // day-0 seed + day-1 retry
        Assert.Contains(payments, p => p.AttemptNumber == 2 && p.Status == SubscriptionPaymentStatus.Failed);
    }

    [Fact]
    public async Task Day3_RetrySucceeds_RestoresActive_ClearsDunning_SendsRecoveryNotification()
    {
        using var harness = new DunningJobTestHarness();
        var (stokvelId, subscriptionId) = await SeedPastDueSubscriptionAsync(harness);
        harness.BillingProvider.ChargeResultToReturn = new ChargeResult(true, "ref", null, null);

        harness.TimeProvider.Advance(TimeSpan.FromDays(3));
        await harness.Sut.RunAsync();

        await using var context = harness.Entitlements.CreateContext();
        var subscription = await context.OrganisationSubscriptions.SingleAsync(s => s.Id == subscriptionId);
        Assert.Equal(SubscriptionStatus.Active, subscription.Status);
        Assert.Null(subscription.DunningStartedAt);
        Assert.NotNull(subscription.LastSuccessfulPaymentAt);

        var recoveryNotifications = await context.NotificationMessages
            .Where(n => n.Type == NotificationType.PaymentRecovered)
            .ToListAsync();
        Assert.NotEmpty(recoveryNotifications);
    }

    [Fact]
    public async Task Day13_RecoveryFromRestricted_RestoresActive()
    {
        using var harness = new DunningJobTestHarness();
        var (stokvelId, subscriptionId) = await SeedPastDueSubscriptionAsync(harness);

        // Walk to day 7 (Restricted) with failing retries, then recover on day 13.
        harness.BillingProvider.ChargeResultToReturn = new ChargeResult(false, "ref", "insufficient_funds", "Insufficient funds.");
        harness.TimeProvider.Advance(TimeSpan.FromDays(1));
        await harness.Sut.RunAsync();
        harness.TimeProvider.Advance(TimeSpan.FromDays(2)); // day 3
        await harness.Sut.RunAsync();
        harness.TimeProvider.Advance(TimeSpan.FromDays(2)); // day 5
        await harness.Sut.RunAsync();
        harness.TimeProvider.Advance(TimeSpan.FromDays(2)); // day 7
        await harness.Sut.RunAsync();

        await using (var context = harness.Entitlements.CreateContext())
        {
            var subscription = await context.OrganisationSubscriptions.SingleAsync(s => s.Id == subscriptionId);
            Assert.Equal(SubscriptionStatus.Restricted, subscription.Status);
        }

        harness.BillingProvider.ChargeResultToReturn = new ChargeResult(true, "ref", null, null);
        harness.TimeProvider.Advance(TimeSpan.FromDays(6)); // day 13
        await harness.Sut.RunAsync();

        await using var verifyContext = harness.Entitlements.CreateContext();
        var recovered = await verifyContext.OrganisationSubscriptions.SingleAsync(s => s.Id == subscriptionId);
        Assert.Equal(SubscriptionStatus.Active, recovered.Status);
        Assert.Null(recovered.DunningStartedAt);
    }

    [Fact]
    public async Task Day7_MovesToRestricted_AndSendsNotification()
    {
        using var harness = new DunningJobTestHarness();
        var (_, subscriptionId) = await SeedPastDueSubscriptionAsync(harness);
        harness.BillingProvider.ChargeResultToReturn = new ChargeResult(false, "ref", "insufficient_funds", "Insufficient funds.");

        harness.TimeProvider.Advance(TimeSpan.FromDays(7));
        await harness.Sut.RunAsync();

        await using var context = harness.Entitlements.CreateContext();
        var subscription = await context.OrganisationSubscriptions.SingleAsync(s => s.Id == subscriptionId);
        Assert.Equal(SubscriptionStatus.Restricted, subscription.Status);

        var notifications = await context.NotificationMessages.Where(n => n.Type == NotificationType.SubscriptionRestricted).ToListAsync();
        Assert.NotEmpty(notifications);
    }

    [Fact]
    public async Task Day14_MovesToSuspended_AndSendsNotification()
    {
        using var harness = new DunningJobTestHarness();
        var (_, subscriptionId) = await SeedPastDueSubscriptionAsync(harness);
        harness.BillingProvider.ChargeResultToReturn = new ChargeResult(false, "ref", "insufficient_funds", "Insufficient funds.");

        harness.TimeProvider.Advance(TimeSpan.FromDays(7));
        await harness.Sut.RunAsync(); // -> Restricted

        harness.TimeProvider.Advance(TimeSpan.FromDays(7)); // day 14
        await harness.Sut.RunAsync();

        await using var context = harness.Entitlements.CreateContext();
        var subscription = await context.OrganisationSubscriptions.SingleAsync(s => s.Id == subscriptionId);
        Assert.Equal(SubscriptionStatus.Suspended, subscription.Status);

        var notifications = await context.NotificationMessages.Where(n => n.Type == NotificationType.SubscriptionSuspended).ToListAsync();
        Assert.NotEmpty(notifications);
    }

    [Fact]
    public async Task JobRerunOnSameDay_SendsNothingExtra_ChangesNoState()
    {
        using var harness = new DunningJobTestHarness();
        var (_, subscriptionId) = await SeedPastDueSubscriptionAsync(harness);
        harness.BillingProvider.ChargeResultToReturn = new ChargeResult(false, "ref", "insufficient_funds", "Insufficient funds.");

        harness.TimeProvider.Advance(TimeSpan.FromDays(1));
        await harness.Sut.RunAsync();
        await harness.Sut.RunAsync(); // re-run, same day
        await harness.Sut.RunAsync(); // and again

        Assert.Equal(1, harness.BillingProvider.ChargeAttempts.Count); // only the first run actually attempted a charge

        await using var context = harness.Entitlements.CreateContext();
        var payments = await context.SubscriptionPayments.Where(p => p.OrganisationSubscriptionId == subscriptionId).ToListAsync();
        Assert.Equal(2, payments.Count); // day-0 seed + the single day-1 attempt, not 4
    }

    [Fact]
    public async Task MissedWindow_JobDoesNotRunForFourDays_StateLandsCorrectlyOnResume()
    {
        using var harness = new DunningJobTestHarness();
        var (_, subscriptionId) = await SeedPastDueSubscriptionAsync(harness);
        harness.BillingProvider.ChargeResultToReturn = new ChargeResult(false, "ref", "insufficient_funds", "Insufficient funds.");

        // Job does not run at all for days 1-4 — jump straight to day 5.
        harness.TimeProvider.Advance(TimeSpan.FromDays(5));
        await harness.Sut.RunAsync();

        await using (var context = harness.Entitlements.CreateContext())
        {
            var subscription = await context.OrganisationSubscriptions.SingleAsync(s => s.Id == subscriptionId);
            Assert.Equal(SubscriptionStatus.PastDue, subscription.Status); // not yet Restricted

            // Only ONE retry attempt happened for day 5 — the missed days 1/3 were not "caught
            // up" by attempting multiple charges in a single run.
            var attemptsToday = await context.SubscriptionPayments
                .Where(p => p.OrganisationSubscriptionId == subscriptionId && p.CreatedAt.Date == harness.TimeProvider.GetUtcNow().UtcDateTime.Date)
                .ToListAsync();
            Assert.Single(attemptsToday);
        }

        // Resume normal cadence — day 7 and day 14 transitions still land correctly regardless
        // of the earlier gap.
        harness.TimeProvider.Advance(TimeSpan.FromDays(2)); // day 7
        await harness.Sut.RunAsync();
        harness.TimeProvider.Advance(TimeSpan.FromDays(7)); // day 14
        await harness.Sut.RunAsync();

        await using var finalContext = harness.Entitlements.CreateContext();
        var final = await finalContext.OrganisationSubscriptions.SingleAsync(s => s.Id == subscriptionId);
        Assert.Equal(SubscriptionStatus.Suspended, final.Status);

        // No duplicate notifications despite the irregular run pattern.
        var restrictedNotifications = await finalContext.NotificationMessages.Where(n => n.Type == NotificationType.SubscriptionRestricted).ToListAsync();
        var restrictedRecipients = restrictedNotifications.Select(n => (n.RecipientMemberId, n.Channel)).Distinct().Count();
        Assert.Equal(restrictedNotifications.Count, restrictedRecipients); // one per (recipient, channel), no dupes
    }

    [Fact]
    public async Task ConcurrentExecutionOnTwoInstances_ProducesOneSetOfEffects()
    {
        // Uses SharedCacheSqliteTestDatabase, not the default SqliteTestDatabase — Task.Run below
        // races two real OS threads against the database (unlike Phase 2's IStokvelOperationLock,
        // this lock's acquisition step itself touches the DB), which a single
        // Microsoft.Data.Sqlite.SqliteConnection instance cannot safely serve concurrently.
        using var db = new SharedCacheSqliteTestDatabase();
        using var harness = new DunningJobTestHarness(db);
        var (_, subscriptionId) = await SeedPastDueSubscriptionAsync(harness);
        harness.BillingProvider.ChargeResultToReturn = new ChargeResult(false, "ref", "insufficient_funds", "Insufficient funds.");

        harness.TimeProvider.Advance(TimeSpan.FromDays(1));

        // Two "instances" racing to process the same day's retry — the distributed lock must
        // ensure only one of them actually does the work.
        await Task.WhenAll(
            Task.Run(() => harness.Sut.RunAsync()),
            Task.Run(() => harness.Sut.RunAsync()));

        await using var context = harness.Entitlements.CreateContext();
        var payments = await context.SubscriptionPayments.Where(p => p.OrganisationSubscriptionId == subscriptionId).ToListAsync();
        Assert.Equal(2, payments.Count); // day-0 seed + exactly one day-1 attempt, not two
    }

    [Fact]
    public async Task PermanentDecline_StopsFurtherAutomaticRetries()
    {
        using var harness = new DunningJobTestHarness();
        var (_, subscriptionId) = await SeedPastDueSubscriptionAsync(harness);
        harness.BillingProvider.ChargeResultToReturn = new ChargeResult(false, "ref", "stolen_card", "Card reported stolen.");

        harness.TimeProvider.Advance(TimeSpan.FromDays(1));
        await harness.Sut.RunAsync();

        harness.TimeProvider.Advance(TimeSpan.FromDays(2)); // day 3
        await harness.Sut.RunAsync();

        // Only the day-1 attempt happened — day 3's retry was skipped because the day-1 decline
        // was classified as permanent.
        Assert.Single(harness.BillingProvider.ChargeAttempts);
    }

    private static async Task<(Guid StokvelId, Guid SubscriptionId)> SeedPastDueSubscriptionAsync(DunningJobTestHarness harness)
    {
        await harness.Entitlements.SeedCatalogueAsync();

        var dunningStartedAt = harness.TimeProvider.GetUtcNow().UtcDateTime;

        await using var context = harness.Entitlements.CreateContext();
        var growingPlan = await context.SubscriptionPlans.SingleAsync(p => p.Code == PlanCodes.Growing);
        var stokvel = TestData.CreateStokvel(context);

        var userId = $"admin-{Guid.NewGuid():N}";
        var admin = TestData.CreateStokvelMember(context, stokvel, role: SisonkeRole.StokvelAdmin);
        admin.ApplicationUserId = userId;

        var subscription = TestData.CreateOrganisationSubscription(context, stokvel, growingPlan.Id, SubscriptionStatus.PastDue);
        subscription.BillingEmail = "chair@example.com";
        subscription.TermsAcceptedByUserId = userId;
        subscription.DunningStartedAt = dunningStartedAt;
        subscription.GracePeriodEndsAt = dunningStartedAt.AddDays(14);

        context.SubscriptionPaymentMethods.Add(new SubscriptionPaymentMethod
        {
            Id = Guid.NewGuid(),
            OrganisationSubscriptionId = subscription.Id,
            Provider = SubscriptionProvider.Paystack,
            ProviderAuthorizationCode = "AUTH_test",
            IsDefault = true,
            IsReusable = true,
            AuthorisedAt = dunningStartedAt
        });

        // Day-0 failure, as BillingWebhookProcessor/TrialExpiryJob would have recorded it.
        context.SubscriptionPayments.Add(new SubscriptionPayment
        {
            Id = Guid.NewGuid(),
            OrganisationSubscriptionId = subscription.Id,
            Amount = growingPlan.MonthlyPrice,
            Currency = "ZAR",
            Status = SubscriptionPaymentStatus.Failed,
            ProviderReference = "day0-failure",
            AttemptNumber = 1,
            FailureCode = "insufficient_funds",
            FailureMessage = "Insufficient funds.",
            CreatedAt = dunningStartedAt
        });

        await context.SaveChangesAsync();

        return (stokvel.Id, subscription.Id);
    }
}
