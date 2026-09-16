using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;
using Sisonke.Web.Services;
using Sisonke.Web.Tests.TestSupport;

namespace Sisonke.Web.Tests.Billing;

public class EntitlementConcurrencyTests
{
    [Fact]
    public async Task TwoSimultaneousAddMemberCommands_AtTheLimit_OnlyOneSucceeds()
    {
        using var harness = new EntitlementTestHarness();
        await harness.SeedCatalogueAsync();

        Guid stokvelId;
        await using (var setupContext = harness.CreateContext())
        {
            var starterPlan = setupContext.SubscriptionPlans.Single(p => p.Code == PlanCodes.Starter);
            var stokvel = TestData.CreateStokvel(setupContext);
            TestData.CreateOrganisationSubscription(setupContext, stokvel, starterPlan.Id, SubscriptionStatus.Active);

            for (var i = 0; i < 29; i++)
            {
                TestData.CreateStokvelMember(setupContext, stokvel);
            }

            await setupContext.SaveChangesAsync();
            stokvelId = stokvel.Id;
        }

        // Task.Run forces genuine thread-pool parallelism (not just async interleaving) so the
        // two commands actually race for IStokvelOperationLock rather than happening to run
        // sequentially by scheduler luck.
        var results = await Task.WhenAll(
            Task.Run(() => TryAddMemberAsync(harness, stokvelId)),
            Task.Run(() => TryAddMemberAsync(harness, stokvelId)));

        Assert.Equal(1, results.Count(created => created is not null));

        await using var verifyContext = harness.CreateContext();
        var finalActiveCount = await verifyContext.Members.CountAsync(m => m.Status == MemberStatus.Active);
        Assert.Equal(30, finalActiveCount);
    }

    private static async Task<Member?> TryAddMemberAsync(EntitlementTestHarness harness, Guid stokvelId)
    {
        await using var context = harness.CreateContext();
        var memberService = new MemberService(
            context,
            new OperatingRuleService(context),
            new FakeWebHostEnvironment(),
            NullLogger<MemberService>.Instance,
            harness.Sut,
            harness.OperationLock);

        var newMember = new Member
        {
            FullName = "Concurrent Member",
            CellphoneNumber = "0821111111",
            Status = MemberStatus.Active,
            DefaultRole = SisonkeRole.Member,
            JoiningDate = DateTime.Today
        };

        return await memberService.AddMemberAsync(stokvelId, newMember);
    }

    private sealed class FakeWebHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Sisonke.Web.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string EnvironmentName { get; set; } = "Test";
        public string WebRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}
