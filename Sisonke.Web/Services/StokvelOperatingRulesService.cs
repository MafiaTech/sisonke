using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;

namespace Sisonke.Web.Services;

public class StokvelOperatingRulesService(ApplicationDbContext context)
{
    public async Task<StokvelOperatingRules?> GetRulesByStokvelIdAsync(Guid stokvelId)
    {
        return await context.StokvelOperatingRules
            .Include(rules => rules.Stokvel)
            .FirstOrDefaultAsync(rules => rules.StokvelId == stokvelId);
    }

    public async Task<StokvelOperatingRules> GetOrCreateDefaultRulesAsync(
        Guid stokvelId,
        string stokvelType,
        Guid? createdByMemberId = null)
    {
        var existingRules = await GetRulesByStokvelIdAsync(stokvelId);

        if (existingRules is not null)
        {
            return existingRules;
        }

        var stokvelExists = await context.Stokvels.AnyAsync(stokvel => stokvel.Id == stokvelId);

        if (!stokvelExists)
        {
            throw new InvalidOperationException("Stokvel could not be found for operating rules.");
        }

        var rules = BuildDefaultRules(stokvelId, stokvelType, createdByMemberId);
        context.StokvelOperatingRules.Add(rules);
        await context.SaveChangesAsync();

        return rules;
    }

    public async Task<bool> SaveRulesAsync(StokvelOperatingRules rules, Guid updatedByMemberId)
    {
        var existingRules = await context.StokvelOperatingRules
            .FirstOrDefaultAsync(currentRules => currentRules.Id == rules.Id);

        if (existingRules is null)
        {
            return false;
        }

        existingRules.StokvelType = rules.StokvelType;
        existingRules.MonthlyContributionAmount = rules.MonthlyContributionAmount;
        existingRules.ContributionDueDay = rules.ContributionDueDay;
        existingRules.GracePeriodDays = rules.GracePeriodDays;
        existingRules.AllowPartialPayments = rules.AllowPartialPayments;
        existingRules.ChargeLatePaymentFine = rules.ChargeLatePaymentFine;
        existingRules.LatePaymentFineAmount = rules.LatePaymentFineAmount;

        existingRules.EnableDependents = rules.EnableDependents;
        existingRules.MaximumDependents = rules.MaximumDependents;
        existingRules.MemberWaitingPeriodMonths = rules.MemberWaitingPeriodMonths;
        existingRules.DependentWaitingPeriodMonths = rules.DependentWaitingPeriodMonths;
        existingRules.RequireDependentIdNumber = rules.RequireDependentIdNumber;

        existingRules.EnableClaims = rules.EnableClaims;
        existingRules.RequireDeathCertificateForClaims = rules.RequireDeathCertificateForClaims;
        existingRules.RequireClaimDocuments = rules.RequireClaimDocuments;
        existingRules.BlockClaimsIfMemberInArrears = rules.BlockClaimsIfMemberInArrears;
        existingRules.BlockClaimsIfMemberSuspended = rules.BlockClaimsIfMemberSuspended;
        existingRules.DefaultClaimPayoutAmount = rules.DefaultClaimPayoutAmount;

        existingRules.EnableAttendanceTracking = rules.EnableAttendanceTracking;
        existingRules.AbsenceReminderThreshold = rules.AbsenceReminderThreshold;
        existingRules.FormalWarningThreshold = rules.FormalWarningThreshold;
        existingRules.ExecutiveReviewThreshold = rules.ExecutiveReviewThreshold;
        existingRules.ApologyDeadlineHoursBeforeMeeting = rules.ApologyDeadlineHoursBeforeMeeting;
        existingRules.ChargeLateApologyFine = rules.ChargeLateApologyFine;
        existingRules.LateApologyFineAmount = rules.LateApologyFineAmount;
        existingRules.ChargeAbsenceWithoutApologyFine = rules.ChargeAbsenceWithoutApologyFine;
        existingRules.AbsenceWithoutApologyFineAmount = rules.AbsenceWithoutApologyFineAmount;

        existingRules.EnableMeetings = rules.EnableMeetings;
        existingRules.RequireMinutesApproval = rules.RequireMinutesApproval;
        existingRules.QuorumPercentage = rules.QuorumPercentage;

        existingRules.EnableVoting = rules.EnableVoting;
        existingRules.DefaultVotingApprovalThreshold = rules.DefaultVotingApprovalThreshold;
        existingRules.AllowAnonymousVoting = rules.AllowAnonymousVoting;

        existingRules.EnableRotationalPayouts = rules.EnableRotationalPayouts;
        existingRules.PayoutFrequency = string.IsNullOrWhiteSpace(rules.PayoutFrequency)
            ? null
            : rules.PayoutFrequency.Trim();
        existingRules.RequireTreasurerConfirmationForPayouts = rules.RequireTreasurerConfirmationForPayouts;

        existingRules.EnableGroceryModule = rules.EnableGroceryModule;
        existingRules.EnableInvestmentModule = rules.EnableInvestmentModule;
        existingRules.EnablePropertyModule = rules.EnablePropertyModule;

        existingRules.UpdatedAt = DateTime.UtcNow;
        existingRules.UpdatedByMemberId = updatedByMemberId;

        await context.SaveChangesAsync();

        return true;
    }

    public StokvelOperatingRules BuildDefaultRules(Guid stokvelId, string stokvelType, Guid? createdByMemberId = null)
    {
        var normalizedType = stokvelType?.Trim() ?? string.Empty;

        if (normalizedType.Contains("Savings", StringComparison.OrdinalIgnoreCase))
        {
            return BuildSavingsDefaults(stokvelId, normalizedType, createdByMemberId);
        }

        if (normalizedType.Contains("Grocery", StringComparison.OrdinalIgnoreCase))
        {
            var rules = BuildGenericDefaults(stokvelId, "Grocery Stokvel", createdByMemberId);
            rules.EnableGroceryModule = true;
            rules.EnableRotationalPayouts = true;
            rules.PayoutFrequency = "Monthly";

            return rules;
        }

        if (normalizedType.Contains("Investment", StringComparison.OrdinalIgnoreCase))
        {
            var rules = BuildGenericDefaults(stokvelId, "Investment Club", createdByMemberId);
            rules.EnableInvestmentModule = true;
            rules.EnableRotationalPayouts = false;

            return rules;
        }

        if (normalizedType.Contains("Property", StringComparison.OrdinalIgnoreCase))
        {
            var rules = BuildGenericDefaults(stokvelId, "Property Stokvel", createdByMemberId);
            rules.EnableInvestmentModule = true;
            rules.EnablePropertyModule = true;
            rules.EnableRotationalPayouts = false;

            return rules;
        }

        return BuildBurialDefaults(stokvelId, createdByMemberId);
    }

    private static StokvelOperatingRules BuildBurialDefaults(Guid stokvelId, Guid? createdByMemberId)
    {
        return new StokvelOperatingRules
        {
            Id = Guid.NewGuid(),
            StokvelId = stokvelId,
            StokvelType = "Burial Society",
            MonthlyContributionAmount = 80,
            ContributionDueDay = 1,
            GracePeriodDays = 7,
            AllowPartialPayments = true,
            ChargeLatePaymentFine = true,
            LatePaymentFineAmount = 50,
            EnableDependents = true,
            MaximumDependents = 6,
            MemberWaitingPeriodMonths = 3,
            DependentWaitingPeriodMonths = 3,
            RequireDependentIdNumber = true,
            EnableClaims = true,
            RequireDeathCertificateForClaims = true,
            RequireClaimDocuments = true,
            BlockClaimsIfMemberInArrears = false,
            BlockClaimsIfMemberSuspended = true,
            DefaultClaimPayoutAmount = 8000,
            EnableAttendanceTracking = true,
            AbsenceReminderThreshold = 2,
            FormalWarningThreshold = 3,
            ExecutiveReviewThreshold = 4,
            ApologyDeadlineHoursBeforeMeeting = 24,
            ChargeLateApologyFine = true,
            LateApologyFineAmount = 50,
            ChargeAbsenceWithoutApologyFine = true,
            AbsenceWithoutApologyFineAmount = 100,
            EnableMeetings = true,
            RequireMinutesApproval = true,
            QuorumPercentage = 50,
            EnableVoting = true,
            DefaultVotingApprovalThreshold = 50,
            AllowAnonymousVoting = false,
            EnableRotationalPayouts = false,
            RequireTreasurerConfirmationForPayouts = true,
            EnableGroceryModule = false,
            EnableInvestmentModule = false,
            EnablePropertyModule = false,
            CreatedAt = DateTime.UtcNow,
            CreatedByMemberId = createdByMemberId
        };
    }

    private static StokvelOperatingRules BuildSavingsDefaults(Guid stokvelId, string stokvelType, Guid? createdByMemberId)
    {
        var rules = BuildGenericDefaults(stokvelId, string.IsNullOrWhiteSpace(stokvelType) ? "Savings Stokvel" : stokvelType, createdByMemberId);
        rules.MonthlyContributionAmount = 500;
        rules.EnableClaims = false;
        rules.EnableDependents = false;
        rules.EnableRotationalPayouts = true;
        rules.EnableMeetings = true;
        rules.EnableVoting = true;
        rules.RequireTreasurerConfirmationForPayouts = true;
        rules.PayoutFrequency = "Monthly";

        return rules;
    }

    private static StokvelOperatingRules BuildGenericDefaults(Guid stokvelId, string stokvelType, Guid? createdByMemberId)
    {
        return new StokvelOperatingRules
        {
            Id = Guid.NewGuid(),
            StokvelId = stokvelId,
            StokvelType = string.IsNullOrWhiteSpace(stokvelType) ? "Generic Stokvel" : stokvelType,
            MonthlyContributionAmount = 100,
            ContributionDueDay = 1,
            GracePeriodDays = 7,
            AllowPartialPayments = true,
            ChargeLatePaymentFine = false,
            LatePaymentFineAmount = 0,
            EnableDependents = false,
            MaximumDependents = 0,
            MemberWaitingPeriodMonths = 0,
            DependentWaitingPeriodMonths = 0,
            RequireDependentIdNumber = false,
            EnableClaims = false,
            RequireDeathCertificateForClaims = false,
            RequireClaimDocuments = false,
            BlockClaimsIfMemberInArrears = false,
            BlockClaimsIfMemberSuspended = false,
            DefaultClaimPayoutAmount = 0,
            EnableAttendanceTracking = true,
            AbsenceReminderThreshold = 2,
            FormalWarningThreshold = 3,
            ExecutiveReviewThreshold = 4,
            ApologyDeadlineHoursBeforeMeeting = 24,
            ChargeLateApologyFine = false,
            LateApologyFineAmount = 0,
            ChargeAbsenceWithoutApologyFine = false,
            AbsenceWithoutApologyFineAmount = 0,
            EnableMeetings = true,
            RequireMinutesApproval = true,
            QuorumPercentage = 50,
            EnableVoting = true,
            DefaultVotingApprovalThreshold = 50,
            AllowAnonymousVoting = false,
            EnableRotationalPayouts = false,
            RequireTreasurerConfirmationForPayouts = true,
            EnableGroceryModule = false,
            EnableInvestmentModule = false,
            EnablePropertyModule = false,
            CreatedAt = DateTime.UtcNow,
            CreatedByMemberId = createdByMemberId
        };
    }
}
