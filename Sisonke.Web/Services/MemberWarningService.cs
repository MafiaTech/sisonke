using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Services;

public class MemberWarningService(ApplicationDbContext context)
{
    private const string StatusOpen = "Open";
    private const string StatusAcknowledged = "Acknowledged";
    private const string StatusResolved = "Resolved";
    private const string WarningReminder = "Reminder";
    private const string WarningFormal = "FormalWarning";
    private const string WarningExecutiveReviewRequired = "ExecutiveReviewRequired";

    public async Task<int> EvaluateAttendanceWarningsAsync(Guid meetingId, Guid processedByMemberId)
    {
        var meeting = await context.Meetings
            .SingleOrDefaultAsync(existingMeeting => existingMeeting.Id == meetingId);

        if (meeting is null)
        {
            return 0;
        }

        var stokvel = await context.Stokvels
            .Where(existingStokvel => existingStokvel.TenantId == meeting.TenantId)
            .OrderBy(existingStokvel => existingStokvel.CreatedAt)
            .ThenBy(existingStokvel => existingStokvel.Name)
            .FirstOrDefaultAsync();

        if (stokvel is null)
        {
            return 0;
        }

        var attendanceRecords = await context.MeetingAttendances
            .Include(attendance => attendance.Member)
            .Where(attendance => attendance.MeetingId == meeting.Id)
            .ToListAsync();

        if (attendanceRecords.Count == 0)
        {
            return 0;
        }

        var apologyMemberIds = await context.MeetingApologies
            .Where(apology => apology.MeetingId == meeting.Id)
            .Select(apology => apology.MemberId)
            .Distinct()
            .ToListAsync();
        var membersWithApology = apologyMemberIds.ToHashSet();
        var warningsCreated = 0;

        foreach (var attendance in attendanceRecords)
        {
            if (attendance.Status != AttendanceStatus.Absent || membersWithApology.Contains(attendance.MemberId))
            {
                continue;
            }

            var absenceCount = await CountAbsencesWithoutApologyAsync(attendance.MemberId, meeting.TenantId, meeting.MeetingDate);
            var warningType = GetWarningType(absenceCount);

            if (warningType is null)
            {
                continue;
            }

            var alreadyExists = await context.MemberWarnings.AnyAsync(warning =>
                warning.MemberId == attendance.MemberId &&
                warning.MeetingId == meeting.Id &&
                warning.WarningType == warningType);

            if (alreadyExists)
            {
                continue;
            }

            context.MemberWarnings.Add(new MemberWarning
            {
                Id = Guid.NewGuid(),
                MemberId = attendance.MemberId,
                StokvelId = stokvel.Id,
                MeetingId = meeting.Id,
                WarningType = warningType,
                Reason = $"Member has {absenceCount} absence(s) without apology.",
                AbsenceCount = absenceCount,
                Status = StatusOpen,
                CreatedAt = DateTime.UtcNow,
                CreatedByMemberId = processedByMemberId,
                Notes = warningType == WarningExecutiveReviewRequired
                    ? "Suspension decision must be taken by executive committee."
                    : null
            });

            warningsCreated++;
        }

        if (warningsCreated > 0)
        {
            await context.SaveChangesAsync();
        }

        return warningsCreated;
    }

    public async Task<List<MemberWarning>> GetOpenWarningsByStokvelIdAsync(Guid stokvelId)
    {
        return await context.MemberWarnings
            .Include(warning => warning.Member)
            .Include(warning => warning.Meeting)
            .Where(warning => warning.StokvelId == stokvelId && warning.Status == StatusOpen)
            .OrderByDescending(warning => warning.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<MemberWarning>> GetWarningsByStokvelIdAsync(Guid stokvelId)
    {
        return await context.MemberWarnings
            .Include(warning => warning.Member)
            .Include(warning => warning.Meeting)
            .Where(warning => warning.StokvelId == stokvelId)
            .OrderByDescending(warning => warning.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<MemberWarning>> GetWarningsByMemberIdAsync(Guid memberId)
    {
        return await context.MemberWarnings
            .Include(warning => warning.Meeting)
            .Include(warning => warning.Stokvel)
            .Where(warning => warning.MemberId == memberId)
            .OrderByDescending(warning => warning.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<MemberWarning>> GetExecutiveReviewRequiredByStokvelIdAsync(Guid stokvelId)
    {
        return await context.MemberWarnings
            .Include(warning => warning.Member)
            .Include(warning => warning.Meeting)
            .Where(warning =>
                warning.StokvelId == stokvelId &&
                warning.WarningType == WarningExecutiveReviewRequired &&
                warning.Status == StatusOpen)
            .OrderByDescending(warning => warning.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> AcknowledgeWarningAsync(Guid warningId, Guid acknowledgedByMemberId, string? notes)
    {
        var warning = await context.MemberWarnings
            .SingleOrDefaultAsync(existingWarning => existingWarning.Id == warningId);

        if (warning is null)
        {
            return false;
        }

        var acknowledgedByMember = await context.Members
            .SingleOrDefaultAsync(member => member.Id == acknowledgedByMemberId);

        if (acknowledgedByMember is null)
        {
            return false;
        }

        warning.Status = StatusAcknowledged;
        warning.AcknowledgedAt = DateTime.UtcNow;
        warning.Notes = string.IsNullOrWhiteSpace(notes)
            ? warning.Notes
            : string.IsNullOrWhiteSpace(warning.Notes)
                ? notes.Trim()
                : $"{warning.Notes}{Environment.NewLine}{notes.Trim()}";

        await context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> ResolveWarningAsync(Guid warningId, Guid resolvedByMemberId, string? notes)
    {
        var warning = await context.MemberWarnings
            .FirstOrDefaultAsync(existingWarning => existingWarning.Id == warningId);

        if (warning is null)
        {
            return false;
        }

        var resolvedByMember = await context.Members
            .FirstOrDefaultAsync(member => member.Id == resolvedByMemberId);

        if (resolvedByMember is null)
        {
            return false;
        }

        warning.Status = StatusResolved;
        warning.Notes = AppendNote(warning.Notes, notes);

        await context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> KeepMemberActiveAsync(Guid warningId, Guid decidedByMemberId, string decisionNotes)
    {
        if (string.IsNullOrWhiteSpace(decisionNotes))
        {
            return false;
        }

        var warning = await context.MemberWarnings
            .Include(existingWarning => existingWarning.Member)
            .FirstOrDefaultAsync(existingWarning => existingWarning.Id == warningId);

        if (warning?.Member is null)
        {
            return false;
        }

        var decidedByMember = await context.Members
            .FirstOrDefaultAsync(member => member.Id == decidedByMemberId);

        if (decidedByMember is null)
        {
            return false;
        }

        warning.Status = StatusResolved;
        warning.Notes = AppendNote(
            warning.Notes,
            $"Executive decision: Member remains active. {decisionNotes.Trim()}");

        await context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> SuspendMemberFromWarningAsync(Guid warningId, Guid decidedByMemberId, string suspensionReason)
    {
        if (string.IsNullOrWhiteSpace(suspensionReason))
        {
            return false;
        }

        var warning = await context.MemberWarnings
            .Include(existingWarning => existingWarning.Member)
            .FirstOrDefaultAsync(existingWarning => existingWarning.Id == warningId);

        if (warning?.Member is null)
        {
            return false;
        }

        var decidedByMember = await context.Members
            .FirstOrDefaultAsync(member => member.Id == decidedByMemberId);

        if (decidedByMember is null)
        {
            return false;
        }

        var now = DateTime.UtcNow;
        var reason = suspensionReason.Trim();

        warning.Member.GovernanceStatus = MemberGovernanceStatus.Suspended;
        warning.Member.Status = MemberStatus.Suspended;
        warning.Member.GovernanceStatusChangedAt = now;
        warning.Member.GovernanceStatusReason = reason;
        warning.Member.SuspendedAt ??= now;
        warning.Status = StatusResolved;
        warning.Notes = AppendNote(
            warning.Notes,
            $"Executive decision: Member suspended. Reason: {reason}");

        await context.SaveChangesAsync();

        return true;
    }

    private async Task<int> CountAbsencesWithoutApologyAsync(Guid memberId, Guid tenantId, DateTime upToMeetingDate)
    {
        var recentMeetings = await context.Meetings
            .Where(meeting =>
                meeting.TenantId == tenantId &&
                meeting.Status != MeetingStatus.Cancelled &&
                meeting.MeetingDate <= upToMeetingDate)
            .OrderByDescending(meeting => meeting.MeetingDate)
            .Take(12)
            .Select(meeting => new
            {
                meeting.Id,
                AttendanceStatus = context.MeetingAttendances
                    .Where(attendance => attendance.MeetingId == meeting.Id && attendance.MemberId == memberId)
                    .Select(attendance => (AttendanceStatus?)attendance.Status)
                    .FirstOrDefault(),
                HasApology = context.MeetingApologies
                    .Any(apology => apology.MeetingId == meeting.Id && apology.MemberId == memberId)
            })
            .ToListAsync();

        return recentMeetings.Count(meeting => meeting.AttendanceStatus == AttendanceStatus.Absent && !meeting.HasApology);
    }

    private static string? GetWarningType(int absenceCount)
    {
        return absenceCount switch
        {
            >= 4 => WarningExecutiveReviewRequired,
            3 => WarningFormal,
            2 => WarningReminder,
            _ => null
        };
    }

    private static string? AppendNote(string? existingNotes, string? newNotes)
    {
        if (string.IsNullOrWhiteSpace(newNotes))
        {
            return existingNotes;
        }

        return string.IsNullOrWhiteSpace(existingNotes)
            ? newNotes.Trim()
            : $"{existingNotes}{Environment.NewLine}{newNotes.Trim()}";
    }
}
