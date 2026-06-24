using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Services;

public class QuestionnaireService(ApplicationDbContext context, FineService fineService)
{
    public async Task<List<QuestionnaireSection>> GetActiveQuestionnaireForStokvelAsync(Guid stokvelId)
    {
        var stokvel = await context.Stokvels
            .SingleOrDefaultAsync(existingStokvel => existingStokvel.Id == stokvelId);

        if (stokvel is null)
        {
            return [];
        }

        var selectedType = GetQuestionnaireStokvelType(stokvel);
        var sections = await GetActiveQuestionnaireAsync();

        return sections
            .Select(section => new QuestionnaireSection
            {
                Id = section.Id,
                Name = section.Name,
                Description = section.Description,
                DisplayOrder = section.DisplayOrder,
                IsActive = section.IsActive,
                Questions = section.Questions
                    .Where(question => IsQuestionApplicableToStokvelType(question, selectedType))
                    .OrderBy(question => question.DisplayOrder)
                    .ToList()
            })
            .Where(section => IsSetupSectionApplicable(section, stokvel) && section.Questions.Any())
            .OrderBy(section => section.DisplayOrder)
            .ToList();
    }

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

        var applicableQuestionIds = (await GetActiveQuestionnaireForStokvelAsync(stokvelId))
            .SelectMany(section => section.Questions)
            .Select(question => question.Id)
            .ToHashSet();

        if (applicableQuestionIds.Count == 0)
        {
            return [];
        }

        return await context.StokvelQuestionnaireAnswers
            .Where(answer =>
                answer.TenantId == stokvel.TenantId &&
                applicableQuestionIds.Contains(answer.QuestionnaireQuestionId))
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

        var applicableQuestionIds = (await GetActiveQuestionnaireForStokvelAsync(stokvelId))
            .SelectMany(section => section.Questions)
            .Select(question => question.Id)
            .ToHashSet();

        var filteredAnswers = answers
            .Where(answer => applicableQuestionIds.Contains(answer.Key))
            .ToDictionary(answer => answer.Key, answer => answer.Value);

        var questionIds = filteredAnswers.Keys.ToList();
        var existingAnswers = await context.StokvelQuestionnaireAnswers
            .Where(answer =>
                answer.TenantId == stokvel.TenantId &&
                questionIds.Contains(answer.QuestionnaireQuestionId))
            .ToDictionaryAsync(answer => answer.QuestionnaireQuestionId);

        foreach (var answer in filteredAnswers)
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

        var sections = await GetActiveQuestionnaireForStokvelAsync(stokvelId);

        var requiredIds = sections
            .SelectMany(s => s.Questions)
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

        var sections = await GetActiveQuestionnaireForStokvelAsync(stokvelId);

        var requiredQuestions = sections
            .SelectMany(s => s.Questions)
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
            "Rotational Basics" => stokvel.EnableRotation,
            "Rotational Payouts" => stokvel.EnableRotation,
            "Rotational Order" => stokvel.EnableRotation,
            "Rotational Sign-off" => stokvel.EnableRotation,
            _ => true
        };

    public static StokvelType GetQuestionnaireStokvelType(Stokvel stokvel) =>
        stokvel.Archetype switch
        {
            StokvelArchetype.BurialSociety => StokvelType.BurialSociety,
            StokvelArchetype.Rotational => StokvelType.RotationalStokvel,
            StokvelArchetype.Grocery => StokvelType.GroceryStokvel,
            StokvelArchetype.InvestmentClub => StokvelType.InvestmentStokvel,
            StokvelArchetype.Borrowing => StokvelType.LoanStokvel,
            StokvelArchetype.SocialClub => StokvelType.SocialClub,
            StokvelArchetype.SavingsClub or
            StokvelArchetype.Education or
            StokvelArchetype.Travel => StokvelType.SavingsStokvel,
            _ => stokvel.Type
        };

    public static bool IsQuestionApplicableToStokvelType(
        QuestionnaireQuestion question,
        StokvelType selectedType) =>
        question.IsActive &&
        (question.StokvelType is null || question.StokvelType == selectedType);

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
