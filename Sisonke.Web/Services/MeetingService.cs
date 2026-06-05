using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Services;

public class MeetingService(ApplicationDbContext context)
{
    public async Task<List<Meeting>> GetMeetingsByStokvelIdAsync(Guid stokvelId)
    {
        var stokvel = await context.Stokvels
            .SingleOrDefaultAsync(existingStokvel => existingStokvel.Id == stokvelId);

        if (stokvel is null)
        {
            return [];
        }

        return await context.Meetings
            .Include(meeting => meeting.AgendaItems)
            .Where(meeting => meeting.TenantId == stokvel.TenantId)
            .OrderByDescending(meeting => meeting.MeetingDate)
            .ToListAsync();
    }

    public async Task<Meeting?> GetMeetingByIdAsync(Guid meetingId)
    {
        var meeting = await context.Meetings
            .Include(existingMeeting => existingMeeting.Tenant)
            .Include(existingMeeting => existingMeeting.AgendaItems)
            .SingleOrDefaultAsync(existingMeeting => existingMeeting.Id == meetingId);

        if (meeting is null)
        {
            return null;
        }

        meeting.AgendaItems = meeting.AgendaItems
            .OrderBy(agendaItem => agendaItem.DisplayOrder)
            .ToList();

        return meeting;
    }

    public async Task<Meeting?> CreateMeetingAsync(
        Guid stokvelId,
        string title,
        DateTime meetingDate,
        string? venue,
        string? purpose)
    {
        var stokvel = await context.Stokvels
            .SingleOrDefaultAsync(existingStokvel => existingStokvel.Id == stokvelId);

        if (stokvel is null)
        {
            return null;
        }

        if (await HasMeetingOnDateAsync(stokvelId, meetingDate))
        {
            return null;
        }

        var meeting = new Meeting
        {
            Id = Guid.NewGuid(),
            TenantId = stokvel.TenantId,
            Title = title,
            MeetingDate = meetingDate,
            Venue = venue,
            Purpose = purpose,
            Status = MeetingStatus.Planned,
            CreatedAt = DateTime.UtcNow,
            AgendaItems = BuildDefaultAgendaItems(stokvel.Type)
        };

        context.Meetings.Add(meeting);
        await context.SaveChangesAsync();

        return meeting;
    }

    public async Task<Meeting?> UpdateMeetingAsync(
        Guid meetingId,
        string title,
        DateTime meetingDate,
        string? venue,
        string? purpose)
    {
        var meeting = await context.Meetings
            .SingleOrDefaultAsync(existingMeeting => existingMeeting.Id == meetingId);

        if (meeting is null)
        {
            return null;
        }

        var hasAnotherMeetingOnDate = await context.Meetings
            .AnyAsync(existingMeeting =>
                existingMeeting.TenantId == meeting.TenantId &&
                existingMeeting.Id != meetingId &&
                existingMeeting.MeetingDate.Date == meetingDate.Date &&
                existingMeeting.Status != MeetingStatus.Cancelled);

        if (hasAnotherMeetingOnDate)
        {
            return null;
        }

        meeting.Title = title;
        meeting.MeetingDate = meetingDate;
        meeting.Venue = venue;
        meeting.Purpose = purpose;

        await context.SaveChangesAsync();

        return meeting;
    }

    public async Task<bool> DeleteMeetingAsync(Guid meetingId)
    {
        var meeting = await context.Meetings
            .Include(existingMeeting => existingMeeting.AgendaItems)
            .SingleOrDefaultAsync(existingMeeting => existingMeeting.Id == meetingId);

        if (meeting is null)
        {
            return false;
        }

        context.Meetings.Remove(meeting);
        await context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> HasMeetingOnDateAsync(Guid stokvelId, DateTime meetingDate)
    {
        var stokvel = await context.Stokvels
            .SingleOrDefaultAsync(existingStokvel => existingStokvel.Id == stokvelId);

        if (stokvel is null)
        {
            return false;
        }

        return await context.Meetings
            .AnyAsync(meeting =>
                meeting.TenantId == stokvel.TenantId &&
                meeting.MeetingDate.Date == meetingDate.Date &&
                meeting.Status != MeetingStatus.Cancelled);
    }

    public async Task<int> GetUpcomingMeetingCountByStokvelIdAsync(Guid stokvelId)
    {
        var stokvel = await context.Stokvels
            .SingleOrDefaultAsync(existingStokvel => existingStokvel.Id == stokvelId);

        if (stokvel is null)
        {
            return 0;
        }

        return await context.Meetings
            .CountAsync(meeting =>
                meeting.TenantId == stokvel.TenantId &&
                meeting.MeetingDate >= DateTime.Today &&
                (meeting.Status == MeetingStatus.Planned || meeting.Status == MeetingStatus.InProgress));
    }

    public async Task<int> GetMeetingsNeedingAttendanceCountByStokvelIdAsync(Guid stokvelId)
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

        return await context.Meetings
            .Where(meeting =>
                meeting.TenantId == stokvel.TenantId &&
                meeting.Status != MeetingStatus.Cancelled &&
                meeting.MeetingDate.Date <= DateTime.Today)
            .CountAsync(meeting =>
                !context.MeetingAttendances.Any(attendance => attendance.MeetingId == meeting.Id) ||
                context.MeetingAttendances
                    .Where(attendance => attendance.MeetingId == meeting.Id)
                    .All(attendance =>
                        attendance.Status == AttendanceStatus.Absent &&
                        !attendance.IsLate &&
                        !attendance.LeftEarly &&
                        (attendance.Notes == null || attendance.Notes == string.Empty)));
    }

    public Task<int> GetMeetingsNeedingAttendanceCaptureCountByStokvelIdAsync(Guid stokvelId)
    {
        return GetMeetingsNeedingAttendanceCountByStokvelIdAsync(stokvelId);
    }

    public async Task<int> GetMeetingsNeedingMinutesCountByStokvelIdAsync(Guid stokvelId)
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

        return await context.Meetings
            .Where(meeting =>
                meeting.TenantId == stokvel.TenantId &&
                meeting.Status != MeetingStatus.Cancelled &&
                meeting.MeetingDate.Date <= DateTime.Today)
            .CountAsync(meeting =>
                !context.MeetingMinutes.Any(minutes => minutes.MeetingId == meeting.Id) ||
                context.MeetingMinutes.Any(minutes =>
                    minutes.MeetingId == meeting.Id &&
                    minutes.Status == "Draft"));
    }

    public async Task<int> GetMinutesAwaitingApprovalCountByStokvelIdAsync(Guid stokvelId)
    {
        return await context.MeetingMinutes
            .CountAsync(minutes =>
                minutes.StokvelId == stokvelId &&
                minutes.Status == "Submitted");
    }

    public async Task<MeetingAgendaItem?> AddAgendaItemAsync(Guid meetingId, string title, string? description)
    {
        var meeting = await context.Meetings
            .SingleOrDefaultAsync(existingMeeting => existingMeeting.Id == meetingId);

        if (meeting is null)
        {
            return null;
        }

        var maxDisplayOrder = await context.MeetingAgendaItems
            .Where(agendaItem => agendaItem.MeetingId == meetingId)
            .MaxAsync(agendaItem => (int?)agendaItem.DisplayOrder) ?? 0;

        var item = new MeetingAgendaItem
        {
            Id = Guid.NewGuid(),
            MeetingId = meetingId,
            Title = title,
            Description = description,
            DisplayOrder = maxDisplayOrder + 1,
            IsCompleted = false
        };

        context.MeetingAgendaItems.Add(item);
        await context.SaveChangesAsync();

        return item;
    }

    public async Task<MeetingAgendaItem?> UpdateAgendaItemAsync(
        Guid agendaItemId,
        string title,
        string? description,
        string? notes,
        bool isCompleted)
    {
        var item = await context.MeetingAgendaItems
            .SingleOrDefaultAsync(existingAgendaItem => existingAgendaItem.Id == agendaItemId);

        if (item is null)
        {
            return null;
        }

        item.Title = title;
        item.Description = description;
        item.Notes = notes;
        item.IsCompleted = isCompleted;

        await context.SaveChangesAsync();

        return item;
    }

    public async Task<bool> DeleteAgendaItemAsync(Guid agendaItemId)
    {
        var item = await context.MeetingAgendaItems
            .SingleOrDefaultAsync(existingAgendaItem => existingAgendaItem.Id == agendaItemId);

        if (item is null)
        {
            return false;
        }

        var meetingId = item.MeetingId;

        context.MeetingAgendaItems.Remove(item);
        await context.SaveChangesAsync();

        await ReorderAgendaItemsAsync(meetingId);

        return true;
    }

    public async Task<bool> MoveAgendaItemUpAsync(Guid agendaItemId)
    {
        var item = await context.MeetingAgendaItems
            .SingleOrDefaultAsync(existingAgendaItem => existingAgendaItem.Id == agendaItemId);

        if (item is null)
        {
            return false;
        }

        var previousItem = await context.MeetingAgendaItems
            .Where(agendaItem =>
                agendaItem.MeetingId == item.MeetingId &&
                agendaItem.DisplayOrder < item.DisplayOrder)
            .OrderByDescending(agendaItem => agendaItem.DisplayOrder)
            .FirstOrDefaultAsync();

        if (previousItem is null)
        {
            return false;
        }

        (item.DisplayOrder, previousItem.DisplayOrder) = (previousItem.DisplayOrder, item.DisplayOrder);
        await context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> MoveAgendaItemDownAsync(Guid agendaItemId)
    {
        var item = await context.MeetingAgendaItems
            .SingleOrDefaultAsync(existingAgendaItem => existingAgendaItem.Id == agendaItemId);

        if (item is null)
        {
            return false;
        }

        var nextItem = await context.MeetingAgendaItems
            .Where(agendaItem =>
                agendaItem.MeetingId == item.MeetingId &&
                agendaItem.DisplayOrder > item.DisplayOrder)
            .OrderBy(agendaItem => agendaItem.DisplayOrder)
            .FirstOrDefaultAsync();

        if (nextItem is null)
        {
            return false;
        }

        (item.DisplayOrder, nextItem.DisplayOrder) = (nextItem.DisplayOrder, item.DisplayOrder);
        await context.SaveChangesAsync();

        return true;
    }

    public async Task<List<MeetingAttendance>> GetAttendanceByMeetingIdAsync(Guid meetingId)
    {
        return await context.MeetingAttendances
            .Include(attendance => attendance.Member)
            .Where(attendance => attendance.MeetingId == meetingId)
            .OrderBy(attendance => attendance.Member.FullName)
            .ToListAsync();
    }

    public async Task<bool> InitializeAttendanceRegisterAsync(Guid meetingId)
    {
        var meeting = await context.Meetings
            .SingleOrDefaultAsync(existingMeeting => existingMeeting.Id == meetingId);

        if (meeting is null)
        {
            return false;
        }

        var activeMembers = await context.Members
            .Where(member =>
                member.TenantId == meeting.TenantId &&
                member.Status == MemberStatus.Active)
            .ToListAsync();

        var existingMemberIds = await context.MeetingAttendances
            .Where(attendance => attendance.MeetingId == meeting.Id)
            .Select(attendance => attendance.MemberId)
            .ToListAsync();

        var now = DateTime.UtcNow;

        foreach (var member in activeMembers)
        {
            if (existingMemberIds.Contains(member.Id))
            {
                continue;
            }

            context.MeetingAttendances.Add(new MeetingAttendance
            {
                Id = Guid.NewGuid(),
                MeetingId = meeting.Id,
                MemberId = member.Id,
                Status = AttendanceStatus.Absent,
                IsLate = false,
                LeftEarly = false,
                MarkedAt = now
            });
        }

        await context.SaveChangesAsync();

        return true;
    }

    public async Task<MeetingAttendance?> UpdateAttendanceAsync(
        Guid attendanceId,
        AttendanceStatus status,
        bool isLate,
        bool leftEarly,
        string? notes)
    {
        var attendance = await context.MeetingAttendances
            .SingleOrDefaultAsync(existingAttendance => existingAttendance.Id == attendanceId);

        if (attendance is null)
        {
            return null;
        }

        attendance.Status = status;
        attendance.IsLate = isLate;
        attendance.LeftEarly = leftEarly;
        attendance.Notes = notes;
        attendance.MarkedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return attendance;
    }

    public async Task<int> GetConsecutiveMissedMeetingsAsync(Guid memberId)
    {
        var member = await context.Members
            .SingleOrDefaultAsync(existingMember => existingMember.Id == memberId);

        if (member is null)
        {
            return 0;
        }

        var meetings = await context.Meetings
            .Where(meeting =>
                meeting.TenantId == member.TenantId &&
                meeting.Status != MeetingStatus.Cancelled &&
                (meeting.Status == MeetingStatus.Completed || meeting.MeetingDate.Date < DateTime.Today))
            .OrderByDescending(meeting => meeting.MeetingDate)
            .Select(meeting => new
            {
                meeting.Id,
                AttendanceStatus = context.MeetingAttendances
                    .Where(attendance =>
                        attendance.MeetingId == meeting.Id &&
                        attendance.MemberId == memberId)
                    .Select(attendance => (AttendanceStatus?)attendance.Status)
                    .FirstOrDefault()
            })
            .ToListAsync();

        var missedMeetings = 0;

        foreach (var meeting in meetings)
        {
            var attendanceStatus = meeting.AttendanceStatus ?? AttendanceStatus.Absent;

            if (attendanceStatus == AttendanceStatus.Absent)
            {
                missedMeetings++;
                continue;
            }

            if (attendanceStatus == AttendanceStatus.Apology)
            {
                continue;
            }

            return missedMeetings;
        }

        return missedMeetings;
    }

    public async Task<bool> EvaluateMemberAttendanceGovernanceAsync(Guid memberId)
    {
        var member = await context.Members
            .SingleOrDefaultAsync(existingMember => existingMember.Id == memberId);

        if (member is null)
        {
            return false;
        }

        // Attendance warnings are now handled by MemberWarningService.
        // Suspension must remain a manual executive/office bearer decision.
        return true;
    }

    public async Task EvaluateAttendanceGovernanceForMeetingAsync(Guid meetingId)
    {
        var attendanceRecords = await context.MeetingAttendances
            .Include(attendance => attendance.Member)
            .Where(attendance => attendance.MeetingId == meetingId)
            .ToListAsync();

        foreach (var attendance in attendanceRecords)
        {
            await EvaluateMemberAttendanceGovernanceAsync(attendance.Member.Id);
        }

        await context.SaveChangesAsync();
    }

    public async Task<(int Present, int Absent, int Apologies, int Late)> GetAttendanceSummaryAsync(Guid meetingId)
    {
        var attendanceRecords = await context.MeetingAttendances
            .Where(attendance => attendance.MeetingId == meetingId)
            .ToListAsync();

        return (
            attendanceRecords.Count(attendance => attendance.Status == AttendanceStatus.Present),
            attendanceRecords.Count(attendance => attendance.Status == AttendanceStatus.Absent),
            attendanceRecords.Count(attendance => attendance.Status == AttendanceStatus.Apology),
            attendanceRecords.Count(attendance => attendance.IsLate));
    }

    private async Task ReorderAgendaItemsAsync(Guid meetingId)
    {
        var agendaItems = await context.MeetingAgendaItems
            .Where(agendaItem => agendaItem.MeetingId == meetingId)
            .OrderBy(agendaItem => agendaItem.DisplayOrder)
            .ToListAsync();

        for (var index = 0; index < agendaItems.Count; index++)
        {
            agendaItems[index].DisplayOrder = index + 1;
        }

        await context.SaveChangesAsync();
    }

    private static List<MeetingAgendaItem> BuildDefaultAgendaItems(StokvelType stokvelType)
    {
        string[] agendaTitles = stokvelType switch
        {
            StokvelType.BurialSociety =>
            [
                "Opening and welcome",
                "Attendance register",
                "Apologies and late coming",
                "Matters arising from previous meeting",
                "Financial report",
                "Contributions and arrears",
                "Fines and penalties",
                "Claims and bereavement support",
                "Member updates, beneficiaries and next of kin",
                "New business",
                "Action items and decisions",
                "Closing"
            ],
            StokvelType.SavingsStokvel =>
            [
                "Opening and welcome",
                "Attendance register",
                "Apologies and late coming",
                "Contributions update",
                "Arrears and fines",
                "Payout schedule",
                "Member issues",
                "Decisions and approvals",
                "New business",
                "Action items",
                "Closing"
            ],
            StokvelType.InvestmentStokvel =>
            [
                "Opening and welcome",
                "Attendance register",
                "Apologies and late coming",
                "Portfolio update",
                "Contributions and capital position",
                "Investment opportunities",
                "Risk review",
                "Voting and approvals",
                "Decisions and action items",
                "Closing"
            ],
            _ =>
            [
                "Opening and welcome",
                "Attendance register",
                "Apologies",
                "Financial update",
                "Matters arising",
                "New business",
                "Decisions and action items",
                "Closing"
            ]
        };

        return agendaTitles
            .Select((title, index) => new MeetingAgendaItem
            {
                Id = Guid.NewGuid(),
                Title = title,
                DisplayOrder = index + 1
            })
            .ToList();
    }
}
