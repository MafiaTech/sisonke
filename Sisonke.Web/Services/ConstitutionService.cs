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
