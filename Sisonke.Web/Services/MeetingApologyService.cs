using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;

namespace Sisonke.Web.Services;

public class MeetingApologyService(ApplicationDbContext context, AuditLogService auditLogService)
{
    public async Task<MeetingApology?> SubmitApologyAsync(Guid meetingId, Guid memberId, string apologyType, string reason)
    {
        if (string.IsNullOrWhiteSpace(apologyType) || string.IsNullOrWhiteSpace(reason))
        {
            return null;
        }

        var existingApology = await GetApologyAsync(meetingId, memberId);

        if (existingApology is not null)
        {
            return existingApology;
        }

        var meeting = await context.Meetings
            .Where(existingMeeting => existingMeeting.Id == meetingId)
            .FirstOrDefaultAsync();

        if (meeting is null || meeting.MeetingDate.Date < DateTime.Today)
        {
            return null;
        }

        var member = await context.Members
            .Where(existingMember => existingMember.Id == memberId)
            .FirstOrDefaultAsync();

        if (member is null || member.TenantId != meeting.TenantId)
        {
            return null;
        }

        var apology = new MeetingApology
        {
            Id = Guid.NewGuid(),
            MeetingId = meetingId,
            MemberId = memberId,
            ApologyType = apologyType.Trim(),
            Reason = reason.Trim(),
            Status = "Submitted",
            SubmittedAt = DateTime.UtcNow
        };

        context.MeetingApologies.Add(apology);
        await context.SaveChangesAsync();
        var stokvel = await context.Stokvels.AsNoTracking().FirstOrDefaultAsync(existingStokvel => existingStokvel.TenantId == meeting.TenantId);
        await auditLogService.RecordAsync(member.ApplicationUserId, stokvel?.Id, "ApologySubmitted", "MeetingApology", apology.Id, $"Apology submitted by {member.FullName}.");

        return apology;
    }

    public async Task<MeetingApology?> GetApologyAsync(Guid meetingId, Guid memberId)
    {
        return await context.MeetingApologies
            .Include(apology => apology.Meeting)
            .Include(apology => apology.Member)
            .Where(apology => apology.MeetingId == meetingId && apology.MemberId == memberId)
            .OrderByDescending(apology => apology.SubmittedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<List<MeetingApology>> GetApologiesByMeetingIdAsync(Guid meetingId)
    {
        return await context.MeetingApologies
            .Include(apology => apology.Member)
            .Where(apology => apology.MeetingId == meetingId)
            .OrderByDescending(apology => apology.SubmittedAt)
            .ToListAsync();
    }

    public async Task<List<MeetingApology>> GetApologiesByMemberIdAsync(Guid memberId)
    {
        return await context.MeetingApologies
            .Include(apology => apology.Meeting)
            .Where(apology => apology.MemberId == memberId)
            .OrderByDescending(apology => apology.SubmittedAt)
            .ToListAsync();
    }

    public async Task<List<MeetingApology>> GetPendingApologiesByStokvelIdAsync(Guid stokvelId)
    {
        var stokvel = await context.Stokvels
            .Where(existingStokvel => existingStokvel.Id == stokvelId)
            .OrderBy(existingStokvel => existingStokvel.CreatedAt)
            .ThenBy(existingStokvel => existingStokvel.Name)
            .FirstOrDefaultAsync();

        if (stokvel is null)
        {
            return [];
        }

        return await context.MeetingApologies
            .Include(apology => apology.Meeting)
            .Include(apology => apology.Member)
            .Where(apology =>
                apology.Meeting != null &&
                apology.Meeting.TenantId == stokvel.TenantId &&
                apology.Status == "Submitted")
            .OrderByDescending(apology => apology.SubmittedAt)
            .ToListAsync();
    }

    public async Task<int> GetPendingApologyCountByStokvelIdAsync(Guid stokvelId)
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

        return await context.MeetingApologies
            .Include(apology => apology.Meeting)
            .CountAsync(apology =>
                apology.Meeting != null &&
                apology.Meeting.TenantId == stokvel.TenantId &&
                apology.Status == "Submitted");
    }
}
