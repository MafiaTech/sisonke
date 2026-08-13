using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;
using Sisonke.Web.Tests.TestSupport;

namespace Sisonke.Web.Tests.Billing;

public class EntitlementUsageProviderTests
{
    [Fact]
    public async Task MaxAdministrators_CountsOnlyStokvelAdminAndCreator_NotElectedOfficeBearers()
    {
        using var harness = new EntitlementTestHarness();
        await using var context = harness.CreateContext();
        var stokvel = TestData.CreateStokvel(context);

        TestData.CreateStokvelMember(context, stokvel, role: SisonkeRole.StokvelAdmin);
        TestData.CreateStokvelMember(context, stokvel, role: SisonkeRole.Creator);
        TestData.CreateStokvelMember(context, stokvel, role: SisonkeRole.Chairperson);
        TestData.CreateStokvelMember(context, stokvel, role: SisonkeRole.Secretary);
        TestData.CreateStokvelMember(context, stokvel, role: SisonkeRole.Member);
        await context.SaveChangesAsync();

        var usage = await harness.UsageProvider.GetCurrentUsageAsync(stokvel.Id, FeatureCodes.MaxAdministrators);

        Assert.Equal(2, usage);
    }

    [Fact]
    public async Task MaxAdministrators_ExcludesNonActiveMembers()
    {
        using var harness = new EntitlementTestHarness();
        await using var context = harness.CreateContext();
        var stokvel = TestData.CreateStokvel(context);

        TestData.CreateStokvelMember(context, stokvel, role: SisonkeRole.StokvelAdmin, status: MemberStatus.Active);
        TestData.CreateStokvelMember(context, stokvel, role: SisonkeRole.StokvelAdmin, status: MemberStatus.Resigned);
        await context.SaveChangesAsync();

        var usage = await harness.UsageProvider.GetCurrentUsageAsync(stokvel.Id, FeatureCodes.MaxAdministrators);

        Assert.Equal(1, usage);
    }

    [Fact]
    public async Task MaxSchemes_CountsStokvelsSharingTheSameTenant()
    {
        using var harness = new EntitlementTestHarness();
        await using var context = harness.CreateContext();
        var stokvel = TestData.CreateStokvel(context);

        // A second Stokvel under the same Tenant — the "second scheme".
        var secondStokvel = new Stokvel
        {
            Id = Guid.NewGuid(),
            TenantId = stokvel.TenantId,
            Name = "Second Scheme",
            Type = StokvelType.SavingsStokvel,
            Archetype = StokvelArchetype.SavingsClub,
            IsActive = true
        };
        context.Stokvels.Add(secondStokvel);
        await context.SaveChangesAsync();

        var usage = await harness.UsageProvider.GetCurrentUsageAsync(stokvel.Id, FeatureCodes.MaxSchemes);

        Assert.Equal(2, usage);
    }

    [Fact]
    public async Task StorageMb_SumsMemberAndClaimAndConstitutionDocuments()
    {
        using var harness = new EntitlementTestHarness();
        await using var context = harness.CreateContext();
        var stokvel = TestData.CreateStokvel(context);
        var member = TestData.CreateStokvelMember(context, stokvel);
        await context.SaveChangesAsync();

        const long oneMb = 1024 * 1024;

        context.MemberDocuments.Add(new MemberDocument
        {
            Id = Guid.NewGuid(),
            MemberId = member.Id,
            DocumentType = "ID",
            OriginalFileName = "id.pdf",
            StoredFilePath = "/tmp/id.pdf",
            FileSizeBytes = oneMb
        });

        context.ConstitutionDocuments.Add(new ConstitutionDocument
        {
            Id = Guid.NewGuid(),
            TenantId = stokvel.TenantId,
            Title = "Constitution",
            FileSizeBytes = oneMb
        });

        await context.SaveChangesAsync();

        var usage = await harness.UsageProvider.GetCurrentUsageAsync(stokvel.Id, FeatureCodes.StorageMb);

        Assert.Equal(2, usage);
    }

    [Fact]
    public async Task UnknownFeatureCode_ReturnsZero()
    {
        using var harness = new EntitlementTestHarness();
        await using var context = harness.CreateContext();
        var stokvel = TestData.CreateStokvel(context);
        await context.SaveChangesAsync();

        var usage = await harness.UsageProvider.GetCurrentUsageAsync(stokvel.Id, FeatureCodes.RotationalStokvel);

        Assert.Equal(0, usage);
    }
}
