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
