using System.Text;
using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Services;

public class MeetingMinuteService(ApplicationDbContext context)
{
    private const string StatusDraft = "Draft";
    private const string StatusSubmitted = "Submitted";
    private const string StatusApproved = "Approved";

    public async Task<MeetingMinute?> GetMinutesByMeetingIdAsync(Guid meetingId)
    {
        return await context.MeetingMinutes
            .Include(minutes => minutes.Meeting)
            .Include(minutes => minutes.Stokvel)
            .Where(minutes => minutes.MeetingId == meetingId)
            .OrderByDescending(minutes => minutes.UpdatedAt ?? minutes.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<List<MeetingMinute>> GetMinutesByStokvelIdAsync(Guid stokvelId)
    {
        return await context.MeetingMinutes
            .Include(minutes => minutes.Meeting)
            .Where(minutes => minutes.StokvelId == stokvelId)
            .OrderByDescending(minutes => minutes.Meeting != null ? minutes.Meeting.MeetingDate : minutes.CreatedAt)
            .ToListAsync();
    }

    public async Task<MeetingMinute?> GenerateDraftMinutesAsync(Guid meetingId, Guid createdByMemberId)
    {
        var existingMinutes = await GetMinutesByMeetingIdAsync(meetingId);

        if (existingMinutes is not null)
        {
            return existingMinutes;
        }

        var meeting = await context.Meetings
            .Include(existingMeeting => existingMeeting.AgendaItems)
            .Where(existingMeeting => existingMeeting.Id == meetingId)
            .OrderBy(existingMeeting => existingMeeting.MeetingDate)
            .FirstOrDefaultAsync();

        if (meeting is null)
        {
            return null;
        }

        var stokvel = await context.Stokvels
            .Where(existingStokvel => existingStokvel.TenantId == meeting.TenantId)
            .OrderBy(existingStokvel => existingStokvel.CreatedAt)
            .ThenBy(existingStokvel => existingStokvel.Name)
            .FirstOrDefaultAsync();

        if (stokvel is null)
        {
            return null;
        }

        var attendanceRecords = await context.MeetingAttendances
            .Where(attendance => attendance.MeetingId == meeting.Id)
            .ToListAsync();
        var apologies = await context.MeetingApologies
            .Include(apology => apology.Member)
            .Where(apology => apology.MeetingId == meeting.Id)
            .OrderBy(apology => apology.Member == null ? string.Empty : apology.Member.FullName)
            .ToListAsync();
        var agendaItems = meeting.AgendaItems
            .OrderBy(agendaItem => agendaItem.DisplayOrder)
            .ToList();
        var titleSource = string.IsNullOrWhiteSpace(meeting.Title)
            ? meeting.Purpose
            : meeting.Title;
        var minutes = new MeetingMinute
        {
            Id = Guid.NewGuid(),
            MeetingId = meeting.Id,
            StokvelId = stokvel.Id,
            Title = $"{(string.IsNullOrWhiteSpace(titleSource) ? "Meeting" : titleSource.Trim())} Minutes",
            OpeningNotes = string.Empty,
            AttendanceSummary = BuildAttendanceSummary(attendanceRecords, apologies.Count),
            ApologySummary = BuildApologySummary(apologies),
            MattersArising = string.Empty,
            DecisionsTaken = BuildAgendaLinkedDecisions(agendaItems),
            ActionItems = string.Empty,
            ClosingNotes = string.Empty,
            Status = StatusDraft,
            CreatedByMemberId = createdByMemberId,
            CreatedAt = DateTime.UtcNow
        };

        context.MeetingMinutes.Add(minutes);
        await context.SaveChangesAsync();

        return minutes;
    }

    public async Task<bool> RegenerateDraftFromAgendaAsync(Guid meetingMinuteId, Guid updatedByMemberId)
    {
        var minutes = await context.MeetingMinutes
            .Where(existingMinutes => existingMinutes.Id == meetingMinuteId)
            .FirstOrDefaultAsync();

        if (minutes is null || minutes.Status != StatusDraft)
        {
            return false;
        }

        var agendaItems = await context.MeetingAgendaItems
            .Where(agendaItem => agendaItem.MeetingId == minutes.MeetingId)
            .OrderBy(agendaItem => agendaItem.DisplayOrder)
            .ToListAsync();

        minutes.DecisionsTaken = BuildAgendaLinkedDecisions(agendaItems);
        minutes.UpdatedByMemberId = updatedByMemberId;
        minutes.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> SaveDraftMinutesAsync(
        Guid meetingMinuteId,
        string openingNotes,
        string mattersArising,
        string decisionsTaken,
        string actionItems,
        string closingNotes,
        Guid updatedByMemberId)
    {
        var minutes = await context.MeetingMinutes
            .Where(existingMinutes => existingMinutes.Id == meetingMinuteId)
            .FirstOrDefaultAsync();

        if (minutes is null || minutes.Status != StatusDraft)
        {
            return false;
        }

        minutes.OpeningNotes = openingNotes.Trim();
        minutes.MattersArising = mattersArising.Trim();
        minutes.DecisionsTaken = decisionsTaken.Trim();
        minutes.ActionItems = actionItems.Trim();
        minutes.ClosingNotes = closingNotes.Trim();
        minutes.UpdatedByMemberId = updatedByMemberId;
        minutes.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> SubmitMinutesAsync(Guid meetingMinuteId, Guid submittedByMemberId)
    {
        var minutes = await context.MeetingMinutes
            .Where(existingMinutes => existingMinutes.Id == meetingMinuteId)
            .FirstOrDefaultAsync();

        if (minutes is null || minutes.Status != StatusDraft)
        {
            return false;
        }

        minutes.Status = StatusSubmitted;
        minutes.UpdatedByMemberId = submittedByMemberId;
        minutes.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> ApproveMinutesAsync(Guid meetingMinuteId, Guid approvedByMemberId)
    {
        var minutes = await context.MeetingMinutes
            .Where(existingMinutes => existingMinutes.Id == meetingMinuteId)
            .FirstOrDefaultAsync();

        if (minutes is null || minutes.Status != StatusSubmitted)
        {
            return false;
        }

        var now = DateTime.UtcNow;
        minutes.Status = StatusApproved;
        minutes.ApprovedByMemberId = approvedByMemberId;
        minutes.ApprovedAt = now;
        minutes.UpdatedByMemberId = approvedByMemberId;
        minutes.UpdatedAt = now;

        await context.SaveChangesAsync();

        return true;
    }

    private static string BuildAttendanceSummary(List<MeetingAttendance> attendanceRecords, int apologiesCount)
    {
        if (attendanceRecords.Count == 0 && apologiesCount == 0)
        {
            return "Attendance register has not been captured yet.";
        }

        var presentCount = attendanceRecords.Count(attendance => attendance.Status == AttendanceStatus.Present);
        var absentCount = attendanceRecords.Count(attendance => attendance.Status == AttendanceStatus.Absent);
        var apologyCount = Math.Max(apologiesCount, attendanceRecords.Count(attendance => attendance.Status == AttendanceStatus.Apology));

        return $"Present: {presentCount}. Absent: {absentCount}. Apologies: {apologyCount}.";
    }

    private static string BuildApologySummary(List<MeetingApology> apologies)
    {
        if (apologies.Count == 0)
        {
            return "No apologies recorded.";
        }

        var summary = new StringBuilder();

        foreach (var apology in apologies)
        {
            var memberName = string.IsNullOrWhiteSpace(apology.Member?.FullName)
                ? "Member"
                : apology.Member.FullName;
            summary.AppendLine($"{memberName}: {apology.ApologyType} - {apology.Reason}");
        }

        return summary.ToString().Trim();
    }

    private static string BuildAgendaLinkedDecisions(List<MeetingAgendaItem> agendaItems)
    {
        if (agendaItems.Count == 0)
        {
            return string.Empty;
        }

        var decisions = agendaItems.Select((agendaItem, index) =>
        {
            var summary = new StringBuilder();
            summary.AppendLine($"{index + 1}. {agendaItem.Title}");

            if (!string.IsNullOrWhiteSpace(agendaItem.Description))
            {
                summary.AppendLine($"Description: {agendaItem.Description.Trim()}");
            }

            summary.AppendLine("Discussion:");
            summary.AppendLine(string.IsNullOrWhiteSpace(agendaItem.Notes)
                ? "No notes captured."
                : agendaItem.Notes.Trim());
            summary.AppendLine($"Status: {(agendaItem.IsCompleted ? "Completed" : "Open")}");

            return summary.ToString().Trim();
        });

        return string.Join($"{Environment.NewLine}{Environment.NewLine}", decisions);
    }
}
