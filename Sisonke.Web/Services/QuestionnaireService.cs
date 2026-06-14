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

        // Deduplicate: keep only the first active section per name (guards against duplicate seed runs)
        sections = sections
            .GroupBy(s => s.Name)
            .Select(g => g.First())
            .OrderBy(s => s.DisplayOrder)
            .ToList();

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

        if (stokvel is null) return 0;

        var sections = await GetActiveQuestionnaireAsync();

        var requiredIds = sections
            .Where(s => IsSetupSectionApplicable(s, stokvel))
            .SelectMany(s => GetSetupApplicableQuestions(s, stokvel))
            .Where(q => q.IsRequired)
            .Select(q => q.Id)
            .ToList();

        if (requiredIds.Count == 0) return 100;

        var answeredCount = await context.StokvelQuestionnaireAnswers
            .CountAsync(a =>
                a.TenantId == stokvel.TenantId &&
                requiredIds.Contains(a.QuestionnaireQuestionId) &&
                !string.IsNullOrWhiteSpace(a.AnswerValue));

        return answeredCount * 100 / requiredIds.Count;
    }

    public async Task<List<string>> GetMissingSetupFieldNamesAsync(Guid stokvelId)
    {
        var stokvel = await context.Stokvels
            .SingleOrDefaultAsync(existingStokvel => existingStokvel.Id == stokvelId);

        if (stokvel is null) return [];

        var sections = await GetActiveQuestionnaireAsync();

        var requiredQuestions = sections
            .Where(s => IsSetupSectionApplicable(s, stokvel))
            .SelectMany(s => GetSetupApplicableQuestions(s, stokvel))
            .Where(q => q.IsRequired)
            .ToList();

        if (requiredQuestions.Count == 0) return [];

        var requiredIds = requiredQuestions.Select(q => q.Id).ToList();

        var answeredIds = (await context.StokvelQuestionnaireAnswers
            .Where(a =>
                a.TenantId == stokvel.TenantId &&
                requiredIds.Contains(a.QuestionnaireQuestionId) &&
                !string.IsNullOrWhiteSpace(a.AnswerValue))
            .Select(a => a.QuestionnaireQuestionId)
            .ToListAsync())
            .ToHashSet();

        return requiredQuestions
            .Where(q => !answeredIds.Contains(q.Id))
            .Select(q => q.QuestionText)
            .ToList();
    }

    private static bool IsSetupSectionApplicable(QuestionnaireSection section, Stokvel stokvel) =>
        section.Name switch
        {
            "Claims and Payouts" => stokvel.EnableClaims,
            _ => true
        };

    private static IEnumerable<QuestionnaireQuestion> GetSetupApplicableQuestions(
        QuestionnaireSection section, Stokvel stokvel)
    {
        if (stokvel.EnableDependents && stokvel.EnableClaims)
            return section.Questions;
        return section.Questions.Where(q =>
            !q.QuestionText.Contains("beneficiaries", StringComparison.OrdinalIgnoreCase) &&
            !q.QuestionText.Contains("dependents", StringComparison.OrdinalIgnoreCase));
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
