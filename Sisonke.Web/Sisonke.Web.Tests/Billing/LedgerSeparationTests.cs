using System.Runtime.CompilerServices;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;
using Sisonke.Web.Services.Billing;
using Sisonke.Web.Services.Billing.Netcash;
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
        typeof(ContributionPaymentAudit), typeof(ClaimPayoutAudit), typeof(MemberFine),
        typeof(MemberLoan), typeof(MemberLoanRepayment), typeof(MemberSurplusWallet),
        typeof(MemberSurplusWalletTransaction), typeof(MemberSurplusWithdrawalRequest),
        typeof(RotationalContributionPayment), typeof(RotationalPayout), typeof(RotationalPayoutOrder),
        typeof(StokvelReserveTransaction)
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
    public void NoStokvelLedgerEntityHasAForeignKeyToASubscriptionBillingEntity()
    {
        using var db = new SqliteTestDatabase();
        using var context = db.CreateContext();
        var violations = new List<string>();

        foreach (var ledgerType in StokvelLedgerEntityTypes)
        {
            var entityType = context.Model.FindEntityType(ledgerType);
            Assert.NotNull(entityType);
            foreach (var foreignKey in entityType.GetForeignKeys())
            {
                if (BillingEntityTypes.Contains(foreignKey.PrincipalEntityType.ClrType))
                {
                    violations.Add($"{ledgerType.Name} has a foreign key to {foreignKey.PrincipalEntityType.ClrType.Name}");
                }
            }
        }

        Assert.Empty(violations);
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
            "Services/ClaimEligibilityService.cs",
            "Services/FineService.cs",
            "Services/ContributionService.cs",
            "Services/LoansWalletService.cs",
            "Services/RotationalContributionPaymentService.cs",
            "Services/RotationalContributionCycleService.cs",
            "Services/RotationalPayoutService.cs",
            "Services/RotationalPayoutOrderService.cs",
            "Services/RotationalStokvelService.cs"
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

    [Fact]
    public void OperationalFinancialModules_DoNotDependOnSubscriptionProviders()
    {
        var repoRoot = GetRepoRootPath();
        var operationalFiles = Directory.GetFiles(Path.Combine(repoRoot, "Services"), "*.cs")
            .Where(path => new[] { "Contribution", "Loan", "Wallet", "Rotational", "Funeral", "Claim", "Payout" }
                .Any(term => Path.GetFileName(path).Contains(term, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        var forbidden = new[]
        {
            "ISubscriptionPaymentProvider", "IBillingProvider", "INetcashClient",
            "INetcashMandateService", "SubscriptionPaymentSetupService"
        };

        var violations = operationalFiles
            .SelectMany(path => File.ReadAllLines(path).Select((line, index) => (path, line, index)))
            .Where(item => forbidden.Any(term => item.line.Contains(term, StringComparison.Ordinal)))
            .Select(item => $"{Path.GetRelativePath(repoRoot, item.path)}:{item.index + 1}: {item.line.Trim()}")
            .ToList();

        Assert.Empty(violations);
    }

    [Fact]
    public void SubscriptionProviderInterfaces_ExposeNoTransferPayoutOrBeneficiaryOperation()
    {
        var providerInterfaces = new[]
        {
            typeof(ISubscriptionPaymentProvider), typeof(ISubscriptionPaymentSetupService),
            typeof(INetcashMandateService), typeof(IBillingProvider)
        };
        var forbiddenMethodTerms = new[]
        {
            "Transfer", "Payout", "Beneficiary", "Contribution", "LoanRepayment",
            "FuneralClaim", "MemberPayment", "Wallet"
        };

        var violations = providerInterfaces.SelectMany(type => type.GetMethods())
            .Where(method => forbiddenMethodTerms.Any(term => method.Name.Contains(term, StringComparison.OrdinalIgnoreCase)))
            .Select(method => $"{method.DeclaringType!.Name}.{method.Name}")
            .ToList();

        Assert.Empty(violations);
    }

    [Fact]
    public void BrowserFacingSetupContracts_CannotSupplyAmountOrBeneficiary()
    {
        var contracts = new[] { typeof(PaymentSetupRequest), typeof(NetcashDebiCheckMandateRequest) };
        var forbiddenTerms = new[] { "Amount", "Price", "Beneficiary", "Payee", "Payout", "Transfer" };

        var violations = contracts.SelectMany(type => type.GetProperties())
            .Where(property => forbiddenTerms.Any(term => property.Name.Contains(term, StringComparison.OrdinalIgnoreCase)))
            .Select(property => $"{property.DeclaringType!.Name}.{property.Name}")
            .ToList();

        Assert.Empty(violations);
        Assert.NotNull(typeof(PaymentSetupRequest).GetProperty(nameof(PaymentSetupRequest.SubscriptionId)));
    }

    [Fact]
    public void SubscriptionChargePurpose_RemainsSubscriptionOnly()
    {
        Assert.Equal(
            new[] { "InitialSubscription", "RecurringSubscription", "Retry", "ManualCollection", "Adjustment" },
            Enum.GetNames<SubscriptionChargePurpose>());
    }

    [Fact]
    public void PaymentSetupCannotBeInitiatedForContributionRecords() =>
        AssertProviderSurfaceRejectsOperationalType<MemberContribution>();

    [Fact]
    public void PaymentSetupCannotBeInitiatedForMemberLoanRepayments() =>
        AssertProviderSurfaceRejectsOperationalType<MemberLoanRepayment>();

    [Fact]
    public void PaymentSetupCannotBeInitiatedForRotationalPayouts() =>
        AssertProviderSurfaceRejectsOperationalType<RotationalPayout>();

    [Fact]
    public void PaymentSetupCannotBeInitiatedForFuneralClaimPayouts() =>
        AssertProviderSurfaceRejectsOperationalType<FuneralClaim>();

    private static void AssertProviderSurfaceRejectsOperationalType<TOperational>()
    {
        var surfaces = new[]
        {
            typeof(ISubscriptionPaymentProvider), typeof(ISubscriptionPaymentSetupService),
            typeof(INetcashMandateService), typeof(IBillingProvider)
        };
        var acceptedTypes = surfaces.SelectMany(type => type.GetMethods())
            .SelectMany(method => method.GetParameters())
            .Select(parameter => parameter.ParameterType)
            .ToList();

        Assert.DoesNotContain(typeof(TOperational), acceptedTypes);
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
