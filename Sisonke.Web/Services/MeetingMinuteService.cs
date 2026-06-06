using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Services;

public class MeetingMinuteService(ApplicationDbContext context, IHttpClientFactory httpClientFactory, IConfiguration configuration)
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
        string attendanceSummary,
        string apologySummary,
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
        minutes.AttendanceSummary = attendanceSummary.Trim();
        minutes.ApologySummary = apologySummary.Trim();
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

    public async Task<bool> ImproveDraftMinutesWithAiAsync(Guid meetingMinuteId, Guid updatedByMemberId)
    {
        var minutes = await context.MeetingMinutes
            .Where(existingMinutes => existingMinutes.Id == meetingMinuteId)
            .FirstOrDefaultAsync();

        if (minutes is null || minutes.Status != StatusDraft)
        {
            return false;
        }

        var meeting = await context.Meetings
            .Include(existingMeeting => existingMeeting.AgendaItems)
            .Where(existingMeeting => existingMeeting.Id == minutes.MeetingId)
            .FirstOrDefaultAsync();

        if (meeting is null)
        {
            return false;
        }

        var stokvel = await context.Stokvels
            .Where(existingStokvel => existingStokvel.TenantId == meeting.TenantId)
            .OrderBy(existingStokvel => existingStokvel.CreatedAt)
            .ThenBy(existingStokvel => existingStokvel.Name)
            .FirstOrDefaultAsync();

        if (stokvel is null)
        {
            return false;
        }

        var apiKey = configuration["Anthropic:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return false;
        }

        var agendaItems = meeting.AgendaItems
            .OrderBy(agendaItem => agendaItem.DisplayOrder)
            .ToList();

        var prompt = BuildAiPrompt(stokvel.Name, meeting, agendaItems, minutes.AttendanceSummary, minutes.ApologySummary);

        var requestBody = new
        {
            model = "claude-sonnet-4-6",
            max_tokens = 2048,
            messages = new[]
            {
                new { role = "user", content = prompt }
            }
        };

        string responseBody;

        try
        {
            using var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("x-api-key", apiKey);
            client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

            var response = await client.PostAsJsonAsync("https://api.anthropic.com/v1/messages", requestBody);

            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            responseBody = await response.Content.ReadAsStringAsync();
        }
        catch
        {
            return false;
        }

        string? aiText;

        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            aiText = doc.RootElement
                .GetProperty("content")[0]
                .GetProperty("text")
                .GetString();
        }
        catch
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(aiText))
        {
            return false;
        }

        var cleaned = aiText.Trim();

        if (cleaned.StartsWith("```"))
        {
            var newline = cleaned.IndexOf('\n');
            cleaned = newline >= 0 ? cleaned[(newline + 1)..] : cleaned[3..];

            if (cleaned.EndsWith("```"))
            {
                cleaned = cleaned[..^3].Trim();
            }
        }

        AiMinutesResult? result;

        try
        {
            result = JsonSerializer.Deserialize<AiMinutesResult>(cleaned, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            return false;
        }

        if (result is null)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(result.MattersArising))
        {
            minutes.MattersArising = result.MattersArising.Trim();
        }

        if (!string.IsNullOrWhiteSpace(result.DecisionsTaken))
        {
            minutes.DecisionsTaken = result.DecisionsTaken.Trim();
        }

        if (!string.IsNullOrWhiteSpace(result.ActionItems))
        {
            minutes.ActionItems = result.ActionItems.Trim();
        }

        if (!string.IsNullOrWhiteSpace(result.ClosingNotes))
        {
            minutes.ClosingNotes = result.ClosingNotes.Trim();
        }

        minutes.UpdatedByMemberId = updatedByMemberId;
        minutes.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return true;
    }

    private static string BuildAiPrompt(
        string stokvelName,
        Meeting meeting,
        List<MeetingAgendaItem> agendaItems,
        string attendanceSummary,
        string apologySummary)
    {
        var meetingTitle = !string.IsNullOrWhiteSpace(meeting.Title)
            ? meeting.Title
            : !string.IsNullOrWhiteSpace(meeting.Purpose)
                ? meeting.Purpose
                : "Meeting";

        var sb = new StringBuilder();
        sb.AppendLine("You are a formal minutes recorder for a South African stokvel or burial society.");
        sb.AppendLine("Generate formal, professional meeting minutes based only on the data below.");
        sb.AppendLine("Return ONLY valid JSON. No markdown fences. No explanation. No preamble.");
        sb.AppendLine();
        sb.AppendLine("Required JSON structure:");
        sb.AppendLine("{");
        sb.AppendLine("  \"mattersArising\": \"string\",");
        sb.AppendLine("  \"decisionsTaken\": \"string\",");
        sb.AppendLine("  \"actionItems\": \"string\",");
        sb.AppendLine("  \"closingNotes\": \"string\"");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("Rules:");
        sb.AppendLine("- Use formal South African English.");
        sb.AppendLine("- The Secretary records minutes. The Chairperson approves separately. Do not conflate these roles.");
        sb.AppendLine("- If discussion notes for an agenda item are missing, write: Discussion notes were not captured for this item.");
        sb.AppendLine("- Identify resolutions clearly using the word RESOLVED.");
        sb.AppendLine("- Identify action items with responsible person and deadline only if mentioned in the notes.");
        sb.AppendLine("- Do not invent attendees, amounts, decisions, names or dates.");
        sb.AppendLine("- Use only the data provided. Return only the JSON object.");
        sb.AppendLine();
        sb.AppendLine("MEETING DATA:");
        sb.AppendLine($"Stokvel: {stokvelName}");
        sb.AppendLine($"Meeting title: {meetingTitle}");
        sb.AppendLine($"Meeting date: {meeting.MeetingDate:dd MMM yyyy HH:mm}");
        sb.AppendLine($"Venue: {(string.IsNullOrWhiteSpace(meeting.Venue) ? "Not specified" : meeting.Venue)}");
        sb.AppendLine();
        sb.AppendLine($"Attendance summary: {(string.IsNullOrWhiteSpace(attendanceSummary) ? "Not captured." : attendanceSummary)}");
        sb.AppendLine($"Apology summary: {(string.IsNullOrWhiteSpace(apologySummary) ? "No apologies recorded." : apologySummary)}");
        sb.AppendLine();
        sb.AppendLine("AGENDA ITEMS:");

        if (agendaItems.Count == 0)
        {
            sb.AppendLine("No agenda items were captured for this meeting.");
        }
        else
        {
            for (var i = 0; i < agendaItems.Count; i++)
            {
                var item = agendaItems[i];
                sb.AppendLine($"{i + 1}. {item.Title}");

                if (!string.IsNullOrWhiteSpace(item.Description))
                {
                    sb.AppendLine($"   Description: {item.Description.Trim()}");
                }

                sb.AppendLine($"   Discussion notes: {(string.IsNullOrWhiteSpace(item.Notes) ? "Not captured." : item.Notes.Trim())}");
                sb.AppendLine($"   Status: {(item.IsCompleted ? "Completed" : "Open")}");
            }
        }

        return sb.ToString().Trim();
    }

    private sealed class AiMinutesResult
    {
        public string? MattersArising { get; init; }
        public string? DecisionsTaken { get; init; }
        public string? ActionItems { get; init; }
        public string? ClosingNotes { get; init; }
    }
}
