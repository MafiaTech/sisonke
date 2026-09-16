using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Tests.TestSupport;

public static class TestData
{
    public static Stokvel CreateStokvel(ApplicationDbContext context)
    {
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Test Tenant",
            Slug = $"test-tenant-{Guid.NewGuid():N}"
        };
        context.Tenants.Add(tenant);

        var stokvel = new Stokvel
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Name = "Test Stokvel",
            Type = StokvelType.BurialSociety,
            Archetype = StokvelArchetype.BurialSociety
        };
        context.Stokvels.Add(stokvel);

        return stokvel;
    }

    public static Member CreateMember(
        ApplicationDbContext context,
        bool emailEnabled = true,
        bool webPushEnabled = true,
        string? applicationUserId = null)
    {
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Test Tenant",
            Slug = $"test-tenant-{Guid.NewGuid():N}"
        };
        context.Tenants.Add(tenant);

        var member = new Member
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            ApplicationUserId = applicationUserId,
            MemberNumber = "M001",
            FullName = "Test Member",
            CellphoneNumber = "0821234567",
            EmailAddress = "member@example.com",
            EmailEnabled = emailEnabled,
            WebPushEnabled = webPushEnabled,
            JoiningDate = DateTime.UtcNow
        };
        context.Members.Add(member);

        return member;
    }

    public static Member CreateStokvelMember(
        ApplicationDbContext context,
        Stokvel stokvel,
        MemberStatus status = MemberStatus.Active,
        SisonkeRole role = SisonkeRole.Member)
    {
        var member = new Member
        {
            Id = Guid.NewGuid(),
            TenantId = stokvel.TenantId,
            MemberNumber = $"M-{Guid.NewGuid():N}"[..10],
            FullName = "Test Member",
            CellphoneNumber = "0821234567",
            Status = status,
            DefaultRole = role,
            JoiningDate = DateTime.UtcNow
        };
        context.Members.Add(member);

        return member;
    }

    public static OrganisationSubscription CreateOrganisationSubscription(
        ApplicationDbContext context,
        Stokvel stokvel,
        Guid? subscriptionPlanId,
        SubscriptionStatus status,
        DateTime? currentPeriodEndsAt = null)
    {
        var subscription = new OrganisationSubscription
        {
            Id = Guid.NewGuid(),
            StokvelId = stokvel.Id,
            SubscriptionPlanId = subscriptionPlanId,
            Status = status,
            CurrentPeriodEndsAt = currentPeriodEndsAt,
            CreatedAt = DateTime.UtcNow
        };
        context.OrganisationSubscriptions.Add(subscription);

        return subscription;
    }
}
