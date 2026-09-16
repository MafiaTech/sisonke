using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;
using Sisonke.Web.Tests.TestSupport;

namespace Sisonke.Web.Tests.Billing;

public class RestrictedAccessAndDataRetentionTests
{
    [Fact]
    public async Task Restricted_AddingAMemberFails()
    {
        using var harness = new EntitlementTestHarness();
        var (stokvelId, _) = await SeedRestrictedSubscriptionAsync(harness);

        var decision = await harness.Sut.AuthorizeAsync(stokvelId, Sisonke.Web.Data.FeatureCodes.MaxMembers, requestedUsage: 1);

        Assert.False(decision.Allowed);
    }

    [Fact]
    public async Task Restricted_ExportingAReportSucceeds()
    {
        using var harness = new EntitlementTestHarness();
        var (stokvelId, _) = await SeedRestrictedSubscriptionAsync(harness);

        var decision = await harness.Sut.AuthorizeAsync(stokvelId, Sisonke.Web.Data.FeatureCodes.OpAccountDataExport);

        Assert.True(decision.Allowed);
    }

    [Fact]
    public async Task Restricted_OpeningTheBillingPageSucceeds()
    {
        using var harness = new EntitlementTestHarness();
        var (stokvelId, _) = await SeedRestrictedSubscriptionAsync(harness);

        var decision = await harness.Sut.AuthorizeAsync(stokvelId, Sisonke.Web.Data.FeatureCodes.OpBillingPageAccess);

        Assert.True(decision.Allowed);
    }

    [Fact]
    public async Task NoCodePathDeletesOrArchivesOrganisationDataForNonPayment()
    {
        // Structural guard: none of the Phase 3/4 billing source files may reference a deletion
        // API (DbSet.Remove/RemoveRange, ExecuteDelete) or an "archive" concept as a consequence
        // of a failed payment. Suspension is read-only, never destructive (brief rule 7).
        var repoRoot = GetRepoRootPath();
        var billingSourceDirectory = Path.Combine(repoRoot, "Services", "Billing");
        var forbiddenPatterns = new[] { ".Remove(", ".RemoveRange(", "ExecuteDelete", "DROP TABLE", "DELETE FROM" };

        var violations = new List<string>();

        foreach (var file in Directory.EnumerateFiles(billingSourceDirectory, "*.cs", SearchOption.AllDirectories))
        {
            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                foreach (var pattern in forbiddenPatterns)
                {
                    if (lines[i].Contains(pattern, StringComparison.Ordinal))
                    {
                        violations.Add($"{Path.GetRelativePath(repoRoot, file)}:{i + 1}: {lines[i].Trim()}");
                    }
                }
            }
        }

        Assert.True(violations.Count == 0, "Found a data-deletion pattern in the billing lifecycle code:\n" + string.Join('\n', violations));
    }

    [Fact]
    public async Task Suspended_StokvelAndMemberRecordsStillExistUnmodified()
    {
        using var harness = new EntitlementTestHarness();
        await harness.SeedCatalogueAsync();

        await using var context = harness.CreateContext();
        var stokvel = TestData.CreateStokvel(context);
        var member = TestData.CreateStokvelMember(context, stokvel);
        var plan = context.SubscriptionPlans.Single(p => p.Code == PlanCodes.Growing);
        TestData.CreateOrganisationSubscription(context, stokvel, plan.Id, SubscriptionStatus.Suspended);
        await context.SaveChangesAsync();

        await using var verifyContext = harness.CreateContext();
        var stokvelStillExists = await verifyContext.Stokvels.AnyAsync(s => s.Id == stokvel.Id && !s.IsDeleted);
        var memberStillExists = await verifyContext.Members.AnyAsync(m => m.Id == member.Id);

        Assert.True(stokvelStillExists);
        Assert.True(memberStillExists);
    }

    private static async Task<(Guid StokvelId, Guid SubscriptionId)> SeedRestrictedSubscriptionAsync(EntitlementTestHarness harness)
    {
        await harness.SeedCatalogueAsync();

        await using var context = harness.CreateContext();
        var plan = context.SubscriptionPlans.Single(p => p.Code == PlanCodes.Growing);
        var stokvel = TestData.CreateStokvel(context);
        var subscription = TestData.CreateOrganisationSubscription(context, stokvel, plan.Id, SubscriptionStatus.Restricted);
        await context.SaveChangesAsync();

        return (stokvel.Id, subscription.Id);
    }

    private static string GetRepoRootPath([System.Runtime.CompilerServices.CallerFilePath] string testFilePath = "")
    {
        var repoRoot = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(testFilePath)!, "..", ".."));

        if (!File.Exists(Path.Combine(repoRoot, "Sisonke.Web.csproj")))
        {
            throw new InvalidOperationException($"Could not locate repo root from test file path (resolved to '{repoRoot}').");
        }

        return repoRoot;
    }
}
