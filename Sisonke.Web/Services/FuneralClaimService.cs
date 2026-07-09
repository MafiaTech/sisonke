using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;
using Sisonke.Web.Services.Dto;

namespace Sisonke.Web.Services;

public class FuneralClaimService(
    ApplicationDbContext context,
    OperatingRuleService operatingRuleService,
    StokvelOperatingRulesService stokvelOperatingRulesService,
    AuditLogService auditLogService,
    IWebHostEnvironment webHostEnvironment)
{
    private const long MaxClaimDocumentUploadSize = 10 * 1024 * 1024;
    private static readonly string[] AllowedClaimDocumentExtensions = [".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx"];

    public async Task<List<FuneralClaim>> GetClaimsByStokvelIdAsync(Guid stokvelId)
    {
        var stokvel = await context.Stokvels
            .Where(existingStokvel => existingStokvel.Id == stokvelId)
            .OrderBy(existingStokvel => existingStokvel.CreatedAt)
            .ThenBy(existingStokvel => existingStokvel.Name)
            .FirstOrDefaultAsync();

        if (stokvel is null || !IsClaimsAvailable(stokvel))
        {
            return [];
        }

        return await context.FuneralClaims
            .Include(claim => claim.Member)
            .Include(claim => claim.Dependent)
            .Include(claim => claim.Documents)
            .Where(claim => claim.TenantId == stokvel.TenantId)
            .OrderByDescending(claim => claim.CreatedAt)
            .ToListAsync();
    }

    public async Task<int> GetOpenClaimCountByStokvelIdAsync(Guid stokvelId)
    {
        var stokvel = await context.Stokvels
            .Where(existingStokvel => existingStokvel.Id == stokvelId)
            .OrderBy(existingStokvel => existingStokvel.CreatedAt)
            .ThenBy(existingStokvel => existingStokvel.Name)
            .FirstOrDefaultAsync();

        if (stokvel is null || !IsClaimsAvailable(stokvel))
        {
            return 0;
        }

        return await context.FuneralClaims
            .CountAsync(claim =>
                claim.Member.TenantId == stokvel.TenantId &&
                claim.Status != FuneralClaimStatus.Paid &&
                claim.Status != FuneralClaimStatus.Rejected &&
                claim.Status != FuneralClaimStatus.Cancelled);
    }

    public async Task<List<FuneralClaim>> GetClaimsByMemberIdAsync(Guid memberId)
    {
        return await context.FuneralClaims
            .Include(claim => claim.Dependent)
            .Include(claim => claim.Documents)
            .Where(claim => claim.MemberId == memberId)
            .OrderByDescending(claim => claim.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<FuneralClaim>> GetClaimsRequiringSecretaryReviewByStokvelIdAsync(Guid stokvelId)
    {
        var stokvel = await context.Stokvels
            .Where(existingStokvel => existingStokvel.Id == stokvelId)
            .OrderBy(existingStokvel => existingStokvel.CreatedAt)
            .ThenBy(existingStokvel => existingStokvel.Name)
            .FirstOrDefaultAsync();

        if (stokvel is null || !IsClaimsAvailable(stokvel))
        {
            return [];
        }

        return await context.FuneralClaims
            .Include(claim => claim.Member)
            .Include(claim => claim.Dependent)
            .Include(claim => claim.Documents)
            .Where(claim =>
                claim.Member.TenantId == stokvel.TenantId &&
                claim.SecretaryReviewedAt == null &&
                (claim.Status == FuneralClaimStatus.Submitted ||
                    claim.Status == FuneralClaimStatus.UnderReview ||
                    claim.Status == FuneralClaimStatus.OnHold))
            .OrderBy(claim => claim.CreatedAt)
            .ToListAsync();
    }

    public async Task<int> GetClaimsRequiringSecretaryReviewCountByStokvelIdAsync(Guid stokvelId)
    {
        var stokvel = await context.Stokvels
            .Where(existingStokvel => existingStokvel.Id == stokvelId)
            .OrderBy(existingStokvel => existingStokvel.CreatedAt)
            .ThenBy(existingStokvel => existingStokvel.Name)
            .FirstOrDefaultAsync();

        if (stokvel is null || !IsClaimsAvailable(stokvel))
        {
            return 0;
        }

        return await context.FuneralClaims
            .CountAsync(claim =>
                claim.Member.TenantId == stokvel.TenantId &&
                claim.SecretaryReviewedAt == null &&
                (claim.Status == FuneralClaimStatus.Submitted ||
                    claim.Status == FuneralClaimStatus.UnderReview ||
                    claim.Status == FuneralClaimStatus.OnHold));
    }

    public Task<int> GetSecretaryReviewRequiredCountByStokvelIdAsync(Guid stokvelId)
    {
        return GetClaimsRequiringSecretaryReviewCountByStokvelIdAsync(stokvelId);
    }

    public async Task<List<FuneralClaim>> GetClaimsRequiringChairpersonApprovalByStokvelIdAsync(Guid stokvelId)
    {
        var stokvel = await context.Stokvels
            .Where(existingStokvel => existingStokvel.Id == stokvelId)
            .OrderBy(existingStokvel => existingStokvel.CreatedAt)
            .ThenBy(existingStokvel => existingStokvel.Name)
            .FirstOrDefaultAsync();

        if (stokvel is null || !IsClaimsAvailable(stokvel))
        {
            return [];
        }

        return await context.FuneralClaims
            .Include(claim => claim.Member)
            .Include(claim => claim.Dependent)
            .Include(claim => claim.Documents)
            .Where(claim =>
                claim.Member.TenantId == stokvel.TenantId &&
                claim.SecretaryReviewedAt != null &&
                claim.ChairpersonDecisionAt == null &&
                claim.Status == FuneralClaimStatus.UnderReview)
            .OrderBy(claim => claim.SecretaryReviewedAt)
            .ThenBy(claim => claim.CreatedAt)
            .ToListAsync();
    }

    public async Task<int> GetClaimsRequiringChairpersonApprovalCountByStokvelIdAsync(Guid stokvelId)
    {
        var stokvel = await context.Stokvels
            .Where(existingStokvel => existingStokvel.Id == stokvelId)
            .OrderBy(existingStokvel => existingStokvel.CreatedAt)
            .ThenBy(existingStokvel => existingStokvel.Name)
            .FirstOrDefaultAsync();

        if (stokvel is null || !IsClaimsAvailable(stokvel))
        {
            return 0;
        }

        return await context.FuneralClaims
            .CountAsync(claim =>
                claim.Member.TenantId == stokvel.TenantId &&
                claim.SecretaryReviewedAt != null &&
                claim.ChairpersonDecisionAt == null &&
                claim.Status == FuneralClaimStatus.UnderReview);
    }

    public async Task<List<FuneralClaim>> GetApprovedClaimsAwaitingPayoutByStokvelIdAsync(Guid stokvelId)
    {
        var stokvel = await context.Stokvels
            .Where(existingStokvel => existingStokvel.Id == stokvelId)
            .OrderBy(existingStokvel => existingStokvel.CreatedAt)
            .ThenBy(existingStokvel => existingStokvel.Name)
            .FirstOrDefaultAsync();

        if (stokvel is null || !IsClaimsAvailable(stokvel))
        {
            return [];
        }

        return await context.FuneralClaims
            .Include(claim => claim.Member)
            .Include(claim => claim.Dependent)
            .Include(claim => claim.Documents)
            .Where(claim =>
                claim.Member.TenantId == stokvel.TenantId &&
                claim.Status == FuneralClaimStatus.Approved &&
                claim.ChairpersonDecisionAt != null &&
                claim.ApprovedAt != null &&
                claim.PayoutPaidAt == null)
            .OrderBy(claim => claim.ApprovedAt ?? claim.ChairpersonDecisionAt ?? claim.SubmittedAt ?? claim.CreatedAt)
            .ThenBy(claim => claim.CreatedAt)
            .ToListAsync();
    }

    public async Task<int> GetApprovedClaimsAwaitingPayoutCountByStokvelIdAsync(Guid stokvelId)
    {
        var stokvel = await context.Stokvels
            .Where(existingStokvel => existingStokvel.Id == stokvelId)
            .OrderBy(existingStokvel => existingStokvel.CreatedAt)
            .ThenBy(existingStokvel => existingStokvel.Name)
            .FirstOrDefaultAsync();

        if (stokvel is null || !IsClaimsAvailable(stokvel))
        {
            return 0;
        }

        return await context.FuneralClaims
            .CountAsync(claim =>
                claim.Member.TenantId == stokvel.TenantId &&
                claim.Status == FuneralClaimStatus.Approved &&
                claim.ChairpersonDecisionAt != null &&
                claim.ApprovedAt != null &&
                claim.PayoutPaidAt == null);
    }

    public async Task<FuneralClaim?> GetClaimByIdAsync(Guid claimId)
    {
        return await context.FuneralClaims
            .Include(claim => claim.Tenant)
            .Include(claim => claim.Member)
            .Include(claim => claim.Dependent)
            .Include(claim => claim.Documents)
            .SingleOrDefaultAsync(claim => claim.Id == claimId);
    }

    public async Task<bool> HasActiveClaimForDependentAsync(Guid dependentId)
    {
        return await context.FuneralClaims
            .AnyAsync(claim =>
                claim.DependentId == dependentId &&
                claim.Status != FuneralClaimStatus.Rejected &&
                claim.Status != FuneralClaimStatus.Cancelled);
    }

    public async Task<FuneralClaim?> GetActiveClaimForDependentAsync(Guid dependentId)
    {
        return await context.FuneralClaims
            .Include(claim => claim.Documents)
            .Where(claim =>
                claim.DependentId == dependentId &&
                claim.Status != FuneralClaimStatus.Rejected &&
                claim.Status != FuneralClaimStatus.Cancelled)
            .OrderByDescending(claim => claim.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<Dictionary<Guid, FuneralClaim>> GetActiveDependentClaimsByMemberIdAsync(Guid memberId)
    {
        var claims = await context.FuneralClaims
            .Include(claim => claim.Documents)
            .Where(claim =>
                claim.MemberId == memberId &&
                claim.DependentId != null &&
                claim.Status != FuneralClaimStatus.Rejected &&
                claim.Status != FuneralClaimStatus.Cancelled)
            .OrderByDescending(claim => claim.CreatedAt)
            .ToListAsync();

        return claims
            .GroupBy(claim => claim.DependentId!.Value)
            .ToDictionary(group => group.Key, group => group.First());
    }

    public async Task<bool> HasDeathCertificateAsync(Guid claimId)
    {
        return await context.FuneralClaimDocuments
            .AnyAsync(document =>
                document.FuneralClaimId == claimId &&
                document.DocumentType == ClaimDocumentType.DeathCertificate);
    }

    public async Task<List<ClaimDocumentChecklistItemDto>> GetClaimDocumentChecklistAsync(Guid claimId)
    {
        var claim = await context.FuneralClaims
            .Include(existingClaim => existingClaim.Documents)
            .FirstOrDefaultAsync(existingClaim => existingClaim.Id == claimId);

        if (claim is null)
        {
            return [];
        }

        var stokvel = await context.Stokvels
            .Where(existingStokvel => existingStokvel.TenantId == claim.TenantId)
            .OrderBy(existingStokvel => existingStokvel.CreatedAt)
            .ThenBy(existingStokvel => existingStokvel.Name)
            .FirstOrDefaultAsync();

        var requireDeathCertificate = true;
        var requireClaimDocuments = true;

        if (stokvel is not null)
        {
            var rules = await stokvelOperatingRulesService.GetOrCreateDefaultRulesAsync(
                stokvel.Id,
                stokvel.Type.ToString(),
                null);
            requireDeathCertificate = rules.RequireDeathCertificateForClaims;
            requireClaimDocuments = rules.RequireClaimDocuments;
        }

        var checklist = new List<ClaimDocumentChecklistItemDto>
        {
            BuildChecklistItem(
                "Death Certificate",
                "Official death certificate for the deceased person.",
                requireDeathCertificate,
                claim.Documents,
                [ClaimDocumentType.DeathCertificate],
                ["death", "certificate"]),
            BuildChecklistItem(
                "Claim Form",
                "Completed society claim form for this claim reference.",
                requireClaimDocuments,
                claim.Documents,
                [ClaimDocumentType.Other],
                ["claim", "form"]),
            BuildChecklistItem(
                "Member ID Copy",
                "Identity document copy for the claiming member.",
                requireClaimDocuments,
                claim.Documents,
                [ClaimDocumentType.IdCopy],
                ["member", "id"]),
            BuildChecklistItem(
                "Deceased ID Copy",
                "Identity document copy for the deceased person.",
                requireClaimDocuments,
                claim.Documents,
                [ClaimDocumentType.IdCopy],
                ["deceased", "id"]),
            BuildChecklistItem(
                "Bank Confirmation / Payment Details",
                "Bank confirmation or payment details for payout processing.",
                requireClaimDocuments,
                claim.Documents,
                [ClaimDocumentType.ProofOfPayment],
                ["bank", "payment", "confirmation"])
        };

        if (claim.SubjectType == FuneralClaimSubjectType.Dependent)
        {
            checklist.Insert(4, BuildChecklistItem(
                "Proof of Relationship",
                "Proof that the deceased dependent is covered under this member.",
                requireClaimDocuments,
                claim.Documents,
                [ClaimDocumentType.ProofOfMembership, ClaimDocumentType.Other],
                ["relationship", "dependent", "cover"]));
        }

        return checklist;
    }

    public async Task<bool> IsWaitingPeriodSatisfiedForMemberAsync(Guid memberId)
    {
        var member = await context.Members
            .SingleOrDefaultAsync(existingMember => existingMember.Id == memberId);

        if (member is null)
        {
            return false;
        }

        var stokvel = await context.Stokvels
            .SingleOrDefaultAsync(existingStokvel => existingStokvel.TenantId == member.TenantId);

        if (stokvel is null || member.JoiningDate == default)
        {
            return false;
        }

        var waitingPeriodMonths = await operatingRuleService.GetWaitingPeriodMonthsAsync(stokvel.Id);

        return member.JoiningDate.AddMonths(waitingPeriodMonths) <= DateTime.Today;
    }

    public async Task<bool> IsMemberStatusEligibleForClaimAsync(Guid memberId)
    {
        var member = await context.Members
            .SingleOrDefaultAsync(existingMember => existingMember.Id == memberId);

        if (member is null)
        {
            return false;
        }

        return member.GovernanceStatus is not (
            MemberGovernanceStatus.Suspended or
            MemberGovernanceStatus.Expelled or
            MemberGovernanceStatus.Deceased);
    }

    public async Task<FuneralClaim?> CreateClaimAsync(
        Guid memberId,
        FuneralClaimSubjectType subjectType,
        Guid? dependentId,
        DateTime? dateOfDeath,
        string? claimReason,
        ClaimType claimType = ClaimType.Funeral)
    {
        var member = await context.Members
            .Include(existingMember => existingMember.Tenant)
            .FirstOrDefaultAsync(existingMember => existingMember.Id == memberId);

        if (member is null)
        {
            return null;
        }

        var stokvel = await context.Stokvels
            .Where(existingStokvel => existingStokvel.TenantId == member.TenantId)
            .OrderBy(existingStokvel => existingStokvel.CreatedAt)
            .ThenBy(existingStokvel => existingStokvel.Name)
            .FirstOrDefaultAsync();

        if (stokvel is null || !IsClaimsAvailable(stokvel))
        {
            return null;
        }

        var rules = await stokvelOperatingRulesService.GetOrCreateDefaultRulesAsync(
            stokvel.Id,
            stokvel.Type.ToString(),
            null);

        if (subjectType == FuneralClaimSubjectType.Dependent && !IsDependentsAvailable(stokvel))
        {
            return null;
        }

        var deceasedFullName = member.FullName;
        Guid? claimDependentId = null;

        if (subjectType == FuneralClaimSubjectType.Dependent)
        {
            if (dependentId is null)
            {
                return null;
            }

            var dependent = await context.MemberDependents
                .SingleOrDefaultAsync(existingDependent =>
                    existingDependent.Id == dependentId.Value &&
                    existingDependent.MemberId == member.Id);

            if (dependent is null)
            {
                return null;
            }

            deceasedFullName = dependent.FullName;
            claimDependentId = dependent.Id;

            if (dateOfDeath is not null)
            {
                dependent.IsDeceased = true;
                dependent.IsActive = false;
                dependent.CoverageStatus = DependentCoverageStatus.Removed;
                dependent.DeceasedDate = dateOfDeath;
                dependent.DeathReportedAt ??= DateTime.UtcNow;
            }
        }

        var isWaitingPeriodSatisfied = await IsWaitingPeriodSatisfiedForMemberAsync(member.Id);
        var isMemberStatusEligible = await IsMemberStatusEligibleForClaimAsync(member.Id);
        var status = !isMemberStatusEligible || !isWaitingPeriodSatisfied
            ? FuneralClaimStatus.OnHold
            : FuneralClaimStatus.Draft;
        var createdAt = DateTime.UtcNow;

        var claim = new FuneralClaim
        {
            Id = Guid.NewGuid(),
            TenantId = member.TenantId,
            StokvelId = stokvel.Id,
            MemberId = member.Id,
            ClaimType = claimType,
            SubjectType = subjectType,
            DependentId = claimDependentId,
            DeceasedFullName = deceasedFullName,
            DateOfDeath = dateOfDeath,
            ClaimReason = claimReason,
            Status = status,
            ClaimReference = await GenerateClaimReferenceAsync(member.TenantId, createdAt),
            IsWaitingPeriodSatisfied = isWaitingPeriodSatisfied,
            IsMemberStatusEligible = isMemberStatusEligible,
            PayoutAmount = rules.DefaultClaimPayoutAmount > 0 ? rules.DefaultClaimPayoutAmount : null,
            CreatedAt = createdAt
        };

        context.FuneralClaims.Add(claim);
        await context.SaveChangesAsync();
        await auditLogService.RecordAsync(
            member.ApplicationUserId,
            stokvel.Id,
            "ClaimCreated",
            nameof(FuneralClaim),
            claim.Id,
            $"Created {claim.ClaimType} claim {claim.ClaimReference} for {claim.DeceasedFullName}.");

        return claim;
    }

    public async Task<FuneralClaim?> SubmitClaimAsync(Guid claimId, string? submittedByUserId = null)
    {
        var claim = await context.FuneralClaims
            .Include(existingClaim => existingClaim.Member)
            .SingleOrDefaultAsync(existingClaim => existingClaim.Id == claimId);

        if (claim is null)
        {
            return null;
        }

        if (!await HasDeathCertificateAsync(claimId))
        {
            return null;
        }

        if (claim.SubjectType == FuneralClaimSubjectType.Dependent && claim.DependentId is not null)
        {
            var dependent = await context.MemberDependents
                .SingleOrDefaultAsync(existingDependent => existingDependent.Id == claim.DependentId.Value);

            if (dependent is not null)
            {
                dependent.IsDeceased = true;
                dependent.IsActive = false;
                dependent.CoverageStatus = DependentCoverageStatus.Removed;
                dependent.DeceasedDate = claim.DateOfDeath;
                dependent.DeathReportedAt ??= DateTime.UtcNow;
            }
        }

        if (claim.Status != FuneralClaimStatus.OnHold)
        {
            claim.Status = FuneralClaimStatus.Submitted;
        }

        if (string.IsNullOrWhiteSpace(claim.ClaimReference))
        {
            claim.ClaimReference = await GenerateClaimReferenceAsync(claim.TenantId, claim.CreatedAt);
        }

        claim.SubmittedAt = DateTime.UtcNow;
        claim.SubmittedByName ??= "Member";

        await context.SaveChangesAsync();
        await auditLogService.RecordAsync(
            submittedByUserId ?? claim.Member.ApplicationUserId,
            await GetClaimStokvelIdAsync(claim),
            "ClaimSubmitted",
            nameof(FuneralClaim),
            claim.Id,
            $"Submitted claim {claim.ClaimReference ?? claim.Id.ToString("N")[..8]} for secretary review.");

        return claim;
    }

    public async Task<FuneralClaim?> ReviewClaimAsSecretaryAsync(
        Guid claimId,
        bool recommendApproval,
        string? reviewNotes,
        string? reviewedByName)
    {
        var claim = await context.FuneralClaims
            .SingleOrDefaultAsync(existingClaim => existingClaim.Id == claimId);

        if (claim is null)
        {
            return null;
        }

        if (claim.Status is not (
            FuneralClaimStatus.Submitted or
            FuneralClaimStatus.UnderReview or
            FuneralClaimStatus.OnHold))
        {
            return null;
        }

        claim.SecretaryRecommendedApproval = recommendApproval;
        claim.SecretaryReviewNotes = reviewNotes;
        claim.SecretaryReviewedByName = reviewedByName ?? "Secretary";
        claim.SecretaryReviewedAt = DateTime.UtcNow;
        claim.Status = FuneralClaimStatus.UnderReview;

        await context.SaveChangesAsync();

        return claim;
    }

    public async Task<bool> CompleteSecretaryReviewAsync(
        Guid claimId,
        string secretaryNotes,
        bool recommendApproval,
        Guid reviewedByMemberId)
    {
        if (string.IsNullOrWhiteSpace(secretaryNotes))
        {
            return false;
        }

        var claim = await context.FuneralClaims
            .Where(existingClaim => existingClaim.Id == claimId)
            .FirstOrDefaultAsync();

        if (claim is null)
        {
            return false;
        }

        if (claim.Status is not (
            FuneralClaimStatus.Submitted or
            FuneralClaimStatus.UnderReview or
            FuneralClaimStatus.OnHold))
        {
            return false;
        }

        var reviewer = await context.Members
            .Where(member => member.Id == reviewedByMemberId)
            .FirstOrDefaultAsync();

        if (reviewer is null || reviewer.TenantId != claim.TenantId)
        {
            return false;
        }

        if (reviewer.DefaultRole != SisonkeRole.Secretary &&
            reviewer.DefaultRole != SisonkeRole.Creator &&
            reviewer.DefaultRole != SisonkeRole.StokvelAdmin)
        {
            return false;
        }

        claim.SecretaryRecommendedApproval = recommendApproval;
        claim.SecretaryReviewNotes = secretaryNotes.Trim();
        claim.SecretaryReviewedByName = reviewer.FullName;
        claim.SecretaryReviewedAt = DateTime.UtcNow;
        claim.ReviewNotes = secretaryNotes.Trim();
        claim.Status = FuneralClaimStatus.UnderReview;

        await context.SaveChangesAsync();
        await auditLogService.RecordAsync(
            reviewer.ApplicationUserId,
            await GetClaimStokvelIdAsync(claim),
            "ClaimSecretaryReviewed",
            nameof(FuneralClaim),
            claim.Id,
            $"Secretary review saved for claim {claim.ClaimReference ?? claim.Id.ToString("N")[..8]}.");

        return true;
    }

    public async Task<FuneralClaim?> ApproveClaimAsChairpersonAsync(
        Guid claimId,
        string? decisionNotes,
        string? decidedByName)
    {
        var claim = await context.FuneralClaims
            .SingleOrDefaultAsync(existingClaim => existingClaim.Id == claimId);

        if (claim is null)
        {
            return null;
        }

        if (claim.Status is not (
            FuneralClaimStatus.UnderReview or
            FuneralClaimStatus.Submitted or
            FuneralClaimStatus.OnHold))
        {
            return null;
        }

        if (!await HasDeathCertificateAsync(claimId))
        {
            return null;
        }

        var decisionAt = DateTime.UtcNow;

        claim.Status = FuneralClaimStatus.Approved;
        claim.ApprovedAt = decisionAt;
        claim.ChairpersonDecisionAt = decisionAt;
        claim.ChairpersonDecisionByName = decidedByName ?? "Chairperson";
        claim.ChairpersonDecisionNotes = decisionNotes;

        await context.SaveChangesAsync();

        return claim;
    }

    public async Task<bool> ApproveClaimAsync(Guid claimId, Guid approvedByMemberId, string? notes)
    {
        var claim = await context.FuneralClaims
            .Where(existingClaim => existingClaim.Id == claimId)
            .FirstOrDefaultAsync();

        if (claim is null || !IsAwaitingChairpersonApproval(claim))
        {
            return false;
        }

        if (!await HasDeathCertificateAsync(claimId))
        {
            return false;
        }

        var approver = await context.Members
            .Where(member => member.Id == approvedByMemberId)
            .FirstOrDefaultAsync();

        if (approver is null ||
            approver.TenantId != claim.TenantId ||
            approver.Id == claim.MemberId ||
            !IsChairpersonDecisionRole(approver.DefaultRole))
        {
            return false;
        }

        var decisionAt = DateTime.UtcNow;
        claim.Status = FuneralClaimStatus.Approved;
        claim.ApprovedAt = decisionAt;
        claim.ChairpersonDecisionAt = decisionAt;
        claim.ChairpersonDecisionByName = approver.FullName;
        claim.ChairpersonDecisionNotes = notes;

        await context.SaveChangesAsync();
        await auditLogService.RecordAsync(
            approver.ApplicationUserId,
            await GetClaimStokvelIdAsync(claim),
            "ClaimApproved",
            nameof(FuneralClaim),
            claim.Id,
            $"Chairperson approved claim {claim.ClaimReference ?? claim.Id.ToString("N")[..8]}.");

        return true;
    }

    public async Task<FuneralClaim?> RejectClaimAsChairpersonAsync(
        Guid claimId,
        string? decisionNotes,
        string? decidedByName)
    {
        var claim = await context.FuneralClaims
            .SingleOrDefaultAsync(existingClaim => existingClaim.Id == claimId);

        if (claim is null)
        {
            return null;
        }

        if (claim.Status is not (
            FuneralClaimStatus.Submitted or
            FuneralClaimStatus.UnderReview or
            FuneralClaimStatus.OnHold or
            FuneralClaimStatus.Draft))
        {
            return null;
        }

        var decisionAt = DateTime.UtcNow;

        claim.Status = FuneralClaimStatus.Rejected;
        claim.RejectedAt = decisionAt;
        claim.ChairpersonDecisionAt = decisionAt;
        claim.ChairpersonDecisionByName = decidedByName ?? "Chairperson";
        claim.ChairpersonDecisionNotes = decisionNotes;

        await context.SaveChangesAsync();

        return claim;
    }

    public async Task<bool> RejectClaimAsync(Guid claimId, Guid rejectedByMemberId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return false;
        }

        var claim = await context.FuneralClaims
            .Where(existingClaim => existingClaim.Id == claimId)
            .FirstOrDefaultAsync();

        if (claim is null || !IsAwaitingChairpersonApproval(claim))
        {
            return false;
        }

        var rejector = await context.Members
            .Where(member => member.Id == rejectedByMemberId)
            .FirstOrDefaultAsync();

        if (rejector is null ||
            rejector.TenantId != claim.TenantId ||
            rejector.Id == claim.MemberId ||
            !IsChairpersonDecisionRole(rejector.DefaultRole))
        {
            return false;
        }

        var decisionAt = DateTime.UtcNow;
        claim.Status = FuneralClaimStatus.Rejected;
        claim.RejectedAt = decisionAt;
        claim.ChairpersonDecisionAt = decisionAt;
        claim.ChairpersonDecisionByName = rejector.FullName;
        claim.ChairpersonDecisionNotes = reason.Trim();

        await context.SaveChangesAsync();
        await auditLogService.RecordAsync(
            rejector.ApplicationUserId,
            await GetClaimStokvelIdAsync(claim),
            "ClaimRejected",
            nameof(FuneralClaim),
            claim.Id,
            $"Chairperson rejected claim {claim.ClaimReference ?? claim.Id.ToString("N")[..8]}.");

        return true;
    }

    public async Task<FuneralClaim?> UpdateClaimStatusAsync(
        Guid claimId,
        FuneralClaimStatus status,
        string? reviewNotes)
    {
        var claim = await context.FuneralClaims
            .SingleOrDefaultAsync(existingClaim => existingClaim.Id == claimId);

        if (claim is null)
        {
            return null;
        }

        claim.Status = status;
        claim.ReviewNotes = reviewNotes;

        if (status == FuneralClaimStatus.Approved)
        {
            claim.ApprovedAt = DateTime.UtcNow;
        }
        else if (status == FuneralClaimStatus.Rejected)
        {
            claim.RejectedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();

        return claim;
    }

    public async Task<bool> MarkClaimPayoutPaidAsync(
        Guid claimId,
        decimal payoutAmount,
        string payoutReference,
        string? payoutNotes,
        Guid capturedByMemberId)
    {
        if (payoutAmount <= 0)
        {
            return false;
        }

        var claim = await context.FuneralClaims
            .Where(existingClaim => existingClaim.Id == claimId)
            .FirstOrDefaultAsync();

        if (claim is null ||
            claim.Status != FuneralClaimStatus.Approved ||
            claim.ChairpersonDecisionAt is null ||
            claim.ApprovedAt is null ||
            claim.PayoutPaidAt is not null)
        {
            return false;
        }

        var stokvel = await context.Stokvels
            .Where(existingStokvel => existingStokvel.TenantId == claim.TenantId)
            .OrderBy(existingStokvel => existingStokvel.CreatedAt)
            .ThenBy(existingStokvel => existingStokvel.Name)
            .FirstOrDefaultAsync();

        if (stokvel is null)
        {
            return false;
        }

        var capturedByMember = await context.Members
            .Where(member => member.Id == capturedByMemberId)
            .FirstOrDefaultAsync();

        if (capturedByMember is null ||
            capturedByMember.TenantId != claim.TenantId ||
            capturedByMember.Id == claim.MemberId ||
            capturedByMember.DefaultRole != SisonkeRole.Treasurer)
        {
            return false;
        }

        var previousPayoutAmount = claim.PayoutAmount;
        var previousStatus = claim.Status.ToString();
        var paidAt = DateTime.UtcNow;
        var trimmedPayoutReference = string.IsNullOrWhiteSpace(payoutReference)
            ? BuildDefaultPayoutReference(claim)
            : payoutReference.Trim();
        var trimmedPayoutNotes = string.IsNullOrWhiteSpace(payoutNotes) ? null : payoutNotes.Trim();

        claim.PayoutAmount = payoutAmount;
        claim.PayoutReference = trimmedPayoutReference;
        claim.PayoutNotes = trimmedPayoutNotes;
        claim.PayoutCapturedByMemberId = capturedByMember.Id;
        claim.PayoutPaidAt = paidAt;
        claim.Status = FuneralClaimStatus.Paid;

        context.ClaimPayoutAudits.Add(new ClaimPayoutAudit
        {
            Id = Guid.NewGuid(),
            FuneralClaimId = claim.Id,
            MemberId = claim.MemberId,
            StokvelId = stokvel.Id,
            Action = "PayoutMarkedPaid",
            PreviousPayoutAmount = previousPayoutAmount,
            NewPayoutAmount = payoutAmount,
            PreviousStatus = previousStatus,
            NewStatus = claim.Status.ToString(),
            PayoutReference = trimmedPayoutReference,
            Notes = trimmedPayoutNotes,
            CapturedByMemberId = capturedByMember.Id,
            CreatedAt = paidAt
        });

        await context.SaveChangesAsync();
        await auditLogService.RecordAsync(
            capturedByMember.ApplicationUserId,
            stokvel.Id,
            "ClaimPaid",
            nameof(FuneralClaim),
            claim.Id,
            $"Treasurer marked claim {claim.ClaimReference ?? claim.Id.ToString("N")[..8]} paid for {payoutAmount:C}.");

        return true;
    }

    public async Task<List<ClaimPayoutAudit>> GetClaimPayoutAuditTrailAsync(Guid funeralClaimId)
    {
        return await context.ClaimPayoutAudits
            .Include(audit => audit.Member)
            .Include(audit => audit.Stokvel)
            .Include(audit => audit.CapturedByMember)
            .Where(audit => audit.FuneralClaimId == funeralClaimId)
            .OrderByDescending(audit => audit.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<ClaimPayoutAudit>> GetClaimPayoutAuditsByStokvelIdAsync(Guid stokvelId, int take = 100)
    {
        return await context.ClaimPayoutAudits
            .Include(audit => audit.FuneralClaim)
            .Include(audit => audit.Member)
            .Include(audit => audit.CapturedByMember)
            .Where(audit => audit.StokvelId == stokvelId)
            .OrderByDescending(audit => audit.CreatedAt)
            .Take(take)
            .ToListAsync();
    }

    public async Task<int> BackfillMissingPayoutAuditsAsync(Guid stokvelId, Guid capturedByMemberId)
    {
        var stokvel = await context.Stokvels
            .Where(existingStokvel => existingStokvel.Id == stokvelId)
            .OrderBy(existingStokvel => existingStokvel.CreatedAt)
            .ThenBy(existingStokvel => existingStokvel.Name)
            .FirstOrDefaultAsync();

        if (stokvel is null)
        {
            return 0;
        }

        var fallbackCapturedByMember = await context.Members
            .Where(member => member.Id == capturedByMemberId)
            .FirstOrDefaultAsync();

        if (fallbackCapturedByMember is null || fallbackCapturedByMember.TenantId != stokvel.TenantId)
        {
            return 0;
        }

        var paidClaims = await context.FuneralClaims
            .Include(claim => claim.Member)
            .Where(claim =>
                claim.TenantId == stokvel.TenantId &&
                claim.PayoutPaidAt != null)
            .OrderBy(claim => claim.PayoutPaidAt)
            .ThenBy(claim => claim.CreatedAt)
            .ToListAsync();

        if (paidClaims.Count == 0)
        {
            return 0;
        }

        var claimIdsWithAudit = await context.ClaimPayoutAudits
            .Where(audit => audit.StokvelId == stokvelId)
            .Select(audit => audit.FuneralClaimId)
            .Distinct()
            .ToListAsync();
        var auditedClaimIds = claimIdsWithAudit.ToHashSet();

        var auditsAdded = 0;

        foreach (var claim in paidClaims)
        {
            if (auditedClaimIds.Contains(claim.Id))
            {
                continue;
            }

            context.ClaimPayoutAudits.Add(new ClaimPayoutAudit
            {
                Id = Guid.NewGuid(),
                FuneralClaimId = claim.Id,
                MemberId = claim.MemberId,
                StokvelId = stokvel.Id,
                Action = "PayoutMarkedPaid",
                PreviousStatus = FuneralClaimStatus.Approved.ToString(),
                NewStatus = claim.Status.ToString(),
                NewPayoutAmount = claim.PayoutAmount,
                PayoutReference = claim.PayoutReference,
                Notes = claim.PayoutNotes,
                CapturedByMemberId = claim.PayoutCapturedByMemberId ?? capturedByMemberId,
                CreatedAt = claim.PayoutPaidAt ?? DateTime.UtcNow
            });

            auditsAdded++;
        }

        if (auditsAdded > 0)
        {
            await context.SaveChangesAsync();
        }

        return auditsAdded;
    }

    public async Task<FuneralClaimDocument?> UploadClaimDocumentAsync(
        Guid claimId,
        ClaimDocumentType documentType,
        IBrowserFile file)
    {
        var claim = await context.FuneralClaims
            .SingleOrDefaultAsync(existingClaim => existingClaim.Id == claimId);

        if (claim is null || file is null)
        {
            return null;
        }

        var originalFileName = Path.GetFileName(file.Name);
        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();

        if (!AllowedClaimDocumentExtensions.Contains(extension) || file.Size > MaxClaimDocumentUploadSize)
        {
            return null;
        }

        var tenantFolderName = claim.TenantId.ToString("D");
        var claimFolderName = claim.Id.ToString("D");
        var webRootPath = webHostEnvironment.WebRootPath
            ?? Path.Combine(webHostEnvironment.ContentRootPath, "wwwroot");
        var uploadFolder = Path.Combine(webRootPath, "uploads", "claims", tenantFolderName, claimFolderName);

        Directory.CreateDirectory(uploadFolder);

        var storedFileName = $"{Guid.NewGuid()}_{GetSafeFileName(originalFileName)}";
        var storedFilePath = Path.Combine(uploadFolder, storedFileName);

        await using (var fileStream = File.Create(storedFilePath))
        await using (var uploadStream = file.OpenReadStream(maxAllowedSize: MaxClaimDocumentUploadSize))
        {
            await uploadStream.CopyToAsync(fileStream);
        }

        var document = new FuneralClaimDocument
        {
            Id = Guid.NewGuid(),
            FuneralClaimId = claim.Id,
            DocumentType = documentType,
            OriginalFileName = file.Name,
            StoredFilePath = $"/uploads/claims/{tenantFolderName}/{claimFolderName}/{storedFileName}",
            ContentType = file.ContentType,
            FileSizeBytes = file.Size,
            UploadedAt = DateTime.UtcNow
        };

        context.FuneralClaimDocuments.Add(document);
        await context.SaveChangesAsync();

        return document;
    }

    private static string GetSafeFileName(string fileName)
    {
        var safeFileName = Path.GetFileName(fileName);

        foreach (var invalidCharacter in Path.GetInvalidFileNameChars())
        {
            safeFileName = safeFileName.Replace(invalidCharacter, '_');
        }

        return safeFileName;
    }

    private static bool IsAwaitingChairpersonApproval(FuneralClaim claim)
    {
        return claim.SecretaryReviewedAt is not null &&
            claim.ChairpersonDecisionAt is null &&
            claim.Status == FuneralClaimStatus.UnderReview;
    }

    private static bool IsChairpersonDecisionRole(SisonkeRole role)
    {
        return role is SisonkeRole.Chairperson or SisonkeRole.Creator or SisonkeRole.StokvelAdmin;
    }

    private async Task<Guid?> GetClaimStokvelIdAsync(FuneralClaim claim)
    {
        if (claim.StokvelId is not null)
        {
            return claim.StokvelId.Value;
        }

        return await context.Stokvels
            .Where(stokvel => stokvel.TenantId == claim.TenantId)
            .OrderBy(stokvel => stokvel.CreatedAt)
            .ThenBy(stokvel => stokvel.Name)
            .Select(stokvel => (Guid?)stokvel.Id)
            .FirstOrDefaultAsync();
    }

    private static bool IsClaimsAvailable(Stokvel stokvel)
    {
        return stokvel.Archetype == StokvelArchetype.BurialSociety || stokvel.EnableClaims;
    }

    private static bool IsDependentsAvailable(Stokvel stokvel)
    {
        return stokvel.Archetype == StokvelArchetype.BurialSociety || stokvel.EnableDependents;
    }

    private static ClaimDocumentChecklistItemDto BuildChecklistItem(
        string documentType,
        string description,
        bool isRequired,
        ICollection<FuneralClaimDocument> documents,
        ClaimDocumentType[] matchingTypes,
        string[] matchingFileNameTerms)
    {
        var submittedDocument = documents
            .OrderByDescending(document => document.UploadedAt)
            .FirstOrDefault(document =>
                matchingTypes.Contains(document.DocumentType) ||
                FileNameMatches(document.OriginalFileName, matchingFileNameTerms));
        var isSubmitted = submittedDocument is not null;

        return new ClaimDocumentChecklistItemDto
        {
            DocumentType = documentType,
            Description = description,
            IsRequired = isRequired,
            IsSubmitted = isSubmitted,
            Status = isSubmitted ? "Submitted" : isRequired ? "Missing" : "Optional",
            FileName = submittedDocument?.OriginalFileName,
            UploadedAt = submittedDocument?.UploadedAt,
            Notes = isSubmitted
                ? null
                : matchingTypes.Contains(ClaimDocumentType.Other) && matchingTypes.Length == 1
                    ? "Upload as Other or use a clearly named file."
                    : null
        };
    }

    private static bool FileNameMatches(string? fileName, string[] terms)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        return terms.Any(term => fileName.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<string> GenerateClaimReferenceAsync(Guid tenantId, DateTime referenceDate)
    {
        var year = referenceDate.Year;
        var stokvelCode = await GetStokvelReferenceCodeAsync(tenantId);
        var sequence = await context.FuneralClaims
            .CountAsync(claim =>
                claim.TenantId == tenantId &&
                claim.CreatedAt.Year == year) + 1;
        var claimReference = FormatClaimReference(stokvelCode, year, sequence);

        while (await context.FuneralClaims.AnyAsync(claim => claim.ClaimReference == claimReference))
        {
            sequence++;
            claimReference = FormatClaimReference(stokvelCode, year, sequence);
        }

        return claimReference;
    }

    private async Task<string> GetStokvelReferenceCodeAsync(Guid tenantId)
    {
        var stokvel = await context.Stokvels
            .Where(existingStokvel => existingStokvel.TenantId == tenantId)
            .OrderBy(existingStokvel => existingStokvel.CreatedAt)
            .ThenBy(existingStokvel => existingStokvel.Name)
            .FirstOrDefaultAsync();

        if (stokvel is null)
        {
            return "STK";
        }

        var normalizedCode = StokvelService.NormalizeStokvelCode(stokvel.Code);

        if (string.IsNullOrWhiteSpace(normalizedCode))
        {
            normalizedCode = BuildFallbackStokvelCode(stokvel.Name);
        }

        var uniqueCode = normalizedCode;
        var suffix = 2;

        while (await context.Stokvels.AnyAsync(existingStokvel =>
            existingStokvel.Id != stokvel.Id &&
            existingStokvel.Code == uniqueCode))
        {
            uniqueCode = $"{normalizedCode}{suffix}";
            suffix++;
        }

        stokvel.Code = uniqueCode;

        return uniqueCode;
    }

    private static string FormatClaimReference(string stokvelCode, int year, int sequence)
    {
        return $"CLM-{stokvelCode}-{year}-{sequence:0000}";
    }

    private static string BuildDefaultPayoutReference(FuneralClaim claim)
    {
        return string.IsNullOrWhiteSpace(claim.ClaimReference)
            ? $"PAY-{claim.Id.ToString("N")[..8].ToUpperInvariant()}"
            : $"PAY-{claim.ClaimReference}";
    }

    private static string BuildFallbackStokvelCode(string stokvelName)
    {
        var words = stokvelName
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(word => new string(word.Where(char.IsLetterOrDigit).ToArray()))
            .Where(word => !string.IsNullOrWhiteSpace(word))
            .ToList();

        var compactName = StokvelService.NormalizeStokvelCode(words.FirstOrDefault() ?? stokvelName);
        var code = words.Count > 1
            ? new string(words.Select(word => char.ToUpperInvariant(word[0])).ToArray())
            : compactName.Length >= 3 ? compactName[..3] : compactName;

        if (code.Length > 6)
        {
            code = code[..6];
        }

        return string.IsNullOrWhiteSpace(code) ? "STK" : code;
    }
}
