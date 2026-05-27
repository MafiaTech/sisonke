using System.Net;
using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;

namespace Sisonke.Web.Services;

public class ConstitutionService(ApplicationDbContext context)
{
    private const string ToBeConfirmed = "To be confirmed by the stokvel.";

    public async Task<ConstitutionDocument?> GetLatestConstitutionAsync(Guid stokvelId)
    {
        var tenantId = await context.Stokvels
            .Where(existingStokvel => existingStokvel.Id == stokvelId)
            .Select(existingStokvel => (Guid?)existingStokvel.TenantId)
            .FirstOrDefaultAsync();

        if (tenantId is null)
        {
            return null;
        }

        return await context.ConstitutionDocuments
            .Where(document => document.TenantId == tenantId.Value)
            .OrderByDescending(document => document.VersionNumber)
            .ThenByDescending(document => document.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<string?> GenerateConstitutionPreviewAsync(Guid stokvelId)
    {
        var stokvel = await context.Stokvels
            .Include(existingStokvel => existingStokvel.Tenant)
            .SingleOrDefaultAsync(existingStokvel => existingStokvel.Id == stokvelId);

        if (stokvel is null)
        {
            return null;
        }

        var answers = await GetAnswersByQuestionTextAsync(stokvel.TenantId);

        var existingConstitutionAnswer = GetAnswer(answers, "Does this stokvel already have a constitution?");
        var existingConstitutionNote = GetExistingConstitutionNote(existingConstitutionAnswer);
        var purpose = GetAnswer(answers, "What is the main purpose of this stokvel?");
        var contributionAmount = GetAnswer(answers, "How much must each member contribute?");
        var contributionFrequency = GetAnswer(answers, "How often must members contribute?");
        var dueDay = GetAnswer(answers, "What day of the month are contributions due?");
        var partialPayments = GetAnswer(answers, "Are partial payments allowed?");
        var latePaymentFine = GetAnswer(answers, "What is the late payment fine amount?");
        var meetingFrequency = GetAnswer(answers, "How often does the stokvel meet?");
        var attendanceCompulsory = GetAnswer(answers, "Is meeting attendance compulsory?");
        var decisionApprovalMethod = GetAnswer(answers, "How should decisions be approved?");
        var claimsAllowed = GetAnswer(answers, "Are claims or payouts allowed?");
        var claimApprover = GetAnswer(answers, "Who approves claims or payouts?");
        var claimDocuments = GetAnswer(answers, "What documents are required for a claim or payout?");
        var maxNextOfKin = GetAnswer(answers, "What is the maximum number of next of kin records allowed per member?");
        var maxBeneficiaries = GetAnswer(answers, "What is the maximum number of beneficiaries allowed per member?");

        return $"""
            <p><strong>Constitution note:</strong> {Encode(existingConstitutionNote)}</p>

            <h2>1. Name of the Stokvel</h2>
            <p>The name of the stokvel is <strong>{EncodeOrDefault(stokvel.Name)}</strong>.</p>
            <p>Type: {Encode(stokvel.Type.ToString())}. Province: {EncodeOrDefault(stokvel.Province)}. Town or area: {EncodeOrDefault(stokvel.TownOrArea)}.</p>

            <h2>2. Purpose</h2>
            <p>{Encode(purpose)}</p>

            <h2>3. Membership</h2>
            <p>Membership rules, admission requirements and member responsibilities will be administered by the stokvel committee and recorded by {EncodeOrDefault(stokvel.Tenant?.Name)}.</p>

            <h2>4. Contributions</h2>
            <p>Each member must contribute {Encode(contributionAmount)} on a {Encode(contributionFrequency)} basis.</p>
            <p>Contributions are due on day {Encode(dueDay)} of the month. Partial payments allowed: {Encode(partialPayments)}.</p>

            <h2>5. Fines and Penalties</h2>
            <p>The late payment fine amount is {Encode(latePaymentFine)}.</p>

            <h2>6. Meetings and Attendance</h2>
            <p>The stokvel meets {Encode(meetingFrequency)}. Attendance compulsory: {Encode(attendanceCompulsory)}.</p>

            <h2>7. Decision-Making</h2>
            <p>Decisions must be approved using the following method: {Encode(decisionApprovalMethod)}.</p>

            <h2>8. Claims and Payouts</h2>
            <p>Claims or payouts allowed: {Encode(claimsAllowed)}.</p>
            <p>Claims or payouts are approved by: {Encode(claimApprover)}.</p>
            <p>Required documents: {Encode(claimDocuments)}.</p>

            <h2>9. Next of Kin and Beneficiaries</h2>
            <p>Maximum next of kin records per member: {Encode(maxNextOfKin)}.</p>
            <p>Maximum beneficiaries per member: {Encode(maxBeneficiaries)}.</p>

            <h2>10. Records and Administration</h2>
            <p>The stokvel must keep accurate records of members, contributions, fines, meetings, claims, payouts and decisions.</p>

            <h2>11. Amendments</h2>
            <p>This constitution may be amended according to the stokvel's approved decision-making method.</p>

            <h2>12. Dissolution</h2>
            <p>If the stokvel is dissolved, remaining funds and obligations must be handled according to a decision approved by the members.</p>

            <h2>13. Declaration</h2>
            <p>Members declare that they understand and accept this constitution as the operating rules of the stokvel.</p>
            """;
    }

    public async Task<Dictionary<string, string>> GetWizardAnswersAsync(Guid stokvelId)
    {
        var stokvel = await context.Stokvels
            .SingleOrDefaultAsync(existingStokvel => existingStokvel.Id == stokvelId);

        if (stokvel is null)
        {
            return [];
        }

        var records = await context.ConstitutionWizardAnswers
            .Where(answer => answer.TenantId == stokvel.TenantId)
            .OrderBy(answer => answer.AnsweredAt)
            .ToListAsync();

        var answers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var record in records)
        {
            if (string.IsNullOrWhiteSpace(record.QuestionKey) || answers.ContainsKey(record.QuestionKey))
            {
                continue;
            }

            answers[record.QuestionKey] = record.AnswerValue;
        }

        return answers;
    }

    public async Task<bool> SaveWizardAnswersAsync(Guid stokvelId, Dictionary<string, string> answers)
    {
        var stokvel = await context.Stokvels
            .SingleOrDefaultAsync(existingStokvel => existingStokvel.Id == stokvelId);

        if (stokvel is null)
        {
            return false;
        }

        var questionKeys = answers.Keys
            .Where(questionKey => !string.IsNullOrWhiteSpace(questionKey))
            .ToList();

        var existingAnswers = await context.ConstitutionWizardAnswers
            .Where(answer =>
                answer.TenantId == stokvel.TenantId &&
                questionKeys.Contains(answer.QuestionKey))
            .ToDictionaryAsync(answer => answer.QuestionKey, StringComparer.OrdinalIgnoreCase);

        var now = DateTime.UtcNow;

        foreach (var answer in answers)
        {
            if (string.IsNullOrWhiteSpace(answer.Key))
            {
                continue;
            }

            var (questionText, stepNumber) = GetWizardQuestionMetadata(answer.Key);

            if (existingAnswers.TryGetValue(answer.Key, out var existingAnswer))
            {
                existingAnswer.QuestionText = questionText;
                existingAnswer.StepNumber = stepNumber;
                existingAnswer.AnswerValue = answer.Value;
                existingAnswer.AnsweredAt = now;
                continue;
            }

            context.ConstitutionWizardAnswers.Add(new ConstitutionWizardAnswer
            {
                Id = Guid.NewGuid(),
                TenantId = stokvel.TenantId,
                QuestionKey = answer.Key,
                QuestionText = questionText,
                StepNumber = stepNumber,
                AnswerValue = answer.Value,
                AnsweredAt = now
            });
        }

        await context.SaveChangesAsync();

        return true;
    }

    public async Task<string?> GenerateConstitutionFromWizardAsync(Guid stokvelId)
    {
        var stokvel = await context.Stokvels
            .Include(existingStokvel => existingStokvel.Tenant)
            .SingleOrDefaultAsync(existingStokvel => existingStokvel.Id == stokvelId);

        if (stokvel is null)
        {
            return null;
        }

        var answers = await GetWizardAnswersAsync(stokvelId);

        var purpose = GetWizardAnswer(answers, "groupPurpose");
        var chairperson = GetWizardAnswer(answers, "executiveChairperson");
        var secretary = GetWizardAnswer(answers, "executiveSecretary");
        var treasurer = GetWizardAnswer(answers, "executiveTreasurer");
        var contributionAmount = GetWizardAnswer(answers, "mandatoryContributionAmount");
        var contributionFrequency = GetWizardAnswer(answers, "contributionFrequency");
        var contributionDueDay = GetWizardAnswer(answers, "contributionDueDay");
        var hasJoiningFee = GetWizardAnswer(answers, "hasJoiningFee");
        var joiningFeeAmount = GetWizardAnswer(answers, "joiningFeeAmount");
        var withdrawalApprovalMandate = GetWizardAnswer(answers, "withdrawalApprovalMandate");
        var constitutionChangeVotingMajority = GetWizardAnswer(answers, "constitutionChangeVotingMajority");
        var missedPaymentSuspensionRule = GetWizardAnswer(answers, "missedPaymentSuspensionRule");
        var memberExitContributionHandling = GetWizardAnswer(answers, "memberExitContributionHandling");
        var deathBenefitHandling = GetWizardAnswer(answers, "deathBenefitHandling");
        var legalDisclosureAccepted = GetWizardAnswer(answers, "legalDisclosureAccepted");

        return $"""
            <h2>1. Name and Type</h2>
            <p>The name of the stokvel is <strong>{EncodeOrDefault(stokvel.Name)}</strong>.</p>
            <p>Type: {Encode(stokvel.Type.ToString())}. Province: {EncodeOrDefault(stokvel.Province)}. Town or area: {EncodeOrDefault(stokvel.TownOrArea)}.</p>
            <p>The stokvel is administered under tenant profile {EncodeOrDefault(stokvel.Tenant?.Name)}.</p>

            <h2>2. Purpose</h2>
            <p>{Encode(purpose)}</p>

            <h2>3. Executive Committee</h2>
            <p>Chairperson: {Encode(chairperson)}.</p>
            <p>Secretary: {Encode(secretary)}.</p>
            <p>Treasurer: {Encode(treasurer)}.</p>

            <h2>4. Contributions and Fees</h2>
            <p>Mandatory contribution amount: {Encode(contributionAmount)}.</p>
            <p>Contribution frequency: {Encode(contributionFrequency)}.</p>
            <p>Contribution due day: {Encode(contributionDueDay)}.</p>
            <p>Joining fee required: {Encode(hasJoiningFee)}. Joining fee amount: {Encode(joiningFeeAmount)}.</p>

            <h2>5. Withdrawal and Approval Mandate</h2>
            <p>{Encode(withdrawalApprovalMandate)}</p>

            <h2>6. Membership Discipline and Exit</h2>
            <p>Missed payment suspension rule: {Encode(missedPaymentSuspensionRule)}.</p>
            <p>Member exit contribution handling: {Encode(memberExitContributionHandling)}.</p>

            <h2>7. Voting and Amendments</h2>
            <p>Constitution changes must follow this voting majority: {Encode(constitutionChangeVotingMajority)}.</p>

            <h2>8. Special Rules Based on Stokvel Type</h2>
            {BuildSpecialRulesHtml(stokvel.Type.ToString(), answers)}

            <h2>9. Next of Kin / Beneficiary Mandate</h2>
            <p>Death benefit handling: {Encode(deathBenefitHandling)}.</p>

            <h2>10. POPIA and Legal Disclosure</h2>
            <p>Legal disclosure accepted: {Encode(legalDisclosureAccepted)}.</p>
            <p>The stokvel must protect member personal information and use it only for lawful stokvel administration, communication, claims, contribution management and related governance records.</p>

            <h2>11. Declaration</h2>
            <p>Members declare that they understand and accept this constitution as the operating rules of the stokvel. Any missing rules marked as "{Encode(ToBeConfirmed)}" must be confirmed by the stokvel before final approval.</p>
            """;
    }

    public async Task<ConstitutionDocument?> SaveDraftAsync(Guid stokvelId, string content)
    {
        var stokvel = await context.Stokvels
            .SingleOrDefaultAsync(existingStokvel => existingStokvel.Id == stokvelId);

        if (stokvel is null)
        {
            return null;
        }

        var latestConstitution = await GetLatestConstitutionAsync(stokvelId);
        var version = (latestConstitution?.VersionNumber ?? 0) + 1;

        var document = new ConstitutionDocument
        {
            Id = Guid.NewGuid(),
            TenantId = stokvel.TenantId,
            Title = $"{stokvel.Name} Constitution",
            Content = string.IsNullOrWhiteSpace(content) ? ToBeConfirmed : content,
            VersionNumber = version,
            IsApproved = false,
            CreatedAt = DateTime.UtcNow
        };

        context.ConstitutionDocuments.Add(document);
        await context.SaveChangesAsync();

        return document;
    }

    public async Task<ConstitutionDocument?> ApproveLatestAsync(Guid stokvelId)
    {
        var document = await GetLatestConstitutionAsync(stokvelId);

        if (document is null)
        {
            var preview = await GenerateConstitutionPreviewAsync(stokvelId);

            if (preview is null)
            {
                return null;
            }

            document = await SaveDraftAsync(stokvelId, preview);
        }

        if (document is null)
        {
            return null;
        }

        document.IsApproved = true;
        document.ApprovedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return document;
    }

    public async Task<string> GetConstitutionStatusAsync(Guid stokvelId)
    {
        var latestConstitution = await GetLatestConstitutionAsync(stokvelId);

        if (latestConstitution is null)
        {
            return "Not Created";
        }

        return latestConstitution.IsApproved
            ? "Approved"
            : "Draft";
    }

    private async Task<Dictionary<string, string>> GetAnswersByQuestionTextAsync(Guid tenantId)
    {
        var records = await context.StokvelQuestionnaireAnswers
            .Include(answer => answer.QuestionnaireQuestion)
            .Where(answer => answer.TenantId == tenantId)
            .OrderByDescending(answer => answer.AnsweredAt)
            .ToListAsync();

        var answers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var record in records)
        {
            var questionText = record.QuestionnaireQuestion?.QuestionText;
            var answerValue = record.AnswerValue;

            if (string.IsNullOrWhiteSpace(questionText) || string.IsNullOrWhiteSpace(answerValue))
            {
                continue;
            }

            if (!answers.ContainsKey(questionText))
            {
                answers[questionText] = answerValue;
            }
        }

        return answers;
    }

    private static string GetAnswer(Dictionary<string, string> answers, string questionText)
    {
        return answers.TryGetValue(questionText, out var answer) && !string.IsNullOrWhiteSpace(answer)
            ? answer
            : ToBeConfirmed;
    }

    private static string GetWizardAnswer(Dictionary<string, string> answers, string questionKey)
    {
        return answers.TryGetValue(questionKey, out var answer) && !string.IsNullOrWhiteSpace(answer)
            ? answer
            : ToBeConfirmed;
    }

    private static (string QuestionText, int StepNumber) GetWizardQuestionMetadata(string questionKey)
    {
        return questionKey switch
        {
            "groupPurpose" => ("What is the purpose of the stokvel?", 1),
            "executiveChairperson" => ("Who is the executive chairperson?", 1),
            "executiveSecretary" => ("Who is the executive secretary?", 1),
            "executiveTreasurer" => ("Who is the executive treasurer?", 1),

            "mandatoryContributionAmount" => ("What is the mandatory contribution amount?", 2),
            "contributionFrequency" => ("How often must members contribute?", 2),
            "contributionDueDay" => ("What day are contributions due?", 2),
            "hasJoiningFee" => ("Does the stokvel have a joining fee?", 2),
            "joiningFeeAmount" => ("What is the joining fee amount?", 2),
            "withdrawalApprovalMandate" => ("What approval mandate is required for withdrawals?", 2),

            "constitutionChangeVotingMajority" => ("What voting majority is required to change the constitution?", 3),
            "missedPaymentSuspensionRule" => ("What is the missed payment suspension rule?", 3),
            "memberExitContributionHandling" => ("How are contributions handled when a member exits?", 3),
            "burialWaitingPeriod" => ("What is the burial waiting period?", 3),
            "burialClaimTurnaround" => ("What is the burial claim turnaround time?", 3),
            "burialFamilyCoverage" => ("Which family members are covered by burial benefits?", 3),
            "investmentVotingPower" => ("How is investment voting power allocated?", 3),
            "investmentLockInPeriod" => ("What is the investment lock-in period?", 3),
            "investmentEarlyExitRule" => ("What is the investment early exit rule?", 3),
            "grocerySplitMethod" => ("How are groceries split?", 3),
            "groceryTransportCostRule" => ("How are grocery transport costs handled?", 3),

            "deathBenefitHandling" => ("How are death benefits handled?", 4),
            "legalDisclosureAccepted" => ("Has the legal disclosure been accepted?", 4),

            _ => (questionKey, 0)
        };
    }

    private static string BuildSpecialRulesHtml(string stokvelType, Dictionary<string, string> answers)
    {
        return stokvelType switch
        {
            "BurialSociety" => $"""
                <p>Burial waiting period: {Encode(GetWizardAnswer(answers, "burialWaitingPeriod"))}.</p>
                <p>Burial claim turnaround: {Encode(GetWizardAnswer(answers, "burialClaimTurnaround"))}.</p>
                <p>Burial family coverage: {Encode(GetWizardAnswer(answers, "burialFamilyCoverage"))}.</p>
                """,
            "InvestmentStokvel" => $"""
                <p>Investment voting power: {Encode(GetWizardAnswer(answers, "investmentVotingPower"))}.</p>
                <p>Investment lock-in period: {Encode(GetWizardAnswer(answers, "investmentLockInPeriod"))}.</p>
                <p>Investment early exit rule: {Encode(GetWizardAnswer(answers, "investmentEarlyExitRule"))}.</p>
                """,
            "GroceryStokvel" => $"""
                <p>Grocery split method: {Encode(GetWizardAnswer(answers, "grocerySplitMethod"))}.</p>
                <p>Grocery transport cost rule: {Encode(GetWizardAnswer(answers, "groceryTransportCostRule"))}.</p>
                """,
            _ => $"<p>{Encode(ToBeConfirmed)}</p>"
        };
    }

    private static string Encode(string value)
    {
        return WebUtility.HtmlEncode(value);
    }

    private static string GetExistingConstitutionNote(string existingConstitutionAnswer)
    {
        return existingConstitutionAnswer switch
        {
            "Yes" => "This stokvel has indicated that an existing constitution is available. This generated version can be used for comparison, review, or future amendments.",
            "No" => "This constitution has been generated to help the stokvel establish formal operating rules.",
            _ => "The stokvel should review whether an existing constitution is available before approving this document."
        };
    }

    private static string EncodeOrDefault(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? ToBeConfirmed
            : Encode(value);
    }
}
