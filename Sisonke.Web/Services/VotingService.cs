using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;
using Sisonke.Web.Services.Dto;

namespace Sisonke.Web.Services;

public class VotingService(
    ApplicationDbContext context,
    StokvelOperatingRulesService stokvelOperatingRulesService)
{
    public async Task<List<MeetingVote>> GetMeetingVotesByMeetingIdAsync(Guid meetingId)
    {
        return await context.MeetingVotes
            .Include(vote => vote.Responses)
            .Where(vote => vote.MeetingId == meetingId)
            .OrderByDescending(vote => vote.OpenedAt)
            .ToListAsync();
    }

    public async Task<MeetingVote?> GetMeetingVoteByIdAsync(Guid voteId)
    {
        return await context.MeetingVotes
            .Include(vote => vote.Meeting)
            .Include(vote => vote.Responses)
                .ThenInclude(response => response.Member)
            .SingleOrDefaultAsync(vote => vote.Id == voteId);
    }

    public async Task<MeetingVote?> CreateVoteAsync(
        Guid meetingId,
        string title,
        string? description,
        VotingMethod votingMethod)
    {
        var meeting = await context.Meetings
            .SingleOrDefaultAsync(existingMeeting => existingMeeting.Id == meetingId);

        if (meeting is null)
        {
            return null;
        }

        var vote = new MeetingVote
        {
            Id = Guid.NewGuid(),
            MeetingId = meeting.Id,
            Title = title,
            Description = description,
            VotingMethod = votingMethod,
            Status = VoteStatus.Open,
            Result = VoteResult.Pending,
            OpenedAt = DateTime.UtcNow
        };

        context.MeetingVotes.Add(vote);
        await context.SaveChangesAsync();

        return vote;
    }

    public async Task<MeetingVoteResponse?> CastVoteAsync(Guid voteId, Guid memberId, VoteChoice choice)
    {
        var vote = await context.MeetingVotes
            .SingleOrDefaultAsync(existingVote => existingVote.Id == voteId);

        if (vote is null || vote.Status != VoteStatus.Open)
        {
            return null;
        }

        var member = await context.Members
            .SingleOrDefaultAsync(existingMember => existingMember.Id == memberId);

        if (member is null)
        {
            return null;
        }

        var response = await context.MeetingVoteResponses
            .SingleOrDefaultAsync(existingResponse =>
                existingResponse.MeetingVoteId == vote.Id &&
                existingResponse.MemberId == member.Id);

        if (response is null)
        {
            response = new MeetingVoteResponse
            {
                Id = Guid.NewGuid(),
                MeetingVoteId = vote.Id,
                MemberId = member.Id,
                Choice = choice,
                VotedAt = DateTime.UtcNow
            };

            context.MeetingVoteResponses.Add(response);
        }
        else
        {
            response.Choice = choice;
            response.VotedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();

        return response;
    }

    public async Task<MeetingVote?> CloseVoteAsync(Guid voteId)
    {
        var vote = await context.MeetingVotes
            .Include(existingVote => existingVote.Responses)
            .SingleOrDefaultAsync(existingVote => existingVote.Id == voteId);

        if (vote is null)
        {
            return null;
        }

        var yesCount = vote.Responses.Count(response => response.Choice == VoteChoice.Yes);
        var noCount = vote.Responses.Count(response => response.Choice == VoteChoice.No);
        var passed = vote.VotingMethod switch
        {
            VotingMethod.SimpleMajority => yesCount > noCount,
            VotingMethod.TwoThirdsMajority => yesCount >= Math.Ceiling((yesCount + noCount) * 2m / 3m),
            VotingMethod.Unanimous => noCount == 0 && yesCount > 0,
            _ => false
        };

        vote.Status = VoteStatus.Closed;
        vote.ClosedAt = DateTime.UtcNow;
        vote.Result = passed ? VoteResult.Passed : VoteResult.Failed;

        await context.SaveChangesAsync();

        return vote;
    }

    public async Task<(int Yes, int No, int Abstain)> GetVoteSummaryAsync(Guid voteId)
    {
        var responses = await context.MeetingVoteResponses
            .Where(response => response.MeetingVoteId == voteId)
            .ToListAsync();

        return (
            responses.Count(response => response.Choice == VoteChoice.Yes),
            responses.Count(response => response.Choice == VoteChoice.No),
            responses.Count(response => response.Choice == VoteChoice.Abstain));
    }

    public async Task<VoteMotion> CreateYesNoVoteAsync(
        Guid stokvelId,
        Guid? meetingId,
        Guid? agendaItemId,
        string title,
        string description,
        bool isAnonymous,
        Guid createdByMemberId)
    {
        var createdAt = DateTime.UtcNow;
        var voteMotion = new VoteMotion
        {
            Id = Guid.NewGuid(),
            StokvelId = stokvelId,
            MeetingId = meetingId,
            AgendaItemId = agendaItemId,
            Title = title.Trim(),
            Description = description.Trim(),
            VoteType = "YesNo",
            Status = "Open",
            IsAnonymous = isAnonymous,
            OpensAt = createdAt,
            CreatedByMemberId = createdByMemberId,
            CreatedAt = createdAt,
            Options =
            [
                new VoteOption
                {
                    Id = Guid.NewGuid(),
                    OptionText = "Yes",
                    SortOrder = 1
                },
                new VoteOption
                {
                    Id = Guid.NewGuid(),
                    OptionText = "No",
                    SortOrder = 2
                }
            ]
        };

        context.VoteMotions.Add(voteMotion);
        await context.SaveChangesAsync();

        return voteMotion;
    }

    public async Task<List<VoteMotion>> GetVotesByStokvelIdAsync(Guid stokvelId)
    {
        return await context.VoteMotions
            .Include(vote => vote.Options)
            .Include(vote => vote.MemberVotes)
            .Where(vote => vote.StokvelId == stokvelId)
            .OrderByDescending(vote => vote.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<VoteMotion>> GetVotesByMeetingIdAsync(Guid meetingId)
    {
        return await context.VoteMotions
            .Include(vote => vote.Options)
            .Include(vote => vote.MemberVotes)
            .Where(vote => vote.MeetingId == meetingId)
            .OrderByDescending(vote => vote.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<VoteMotion>> GetVotesByAgendaItemIdAsync(Guid agendaItemId)
    {
        return await context.VoteMotions
            .Include(vote => vote.Options)
            .Include(vote => vote.MemberVotes)
            .Where(vote => vote.AgendaItemId == agendaItemId)
            .OrderByDescending(vote => vote.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<VoteMotion>> GetOpenVotesForMemberAsync(Guid memberId, Guid stokvelId)
    {
        var member = await context.Members
            .FirstOrDefaultAsync(existingMember => existingMember.Id == memberId);

        if (member is null || !await MemberBelongsToStokvelAsync(member, stokvelId))
        {
            return [];
        }

        var now = DateTime.UtcNow;

        return await context.VoteMotions
            .Include(vote => vote.Options)
            .Include(vote => vote.MemberVotes)
            .Where(vote =>
                vote.StokvelId == stokvelId &&
                vote.Status == "Open" &&
                vote.OpensAt <= now &&
                (vote.ClosesAt == null || vote.ClosesAt >= now) &&
                !vote.MemberVotes.Any(memberVote => memberVote.MemberId == memberId))
            .OrderBy(vote => vote.OpensAt)
            .ToListAsync();
    }

    public async Task<VoteMotion?> GetVoteByIdAsync(Guid voteMotionId)
    {
        return await context.VoteMotions
            .Include(vote => vote.Stokvel)
            .Include(vote => vote.Meeting)
            .Include(vote => vote.AgendaItem)
            .Include(vote => vote.Options)
            .Include(vote => vote.MemberVotes)
                .ThenInclude(memberVote => memberVote.VoteOption)
            .FirstOrDefaultAsync(vote => vote.Id == voteMotionId);
    }

    public async Task<VoteMotionSummaryDto?> GetVoteSummaryAsync(Guid voteMotionId, Guid? currentMemberId = null)
    {
        var vote = await GetVoteByIdAsync(voteMotionId);

        if (vote is null)
        {
            return null;
        }

        var totalVotesCast = vote.MemberVotes.Count;
        var eligibleMemberCount = await GetEligibleMemberCountAsync(vote.StokvelId);

        return new VoteMotionSummaryDto
        {
            VoteMotionId = vote.Id,
            Title = vote.Title,
            Description = vote.Description,
            VoteType = vote.VoteType,
            Status = vote.Status,
            IsAnonymous = vote.IsAnonymous,
            OpensAt = vote.OpensAt,
            ClosesAt = vote.ClosesAt,
            EligibleMemberCount = eligibleMemberCount,
            TotalVotesCast = totalVotesCast,
            CurrentUserHasVoted = currentMemberId is not null &&
                vote.MemberVotes.Any(memberVote => memberVote.MemberId == currentMemberId.Value),
            Results = vote.Options
                .OrderBy(option => option.SortOrder)
                .Select(option =>
                {
                    var voteCount = vote.MemberVotes.Count(memberVote => memberVote.VoteOptionId == option.Id);

                    return new VoteOptionResultDto
                    {
                        VoteOptionId = option.Id,
                        OptionText = option.OptionText,
                        VoteCount = voteCount,
                        Percentage = totalVotesCast == 0 ? 0 : Math.Round(voteCount * 100m / totalVotesCast, 1)
                    };
                })
                .ToList()
        };
    }

    public async Task<bool> CastVoteAsync(Guid voteMotionId, Guid memberId, Guid voteOptionId, string? notes = null)
    {
        var now = DateTime.UtcNow;
        var vote = await context.VoteMotions
            .Include(existingVote => existingVote.Stokvel)
            .Include(existingVote => existingVote.Options)
            .FirstOrDefaultAsync(existingVote => existingVote.Id == voteMotionId);

        if (vote is null ||
            vote.Status != "Open" ||
            vote.OpensAt > now ||
            (vote.ClosesAt is not null && vote.ClosesAt < now))
        {
            return false;
        }

        if (!vote.Options.Any(option => option.Id == voteOptionId))
        {
            return false;
        }

        var member = await context.Members
            .FirstOrDefaultAsync(existingMember => existingMember.Id == memberId);

        if (member is null ||
            !await MemberBelongsToStokvelAsync(member, vote.StokvelId) ||
            member.Status != MemberStatus.Active)
        {
            return false;
        }

        var alreadyVoted = await context.MemberVotes
            .AnyAsync(memberVote =>
                memberVote.VoteMotionId == vote.Id &&
                memberVote.MemberId == member.Id);

        if (alreadyVoted)
        {
            return false;
        }

        context.MemberVotes.Add(new MemberVote
        {
            Id = Guid.NewGuid(),
            VoteMotionId = vote.Id,
            MemberId = member.Id,
            VoteOptionId = voteOptionId,
            VotedAt = now,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()
        });

        await context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> OpenVoteAsync(Guid voteMotionId, Guid openedByMemberId)
    {
        var vote = await context.VoteMotions
            .FirstOrDefaultAsync(existingVote => existingVote.Id == voteMotionId);

        if (vote is null || vote.Status is "Closed" or "Cancelled")
        {
            return false;
        }

        vote.Status = "Open";
        vote.OpensAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> CloseVoteAsync(Guid voteMotionId, Guid closedByMemberId)
    {
        var vote = await context.VoteMotions
            .Include(existingVote => existingVote.Stokvel)
            .Include(existingVote => existingVote.Options)
            .Include(existingVote => existingVote.MemberVotes)
            .FirstOrDefaultAsync(existingVote => existingVote.Id == voteMotionId);

        if (vote is null || vote.Status != "Open")
        {
            return false;
        }

        vote.Status = "Closed";
        vote.ClosedAt = DateTime.UtcNow;
        vote.ClosedByMemberId = closedByMemberId;
        vote.ClosesAt ??= vote.ClosedAt;
        vote.ResultSummary = BuildResultSummary(vote);
        vote.DecisionOutcome = await DetermineDecisionOutcomeAsync(vote);

        await context.SaveChangesAsync();

        return true;
    }

    private async Task<int> GetEligibleMemberCountAsync(Guid stokvelId)
    {
        var stokvel = await context.Stokvels
            .FirstOrDefaultAsync(existingStokvel => existingStokvel.Id == stokvelId);

        if (stokvel is null)
        {
            return 0;
        }

        return await context.Members
            .CountAsync(member =>
                member.TenantId == stokvel.TenantId &&
                member.Status == MemberStatus.Active);
    }

    private async Task<bool> MemberBelongsToStokvelAsync(Member member, Guid stokvelId)
    {
        return await context.Stokvels
            .AnyAsync(stokvel =>
                stokvel.Id == stokvelId &&
                stokvel.TenantId == member.TenantId);
    }

    private static string BuildResultSummary(VoteMotion vote)
    {
        var totalVotes = vote.MemberVotes.Count;
        var parts = vote.Options
            .OrderBy(option => option.SortOrder)
            .Select(option =>
            {
                var voteCount = vote.MemberVotes.Count(memberVote => memberVote.VoteOptionId == option.Id);
                var percentage = totalVotes == 0 ? 0 : Math.Round(voteCount * 100m / totalVotes, 1);

                return $"{option.OptionText}: {voteCount} vote{(voteCount == 1 ? string.Empty : "s")} ({percentage:0.#}%)";
            });

        return $"{string.Join(", ", parts)}.";
    }

    private async Task<string?> DetermineDecisionOutcomeAsync(VoteMotion vote)
    {
        if (!vote.VoteType.Equals("YesNo", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var totalVotes = vote.MemberVotes.Count;

        if (totalVotes == 0)
        {
            return "Not Carried";
        }

        var yesOption = vote.Options
            .FirstOrDefault(option => option.OptionText.Equals("Yes", StringComparison.OrdinalIgnoreCase));

        if (yesOption is null)
        {
            return null;
        }

        var yesCount = vote.MemberVotes.Count(memberVote => memberVote.VoteOptionId == yesOption.Id);
        var yesPercentage = yesCount * 100m / totalVotes;
        var threshold = 50m;

        if (vote.Stokvel is not null)
        {
            var rules = await stokvelOperatingRulesService.GetOrCreateDefaultRulesAsync(
                vote.StokvelId,
                vote.Stokvel.Type.ToString(),
                null);
            threshold = rules.DefaultVotingApprovalThreshold <= 0
                ? 50m
                : rules.DefaultVotingApprovalThreshold;
        }

        return yesPercentage >= threshold ? "Carried" : "Not Carried";
    }
}
