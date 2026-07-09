using System.Reflection;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;
using Sisonke.Web.Services.Dto;

namespace Sisonke.Web.Services;

public class AdminControlsService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    IConfiguration configuration,
    IWebHostEnvironment environment,
    EmailSettings emailSettings,
    AuditLogService auditLogService)
{
    private static readonly SisonkeRole[] AssignableStokvelRoles =
    [
        SisonkeRole.StokvelAdmin,
        SisonkeRole.Chairperson,
        SisonkeRole.Secretary,
        SisonkeRole.Treasurer,
        SisonkeRole.CommitteeMember,
        SisonkeRole.Member
    ];

    public IReadOnlyList<SisonkeRole> GetAssignableStokvelRoles() => AssignableStokvelRoles;

    public async Task<RoleManagementViewModel> GetRoleManagementAsync(Guid stokvelId, ClaimsPrincipal principal)
    {
        var currentUserId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        await using var context = await dbFactory.CreateDbContextAsync();

        var stokvel = await context.Stokvels
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == stokvelId && s.IsActive && !s.IsDeleted);

        if (stokvel is null)
        {
            return new RoleManagementViewModel { StokvelId = stokvelId, DenialReason = "Stokvel not found." };
        }

        var linkedMember = await GetLinkedMemberAsync(context, stokvel.TenantId, currentUserId);
        var authorized = CanManageRoles(linkedMember?.DefaultRole, principal);

        var model = new RoleManagementViewModel
        {
            StokvelId = stokvel.Id,
            StokvelName = stokvel.Name,
            IsAuthorized = authorized,
            DenialReason = authorized ? null : "Only linked chairpersons, stokvel admins, creators, or platform admins can view role assignments."
        };

        if (!authorized)
        {
            return model;
        }

        model.Members = await context.Members
            .AsNoTracking()
            .Where(member => member.TenantId == stokvel.TenantId)
            .OrderBy(member => member.FullName)
            .Select(member => new RoleAssignmentRow
            {
                MemberId = member.Id,
                FullName = member.FullName,
                EmailAddress = member.EmailAddress,
                ApplicationUserId = member.ApplicationUserId,
                Role = member.DefaultRole,
                IsCurrentUser = member.ApplicationUserId == currentUserId
            })
            .ToListAsync();

        return model;
    }

    public async Task<(bool Succeeded, string Message)> UpdateMemberRoleAsync(
        Guid stokvelId,
        Guid memberId,
        SisonkeRole newRole,
        ClaimsPrincipal principal)
    {
        if (!AssignableStokvelRoles.Contains(newRole))
        {
            return (false, "That role cannot be assigned from this screen.");
        }

        var currentUserId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        await using var context = await dbFactory.CreateDbContextAsync();

        var stokvel = await context.Stokvels.FirstOrDefaultAsync(s => s.Id == stokvelId && s.IsActive && !s.IsDeleted);
        if (stokvel is null)
        {
            return (false, "Stokvel not found.");
        }

        var actor = await GetLinkedMemberAsync(context, stokvel.TenantId, currentUserId);
        if (!CanManageRoles(actor?.DefaultRole, principal))
        {
            return (false, "You are not authorized to change roles for this stokvel.");
        }

        var target = await context.Members.FirstOrDefaultAsync(member => member.Id == memberId && member.TenantId == stokvel.TenantId);
        if (target is null)
        {
            return (false, "Member not found in this stokvel.");
        }

        if (!string.IsNullOrWhiteSpace(target.ApplicationUserId) && target.ApplicationUserId == currentUserId)
        {
            return (false, "Use another authorized administrator to change your own role.");
        }

        if (IsProtectedRole(target.DefaultRole) && !IsProtectedRole(newRole))
        {
            var remainingProtectedCount = await context.Members.CountAsync(member =>
                member.TenantId == stokvel.TenantId &&
                member.Id != target.Id &&
                (member.DefaultRole == SisonkeRole.Creator ||
                 member.DefaultRole == SisonkeRole.StokvelAdmin ||
                 member.DefaultRole == SisonkeRole.Chairperson));

            if (remainingProtectedCount == 0)
            {
                return (false, "This would remove the last chairperson/admin from the stokvel.");
            }
        }

        var oldRole = target.DefaultRole;
        target.DefaultRole = newRole;
        await context.SaveChangesAsync();

        await auditLogService.RecordAsync(
            currentUserId,
            stokvel.Id,
            "RoleChanged",
            "Member",
            target.Id,
            $"Role changed for {target.FullName} from {oldRole} to {newRole}.");

        return (true, "Role updated.");
    }

    public async Task<ConfigurationReviewViewModel> GetConfigurationReviewAsync(Guid stokvelId, ClaimsPrincipal principal)
    {
        var currentUserId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        await using var context = await dbFactory.CreateDbContextAsync();

        var stokvel = await context.Stokvels
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == stokvelId && s.IsActive && !s.IsDeleted);

        if (stokvel is null)
        {
            return new ConfigurationReviewViewModel { StokvelId = stokvelId, DenialReason = "Stokvel not found." };
        }

        var linkedMember = await GetLinkedMemberAsync(context, stokvel.TenantId, currentUserId);
        var isLinked = linkedMember is not null || IsPlatformAdmin(principal);
        var canManage = CanManageConfiguration(linkedMember?.DefaultRole, principal);

        var model = new ConfigurationReviewViewModel
        {
            StokvelId = stokvel.Id,
            StokvelName = stokvel.Name,
            IsLinkedMember = isLinked,
            CanManage = canManage,
            DenialReason = isLinked ? null : "You are not linked to this stokvel."
        };

        if (!isLinked)
        {
            return model;
        }

        var contributionRule = await context.ContributionRules
            .AsNoTracking()
            .Where(rule => rule.TenantId == stokvel.TenantId && rule.IsActive)
            .OrderByDescending(rule => rule.EffectiveFrom)
            .FirstOrDefaultAsync();

        var fineTypes = await context.FineTypes
            .AsNoTracking()
            .Where(fineType => fineType.TenantId == stokvel.TenantId && fineType.IsActive)
            .OrderBy(fineType => fineType.Name)
            .ToListAsync();

        var operatingRules = await context.StokvelOperatingRules
            .AsNoTracking()
            .FirstOrDefaultAsync(rules => rules.StokvelId == stokvel.Id);

        var loanConfig = await context.StokvelLoanConfigurations
            .AsNoTracking()
            .Where(config => config.StokvelId == stokvel.Id && config.IsActive)
            .OrderByDescending(config => config.UpdatedAt ?? config.CreatedAt)
            .FirstOrDefaultAsync();

        var rotationalConfig = await context.RotationalStokvelConfigurations
            .AsNoTracking()
            .Where(config => config.StokvelId == stokvel.Id && config.IsActive)
            .OrderByDescending(config => config.UpdatedAt ?? config.CreatedAt)
            .FirstOrDefaultAsync();

        model.Sections =
        [
            BuildStokvelSection(stokvel),
            BuildContributionSection(stokvel.Id, contributionRule),
            BuildFineSection(stokvel.Id, fineTypes),
            BuildLoanSection(stokvel.Id, loanConfig),
            BuildOperatingSection(stokvel.Id, operatingRules),
            BuildRotationalSection(stokvel.Id, stokvel, rotationalConfig)
        ];

        return model;
    }

    public async Task<ProductionReadinessViewModel> GetProductionReadinessAsync(ClaimsPrincipal principal)
    {
        var isAuthorized = await IsPlatformAdminAsync(principal);
        var model = new ProductionReadinessViewModel
        {
            IsAuthorized = isAuthorized,
            EnvironmentName = environment.EnvironmentName,
            AppVersion = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                ?? Assembly.GetEntryAssembly()?.GetName().Version?.ToString()
                ?? "Unavailable"
        };

        if (!model.IsAuthorized)
        {
            return model;
        }

        await using var context = await dbFactory.CreateDbContextAsync();
        var databaseReachable = false;
        var pendingMigrationsCount = -1;

        try
        {
            databaseReachable = await context.Database.CanConnectAsync();
            pendingMigrationsCount = (await context.Database.GetPendingMigrationsAsync()).Count();
        }
        catch
        {
            databaseReachable = false;
        }

        model.Checks.Add(new ReadinessCheckRow
        {
            Name = "Database",
            Status = databaseReachable ? "OK" : "Attention",
            Detail = databaseReachable ? "Database connection is reachable." : "Database connection check failed.",
            IsHealthy = databaseReachable
        });

        model.Checks.Add(new ReadinessCheckRow
        {
            Name = "Pending migrations",
            Status = pendingMigrationsCount == 0 ? "OK" : "Review",
            Detail = pendingMigrationsCount < 0 ? "Pending migration count unavailable." : $"{pendingMigrationsCount} pending migration(s).",
            IsHealthy = pendingMigrationsCount == 0
        });

        model.Checks.Add(new ReadinessCheckRow
        {
            Name = "Email configuration",
            Status = string.IsNullOrWhiteSpace(emailSettings.SmtpHost) ? "Review" : "Configured",
            Detail = string.IsNullOrWhiteSpace(emailSettings.SmtpHost) ? "SMTP host is not configured." : "SMTP settings are present. Values are hidden.",
            IsHealthy = !string.IsNullOrWhiteSpace(emailSettings.SmtpHost)
        });

        model.Checks.Add(new ReadinessCheckRow
        {
            Name = "External authentication",
            Status = IsExternalAuthConfigured() ? "Configured" : "Review",
            Detail = IsExternalAuthConfigured() ? "External authentication settings are present. Values are hidden." : "External authentication settings are incomplete.",
            IsHealthy = IsExternalAuthConfigured()
        });

        model.RecentActivity = await auditLogService.GetRecentAsync(25);
        return model;
    }

    private static async Task<Member?> GetLinkedMemberAsync(ApplicationDbContext context, Guid tenantId, string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        return await context.Members.FirstOrDefaultAsync(member =>
            member.TenantId == tenantId &&
            member.ApplicationUserId == userId);
    }

    private static bool CanManageRoles(SisonkeRole? role, ClaimsPrincipal principal) =>
        IsPlatformAdmin(principal) ||
        role is SisonkeRole.PlatformSuperAdmin or SisonkeRole.PlatformSupportAdmin or SisonkeRole.Creator or SisonkeRole.StokvelAdmin or SisonkeRole.Chairperson;

    private static bool CanManageConfiguration(SisonkeRole? role, ClaimsPrincipal principal) =>
        IsPlatformAdmin(principal) ||
        role is SisonkeRole.PlatformSuperAdmin or SisonkeRole.PlatformSupportAdmin or SisonkeRole.Creator or SisonkeRole.StokvelAdmin or SisonkeRole.Chairperson or SisonkeRole.Secretary or SisonkeRole.Treasurer;

    private static bool IsProtectedRole(SisonkeRole role) =>
        role is SisonkeRole.Creator or SisonkeRole.StokvelAdmin or SisonkeRole.Chairperson;

    private static bool IsPlatformAdmin(ClaimsPrincipal principal) =>
        principal.IsInRole("PlatformAdmin") || principal.IsInRole("PlatformSupport");

    private async Task<bool> IsPlatformAdminAsync(ClaimsPrincipal principal)
    {
        if (IsPlatformAdmin(principal))
        {
            return true;
        }

        var currentUserId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            return false;
        }

        await using var context = await dbFactory.CreateDbContextAsync();
        return await context.Members.AsNoTracking().AnyAsync(member =>
            member.ApplicationUserId == currentUserId &&
            (member.DefaultRole == SisonkeRole.PlatformSuperAdmin ||
             member.DefaultRole == SisonkeRole.PlatformSupportAdmin));
    }

    private bool IsExternalAuthConfigured() =>
        !string.IsNullOrWhiteSpace(configuration["AzureAd:ClientId"]) &&
        !string.IsNullOrWhiteSpace(configuration["AzureAd:ClientSecret"]) &&
        (!string.IsNullOrWhiteSpace(configuration["AzureAd:MetadataAddress"]) ||
         (!string.IsNullOrWhiteSpace(configuration["AzureAd:Instance"]) &&
          (!string.IsNullOrWhiteSpace(configuration["AzureAd:Domain"]) ||
           !string.IsNullOrWhiteSpace(configuration["AzureAd:TenantId"]))));

    private static ConfigurationSectionRow BuildStokvelSection(Stokvel stokvel) => new()
    {
        Title = "Stokvel profile",
        EditHref = $"/stokvel-settings/{stokvel.Id}",
        Values =
        [
            Value("Type", stokvel.Type.ToString(), false),
            Value("Archetype", stokvel.Archetype.ToString(), false),
            Value("Claims", stokvel.EnableClaims ? "Enabled" : "Disabled", false),
            Value("Dependents", stokvel.EnableDependents ? "Enabled" : "Disabled", false),
            Value("Rotation", stokvel.EnableRotation ? "Enabled" : "Disabled", false),
            Value("Lending", stokvel.EnableLending ? "Enabled" : "Disabled", false)
        ]
    };

    private static ConfigurationSectionRow BuildContributionSection(Guid stokvelId, ContributionRule? rule) => new()
    {
        Title = "Contribution rules",
        EditHref = $"/contribution-rules/{stokvelId}",
        Values = rule is null
            ? [Value("Active rule", "Missing", true)]
            :
            [
                Value("Amount", Money(rule.Amount), rule.Amount <= 0),
                Value("Frequency", rule.Frequency.ToString(), false),
                Value("Due day", rule.DueDayOfMonth.ToString(), rule.DueDayOfMonth is < 1 or > 31),
                Value("Grace period", $"{rule.GracePeriodDays} day(s)", rule.GracePeriodDays < 0),
                Value("Late fine", Money(rule.LatePaymentFineAmount), rule.LatePaymentFineAmount <= 0)
            ]
    };

    private static ConfigurationSectionRow BuildFineSection(Guid stokvelId, IReadOnlyCollection<FineType> fineTypes) => new()
    {
        Title = "Fine rules",
        EditHref = $"/fine-types/{stokvelId}",
        Values =
        [
            Value("Active fine types", fineTypes.Count.ToString(), fineTypes.Count == 0),
            Value("Default amounts", fineTypes.Count == 0 ? "Missing" : string.Join(", ", fineTypes.Take(4).Select(f => $"{f.Name}: {Money(f.DefaultAmount)}")), fineTypes.Count == 0)
        ]
    };

    private static ConfigurationSectionRow BuildLoanSection(Guid stokvelId, StokvelLoanConfiguration? config) => new()
    {
        Title = "Loan and wallet rules",
        EditHref = $"/stokvel/{stokvelId}/loans-wallet",
        Values = config is null
            ? [Value("Loan configuration", "Missing", true)]
            :
            [
                Value("Loans", config.LoansEnabled ? "Enabled" : "Disabled", false),
                Value("Minimum loan", Money(config.MinLoanAmount), config.LoansEnabled && config.MinLoanAmount <= 0),
                Value("Maximum loan", Money(config.MaxLoanAmount), config.LoansEnabled && config.MaxLoanAmount <= 0),
                Value("Repayment months", $"{config.DefaultRepaymentMonths} default / {config.MaxRepaymentMonths} max", config.LoansEnabled && config.DefaultRepaymentMonths <= 0),
                Value("Surplus lending", config.SurplusBackedLoansEnabled ? $"Enabled ({config.SurplusEquityLoanMultiplier:N2}x)" : "Disabled", false),
                Value("Early payout advance", config.EarlyPayoutLoansEnabled ? $"Enabled ({config.EarlyPayoutDiscountRatePercent:N2}% adjustment)" : "Disabled", false),
                Value("Mandatory guarantors", config.RequiredGuarantorCount.ToString(), config.RequiredGuarantorCount < 0)
            ]
    };

    private static ConfigurationSectionRow BuildOperatingSection(Guid stokvelId, StokvelOperatingRules? rules) => new()
    {
        Title = "Meetings, apologies and burial rules",
        EditHref = $"/stokvel-operating-rules/{stokvelId}",
        Values = rules is null
            ? [Value("Operating rules", "Missing", true)]
            :
            [
                Value("Meetings", rules.EnableMeetings ? "Enabled" : "Disabled", false),
                Value("Attendance tracking", rules.EnableAttendanceTracking ? "Enabled" : "Disabled", false),
                Value("Apology deadline", $"{rules.ApologyDeadlineHoursBeforeMeeting} hour(s) before meeting", rules.ApologyDeadlineHoursBeforeMeeting <= 0),
                Value("Minutes approval", rules.RequireMinutesApproval ? "Required" : "Not required", false),
                Value("Dependents", rules.EnableDependents ? $"Enabled, max {rules.MaximumDependents}" : "Disabled", rules.EnableDependents && rules.MaximumDependents <= 0),
                Value("Claim payout", Money(rules.DefaultClaimPayoutAmount), rules.EnableClaims && rules.DefaultClaimPayoutAmount <= 0)
            ]
    };

    private static ConfigurationSectionRow BuildRotationalSection(Guid stokvelId, Stokvel stokvel, RotationalStokvelConfiguration? config) => new()
    {
        Title = "Rotational rules",
        EditHref = $"/rotational-configuration/{stokvelId}",
        Values = !stokvel.EnableRotation
            ? [Value("Rotational module", "Disabled", false)]
            : config is null
                ? [Value("Rotational configuration", "Missing", true)]
                :
                [
                    Value("Contribution", $"{Money(config.ContributionAmount)} {config.ContributionFrequency}", config.ContributionAmount <= 0),
                    Value("Payout", $"{Money(config.PayoutAmount)} {config.PayoutFrequency}", config.PayoutAmount <= 0),
                    Value("Due day", config.ContributionDueDay.ToString(), config.ContributionDueDay is < 1 or > 31),
                    Value("Treasurer confirmation", config.TreasurerConfirmationRequired ? "Required" : "Not required", false)
                ]
    };

    private static ConfigurationValueRow Value(string label, string value, bool needsAttention) => new()
    {
        Label = label,
        Value = value,
        NeedsAttention = needsAttention
    };

    private static string Money(decimal amount) => $"R {amount:N2}";
}
