using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;
using Sisonke.Web.Services.Dto;

namespace Sisonke.Web.Services;

public class ClaimEligibilityService(
    ApplicationDbContext context,
    StokvelOperatingRulesService stokvelOperatingRulesService,
    FuneralClaimService funeralClaimService)
{
    public async Task<ClaimEligibilityAssessmentDto?> AssessClaimEligibilityAsync(Guid claimId)
    {
        var claim = await context.FuneralClaims
            .Include(existingClaim => existingClaim.Member)
            .Include(existingClaim => existingClaim.Dependent)
            .Include(existingClaim => existingClaim.Documents)
            .FirstOrDefaultAsync(existingClaim => existingClaim.Id == claimId);

        if (claim is null)
        {
            return null;
        }

        var stokvel = await context.Stokvels
            .Where(existingStokvel => existingStokvel.TenantId == claim.TenantId)
            .OrderBy(existingStokvel => existingStokvel.CreatedAt)
            .ThenBy(existingStokvel => existingStokvel.Name)
            .FirstOrDefaultAsync();

        if (stokvel is null)
        {
            return BuildUnresolvedStokvelAssessment(claim);
        }

        var rules = await stokvelOperatingRulesService.GetOrCreateDefaultRulesAsync(
            stokvel.Id,
            stokvel.Type.ToString(),
            null);

        var assessment = new ClaimEligibilityAssessmentDto
        {
            ClaimId = claim.Id,
            MemberId = claim.MemberId,
            DependentId = claim.DependentId,
            StokvelId = stokvel.Id,
            MemberName = claim.Member?.FullName ?? string.Empty,
            ClaimSubjectName = claim.DeceasedFullName,
            IsDependentClaim = claim.SubjectType == FuneralClaimSubjectType.Dependent,
            WaitingPeriodSatisfied = claim.IsWaitingPeriodSatisfied,
            HasRequiredDocuments = HasDeathCertificate(claim),
            AssessedAt = DateTime.UtcNow
        };

        AssessClaimsEnabled(rules, assessment);
        AssessMemberStatus(claim, rules, assessment);
        AssessDependent(claim, rules, assessment);
        AssessWaitingPeriod(claim, rules, assessment);
        await AssessContributionStandingAsync(claim, rules, assessment);
        await AssessFinesAsync(claim, assessment);
        await AssessRequiredDocumentsAsync(claim, rules, assessment);
        await AssessDuplicateClaimsAsync(claim, assessment);
        FinalizeEligibility(assessment);

        return assessment;
    }

    private static ClaimEligibilityAssessmentDto BuildUnresolvedStokvelAssessment(FuneralClaim claim)
    {
        var assessment = new ClaimEligibilityAssessmentDto
        {
            ClaimId = claim.Id,
            MemberId = claim.MemberId,
            DependentId = claim.DependentId,
            MemberName = claim.Member?.FullName ?? string.Empty,
            ClaimSubjectName = claim.DeceasedFullName,
            IsDependentClaim = claim.SubjectType == FuneralClaimSubjectType.Dependent,
            WaitingPeriodSatisfied = claim.IsWaitingPeriodSatisfied,
            HasRequiredDocuments = HasDeathCertificate(claim),
            AssessedAt = DateTime.UtcNow
        };

        assessment.FailedChecks.Add("Stokvel operating rules could not be resolved for this claim.");
        FinalizeEligibility(assessment);

        return assessment;
    }

    private static void AssessClaimsEnabled(StokvelOperatingRules rules, ClaimEligibilityAssessmentDto assessment)
    {
        if (rules.EnableClaims)
        {
            assessment.PassedChecks.Add("Claims are enabled for this stokvel.");
            return;
        }

        assessment.FailedChecks.Add("Claims are not enabled for this stokvel type.");
    }

    private static void AssessMemberStatus(
        FuneralClaim claim,
        StokvelOperatingRules rules,
        ClaimEligibilityAssessmentDto assessment)
    {
        if (claim.Member is null)
        {
            assessment.Warnings.Add("Member status could not be fully verified.");
            return;
        }

        assessment.IsMemberSuspended =
            claim.Member.Status == MemberStatus.Suspended ||
            claim.Member.GovernanceStatus == MemberGovernanceStatus.Suspended ||
            claim.Member.SuspendedAt is not null;

        assessment.IsMemberActive =
            claim.Member.Status == MemberStatus.Active &&
            claim.Member.GovernanceStatus is MemberGovernanceStatus.Active or MemberGovernanceStatus.Warning &&
            !claim.Member.IsDeceased &&
            !assessment.IsMemberSuspended;

        if (assessment.IsMemberSuspended)
        {
            if (rules.BlockClaimsIfMemberSuspended)
            {
                assessment.FailedChecks.Add("Member is suspended and claim rules block suspended members.");
            }
            else
            {
                assessment.Warnings.Add("Member is suspended, but rules do not automatically block the claim.");
            }

            return;
        }

        if (assessment.IsMemberActive)
        {
            assessment.PassedChecks.Add("Member is active.");
            return;
        }

        if (claim.Member.IsDeceased || claim.Member.Status == MemberStatus.Deceased || claim.Member.GovernanceStatus == MemberGovernanceStatus.Deceased)
        {
            assessment.Warnings.Add("Member is marked deceased. Confirm that this claim subject is correct.");
            return;
        }

        assessment.Warnings.Add($"Member status requires review: {claim.Member.Status} / {claim.Member.GovernanceStatus}.");
    }

    private static void AssessDependent(
        FuneralClaim claim,
        StokvelOperatingRules rules,
        ClaimEligibilityAssessmentDto assessment)
    {
        if (!assessment.IsDependentClaim)
        {
            assessment.IsDependentCovered = true;
            assessment.PassedChecks.Add("Claim is for the member, so dependent cover is not required.");
            return;
        }

        if (!rules.EnableDependents)
        {
            assessment.IsDependentCovered = false;
            assessment.FailedChecks.Add("Dependents are not enabled for this stokvel.");
            return;
        }

        if (claim.DependentId is null || claim.Dependent is null)
        {
            assessment.IsDependentCovered = false;
            assessment.FailedChecks.Add("Dependent record could not be found.");
            return;
        }

        if (claim.Dependent.MemberId != claim.MemberId)
        {
            assessment.IsDependentCovered = false;
            assessment.FailedChecks.Add("Dependent does not belong to the claiming member.");
            return;
        }

        assessment.IsDependentCovered = claim.Dependent.IsActive || claim.Dependent.IsDeceased;

        if (assessment.IsDependentCovered)
        {
            assessment.PassedChecks.Add("Dependent is linked to member.");
        }
        else
        {
            assessment.Warnings.Add("Dependent is linked to the member but is not currently marked active.");
        }
    }

    private static void AssessWaitingPeriod(
        FuneralClaim claim,
        StokvelOperatingRules rules,
        ClaimEligibilityAssessmentDto assessment)
    {
        var requiredMonths = assessment.IsDependentClaim
            ? rules.DependentWaitingPeriodMonths
            : rules.MemberWaitingPeriodMonths;

        if (claim.IsWaitingPeriodSatisfied)
        {
            assessment.WaitingPeriodSatisfied = true;
            assessment.PassedChecks.Add("Waiting period appears satisfied.");
        }
        else if (requiredMonths <= 0)
        {
            assessment.WaitingPeriodSatisfied = true;
            assessment.PassedChecks.Add("No waiting period is configured for this claim type.");
        }
        else
        {
            assessment.WaitingPeriodSatisfied = false;
            assessment.Warnings.Add("Waiting period has not been satisfied.");
        }

        if (claim.IsMemberStatusEligible)
        {
            assessment.PassedChecks.Add("Existing member status eligibility flag is satisfied.");
        }
        else
        {
            assessment.Warnings.Add("Existing member status eligibility flag is not satisfied.");
        }
    }

    private async Task AssessContributionStandingAsync(
        FuneralClaim claim,
        StokvelOperatingRules rules,
        ClaimEligibilityAssessmentDto assessment)
    {
        try
        {
            assessment.OutstandingContributionBalance = await context.MemberContributions
                .Where(contribution =>
                    contribution.MemberId == claim.MemberId &&
                    contribution.TenantId == claim.TenantId &&
                    contribution.OutstandingAmount > 0 &&
                    contribution.Status != PaymentStatus.Paid &&
                    contribution.Status != PaymentStatus.Exempted &&
                    contribution.Status != PaymentStatus.WrittenOff &&
                    contribution.Status != PaymentStatus.Reversed)
                .SumAsync(contribution => contribution.OutstandingAmount);
        }
        catch (InvalidOperationException)
        {
            assessment.Warnings.Add("Contribution standing could not be fully verified.");
            return;
        }

        assessment.HasOutstandingContributionArrears = assessment.OutstandingContributionBalance > 0;

        if (!assessment.HasOutstandingContributionArrears)
        {
            assessment.PassedChecks.Add("No outstanding contribution arrears were found.");
            return;
        }

        if (rules.BlockClaimsIfMemberInArrears)
        {
            assessment.FailedChecks.Add("Member has outstanding contribution arrears and rules block claims in arrears.");
        }
        else
        {
            assessment.Warnings.Add("Member has outstanding contribution arrears but rules do not automatically block claim.");
        }
    }

    private async Task AssessFinesAsync(FuneralClaim claim, ClaimEligibilityAssessmentDto assessment)
    {
        assessment.OutstandingFinesBalance = await context.MemberFines
            .Where(fine =>
                fine.MemberId == claim.MemberId &&
                fine.TenantId == claim.TenantId &&
                fine.Status == FineStatus.Unpaid)
            .SumAsync(fine => fine.Amount);

        assessment.HasOutstandingFines = assessment.OutstandingFinesBalance > 0;

        if (assessment.HasOutstandingFines)
        {
            assessment.Warnings.Add("Member has outstanding fines.");
        }
        else
        {
            assessment.PassedChecks.Add("No unpaid fines were found.");
        }
    }

    private async Task AssessRequiredDocumentsAsync(
        FuneralClaim claim,
        StokvelOperatingRules rules,
        ClaimEligibilityAssessmentDto assessment)
    {
        var checklist = await funeralClaimService.GetClaimDocumentChecklistAsync(claim.Id);
        var missingRequiredDocuments = checklist
            .Where(item => item.IsRequired && !item.IsSubmitted)
            .Select(item => item.DocumentType)
            .ToList();

        assessment.HasRequiredDocuments = missingRequiredDocuments.Count == 0;

        if (missingRequiredDocuments.Count == 0)
        {
            assessment.PassedChecks.Add("Required documents appear captured.");
            return;
        }

        if (rules.RequireDeathCertificateForClaims &&
            missingRequiredDocuments.Contains("Death Certificate", StringComparer.OrdinalIgnoreCase))
        {
            assessment.FailedChecks.Add("Required death certificate is missing.");
        }

        if (rules.RequireClaimDocuments)
        {
            var missingDocuments = string.Join(", ", missingRequiredDocuments);
            assessment.Warnings.Add($"Required claim documents may be missing: {missingDocuments}.");
            return;
        }
    }

    private async Task AssessDuplicateClaimsAsync(FuneralClaim claim, ClaimEligibilityAssessmentDto assessment)
    {
        var activeStatuses = new[]
        {
            FuneralClaimStatus.Draft,
            FuneralClaimStatus.Submitted,
            FuneralClaimStatus.UnderReview,
            FuneralClaimStatus.OnHold,
            FuneralClaimStatus.Approved
        };

        IQueryable<FuneralClaim> duplicateQuery = context.FuneralClaims
            .Where(existingClaim =>
                existingClaim.Id != claim.Id &&
                existingClaim.MemberId == claim.MemberId &&
                activeStatuses.Contains(existingClaim.Status));

        if (claim.SubjectType == FuneralClaimSubjectType.Dependent)
        {
            duplicateQuery = duplicateQuery.Where(existingClaim =>
                existingClaim.DependentId == claim.DependentId &&
                existingClaim.DependentId != null);
        }
        else
        {
            var subjectName = claim.DeceasedFullName.Trim().ToUpper();
            duplicateQuery = duplicateQuery.Where(existingClaim =>
                existingClaim.SubjectType == FuneralClaimSubjectType.Member &&
                existingClaim.DependentId == null &&
                existingClaim.DeceasedFullName.ToUpper() == subjectName);
        }

        assessment.HasDuplicateActiveClaim = await duplicateQuery.AnyAsync();

        if (assessment.HasDuplicateActiveClaim)
        {
            assessment.FailedChecks.Add("Duplicate active claim exists for the same subject.");
        }
        else
        {
            assessment.PassedChecks.Add("No duplicate active claim found.");
        }
    }

    private static bool HasDeathCertificate(FuneralClaim claim)
    {
        return claim.Documents.Any(document => document.DocumentType == ClaimDocumentType.DeathCertificate);
    }

    private static void FinalizeEligibility(ClaimEligibilityAssessmentDto assessment)
    {
        if (assessment.FailedChecks.Count > 0)
        {
            assessment.IsEligible = false;
            assessment.EligibilityStatus = "NotEligible";
            return;
        }

        assessment.IsEligible = true;

        if (assessment.Warnings.Count == 0)
        {
            assessment.EligibilityStatus = "Eligible";
            return;
        }

        assessment.EligibilityStatus = assessment.Warnings.Any(IsManualReviewWarning)
            ? "RequiresReview"
            : "EligibleWithWarnings";
    }

    private static bool IsManualReviewWarning(string warning)
    {
        return warning.Contains("suspended", StringComparison.OrdinalIgnoreCase) ||
            warning.Contains("status requires review", StringComparison.OrdinalIgnoreCase) ||
            warning.Contains("marked deceased", StringComparison.OrdinalIgnoreCase) ||
            warning.Contains("waiting period", StringComparison.OrdinalIgnoreCase) ||
            warning.Contains("eligibility flag", StringComparison.OrdinalIgnoreCase) ||
            warning.Contains("could not be fully verified", StringComparison.OrdinalIgnoreCase);
    }
}
