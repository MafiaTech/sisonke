using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Services;

public class VotingService(ApplicationDbContext context)
{
    public async Task<List<MeetingVote>> GetVotesByMeetingIdAsync(Guid meetingId)
    {
        return await context.MeetingVotes
            .Include(vote => vote.Responses)
            .Where(vote => vote.MeetingId == meetingId)
            .OrderByDescending(vote => vote.OpenedAt)
            .ToListAsync();
    }

    public async Task<MeetingVote?> GetVoteByIdAsync(Guid voteId)
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
}
