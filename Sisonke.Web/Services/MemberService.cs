using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Services;

public class MemberService(
    ApplicationDbContext context,
    OperatingRuleService operatingRuleService,
    StokvelService stokvelService)
{
    public async Task<Member?> GetMemberByIdAsync(Guid memberId)
    {
        return await context.Members
            .Include(member => member.Tenant)
            .Include(member => member.NextOfKinRecords)
            .Include(member => member.Beneficiaries)
            .SingleOrDefaultAsync(member => member.Id == memberId);
    }

    public async Task<List<Member>> GetMembersByStokvelIdAsync(Guid stokvelId)
    {
        var stokvel = await context.Stokvels
            .SingleOrDefaultAsync(existingStokvel => existingStokvel.Id == stokvelId);

        if (stokvel is null)
        {
            return [];
        }

        return await context.Members
            .Include(member => member.NextOfKinRecords)
            .Include(member => member.Beneficiaries)
            .Where(member => member.TenantId == stokvel.TenantId)
            .OrderBy(member => member.FullName)
            .ToListAsync();
    }

    public async Task<int> GetMemberCountByStokvelIdAsync(Guid stokvelId)
    {
        var stokvel = await context.Stokvels
            .SingleOrDefaultAsync(existingStokvel => existingStokvel.Id == stokvelId);

        if (stokvel is null)
        {
            return 0;
        }

        return await context.Members
            .CountAsync(member => member.TenantId == stokvel.TenantId);
    }

    public async Task<Member?> AddMemberAsync(Guid stokvelId, Member member)
    {
        if (!await stokvelService.CanAddMemberAsync(stokvelId))
        {
            return null;
        }

        var stokvel = await context.Stokvels
            .SingleOrDefaultAsync(existingStokvel => existingStokvel.Id == stokvelId);

        if (stokvel is null)
        {
            return null;
        }

        member.TenantId = stokvel.TenantId;

        if (member.Id == Guid.Empty)
        {
            member.Id = Guid.NewGuid();
        }

        if (member.JoiningDate == default)
        {
            member.JoiningDate = DateTime.Today;
        }

        if (string.IsNullOrWhiteSpace(member.MemberNumber))
        {
            member.MemberNumber = $"MEM-{DateTime.Today.Year}-{Random.Shared.Next(1000, 10000)}";
        }

        member.CreatedAt = DateTime.UtcNow;

        context.Members.Add(member);
        await context.SaveChangesAsync();

        return member;
    }

    public async Task<Member?> EnsureCreatorMemberForStokvelAsync(
        Guid stokvelId,
        string applicationUserId,
        string? fullName,
        string? emailAddress,
        string? cellphoneNumber,
        string? idNumber,
        string? residentialArea)
    {
        if (string.IsNullOrWhiteSpace(applicationUserId))
        {
            return null;
        }

        var stokvel = await context.Stokvels
            .SingleOrDefaultAsync(existingStokvel => existingStokvel.Id == stokvelId);

        if (stokvel is null)
        {
            return null;
        }

        var normalizedIdNumber = NormalizeIdNumber(idNumber);
        var tenantMembers = await context.Members
            .Where(member => member.TenantId == stokvel.TenantId)
            .ToListAsync();

        var existingMember = string.IsNullOrWhiteSpace(normalizedIdNumber)
            ? tenantMembers.FirstOrDefault(member => member.ApplicationUserId == applicationUserId)
            : tenantMembers.FirstOrDefault(member => NormalizeIdNumber(member.IdNumber) == normalizedIdNumber);

        var hasOfficeBearer = tenantMembers.Any(member => IsOfficeBearerRole(member.DefaultRole));

        if (existingMember is not null)
        {
            existingMember.ApplicationUserId = applicationUserId;
            existingMember.EmailAddress = string.IsNullOrWhiteSpace(existingMember.EmailAddress) ? emailAddress : existingMember.EmailAddress;
            existingMember.CellphoneNumber = string.IsNullOrWhiteSpace(existingMember.CellphoneNumber) ? cellphoneNumber ?? "Not captured" : existingMember.CellphoneNumber;
            existingMember.IdNumber = string.IsNullOrWhiteSpace(existingMember.IdNumber) ? idNumber : existingMember.IdNumber;
            existingMember.ResidentialArea = string.IsNullOrWhiteSpace(existingMember.ResidentialArea) ? residentialArea : existingMember.ResidentialArea;

            if (!hasOfficeBearer)
            {
                existingMember.DefaultRole = SisonkeRole.Creator;
            }

            if (existingMember.Status != MemberStatus.Active)
            {
                existingMember.Status = MemberStatus.Active;
            }

            await context.SaveChangesAsync();
            return existingMember;
        }

        var creatorMember = new Member
        {
            Id = Guid.NewGuid(),
            TenantId = stokvel.TenantId,
            ApplicationUserId = applicationUserId,
            MemberNumber = $"MEM-{DateTime.Today.Year}-{Random.Shared.Next(1000, 10000)}",
            FullName = string.IsNullOrWhiteSpace(fullName) ? emailAddress ?? "Stokvel Creator" : fullName.Trim(),
            CellphoneNumber = string.IsNullOrWhiteSpace(cellphoneNumber) ? "Not captured" : cellphoneNumber.Trim(),
            EmailAddress = emailAddress,
            IdNumber = idNumber,
            ResidentialArea = residentialArea,
            JoiningDate = DateTime.Today,
            Status = MemberStatus.Active,
            GovernanceStatus = MemberGovernanceStatus.Active,
            DefaultRole = hasOfficeBearer ? SisonkeRole.Member : SisonkeRole.Creator,
            CreatedAt = DateTime.UtcNow
        };

        context.Members.Add(creatorMember);
        await context.SaveChangesAsync();

        return creatorMember;
    }

    public async Task<Member?> UpdateMemberAsync(Member updatedMember)
    {
        var member = await context.Members
            .SingleOrDefaultAsync(existingMember => existingMember.Id == updatedMember.Id);

        if (member is null)
        {
            return null;
        }

        member.FullName = updatedMember.FullName;
        member.CellphoneNumber = updatedMember.CellphoneNumber;
        member.EmailAddress = updatedMember.EmailAddress;
        member.IdNumber = updatedMember.IdNumber;
        member.ResidentialArea = updatedMember.ResidentialArea;
        member.JoiningDate = updatedMember.JoiningDate;
        member.Status = updatedMember.Status;
        member.DefaultRole = updatedMember.DefaultRole;
        member.IsInCoolingPeriod = updatedMember.IsInCoolingPeriod;
        member.CoolingPeriodEndDate = updatedMember.CoolingPeriodEndDate;
        member.IsDeceased = updatedMember.IsDeceased;

        if (updatedMember.IsDeceased)
        {
            member.DeceasedDate = updatedMember.DeceasedDate;

            if (member.DeathReportedAt is null)
            {
                member.DeathReportedAt = DateTime.UtcNow;
            }
        }
        else
        {
            member.DeceasedDate = null;
            member.DeathReportedAt = null;
        }

        await context.SaveChangesAsync();

        return member;
    }

    public async Task<Member?> UpdateMemberGovernanceStatusAsync(
        Guid memberId,
        MemberGovernanceStatus governanceStatus,
        string? reason)
    {
        var member = await context.Members
            .SingleOrDefaultAsync(existingMember => existingMember.Id == memberId);

        if (member is null)
        {
            return null;
        }

        var now = DateTime.UtcNow;

        member.GovernanceStatus = governanceStatus;
        member.GovernanceStatusReason = reason;
        member.GovernanceStatusChangedAt = now;

        if (governanceStatus == MemberGovernanceStatus.Suspended)
        {
            member.SuspendedAt ??= now;
        }
        else if (governanceStatus == MemberGovernanceStatus.Expelled)
        {
            member.ExpelledAt ??= now;
        }
        else if (governanceStatus == MemberGovernanceStatus.Warning)
        {
            member.LastWarningIssuedAt ??= now;
        }
        else if (governanceStatus == MemberGovernanceStatus.Active)
        {
            member.SuspendedAt = null;
            member.ExpelledAt = null;
        }
        else if (governanceStatus == MemberGovernanceStatus.Deceased)
        {
            member.IsDeceased = true;
            member.DeathReportedAt ??= now;
        }

        await context.SaveChangesAsync();

        return member;
    }

    public async Task<NextOfKin?> AddNextOfKinAsync(Guid memberId, NextOfKin nextOfKin)
    {
        var member = await context.Members
            .SingleOrDefaultAsync(existingMember => existingMember.Id == memberId);

        if (member is null)
        {
            return null;
        }

        if (!await CanAddNextOfKinAsync(memberId))
        {
            return null;
        }

        if (nextOfKin.Id == Guid.Empty)
        {
            nextOfKin.Id = Guid.NewGuid();
        }

        nextOfKin.MemberId = memberId;

        context.NextOfKinRecords.Add(nextOfKin);
        await context.SaveChangesAsync();

        return nextOfKin;
    }

    public async Task<NextOfKin?> GetNextOfKinByIdAsync(Guid nextOfKinId)
    {
        return await context.NextOfKinRecords
            .Include(nextOfKin => nextOfKin.Member)
            .SingleOrDefaultAsync(nextOfKin => nextOfKin.Id == nextOfKinId);
    }

    public async Task<NextOfKin?> UpdateNextOfKinAsync(NextOfKin updatedNextOfKin)
    {
        var nextOfKin = await context.NextOfKinRecords
            .SingleOrDefaultAsync(existingNextOfKin => existingNextOfKin.Id == updatedNextOfKin.Id);

        if (nextOfKin is null)
        {
            return null;
        }

        nextOfKin.FullName = updatedNextOfKin.FullName;
        nextOfKin.Relationship = updatedNextOfKin.Relationship;
        nextOfKin.CellphoneNumber = updatedNextOfKin.CellphoneNumber;
        nextOfKin.Address = updatedNextOfKin.Address;
        nextOfKin.IsPrimary = updatedNextOfKin.IsPrimary;

        await context.SaveChangesAsync();

        return nextOfKin;
    }

    public async Task<Beneficiary?> AddBeneficiaryAsync(Guid memberId, Beneficiary beneficiary)
    {
        var member = await context.Members
            .SingleOrDefaultAsync(existingMember => existingMember.Id == memberId);

        if (member is null)
        {
            return null;
        }

        if (!await CanAddBeneficiaryAsync(memberId))
        {
            return null;
        }

        if (beneficiary.Id == Guid.Empty)
        {
            beneficiary.Id = Guid.NewGuid();
        }

        beneficiary.MemberId = memberId;

        context.Beneficiaries.Add(beneficiary);
        await context.SaveChangesAsync();

        return beneficiary;
    }

    public async Task<Beneficiary?> GetBeneficiaryByIdAsync(Guid beneficiaryId)
    {
        return await context.Beneficiaries
            .Include(beneficiary => beneficiary.Member)
            .SingleOrDefaultAsync(beneficiary => beneficiary.Id == beneficiaryId);
    }

    public async Task<Beneficiary?> UpdateBeneficiaryAsync(Beneficiary updatedBeneficiary)
    {
        var beneficiary = await context.Beneficiaries
            .SingleOrDefaultAsync(existingBeneficiary => existingBeneficiary.Id == updatedBeneficiary.Id);

        if (beneficiary is null)
        {
            return null;
        }

        beneficiary.FullName = updatedBeneficiary.FullName;
        beneficiary.Relationship = updatedBeneficiary.Relationship;
        beneficiary.IdNumber = updatedBeneficiary.IdNumber;
        beneficiary.CellphoneNumber = updatedBeneficiary.CellphoneNumber;
        beneficiary.DateOfBirth = updatedBeneficiary.DateOfBirth;
        beneficiary.IsActive = updatedBeneficiary.IsActive;

        await context.SaveChangesAsync();

        return beneficiary;
    }

    public async Task<List<MemberDependent>> GetDependentsByMemberIdAsync(Guid memberId)
    {
        return await context.MemberDependents
            .Where(dependent => dependent.MemberId == memberId)
            .OrderBy(dependent => dependent.FullName)
            .ToListAsync();
    }

    public async Task<bool> CanAddDependentAsync(Guid memberId)
    {
        var member = await context.Members
            .SingleOrDefaultAsync(existingMember => existingMember.Id == memberId);

        if (member is null)
        {
            return false;
        }

        var stokvel = await context.Stokvels
            .SingleOrDefaultAsync(existingStokvel => existingStokvel.TenantId == member.TenantId);

        if (stokvel is null)
        {
            return false;
        }

        var maxDependents = await operatingRuleService.GetMaxDependentsAsync(stokvel.Id);

        if (maxDependents <= 0)
        {
            return false;
        }

        var dependentCount = await context.MemberDependents
            .CountAsync(dependent => dependent.MemberId == memberId && dependent.IsActive);

        return dependentCount < maxDependents;
    }

    public async Task<MemberDependent?> AddDependentAsync(Guid memberId, MemberDependent dependent)
    {
        if (!await CanAddDependentAsync(memberId))
        {
            return null;
        }

        dependent.Id = Guid.NewGuid();
        dependent.MemberId = memberId;

        if (dependent.IsDeceased)
        {
            dependent.IsActive = false;
            dependent.DeathReportedAt ??= DateTime.UtcNow;
        }
        else
        {
            dependent.IsActive = true;
        }

        dependent.CreatedAt = DateTime.UtcNow;

        context.MemberDependents.Add(dependent);
        await context.SaveChangesAsync();

        return dependent;
    }

    public async Task<MemberDependent?> GetDependentByIdAsync(Guid dependentId)
    {
        return await context.MemberDependents
            .Include(dependent => dependent.Member)
            .SingleOrDefaultAsync(dependent => dependent.Id == dependentId);
    }

    public async Task<MemberDependent?> UpdateDependentAsync(Guid dependentId, MemberDependent updatedDependent)
    {
        var dependent = await context.MemberDependents
            .SingleOrDefaultAsync(existingDependent => existingDependent.Id == dependentId);

        if (dependent is null)
        {
            return null;
        }

        dependent.FullName = updatedDependent.FullName;
        dependent.Relationship = updatedDependent.Relationship;
        dependent.DateOfBirth = updatedDependent.DateOfBirth;
        dependent.IdNumber = updatedDependent.IdNumber;
        dependent.CellphoneNumber = updatedDependent.CellphoneNumber;

        if (updatedDependent.IsDeceased)
        {
            dependent.IsDeceased = true;
            dependent.IsActive = false;
            dependent.DeceasedDate = updatedDependent.DeceasedDate;

            if (dependent.DeathReportedAt is null)
            {
                dependent.DeathReportedAt = DateTime.UtcNow;
            }
        }
        else
        {
            dependent.IsDeceased = false;
            dependent.IsActive = updatedDependent.IsActive;
            dependent.DeceasedDate = null;
            dependent.DeathReportedAt = null;
        }

        await context.SaveChangesAsync();

        return dependent;
    }

    public async Task<bool> DeleteDependentAsync(Guid dependentId)
    {
        var dependent = await context.MemberDependents
            .SingleOrDefaultAsync(existingDependent => existingDependent.Id == dependentId);

        if (dependent is null)
        {
            return false;
        }

        context.MemberDependents.Remove(dependent);
        await context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> CanAddNextOfKinAsync(Guid memberId)
    {
        var member = await context.Members
            .SingleOrDefaultAsync(existingMember => existingMember.Id == memberId);

        if (member is null)
        {
            return false;
        }

        var stokvel = await context.Stokvels
            .SingleOrDefaultAsync(existingStokvel => existingStokvel.TenantId == member.TenantId);

        if (stokvel is null)
        {
            return false;
        }

        var maxNextOfKin = await operatingRuleService.GetMaxNextOfKinAsync(stokvel.Id);
        var nextOfKinCount = await context.NextOfKinRecords
            .CountAsync(nextOfKin => nextOfKin.MemberId == memberId);

        return nextOfKinCount < maxNextOfKin;
    }

    public async Task<bool> CanAddBeneficiaryAsync(Guid memberId)
    {
        var member = await context.Members
            .SingleOrDefaultAsync(existingMember => existingMember.Id == memberId);

        if (member is null)
        {
            return false;
        }

        var stokvel = await context.Stokvels
            .SingleOrDefaultAsync(existingStokvel => existingStokvel.TenantId == member.TenantId);

        if (stokvel is null)
        {
            return false;
        }

        var maxBeneficiaries = await operatingRuleService.GetMaxBeneficiariesAsync(stokvel.Id);
        var beneficiaryCount = await context.Beneficiaries
            .CountAsync(beneficiary => beneficiary.MemberId == memberId);

        return beneficiaryCount < maxBeneficiaries;
    }

    private static string NormalizeIdNumber(string? idNumber)
    {
        return string.IsNullOrWhiteSpace(idNumber)
            ? string.Empty
            : idNumber.Trim().Replace(" ", string.Empty).Replace("-", string.Empty);
    }

    private static bool IsOfficeBearerRole(SisonkeRole role)
    {
        return role is SisonkeRole.StokvelAdmin
            or SisonkeRole.Chairperson
            or SisonkeRole.Secretary
            or SisonkeRole.Treasurer
            or SisonkeRole.CommitteeMember
            or SisonkeRole.Creator;
    }
}
