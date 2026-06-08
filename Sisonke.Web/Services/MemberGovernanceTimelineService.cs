using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;
using Sisonke.Web.Services.Dto;

namespace Sisonke.Web.Services;

public class MemberGovernanceTimelineService(ApplicationDbContext context)
{
    private static readonly CultureInfo SouthAfricanCulture = new("en-ZA");

    public async Task<List<MemberGovernanceTimelineItemDto>> GetTimelineByMemberIdAsync(Guid memberId, Guid stokvelId)
    {
        var stokvel = await context.Stokvels
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == stokvelId);

        var member = await context.Members
            .AsNoTracking()
            .Include(m => m.Tenant)
            .FirstOrDefaultAsync(m => m.Id == memberId);

        if (member is null || stokvel is null || member.TenantId != stokvel.TenantId)
        {
            return [];
        }

        var items = new List<MemberGovernanceTimelineItemDto>
        {
            new()
            {
                EventDate = member.JoiningDate == default ? member.CreatedAt : member.JoiningDate,
                EventType = "Membership",
                Title = "Member joined",
                Description = "Member joined the stokvel.",
                Status = member.GovernanceStatus.ToString(),
                Reference = member.MemberNumber,
                Source = "Member",
                BadgeStyle = "neutral",
                LinkUrl = $"/member-profile/{member.Id}"
            }
        };

        await AddDependentEventsAsync(items, memberId);
        await AddApologyEventsAsync(items, memberId, stokvel.TenantId);
        await AddAttendanceEventsAsync(items, memberId, stokvel.TenantId);
        await AddWarningEventsAsync(items, memberId, stokvelId);
        await AddClaimEventsAsync(items, memberId, stokvel.TenantId);
        await AddPayoutAuditEventsAsync(items, memberId, stokvelId);
        await AddContributionEventsAsync(items, memberId, stokvel.TenantId, stokvelId);
        await AddContributionAuditEventsAsync(items, memberId, stokvelId);
        await AddFineEventsAsync(items, memberId, stokvel.TenantId);
        await AddVotingEventsAsync(items, memberId, stokvelId);

        return items
            .OrderByDescending(i => i.EventDate == default ? DateTime.MinValue : i.EventDate)
            .ThenBy(i => i.EventType)
            .ToList();
    }

    private async Task AddDependentEventsAsync(List<MemberGovernanceTimelineItemDto> items, Guid memberId)
    {
        var dependents = await context.MemberDependents
            .AsNoTracking()
            .Where(d => d.MemberId == memberId)
            .ToListAsync();

        foreach (var dependent in dependents)
        {
            items.Add(new MemberGovernanceTimelineItemDto
            {
                EventDate = dependent.IsDeceased
                    ? dependent.DeathReportedAt ?? dependent.DeceasedDate ?? dependent.CreatedAt
                    : dependent.CreatedAt,
                EventType = "Covered Life",
                Title = "Dependent added",
                Description = $"{dependent.FullName} ({DisplayOrDash(dependent.Relationship)})",
                Status = GetDependentStatus(dependent),
                Reference = dependent.IdNumber,
                Source = "Dependent",
                BadgeStyle = dependent.IsActive && !dependent.IsDeceased ? "green" : "neutral"
            });
        }
    }

    private async Task AddApologyEventsAsync(List<MemberGovernanceTimelineItemDto> items, Guid memberId, Guid tenantId)
    {
        var apologies = await context.MeetingApologies
            .AsNoTracking()
            .Include(a => a.Meeting)
            .Where(a => a.MemberId == memberId && a.Meeting != null && a.Meeting.TenantId == tenantId)
            .ToListAsync();

        foreach (var apology in apologies)
        {
            items.Add(new MemberGovernanceTimelineItemDto
            {
                EventDate = apology.SubmittedAt,
                EventType = "Apology",
                Title = "Meeting apology submitted",
                Description = $"{DisplayOrDash(apology.ApologyType)}: {DisplayOrDash(apology.Reason)}",
                Status = DisplayOrDash(apology.Status),
                Reference = GetMeetingReference(apology.Meeting),
                Source = "MeetingApology",
                BadgeStyle = "blue",
                LinkUrl = apology.MeetingId == Guid.Empty ? null : $"/meeting-details/{apology.MeetingId}"
            });
        }
    }

    private async Task AddAttendanceEventsAsync(List<MemberGovernanceTimelineItemDto> items, Guid memberId, Guid tenantId)
    {
        var attendanceRecords = await context.MeetingAttendances
            .AsNoTracking()
            .Include(a => a.Meeting)
            .Where(a => a.MemberId == memberId && a.Meeting.TenantId == tenantId)
            .ToListAsync();

        foreach (var attendance in attendanceRecords)
        {
            var attendanceDetail = attendance.Status.ToString();
            if (attendance.IsLate)
            {
                attendanceDetail += ", late";
            }

            if (attendance.LeftEarly)
            {
                attendanceDetail += ", left early";
            }

            items.Add(new MemberGovernanceTimelineItemDto
            {
                EventDate = attendance.MarkedAt,
                EventType = "Attendance",
                Title = "Meeting attendance recorded",
                Description = string.IsNullOrWhiteSpace(attendance.Notes)
                    ? $"Attendance marked as {attendanceDetail}."
                    : $"Attendance marked as {attendanceDetail}. Notes: {attendance.Notes}",
                Status = attendance.Status.ToString(),
                Reference = GetMeetingReference(attendance.Meeting),
                Source = "MeetingAttendance",
                BadgeStyle = attendance.Status == AttendanceStatus.Present ? "green" : "gold",
                LinkUrl = attendance.MeetingId == Guid.Empty ? null : $"/meeting-details/{attendance.MeetingId}"
            });
        }
    }

    private async Task AddWarningEventsAsync(List<MemberGovernanceTimelineItemDto> items, Guid memberId, Guid stokvelId)
    {
        var warnings = await context.MemberWarnings
            .AsNoTracking()
            .Include(w => w.Meeting)
            .Where(w => w.MemberId == memberId && w.StokvelId == stokvelId)
            .ToListAsync();

        foreach (var warning in warnings)
        {
            items.Add(new MemberGovernanceTimelineItemDto
            {
                EventDate = warning.CreatedAt,
                EventType = "Attendance Warning",
                Title = "Warning issued",
                Description = $"{DisplayOrDash(warning.Reason)} Absence count: {warning.AbsenceCount}.",
                Status = DisplayOrDash(warning.Status),
                Reference = GetMeetingReference(warning.Meeting),
                Source = "MemberWarning",
                BadgeStyle = warning.WarningType == "ExecutiveReviewRequired" ? "red" : "gold",
                LinkUrl = $"/member-warnings/{stokvelId}"
            });
        }
    }

    private async Task AddClaimEventsAsync(List<MemberGovernanceTimelineItemDto> items, Guid memberId, Guid tenantId)
    {
        var claims = await context.FuneralClaims
            .AsNoTracking()
            .Include(c => c.Dependent)
            .Where(c => c.MemberId == memberId && c.TenantId == tenantId)
            .ToListAsync();

        foreach (var claim in claims)
        {
            var reference = GetClaimReference(claim);
            var subject = string.IsNullOrWhiteSpace(claim.DeceasedFullName)
                ? claim.Dependent?.FullName ?? "Claim subject"
                : claim.DeceasedFullName;

            items.Add(new MemberGovernanceTimelineItemDto
            {
                EventDate = claim.SubmittedAt ?? claim.CreatedAt,
                EventType = "Claim",
                Title = claim.SubmittedAt is null ? "Claim captured" : "Claim submitted",
                Description = $"{subject} claim is currently {claim.Status}.",
                Status = claim.Status.ToString(),
                Reference = reference,
                Source = "FuneralClaim",
                BadgeStyle = "gold",
                LinkUrl = $"/claim-details/{claim.Id}"
            });

            if (claim.SecretaryReviewedAt is not null)
            {
                items.Add(new MemberGovernanceTimelineItemDto
                {
                    EventDate = claim.SecretaryReviewedAt.Value,
                    EventType = "Claim",
                    Title = "Claim reviewed by Secretary",
                    Description = GetSecretaryReviewDescription(claim),
                    Status = "Reviewed",
                    Reference = reference,
                    Source = "FuneralClaim",
                    BadgeStyle = "blue",
                    LinkUrl = $"/claim-details/{claim.Id}"
                });
            }

            if (claim.ChairpersonDecisionAt is not null)
            {
                var approved = claim.Status is FuneralClaimStatus.Approved or FuneralClaimStatus.Paid || claim.ApprovedAt is not null;
                items.Add(new MemberGovernanceTimelineItemDto
                {
                    EventDate = claim.ChairpersonDecisionAt.Value,
                    EventType = "Claim",
                    Title = approved ? "Claim approved by Chairperson" : "Claim rejected by Chairperson",
                    Description = DisplayOrDash(claim.ChairpersonDecisionNotes),
                    Status = approved ? "Approved" : "Rejected",
                    Reference = reference,
                    Source = "FuneralClaim",
                    BadgeStyle = approved ? "green" : "red",
                    LinkUrl = $"/claim-details/{claim.Id}"
                });
            }

            if (claim.PayoutPaidAt is not null)
            {
                items.Add(new MemberGovernanceTimelineItemDto
                {
                    EventDate = claim.PayoutPaidAt.Value,
                    EventType = "Payout",
                    Title = "Claim payout marked paid",
                    Description = $"Payout amount: {FormatCurrency(claim.PayoutAmount)}.",
                    Status = "Paid",
                    Reference = claim.PayoutReference ?? reference,
                    Source = "FuneralClaim",
                    BadgeStyle = "green",
                    LinkUrl = $"/claim-details/{claim.Id}"
                });
            }
        }
    }

    private async Task AddPayoutAuditEventsAsync(List<MemberGovernanceTimelineItemDto> items, Guid memberId, Guid stokvelId)
    {
        var audits = await context.ClaimPayoutAudits
            .AsNoTracking()
            .Include(a => a.FuneralClaim)
            .Where(a => a.MemberId == memberId && a.StokvelId == stokvelId)
            .ToListAsync();

        foreach (var audit in audits)
        {
            items.Add(new MemberGovernanceTimelineItemDto
            {
                EventDate = audit.CreatedAt,
                EventType = "Payout",
                Title = "Claim payout audit recorded",
                Description = $"{DisplayOrDash(audit.Action)}. Amount: {FormatCurrency(audit.NewPayoutAmount)}. {DisplayOrDash(audit.Notes)}",
                Status = DisplayOrDash(audit.NewStatus ?? audit.Action),
                Reference = audit.PayoutReference ?? (audit.FuneralClaim is null ? null : GetClaimReference(audit.FuneralClaim)),
                Source = "ClaimPayoutAudit",
                BadgeStyle = "green",
                LinkUrl = audit.FuneralClaimId == Guid.Empty ? null : $"/claim-details/{audit.FuneralClaimId}"
            });
        }
    }

    private async Task AddContributionEventsAsync(List<MemberGovernanceTimelineItemDto> items, Guid memberId, Guid tenantId, Guid stokvelId)
    {
        var contributions = await context.MemberContributions
            .AsNoTracking()
            .Include(c => c.ContributionCycle)
            .Where(c => c.MemberId == memberId && c.TenantId == tenantId)
            .ToListAsync();

        foreach (var contribution in contributions)
        {
            var cycleName = contribution.ContributionCycle?.Name ?? contribution.ContributionCycle?.PeriodStart.ToString("MMM yyyy") ?? "Contribution";
            items.Add(new MemberGovernanceTimelineItemDto
            {
                EventDate = contribution.FullyPaidDate ?? contribution.ContributionCycle?.DueDate ?? contribution.CreatedAt,
                EventType = "Contribution",
                Title = "Contribution payment status",
                Description = $"{cycleName}: expected {FormatCurrency(contribution.ExpectedAmount)}, paid {FormatCurrency(contribution.PaidAmount)}, outstanding {FormatCurrency(contribution.OutstandingAmount)}.",
                Status = contribution.Status.ToString(),
                Reference = cycleName,
                Source = "MemberContribution",
                BadgeStyle = contribution.Status == PaymentStatus.Paid ? "green" : contribution.OutstandingAmount > 0 ? "red" : "neutral",
                LinkUrl = $"/member-statement/{memberId}/{stokvelId}"
            });
        }
    }

    private async Task AddContributionAuditEventsAsync(List<MemberGovernanceTimelineItemDto> items, Guid memberId, Guid stokvelId)
    {
        var audits = await context.ContributionPaymentAudits
            .AsNoTracking()
            .Include(a => a.ContributionPayment)
            .ThenInclude(c => c!.ContributionCycle)
            .Where(a => a.MemberId == memberId && a.StokvelId == stokvelId)
            .ToListAsync();

        foreach (var audit in audits)
        {
            items.Add(new MemberGovernanceTimelineItemDto
            {
                EventDate = audit.CreatedAt,
                EventType = "Contribution Audit",
                Title = "Contribution payment captured",
                Description = $"{DisplayOrDash(audit.Action)}. Amount paid: {FormatCurrency(audit.NewAmountPaid)}. {DisplayOrDash(audit.Notes)}",
                Status = DisplayOrDash(audit.NewStatus ?? audit.Action),
                Reference = audit.PaymentReference ?? audit.ContributionPayment?.ContributionCycle?.Name,
                Source = "ContributionPaymentAudit",
                BadgeStyle = "green",
                LinkUrl = $"/member-statement/{memberId}/{stokvelId}"
            });
        }
    }

    private async Task AddFineEventsAsync(List<MemberGovernanceTimelineItemDto> items, Guid memberId, Guid tenantId)
    {
        var fines = await context.MemberFines
            .AsNoTracking()
            .Include(f => f.FineType)
            .Where(f => f.MemberId == memberId && f.TenantId == tenantId)
            .ToListAsync();

        foreach (var fine in fines)
        {
            items.Add(new MemberGovernanceTimelineItemDto
            {
                EventDate = fine.PaidDate ?? fine.FineDate,
                EventType = "Fine",
                Title = fine.Status == FineStatus.Paid ? "Fine paid" : "Fine issued",
                Description = $"{fine.FineType.Name}: {FormatCurrency(fine.Amount)}. Reason: {DisplayOrDash(fine.Reason)}",
                Status = fine.Status.ToString(),
                Reference = fine.FineType.Name,
                Source = "MemberFine",
                BadgeStyle = fine.Status == FineStatus.Paid ? "green" : "red"
            });
        }
    }

    private async Task AddVotingEventsAsync(List<MemberGovernanceTimelineItemDto> items, Guid memberId, Guid stokvelId)
    {
        var votes = await context.MemberVotes
            .AsNoTracking()
            .Include(v => v.VoteMotion)
            .Include(v => v.VoteOption)
            .Where(v => v.MemberId == memberId && v.VoteMotion != null && v.VoteMotion.StokvelId == stokvelId)
            .ToListAsync();

        foreach (var vote in votes)
        {
            var motion = vote.VoteMotion;
            items.Add(new MemberGovernanceTimelineItemDto
            {
                EventDate = vote.VotedAt,
                EventType = "Vote",
                Title = "Vote cast",
                Description = motion?.Title ?? "Vote motion",
                Status = "Voted",
                Reference = motion is null
                    ? vote.VoteOption?.OptionText
                    : $"{motion.Status}{(string.IsNullOrWhiteSpace(motion.DecisionOutcome) ? string.Empty : $" - {motion.DecisionOutcome}")}",
                Source = "MemberVote",
                BadgeStyle = "gold",
                LinkUrl = $"/votes/{stokvelId}"
            });
        }
    }

    private static string GetDependentStatus(MemberDependent dependent)
    {
        if (dependent.IsDeceased)
        {
            return "Deceased";
        }

        return dependent.IsActive ? "Active" : "Inactive";
    }

    private static string GetMeetingReference(Meeting? meeting)
    {
        return meeting is null
            ? null!
            : $"{meeting.Title} - {meeting.MeetingDate:dd MMM yyyy}";
    }

    private static string GetClaimReference(FuneralClaim claim)
    {
        return string.IsNullOrWhiteSpace(claim.ClaimReference)
            ? $"CLM-{claim.Id.ToString("N")[..8].ToUpperInvariant()}"
            : claim.ClaimReference;
    }

    private static string GetSecretaryReviewDescription(FuneralClaim claim)
    {
        var recommendation = claim.SecretaryRecommendedApproval switch
        {
            true => "Secretary recommended approval.",
            false => "Secretary recommended rejection.",
            _ => "Secretary reviewed the claim."
        };

        return string.IsNullOrWhiteSpace(claim.SecretaryReviewNotes)
            ? recommendation
            : $"{recommendation} Notes: {claim.SecretaryReviewNotes}";
    }

    private static string FormatCurrency(decimal? amount)
    {
        return (amount ?? 0m).ToString("C0", SouthAfricanCulture);
    }

    private static string DisplayOrDash(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
    }
}
