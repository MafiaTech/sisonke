using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;

namespace Sisonke.Web.Services;

public class QuestionnaireService(ApplicationDbContext context, FineService fineService)
{
    public async Task<List<QuestionnaireSection>> GetActiveQuestionnaireAsync()
    {
        var sections = await context.QuestionnaireSections
            .Where(section => section.IsActive)
            .Include(section => section.Questions
                .Where(question => question.IsActive)
                .OrderBy(question => question.DisplayOrder))
            .ThenInclude(question => question.Options
                .Where(option => option.IsActive)
                .OrderBy(option => option.DisplayOrder))
            .OrderBy(section => section.DisplayOrder)
            .ToListAsync();

        foreach (var section in sections)
        {
            section.Questions = section.Questions
                .OrderBy(question => question.DisplayOrder)
                .ToList();

            foreach (var question in section.Questions)
            {
                question.Options = question.Options
                    .OrderBy(option => option.DisplayOrder)
                    .ToList();
            }
        }

        return sections;
    }

    public async Task<Dictionary<Guid, string>> GetAnswersByStokvelIdAsync(Guid stokvelId)
    {
        var stokvel = await context.Stokvels
            .SingleOrDefaultAsync(existingStokvel => existingStokvel.Id == stokvelId);

        if (stokvel is null)
        {
            return [];
        }

        return await context.StokvelQuestionnaireAnswers
            .Where(answer => answer.TenantId == stokvel.TenantId)
            .ToDictionaryAsync(
                answer => answer.QuestionnaireQuestionId,
                answer => answer.AnswerValue);
    }

    public async Task<bool> SaveAnswersAsync(Guid stokvelId, Dictionary<Guid, string> answers)
    {
        var stokvel = await context.Stokvels
            .SingleOrDefaultAsync(existingStokvel => existingStokvel.Id == stokvelId);

        if (stokvel is null)
        {
            return false;
        }

        var questionIds = answers.Keys.ToList();
        var existingAnswers = await context.StokvelQuestionnaireAnswers
            .Where(answer =>
                answer.TenantId == stokvel.TenantId &&
                questionIds.Contains(answer.QuestionnaireQuestionId))
            .ToDictionaryAsync(answer => answer.QuestionnaireQuestionId);

        foreach (var answer in answers)
        {
            if (existingAnswers.TryGetValue(answer.Key, out var existingAnswer))
            {
                existingAnswer.AnswerValue = answer.Value;
                existingAnswer.AnsweredAt = DateTime.UtcNow;
                continue;
            }

            context.StokvelQuestionnaireAnswers.Add(new StokvelQuestionnaireAnswer
            {
                Id = Guid.NewGuid(),
                TenantId = stokvel.TenantId,
                QuestionnaireQuestionId = answer.Key,
                AnswerValue = answer.Value,
                AnsweredAt = DateTime.UtcNow
            });
        }

        await context.SaveChangesAsync();

        return true;
    }

    public async Task<int> GetCompletionPercentageAsync(Guid stokvelId)
    {
        var stokvel = await context.Stokvels
            .SingleOrDefaultAsync(existingStokvel => existingStokvel.Id == stokvelId);

        if (stokvel is null)
        {
            return 0;
        }

        var requiredQuestionIds = await context.QuestionnaireQuestions
            .Where(question =>
                question.IsActive &&
                question.IsRequired &&
                question.QuestionnaireSection.IsActive)
            .Select(question => question.Id)
            .ToListAsync();

        if (requiredQuestionIds.Count == 0)
        {
            return 100;
        }

        var answeredRequiredQuestionCount = await context.StokvelQuestionnaireAnswers
            .CountAsync(answer =>
                answer.TenantId == stokvel.TenantId &&
                requiredQuestionIds.Contains(answer.QuestionnaireQuestionId) &&
                !string.IsNullOrWhiteSpace(answer.AnswerValue));

        return answeredRequiredQuestionCount * 100 / requiredQuestionIds.Count;
    }

    public async Task<bool> CanSubmitRegistrationAsync(Guid stokvelId)
    {
        return await GetCompletionPercentageAsync(stokvelId) == 100;
    }

    public async Task<bool> SubmitRegistrationAsync(Guid stokvelId)
    {
        var stokvel = await context.Stokvels
            .SingleOrDefaultAsync(existingStokvel => existingStokvel.Id == stokvelId);

        if (stokvel is null)
        {
            return false;
        }

        var completionPercentage = await GetCompletionPercentageAsync(stokvelId);

        if (completionPercentage < 100)
        {
            return false;
        }

        stokvel.IsSetupComplete = true;
        stokvel.SetupCompletedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        await fineService.EnsureDefaultFineTypesForStokvelAsync(stokvelId);

        return true;
    }
}
