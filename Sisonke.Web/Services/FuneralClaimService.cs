using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Services;

public class FuneralClaimService(
    ApplicationDbContext context,
    OperatingRuleService operatingRuleService,
    IWebHostEnvironment webHostEnvironment)
{
    private const long MaxClaimDocumentUploadSize = 10 * 1024 * 1024;
    private static readonly string[] AllowedClaimDocumentExtensions = [".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx"];

    public async Task<List<FuneralClaim>> GetClaimsByStokvelIdAsync(Guid stokvelId)
    {
        var stokvel = await context.Stokvels
            .SingleOrDefaultAsync(existingStokvel => existingStokvel.Id == stokvelId);

        if (stokvel is null)
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

    public async Task<List<FuneralClaim>> GetClaimsByMemberIdAsync(Guid memberId)
    {
        return await context.FuneralClaims
            .Include(claim => claim.Dependent)
            .Include(claim => claim.Documents)
            .Where(claim => claim.MemberId == memberId)
            .OrderByDescending(claim => claim.CreatedAt)
            .ToListAsync();
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
        string? claimReason)
    {
        var member = await context.Members
            .Include(existingMember => existingMember.Tenant)
            .SingleOrDefaultAsync(existingMember => existingMember.Id == memberId);

        if (member is null)
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
                dependent.DeceasedDate = dateOfDeath;
                dependent.DeathReportedAt ??= DateTime.UtcNow;
            }
        }

        var isWaitingPeriodSatisfied = await IsWaitingPeriodSatisfiedForMemberAsync(member.Id);
        var isMemberStatusEligible = await IsMemberStatusEligibleForClaimAsync(member.Id);
        var status = !isMemberStatusEligible || !isWaitingPeriodSatisfied
            ? FuneralClaimStatus.OnHold
            : FuneralClaimStatus.Draft;

        var claim = new FuneralClaim
        {
            Id = Guid.NewGuid(),
            TenantId = member.TenantId,
            MemberId = member.Id,
            SubjectType = subjectType,
            DependentId = claimDependentId,
            DeceasedFullName = deceasedFullName,
            DateOfDeath = dateOfDeath,
            ClaimReason = claimReason,
            Status = status,
            IsWaitingPeriodSatisfied = isWaitingPeriodSatisfied,
            IsMemberStatusEligible = isMemberStatusEligible,
            CreatedAt = DateTime.UtcNow
        };

        context.FuneralClaims.Add(claim);
        await context.SaveChangesAsync();

        return claim;
    }

    public async Task<FuneralClaim?> SubmitClaimAsync(Guid claimId)
    {
        var claim = await context.FuneralClaims
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
                dependent.DeceasedDate = claim.DateOfDeath;
                dependent.DeathReportedAt ??= DateTime.UtcNow;
            }
        }

        if (claim.Status != FuneralClaimStatus.OnHold)
        {
            claim.Status = FuneralClaimStatus.Submitted;
        }

        claim.SubmittedAt = DateTime.UtcNow;
        claim.SubmittedByName ??= "Member";

        await context.SaveChangesAsync();

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
}
