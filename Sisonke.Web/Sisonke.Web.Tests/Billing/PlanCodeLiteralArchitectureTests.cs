using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Sisonke.Web.Tests.Billing;

/// <summary>
/// Fails the build if a plan-code string literal (STARTER/GROWING/PROFESSIONAL/ENTERPRISE)
/// appears anywhere outside the billing module or a test project. Brief: "No scattered
/// plan-name conditionals — if (plan == "Professional") anywhere in feature code is a defect."
/// New feature code must gate on FeatureCodes constants via IEntitlementService instead.
/// </summary>
public class PlanCodeLiteralArchitectureTests
{
    private static readonly Regex PlanCodeLiteral = new(
        "\"(STARTER|GROWING|PROFESSIONAL|ENTERPRISE)\"", RegexOptions.Compiled);

    private static readonly string[] ExcludedDirectorySegments =
    [
        $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}.git{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}Sisonke.Web.Tests{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}Sisonke.MigrationTool{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}Services{Path.DirectorySeparatorChar}Entitlements{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}Data{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}"
    ];

    // The billing catalogue itself is the one place allowed to spell out plan codes.
    private static readonly string[] ExcludedFileNames =
    [
        "PlanCodes.cs",
        "FeatureCodes.cs",
        "BillingCatalogueSeed.cs",
        "SubscriptionPlan.cs"
    ];

    [Fact]
    public void NoPlanCodeStringLiteralsOutsideBillingModule()
    {
        var repoRoot = GetRepoRootPath();
        var violations = new List<string>();

        foreach (var file in Directory.EnumerateFiles(repoRoot, "*.cs", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(repoRoot, "*.razor", SearchOption.AllDirectories)))
        {
            if (IsExcluded(file))
            {
                continue;
            }

            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                if (PlanCodeLiteral.IsMatch(lines[i]))
                {
                    violations.Add($"{Path.GetRelativePath(repoRoot, file)}:{i + 1}: {lines[i].Trim()}");
                }
            }
        }

        Assert.True(violations.Count == 0,
            "Plan-code string literal(s) found outside the billing module — route through FeatureCodes/PlanCodes and IEntitlementService instead:\n" +
            string.Join('\n', violations));
    }

    private static bool IsExcluded(string filePath)
    {
        if (ExcludedDirectorySegments.Any(segment => filePath.Contains(segment, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return ExcludedFileNames.Contains(Path.GetFileName(filePath));
    }

    private static string GetRepoRootPath([CallerFilePath] string testFilePath = "")
    {
        // This file lives at <repoRoot>/Sisonke.Web.Tests/Billing/PlanCodeLiteralArchitectureTests.cs
        var repoRoot = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(testFilePath)!, "..", ".."));

        if (!File.Exists(Path.Combine(repoRoot, "Sisonke.Web.csproj")))
        {
            throw new InvalidOperationException(
                $"Could not locate repo root from test file path (resolved to '{repoRoot}'); Sisonke.Web.csproj not found there.");
        }

        return repoRoot;
    }
}
