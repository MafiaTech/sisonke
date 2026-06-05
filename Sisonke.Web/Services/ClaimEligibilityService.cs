using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;
using Sisonke.Web.Services.Dto;

namespace Sisonke.Web.Services;

public class ClaimEligibilityService(ApplicationDbContext context)
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

        var assessment = new ClaimEligibilityAssessmentDto
        {
            ClaimId = claim.Id,
            MemberId = claim.MemberId,
            DependentId = claim.DependentId,
            MemberName = claim.Member?.FullName ?? string.Empty,
            ClaimSubjectName = claim.DeceasedFullName,
            IsDependentClaim = claim.SubjectType == FuneralClaimSubjectType.Dependent,
            WaitingPeriodSatisfied = claim.IsWaitingPeriodSatisfied,
            HasRequiredDocuments = claim.Documents.Any(document => document.DocumentType == ClaimDocumentType.DeathCertificate),
            AssessedAt = DateTime.UtcNow
        };

        AssessMemberStatus(claim, assessment);
        AssessDependent(claim, assessment);
        AssessWaitingPeriod(claim, assessment);
        await AssessContributionStandingAsync(claim, assessment);
        await AssessFinesAsync(claim, assessment);
        AssessRequiredDocuments(assessment);
        await AssessDuplicateClaimsAsync(claim, assessment);
        FinalizeEligibility(assessment);

        return assessment;
    }

    private static void AssessMemberStatus(FuneralClaim claim, ClaimEligibilityAssessmentDto assessment)
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

        if (assessment.IsMemberActive)
        {
            assessment.PassedChecks.Add("Member is active for claim assessment.");
            return;
        }

        if (assessment.IsMemberSuspended)
        {
            assessment.Warnings.Add("Member is currently suspended and requires executive review before approval.");
        }
        else if (claim.Member.IsDeceased || claim.Member.Status == MemberStatus.Deceased || claim.Member.GovernanceStatus == MemberGovernanceStatus.Deceased)
        {
            assessment.Warnings.Add("Member is marked deceased. Confirm that this claim subject is correct.");
        }
        else
        {
            assessment.Warnings.Add($"Member status requires review: {claim.Member.Status} / {claim.Member.GovernanceStatus}.");
        }
    }

    private static void AssessDependent(FuneralClaim claim, ClaimEligibilityAssessmentDto assessment)
    {
        if (!assessment.IsDependentClaim)
        {
            assessment.IsDependentCovered = true;
            assessment.PassedChecks.Add("Claim is for the member, so dependent cover is not required.");
            return;
        }

        if (claim.DependentId is null || claim.Dependent is null)
        {
            assessment.IsDependentCovered = false;
            assessment.FailedChecks.Add("Dependent record could not be found for this claim.");
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
            assessment.PassedChecks.Add("Dependent is linked to this member and appears covered.");
        }
        else
        {
            assessment.Warnings.Add("Dependent is linked to the member but is not currently marked active.");
        }
    }

    private static void AssessWaitingPeriod(FuneralClaim claim, ClaimEligibilityAssessmentDto assessment)
    {
        if (claim.IsWaitingPeriodSatisfied)
        {
            assessment.PassedChecks.Add("Waiting period is marked as satisfied.");
        }
        else
        {
            assessment.Warnings.Add("Waiting period is not marked as satisfied. Chairperson review is required.");
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

    private async Task AssessContributionStandingAsync(FuneralClaim claim, ClaimEligibilityAssessmentDto assessment)
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

        assessment.HasOutstandingContributionArrears = assessment.OutstandingContributionBalance > 0;

        if (assessment.HasOutstandingContributionArrears)
        {
            assessment.Warnings.Add($"Outstanding contribution balance: {assessment.OutstandingContributionBalance:C}.");
        }
        else
        {
            assessment.PassedChecks.Add("No outstanding contribution arrears were found.");
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
            assessment.Warnings.Add($"Outstanding fines balance: {assessment.OutstandingFinesBalance:C}.");
        }
        else
        {
            assessment.PassedChecks.Add("No unpaid fines were found.");
        }
    }

    private static void AssessRequiredDocuments(ClaimEligibilityAssessmentDto assessment)
    {
        if (assessment.HasRequiredDocuments)
        {
            assessment.PassedChecks.Add("Death certificate has been uploaded.");
        }
        else
        {
            assessment.Warnings.Add("Death certificate has not been uploaded.");
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
            var subjectName = claim.DeceasedFullName.Trim();
            duplicateQuery = duplicateQuery.Where(existingClaim =>
                existingClaim.SubjectType == FuneralClaimSubjectType.Member &&
                existingClaim.DependentId == null &&
                existingClaim.DeceasedFullName == subjectName);
        }

        assessment.HasDuplicateActiveClaim = await duplicateQuery.AnyAsync();

        if (assessment.HasDuplicateActiveClaim)
        {
            assessment.FailedChecks.Add("Another active claim exists for the same claim subject.");
        }
        else
        {
            assessment.PassedChecks.Add("No duplicate active claim was found.");
        }
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
        assessment.EligibilityStatus = assessment.Warnings.Count > 0
            ? "RequiresReview"
            : "Eligible";
    }
}
