using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;

namespace Sisonke.Web.Services;

public class OperatingRuleService(ApplicationDbContext context)
{
    private async Task<string?> GetAnswerByQuestionTextAsync(Guid stokvelId, string questionText)
    {
        var stokvel = await context.Stokvels
            .SingleOrDefaultAsync(existingStokvel => existingStokvel.Id == stokvelId);

        if (stokvel is null)
        {
            return null;
        }

        return await context.StokvelQuestionnaireAnswers
            .Where(answer =>
                answer.TenantId == stokvel.TenantId &&
                answer.QuestionnaireQuestion.QuestionText == questionText)
            .Select(answer => answer.AnswerValue)
            .FirstOrDefaultAsync();
    }

    public async Task<int> GetMaxNextOfKinAsync(Guid stokvelId)
    {
        var answer = await GetAnswerByQuestionTextAsync(
            stokvelId,
            "What is the maximum number of next of kin records allowed per member?");

        return int.TryParse(answer, out var maxNextOfKin)
            ? maxNextOfKin
            : 2;
    }

    public async Task<int> GetMaxBeneficiariesAsync(Guid stokvelId)
    {
        var answer = await GetAnswerByQuestionTextAsync(
            stokvelId,
            "What is the maximum number of beneficiaries allowed per member?");

        return int.TryParse(answer, out var maxBeneficiaries)
            ? maxBeneficiaries
            : 1;
    }

    public async Task<int> GetMaxDependentsAsync(Guid stokvelId)
    {
        var answer = await GetAnswerByQuestionTextAsync(
            stokvelId,
            "What is the maximum number of dependents allowed per member?");

        return int.TryParse(answer, out var maxDependents)
            ? maxDependents
            : 0;
    }

    public async Task<decimal> GetContributionAmountAsync(Guid stokvelId)
    {
        var answer = await GetAnswerByQuestionTextAsync(
            stokvelId,
            "How much must each member contribute?");

        return decimal.TryParse(answer, out var contributionAmount)
            ? contributionAmount
            : 0;
    }

    public async Task<int> GetContributionDueDayAsync(Guid stokvelId)
    {
        var answer = await GetAnswerByQuestionTextAsync(
            stokvelId,
            "What day of the month are contributions due?");

        return int.TryParse(answer, out var contributionDueDay)
            ? contributionDueDay
            : 5;
    }

    public async Task<decimal> GetLatePaymentFineAmountAsync(Guid stokvelId)
    {
        var answer = await GetAnswerByQuestionTextAsync(
            stokvelId,
            "What is the late payment fine amount?");

        return decimal.TryParse(answer, out var latePaymentFineAmount)
            ? latePaymentFineAmount
            : 0;
    }

    public async Task<int> GetCoolingPeriodMonthsAsync(Guid stokvelId)
    {
        var answer = await GetAnswerByQuestionTextAsync(
            stokvelId,
            "How many months is the cooling period?");

        return int.TryParse(answer, out var coolingPeriodMonths)
            ? coolingPeriodMonths
            : 0;
    }

    public async Task<string?> GetDecisionApprovalMethodAsync(Guid stokvelId)
    {
        return await GetAnswerByQuestionTextAsync(
            stokvelId,
            "How should decisions be approved?");
    }
}
