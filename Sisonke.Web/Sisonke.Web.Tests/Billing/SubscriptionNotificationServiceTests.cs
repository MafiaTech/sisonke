using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;
using Sisonke.Web.Services;
using Sisonke.Web.Services.Billing;
using Sisonke.Web.Services.Notifications;
using Sisonke.Web.Tests.TestSupport;

namespace Sisonke.Web.Tests.Billing;

public class SubscriptionNotificationServiceTests
{
    [Fact]
    public async Task SendTrialReminder_SameBucketTwice_IsIdempotent_SecondCallSendsNothingNew()
    {
        using var harness = new EntitlementTestHarness();
        var seed = await SeedAsync(harness);
        var service = BuildService(harness);

        await using (var context = harness.CreateContext())
        {
            var attached = await context.OrganisationSubscriptions.SingleAsync(s => s.Id == seed.SubscriptionId);
            var attachedPlan = await context.SubscriptionPlans.SingleAsync(p => p.Id == seed.PlanId);
            await service.SendTrialReminderAsync(context, attached, attachedPlan, 7);
            await service.SendTrialReminderAsync(context, attached, attachedPlan, 7); // same period+bucket, replayed
        }

        await using var verifyContext = harness.CreateContext();
        var notifications = await verifyContext.NotificationMessages
            .Where(n => n.RecipientMemberId == seed.OfficeBearerMemberId && n.Type == NotificationType.TrialReminder)
            .ToListAsync();

        // One per channel (Email + WebPush), not doubled by the replay.
        Assert.Equal(2, notifications.Count);
    }

    [Fact]
    public async Task SendTrialReminder_DifferentBuckets_AreDistinctSends()
    {
        using var harness = new EntitlementTestHarness();
        var seed = await SeedAsync(harness);
        var service = BuildService(harness);

        await using (var context = harness.CreateContext())
        {
            var attached = await context.OrganisationSubscriptions.SingleAsync(s => s.Id == seed.SubscriptionId);
            var attachedPlan = await context.SubscriptionPlans.SingleAsync(p => p.Id == seed.PlanId);
            await service.SendTrialReminderAsync(context, attached, attachedPlan, 7);
            await service.SendTrialReminderAsync(context, attached, attachedPlan, 3); // different bucket = different period
        }

        await using var verifyContext = harness.CreateContext();
        var notifications = await verifyContext.NotificationMessages
            .Where(n => n.RecipientMemberId == seed.OfficeBearerMemberId && n.Type == NotificationType.TrialReminder)
            .ToListAsync();

        Assert.Equal(4, notifications.Count); // 2 buckets x 2 channels
    }

    [Fact]
    public async Task Notifications_NeverGoToOrdinaryMembers_OnlyOfficeBearersAndBillingAdmin()
    {
        using var harness = new EntitlementTestHarness();
        var seed = await SeedAsync(harness);
        var service = BuildService(harness);

        Guid ordinaryMemberId;
        await using (var context = harness.CreateContext())
        {
            var stokvel = await context.Stokvels.SingleAsync(s => s.Id == seed.StokvelId);
            var ordinaryMember = TestData.CreateStokvelMember(context, stokvel, role: SisonkeRole.Member);
            await context.SaveChangesAsync();
            ordinaryMemberId = ordinaryMember.Id;

            var attached = await context.OrganisationSubscriptions.SingleAsync(s => s.Id == seed.SubscriptionId);
            var attachedPlan = await context.SubscriptionPlans.SingleAsync(p => p.Id == seed.PlanId);
            await service.SendTrialStartedAsync(context, attached, attachedPlan);
        }

        await using var verifyContext = harness.CreateContext();
        var recipients = await verifyContext.NotificationMessages
            .Where(n => n.Type == NotificationType.TrialStarted)
            .Select(n => n.RecipientMemberId)
            .Distinct()
            .ToListAsync();

        Assert.Contains(seed.OfficeBearerMemberId, recipients);
        Assert.DoesNotContain(ordinaryMemberId, recipients);
    }

    private static SubscriptionNotificationService BuildService(EntitlementTestHarness harness)
    {
        var memberAccessService = new MemberAccessService(harness.CreateContext());
        var notificationEnqueuer = new NotificationEnqueuer(
            new NotificationEmailTemplateRenderer(new AppSettings(), NullLogger<NotificationEmailTemplateRenderer>.Instance));

        return new SubscriptionNotificationService(memberAccessService, notificationEnqueuer, NullLogger<SubscriptionNotificationService>.Instance);
    }

    private static async Task<(Guid StokvelId, Guid SubscriptionId, Guid PlanId, Guid OfficeBearerMemberId)> SeedAsync(EntitlementTestHarness harness)
    {
        await harness.SeedCatalogueAsync();

        await using var context = harness.CreateContext();
        var plan = await context.SubscriptionPlans.SingleAsync(p => p.Code == PlanCodes.Growing);
        var stokvel = TestData.CreateStokvel(context);
        var officeBearer = TestData.CreateStokvelMember(context, stokvel, role: SisonkeRole.Chairperson);
        var subscription = TestData.CreateOrganisationSubscription(context, stokvel, plan.Id, SubscriptionStatus.Trialing);
        subscription.TrialEndsAt = DateTime.UtcNow.AddDays(7);
        subscription.NextBillingAt = subscription.TrialEndsAt;

        await context.SaveChangesAsync();

        return (stokvel.Id, subscription.Id, plan.Id, officeBearer.Id);
    }
}
