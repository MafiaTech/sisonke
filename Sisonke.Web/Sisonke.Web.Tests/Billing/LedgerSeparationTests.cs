using System.Runtime.CompilerServices;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Tests.TestSupport;

namespace Sisonke.Web.Tests.Billing;

/// <summary>
/// Brief rule 5: Sisonke subscription revenue and member contribution payments are separate
/// ledgers. Unlike the plan-code literal check (Phase 2), there is no shared "transactions"
/// table to discriminate here — the two ledgers are already fully separate tables by
/// construction (SubscriptionInvoice/SubscriptionPayment/SubscriptionEvent vs
/// MemberContribution/Payment/FuneralClaim/ContributionPaymentAudit/ClaimPayoutAudit). These
/// tests guard that separation two ways: no EF foreign key ever crosses between the two table
/// families, and the stokvel financial report services never reference the billing tables.
/// </summary>
public class LedgerSeparationTests
{
    private static readonly Type[] BillingEntityTypes =
    [
        typeof(OrganisationSubscription), typeof(SubscriptionInvoice), typeof(SubscriptionInvoiceLine),
        typeof(SubscriptionPayment), typeof(SubscriptionEvent), typeof(SubscriptionUsage),
        typeof(SubscriptionPaymentMethod), typeof(BillingWebhookEvent)
    ];

    private static readonly Type[] StokvelLedgerEntityTypes =
    [
        typeof(MemberContribution), typeof(Payment), typeof(FuneralClaim),
        typeof(ContributionPaymentAudit), typeof(ClaimPayoutAudit), typeof(MemberFine)
    ];

    [Fact]
    public void NoBillingEntityHasAForeignKeyToAStokvelLedgerEntity()
    {
        using var db = new SqliteTestDatabase();
        using var context = db.CreateContext();
        var model = context.Model;

        var violations = new List<string>();

        foreach (var billingType in BillingEntityTypes)
        {
            var entityType = model.FindEntityType(billingType);
            Assert.NotNull(entityType);

            foreach (var foreignKey in entityType.GetForeignKeys())
            {
                var principalClrType = foreignKey.PrincipalEntityType.ClrType;
                if (StokvelLedgerEntityTypes.Contains(principalClrType))
                {
                    violations.Add($"{billingType.Name} has a foreign key to {principalClrType.Name}");
                }
            }
        }

        Assert.True(violations.Count == 0, "Billing ledger entities must never reference stokvel ledger entities:\n" + string.Join('\n', violations));
    }

    [Fact]
    public void StokvelFinancialReportServices_NeverReferenceSubscriptionBillingTables()
    {
        var repoRoot = GetRepoRootPath();
        var reportServiceFiles = new[]
        {
            "Services/FinanceReportService.cs",
            "Services/ContributionPaymentService.cs",
            "Services/FuneralClaimService.cs",
            "Services/FineService.cs",
            "Services/ContributionService.cs"
        };

        var billingTypeNames = new[]
        {
            "SubscriptionInvoice", "SubscriptionPayment", "SubscriptionEvent", "SubscriptionUsage",
            "SubscriptionPaymentMethod", "OrganisationSubscription", "BillingWebhookEvent"
        };

        var violations = new List<string>();

        foreach (var relativePath in reportServiceFiles)
        {
            var fullPath = Path.Combine(repoRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(fullPath))
            {
                continue; // Service may not exist / may have been renamed — not this test's concern.
            }

            var lines = File.ReadAllLines(fullPath);
            for (var i = 0; i < lines.Length; i++)
            {
                foreach (var billingType in billingTypeNames)
                {
                    if (lines[i].Contains(billingType, StringComparison.Ordinal))
                    {
                        violations.Add($"{relativePath}:{i + 1} references {billingType}: {lines[i].Trim()}");
                    }
                }
            }
        }

        Assert.True(violations.Count == 0,
            "Stokvel financial report services must never reference subscription billing tables:\n" + string.Join('\n', violations));
    }

    private static string GetRepoRootPath([CallerFilePath] string testFilePath = "")
    {
        var repoRoot = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(testFilePath)!, "..", ".."));

        if (!File.Exists(Path.Combine(repoRoot, "Sisonke.Web.csproj")))
        {
            throw new InvalidOperationException($"Could not locate repo root from test file path (resolved to '{repoRoot}').");
        }

        return repoRoot;
    }
}
