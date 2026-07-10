using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;

namespace Sisonke.Web.Tests.TestSupport;

public static class TestData
{
    public static Member CreateMember(ApplicationDbContext context, bool emailEnabled = true)
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
            MemberNumber = "M001",
            FullName = "Test Member",
            CellphoneNumber = "0821234567",
            EmailAddress = "member@example.com",
            EmailEnabled = emailEnabled,
            JoiningDate = DateTime.UtcNow
        };
        context.Members.Add(member);

        return member;
    }
}
