using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Services.Entitlements;

/// <summary>
/// Per-feature usage resolvers backing MAX_MEMBERS / MAX_ADMINISTRATORS / MAX_SCHEMES /
/// STORAGE_MB. Definitions (confirmed with product 2026-08-03):
///  - MAX_MEMBERS: active members (Member.Status == Active) on the stokvel's tenant — matches
///    the pre-existing StokvelService.CanAddMemberAsync definition of "active".
///  - MAX_ADMINISTRATORS: active members whose DefaultRole is StokvelAdmin or Creator. Elected
///    office bearers (Chairperson/Secretary/Treasurer/CommitteeMember) are governance roles, not
///    billable admin seats, and are not counted.
///  - MAX_SCHEMES: number of Stokvels (active, not soft-deleted) sharing the stokvel's Tenant.
///    There is no separate "Scheme" entity yet — each Stokvel is treated as one scheme.
///  - STORAGE_MB: MemberDocument + FuneralClaimDocument + ConstitutionDocument FileSizeBytes
///    for the tenant, converted to MB (rounded up).
/// </summary>
public sealed class EntitlementUsageProvider(IDbContextFactory<ApplicationDbContext> dbFactory) : IEntitlementUsageProvider
{
    private static readonly SisonkeRole[] AdministratorRoles = [SisonkeRole.StokvelAdmin, SisonkeRole.Creator];

    public async Task<int> GetCurrentUsageAsync(Guid stokvelId, string featureCode, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var tenantId = await context.Stokvels
            .Where(s => s.Id == stokvelId)
            .Select(s => (Guid?)s.TenantId)
            .FirstOrDefaultAsync(ct);

        if (tenantId is null)
        {
            return 0;
        }

        return featureCode switch
        {
            FeatureCodes.MaxMembers => await context.Members
                .CountAsync(m => m.TenantId == tenantId && m.Status == MemberStatus.Active, ct),

            FeatureCodes.MaxAdministrators => await context.Members
                .CountAsync(m =>
                    m.TenantId == tenantId &&
                    m.Status == MemberStatus.Active &&
                    AdministratorRoles.Contains(m.DefaultRole), ct),

            FeatureCodes.MaxSchemes => await context.Stokvels
                .CountAsync(s => s.TenantId == tenantId && s.IsActive && !s.IsDeleted, ct),

            FeatureCodes.StorageMb => await GetStorageUsageMbAsync(context, tenantId.Value, ct),

            _ => 0
        };
    }

    private static async Task<int> GetStorageUsageMbAsync(ApplicationDbContext context, Guid tenantId, CancellationToken ct)
    {
        var memberDocumentBytes = await context.MemberDocuments
            .Where(d => d.Member.TenantId == tenantId)
            .SumAsync(d => (long?)d.FileSizeBytes, ct) ?? 0;

        var claimDocumentBytes = await context.FuneralClaimDocuments
            .Where(d => d.FuneralClaim.TenantId == tenantId)
            .SumAsync(d => (long?)d.FileSizeBytes, ct) ?? 0;

        var constitutionDocumentBytes = await context.ConstitutionDocuments
            .Where(d => d.TenantId == tenantId)
            .SumAsync(d => (long?)d.FileSizeBytes, ct) ?? 0;

        var totalBytes = memberDocumentBytes + claimDocumentBytes + constitutionDocumentBytes;

        return (int)Math.Ceiling(totalBytes / (1024.0 * 1024.0));
    }
}
