using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Enums;
using Sisonke.Web.Services;
using Sisonke.Web.Services.Billing.Jobs;
using Sisonke.Web.Services.Jobs;
using Sisonke.Web.Services.Notifications;
using Sisonke.Web.Tests.TestSupport;

namespace Sisonke.Web.Tests.Billing;

public class TrialReminderJobTests
{
    [Fact]
    public async Task MissedDay14_ResumingAtDay12_SendsOnlyThe14DayBucket_NotTheWholeBacklog()
    {
        using var harness = new EntitlementTestHarness();
        await harness.SeedCatalogueAsync();
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);

        Guid subscriptionId;
        Guid officeBearerId;
        await using (var context = harness.CreateContext())
        {
            var plan = await context.SubscriptionPlans.SingleAsync(p => p.Code == PlanCodes.Growing);
            var stokvel = TestData.CreateStokvel(context);
            var officeBearer = TestData.CreateStokvelMember(context, stokvel, role: SisonkeRole.Chairperson);
            var subscription = TestData.CreateOrganisationSubscription(context, stokvel, plan.Id, SubscriptionStatus.Trialing);
            // Trial ends in 12 days — the 30 and 14 day buckets have both already passed without
            // the job ever running (a 4+ day outage covering day 14).
            subscription.TrialEndsAt = timeProvider.GetUtcNow().UtcDateTime.AddDays(12);
            await context.SaveChangesAsync();

            subscriptionId = subscription.Id;
            officeBearerId = officeBearer.Id;
        }

        var job = BuildJob(harness, timeProvider);
        await job.RunAsync();

        await using var verifyContext = harness.CreateContext();
        var sentBuckets = await verifyContext.TrialReminderSents
            .Where(t => t.OrganisationSubscriptionId == subscriptionId)
            .Select(t => t.Bucket)
            .ToListAsync();

        // Only the most urgent unsent bucket (14) is sent — the 30-day bucket is permanently
        // skipped since it would be a misleading "30 days left" message with 12 actually left.
        Assert.Equal([14], sentBuckets);

        var notifications = await verifyContext.NotificationMessages
            .Where(n => n.RecipientMemberId == officeBearerId && n.Type == NotificationType.TrialReminder)
            .ToListAsync();
        Assert.Equal(2, notifications.Count); // one bucket x 2 channels, not two buckets' worth
    }

    [Fact]
    public async Task RunTwiceSameDay_DoesNotDoubleSend()
    {
        using var harness = new EntitlementTestHarness();
        await harness.SeedCatalogueAsync();
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);

        Guid subscriptionId;
        await using (var context = harness.CreateContext())
        {
            var plan = await context.SubscriptionPlans.SingleAsync(p => p.Code == PlanCodes.Growing);
            var stokvel = TestData.CreateStokvel(context);
            TestData.CreateStokvelMember(context, stokvel, role: SisonkeRole.Chairperson);
            var subscription = TestData.CreateOrganisationSubscription(context, stokvel, plan.Id, SubscriptionStatus.Trialing);
            subscription.TrialEndsAt = timeProvider.GetUtcNow().UtcDateTime.AddDays(7);
            await context.SaveChangesAsync();
            subscriptionId = subscription.Id;
        }

        var job = BuildJob(harness, timeProvider);
        await job.RunAsync();
        await job.RunAsync();
        await job.RunAsync();

        await using var verifyContext = harness.CreateContext();
        var sentBuckets = await verifyContext.TrialReminderSents.Where(t => t.OrganisationSubscriptionId == subscriptionId).ToListAsync();
        Assert.Single(sentBuckets);
    }

    private static TrialReminderJob BuildJob(EntitlementTestHarness harness, FakeTimeProvider timeProvider)
    {
        var memberAccessService = new MemberAccessService(harness.CreateContext());
        var notificationEnqueuer = new NotificationEnqueuer(
            new NotificationEmailTemplateRenderer(new AppSettings(), NullLogger<NotificationEmailTemplateRenderer>.Instance));
        var notificationService = new Sisonke.Web.Services.Billing.SubscriptionNotificationService(
            memberAccessService, notificationEnqueuer, NullLogger<Sisonke.Web.Services.Billing.SubscriptionNotificationService>.Instance);
        var jobLock = new DistributedJobLock(harness.DbFactory);

        return new TrialReminderJob(harness.DbFactory, jobLock, notificationService, timeProvider, NullLogger<TrialReminderJob>.Instance);
    }
}
