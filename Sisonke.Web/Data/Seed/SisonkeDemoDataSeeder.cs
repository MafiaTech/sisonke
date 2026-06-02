using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Data.Seed;

public static class SisonkeDemoDataSeeder
{
    private const string DemoPassword = "Test@12345";

    // Demo seeder is duplicate tolerant because local test data may be reseeded multiple times.
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var scopedProvider = scope.ServiceProvider;

        var context = scopedProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scopedProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scopedProvider.GetService<RoleManager<IdentityRole>>();

        await EnsureRoleAsync(context, roleManager, "PlatformAdmin");
        await EnsureRoleAsync(context, roleManager, "PlatformSupport");

        var platformAdmin = await EnsureUserAsync(
            userManager,
            "platformadmin@test.co.za",
            "Platform Admin",
            "7001015009081",
            "0710000001",
            "Pretoria",
            "PlatformAdmin");

        var supportUser = await EnsureUserAsync(
            userManager,
            "support@test.co.za",
            "Support User",
            "7001015009082",
            "0710000002",
            "Johannesburg",
            "PlatformSupport");

        var chairpersonUser = await EnsureUserAsync(
            userManager,
            "chairperson@test.co.za",
            "Chairperson User",
            "8001015009083",
            "0710000003",
            "Soweto");

        var secretaryUser = await EnsureUserAsync(
            userManager,
            "secretary@test.co.za",
            "Secretary User",
            "8001015009084",
            "0710000004",
            "Mabopane");

        var treasurerUser = await EnsureUserAsync(
            userManager,
            "treasurer@test.co.za",
            "Treasurer User",
            "8001015009085",
            "0710000005",
            "Soshanguve");

        var ordinaryUser = await EnsureUserAsync(
            userManager,
            "member@test.co.za",
            "Ordinary Member",
            "8001015009086",
            "0710000006",
            "Ga-Rankuwa");

        var multiUser = await EnsureUserAsync(
            userManager,
            "multi@test.co.za",
            "Multi Role User",
            "8001015009087",
            "0710000007",
            "Centurion");

        await EnsureUserInRoleAsync(context, userManager, platformAdmin, "PlatformAdmin");
        await EnsureUserInRoleAsync(context, userManager, supportUser, "PlatformSupport");

        var pilotPlan = await GetOrCreateSubscriptionPlanAsync(context, "Pilot", 1, null, 0, 0);
        var basicPlan = await GetOrCreateSubscriptionPlanAsync(context, "Basic", 1, 30, 149, 1490);
        var standardPlan = await GetOrCreateSubscriptionPlanAsync(context, "Standard", 31, 50, 279, 2790);
        var premiumPlan = await GetOrCreateSubscriptionPlanAsync(context, "Premium", 51, null, 459, 4590);

        var aganang = await GetOrCreateStokvelAsync(
            context,
            "Aganang Burial Society",
            "aganang-burial-society",
            StokvelType.BurialSociety,
            "Gauteng",
            "Soweto",
            standardPlan ?? pilotPlan);

        var letsGrow = await GetOrCreateStokvelAsync(
            context,
            "Let’s Grow Together",
            "lets-grow-together",
            StokvelType.SavingsStokvel,
            "Gauteng",
            "Mabopane",
            basicPlan ?? pilotPlan);

        var wealthBuilders = await GetOrCreateStokvelAsync(
            context,
            "Wealth Builders Investment Club",
            "wealth-builders-investment-club",
            StokvelType.InvestmentStokvel,
            "Gauteng",
            "Johannesburg",
            premiumPlan ?? pilotPlan);

        var aganangChairperson = await GetOrCreateMemberAsync(context, aganang, chairpersonUser, SisonkeRole.Chairperson);
        await GetOrCreateMemberAsync(context, aganang, secretaryUser, SisonkeRole.Secretary);
        await GetOrCreateMemberAsync(context, aganang, treasurerUser, SisonkeRole.Treasurer);
        var aganangOrdinaryMember = await GetOrCreateMemberAsync(context, aganang, ordinaryUser, SisonkeRole.Member);
        await GetOrCreateMemberAsync(context, aganang, multiUser, SisonkeRole.Member);

        await GetOrCreateMemberAsync(context, letsGrow, multiUser, SisonkeRole.Secretary);
        await GetOrCreateMemberAsync(context, wealthBuilders, ordinaryUser, SisonkeRole.Member);

        var spouseDependent = await GetOrCreateDependentAsync(
            context,
            aganangOrdinaryMember,
            "Spouse Dependent",
            "Spouse",
            DateTime.Today.AddYears(-38));

        await GetOrCreateDependentAsync(
            context,
            aganangOrdinaryMember,
            "Child Dependent",
            "Child",
            DateTime.Today.AddYears(-12));

        await GetOrCreateDependentAsync(
            context,
            aganangOrdinaryMember,
            "Parent Dependent",
            "Parent",
            DateTime.Today.AddYears(-68));

        var lateComingFineType = await GetOrCreateFineTypeAsync(context, aganang.TenantId, "Late Coming Fine", 50);
        await GetOrCreateFineAsync(context, aganangOrdinaryMember, lateComingFineType);

        await GetOrCreateMeetingAsync(context, aganang);
        await GetOrCreateConstitutionAsync(context, aganang);
        await GetOrCreateFuneralClaimAsync(context, aganangOrdinaryMember, spouseDependent);

        await context.SaveChangesAsync();
    }

    private static async Task EnsureRoleAsync(
        ApplicationDbContext context,
        RoleManager<IdentityRole>? roleManager,
        string roleName)
    {
        if (roleManager is not null)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }

            return;
        }

        if (!await context.Roles.AnyAsync(role => role.Name == roleName))
        {
            context.Roles.Add(new IdentityRole
            {
                Id = Guid.NewGuid().ToString(),
                Name = roleName,
                NormalizedName = roleName.ToUpperInvariant()
            });

            await context.SaveChangesAsync();
        }
    }

    private static async Task<ApplicationUser> EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string fullName,
        string idNumber,
        string cellphoneNumber,
        string residentialArea,
        string? roleName = null)
    {
        var user = await userManager.FindByEmailAsync(email);

        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = fullName,
                IdNumber = idNumber,
                CellphoneNumber = cellphoneNumber,
                ResidentialArea = residentialArea
            };

            var result = await userManager.CreateAsync(user, DemoPassword);

            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Could not create demo user {email}: {string.Join(", ", result.Errors.Select(error => error.Description))}");
            }
        }
        else
        {
            user.EmailConfirmed = true;
            user.FullName = fullName;
            user.IdNumber = idNumber;
            user.CellphoneNumber = cellphoneNumber;
            user.ResidentialArea = residentialArea;
            await userManager.UpdateAsync(user);
        }

        if (!string.IsNullOrWhiteSpace(roleName))
        {
            await userManager.AddToRoleAsync(user, roleName);
        }

        return user;
    }

    private static async Task EnsureUserInRoleAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ApplicationUser user,
        string roleName)
    {
        if (userManager.SupportsUserRole)
        {
            var roles = await userManager.GetRolesAsync(user);

            if (!roles.Contains(roleName))
            {
                await userManager.AddToRoleAsync(user, roleName);
            }

            return;
        }

        var role = await context.Roles
            .Where(existingRole => existingRole.Name == roleName)
            .OrderBy(existingRole => existingRole.Id)
            .FirstOrDefaultAsync();

        if (role is null)
        {
            return;
        }

        var alreadyLinked = await context.UserRoles
            .AnyAsync(userRole => userRole.UserId == user.Id && userRole.RoleId == role.Id);

        if (!alreadyLinked)
        {
            context.UserRoles.Add(new IdentityUserRole<string>
            {
                UserId = user.Id,
                RoleId = role.Id
            });

            await context.SaveChangesAsync();
        }
    }

    private static async Task<SubscriptionPlan> GetOrCreateSubscriptionPlanAsync(
        ApplicationDbContext context,
        string name,
        int minMembers,
        int? maxMembers,
        decimal monthlyPrice,
        decimal annualPrice)
    {
        var plan = await context.SubscriptionPlans
            .Where(existingPlan => existingPlan.Name == name)
            .OrderBy(existingPlan => existingPlan.MinMembers)
            .ThenBy(existingPlan => existingPlan.Id)
            .FirstOrDefaultAsync();

        if (plan is null)
        {
            plan = new SubscriptionPlan
            {
                Id = Guid.NewGuid(),
                Name = name
            };

            context.SubscriptionPlans.Add(plan);
        }

        plan.MinMembers = minMembers;
        plan.MaxMembers = maxMembers;
        plan.MonthlyPrice = monthlyPrice;
        plan.AnnualPrice = annualPrice;
        plan.IsActive = true;

        await context.SaveChangesAsync();

        return plan;
    }

    private static async Task<Stokvel> GetOrCreateStokvelAsync(
        ApplicationDbContext context,
        string name,
        string slug,
        StokvelType type,
        string province,
        string townOrArea,
        SubscriptionPlan subscriptionPlan)
    {
        var stokvel = await context.Stokvels
            .Include(existingStokvel => existingStokvel.Tenant)
            .Where(existingStokvel =>
                existingStokvel.Name == name ||
                existingStokvel.Tenant.Slug == slug ||
                existingStokvel.Tenant.Name == name)
            .OrderBy(existingStokvel => existingStokvel.CreatedAt)
            .ThenBy(existingStokvel => existingStokvel.Id)
            .FirstOrDefaultAsync();

        Tenant tenant;

        if (stokvel is null)
        {
            tenant = await context.Tenants
                .Where(existingTenant =>
                    existingTenant.Slug == slug ||
                    existingTenant.Name == name)
                .OrderBy(existingTenant => existingTenant.CreatedAt)
                .ThenBy(existingTenant => existingTenant.Id)
                .FirstOrDefaultAsync()
                ?? new Tenant
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    Slug = slug,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

            if (context.Entry(tenant).State == EntityState.Detached)
            {
                context.Tenants.Add(tenant);
            }

            stokvel = new Stokvel
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Tenant = tenant,
                Name = name,
                CreatedAt = DateTime.UtcNow
            };

            context.Stokvels.Add(stokvel);
        }
        else
        {
            tenant = stokvel.Tenant;
        }

        tenant.Name = name;
        tenant.IsActive = true;
        stokvel.Type = type;
        stokvel.Province = province;
        stokvel.TownOrArea = townOrArea;
        stokvel.ExpectedMemberCount ??= 30;
        stokvel.IsActive = true;
        stokvel.IsSetupComplete = true;
        stokvel.SetupCompletedAt ??= DateTime.UtcNow;

        await context.SaveChangesAsync();

        await EnsureActiveTenantSubscriptionAsync(context, tenant.Id, subscriptionPlan);

        return stokvel;
    }

    private static async Task EnsureActiveTenantSubscriptionAsync(
        ApplicationDbContext context,
        Guid tenantId,
        SubscriptionPlan subscriptionPlan)
    {
        var activeSubscription = await context.TenantSubscriptions
            .Where(subscription =>
                subscription.TenantId == tenantId &&
                subscription.Status == SubscriptionStatus.Active)
            .OrderByDescending(subscription => subscription.StartDate)
            .ThenByDescending(subscription => subscription.Id)
            .FirstOrDefaultAsync();

        if (activeSubscription is null)
        {
            context.TenantSubscriptions.Add(new TenantSubscription
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                SubscriptionPlanId = subscriptionPlan.Id,
                Status = SubscriptionStatus.Active,
                StartDate = DateTime.UtcNow,
                IsTrial = subscriptionPlan.MonthlyPrice == 0
            });
        }
        else
        {
            activeSubscription.SubscriptionPlanId = subscriptionPlan.Id;
            activeSubscription.IsTrial = subscriptionPlan.MonthlyPrice == 0;
        }

        await context.SaveChangesAsync();
    }

    private static async Task<Member> GetOrCreateMemberAsync(
        ApplicationDbContext context,
        Stokvel stokvel,
        ApplicationUser user,
        SisonkeRole role)
    {
        var normalizedIdNumber = NormalizeIdNumber(user.IdNumber);

        var member = await context.Members
            .Where(existingMember =>
                existingMember.TenantId == stokvel.TenantId &&
                existingMember.IdNumber != null &&
                existingMember.IdNumber.Replace(" ", "").Replace("-", "") == normalizedIdNumber)
            .OrderBy(existingMember => existingMember.CreatedAt)
            .ThenBy(existingMember => existingMember.Id)
            .FirstOrDefaultAsync();

        if (member is null)
        {
            member = new Member
            {
                Id = Guid.NewGuid(),
                TenantId = stokvel.TenantId,
                MemberNumber = $"DEMO-{DateTime.Today.Year}-{Random.Shared.Next(1000, 10000)}",
                JoiningDate = DateTime.Today.AddMonths(-6),
                CreatedAt = DateTime.UtcNow
            };

            context.Members.Add(member);
        }

        member.ApplicationUserId = user.Id;
        member.FullName = user.FullName ?? user.Email ?? "Demo Member";
        member.CellphoneNumber = user.CellphoneNumber ?? "0710000000";
        member.EmailAddress = user.Email;
        member.IdNumber = user.IdNumber;
        member.ResidentialArea = user.ResidentialArea;
        member.Status = MemberStatus.Active;
        member.GovernanceStatus = MemberGovernanceStatus.Active;
        member.DefaultRole = role;

        await context.SaveChangesAsync();

        return member;
    }

    private static async Task<MemberDependent> GetOrCreateDependentAsync(
        ApplicationDbContext context,
        Member member,
        string fullName,
        string relationship,
        DateTime dateOfBirth)
    {
        var dependent = await context.MemberDependents
            .Where(existingDependent =>
                existingDependent.MemberId == member.Id &&
                existingDependent.FullName == fullName)
            .OrderBy(existingDependent => existingDependent.CreatedAt)
            .ThenBy(existingDependent => existingDependent.Id)
            .FirstOrDefaultAsync();

        if (dependent is null)
        {
            dependent = new MemberDependent
            {
                Id = Guid.NewGuid(),
                MemberId = member.Id,
                FullName = fullName,
                CreatedAt = DateTime.UtcNow
            };

            context.MemberDependents.Add(dependent);
        }

        dependent.Relationship = relationship;
        dependent.DateOfBirth = dateOfBirth;
        dependent.IsActive = true;
        dependent.IsDeceased = false;

        await context.SaveChangesAsync();

        return dependent;
    }

    private static async Task<FineType> GetOrCreateFineTypeAsync(
        ApplicationDbContext context,
        Guid tenantId,
        string name,
        decimal defaultAmount)
    {
        var fineType = await context.FineTypes
            .Where(existingFineType =>
                existingFineType.TenantId == tenantId &&
                existingFineType.Name == name)
            .OrderBy(existingFineType => existingFineType.CreatedAt)
            .ThenBy(existingFineType => existingFineType.Id)
            .FirstOrDefaultAsync();

        if (fineType is null)
        {
            fineType = new FineType
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Name = name,
                CreatedAt = DateTime.UtcNow
            };

            context.FineTypes.Add(fineType);
        }

        fineType.DefaultAmount = defaultAmount;
        fineType.IsActive = true;

        await context.SaveChangesAsync();

        return fineType;
    }

    private static async Task GetOrCreateFineAsync(
        ApplicationDbContext context,
        Member member,
        FineType fineType)
    {
        var fineExists = await context.MemberFines.AnyAsync(existingFine =>
            existingFine.MemberId == member.Id &&
            existingFine.FineTypeId == fineType.Id &&
            existingFine.Reason == "Demo late coming fine");

        if (fineExists)
        {
            return;
        }

        context.MemberFines.Add(new MemberFine
        {
            Id = Guid.NewGuid(),
            TenantId = member.TenantId,
            MemberId = member.Id,
            FineTypeId = fineType.Id,
            Amount = 50,
            Reason = "Demo late coming fine",
            FineDate = DateTime.Today.AddDays(-3),
            Status = FineStatus.Unpaid,
            CreatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();
    }

    private static async Task GetOrCreateMeetingAsync(ApplicationDbContext context, Stokvel stokvel)
    {
        var meeting = await context.Meetings
            .Include(existingMeeting => existingMeeting.AgendaItems)
            .Where(existingMeeting =>
                existingMeeting.TenantId == stokvel.TenantId &&
                existingMeeting.Title == "Monthly General Meeting")
            .OrderBy(existingMeeting => existingMeeting.CreatedAt)
            .ThenBy(existingMeeting => existingMeeting.Id)
            .FirstOrDefaultAsync();

        if (meeting is null)
        {
            meeting = new Meeting
            {
                Id = Guid.NewGuid(),
                TenantId = stokvel.TenantId,
                Title = "Monthly General Meeting",
                CreatedAt = DateTime.UtcNow
            };

            context.Meetings.Add(meeting);
        }

        meeting.MeetingDate = DateTime.Today.AddDays(7);
        meeting.Venue = "Community Hall";
        meeting.Purpose = "Monthly stokvel administration";
        meeting.Status = MeetingStatus.Planned;

        if (meeting.AgendaItems.Count == 0)
        {
            meeting.AgendaItems.Add(new MeetingAgendaItem
            {
                Id = Guid.NewGuid(),
                Title = "Attendance register",
                DisplayOrder = 1
            });

            meeting.AgendaItems.Add(new MeetingAgendaItem
            {
                Id = Guid.NewGuid(),
                Title = "Financial report",
                DisplayOrder = 2
            });

            meeting.AgendaItems.Add(new MeetingAgendaItem
            {
                Id = Guid.NewGuid(),
                Title = "Claims and member updates",
                DisplayOrder = 3
            });
        }

        await context.SaveChangesAsync();
    }

    private static async Task GetOrCreateConstitutionAsync(ApplicationDbContext context, Stokvel stokvel)
    {
        var constitution = await context.ConstitutionDocuments
            .OrderByDescending(existingConstitution => existingConstitution.CreatedAt)
            .FirstOrDefaultAsync(existingConstitution => existingConstitution.TenantId == stokvel.TenantId);

        if (constitution is not null)
        {
            return;
        }

        context.ConstitutionDocuments.Add(new ConstitutionDocument
        {
            Id = Guid.NewGuid(),
            TenantId = stokvel.TenantId,
            Title = "Demo Constitution",
            Content = "Demo constitution for Aganang Burial Society.",
            IsUploadedDocument = false,
            VersionNumber = 1,
            IsApproved = false,
            CreatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();
    }

    private static async Task GetOrCreateFuneralClaimAsync(
        ApplicationDbContext context,
        Member member,
        MemberDependent dependent)
    {
        var claimExists = await context.FuneralClaims.AnyAsync(existingClaim =>
            existingClaim.MemberId == member.Id &&
            existingClaim.DependentId == dependent.Id &&
            existingClaim.DeceasedFullName == dependent.FullName);

        if (claimExists)
        {
            return;
        }

        context.FuneralClaims.Add(new FuneralClaim
        {
            Id = Guid.NewGuid(),
            TenantId = member.TenantId,
            MemberId = member.Id,
            SubjectType = FuneralClaimSubjectType.Dependent,
            DependentId = dependent.Id,
            DeceasedFullName = dependent.FullName,
            DateOfDeath = DateTime.Today.AddDays(-10),
            Status = FuneralClaimStatus.Draft,
            ClaimReason = "Demo funeral claim for testing.",
            IsWaitingPeriodSatisfied = true,
            IsMemberStatusEligible = true,
            CreatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();
    }

    private static string NormalizeIdNumber(string? idNumber)
    {
        return string.IsNullOrWhiteSpace(idNumber)
            ? string.Empty
            : idNumber.Trim().Replace(" ", string.Empty).Replace("-", string.Empty);
    }
}
