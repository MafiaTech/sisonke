using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Data.Seed;

public static class SisonkeSeedData
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        await SeedSubscriptionPlansAsync(context);
        await SeedPilotTenantsAsync(context);
        await SeedQuestionnaireAsync(context);
    }

    private static async Task SeedSubscriptionPlansAsync(ApplicationDbContext context)
    {
        var plans = new[]
        {
            new SubscriptionPlan
            {
                Id = Guid.NewGuid(),
                Name = "Pilot",
                MinMembers = 1,
                MaxMembers = null,
                MonthlyPrice = 0,
                AnnualPrice = 0,
                IsActive = true
            },
            new SubscriptionPlan
            {
                Id = Guid.NewGuid(),
                Name = "Starter",
                MinMembers = 1,
                MaxMembers = 30,
                MonthlyPrice = 50,
                AnnualPrice = 500,
                IsActive = true
            },
            new SubscriptionPlan
            {
                Id = Guid.NewGuid(),
                Name = "Growth",
                MinMembers = 31,
                MaxMembers = 50,
                MonthlyPrice = 70,
                AnnualPrice = 700,
                IsActive = true
            },
            new SubscriptionPlan
            {
                Id = Guid.NewGuid(),
                Name = "Premium",
                MinMembers = 51,
                MaxMembers = null,
                MonthlyPrice = 100,
                AnnualPrice = 1000,
                IsActive = true
            }
        };

        var existingPlanNames = await context.SubscriptionPlans
            .Select(plan => plan.Name)
            .ToListAsync();

        var missingPlans = plans
            .Where(plan => !existingPlanNames.Contains(plan.Name))
            .ToList();

        if (missingPlans.Count > 0)
        {
            context.SubscriptionPlans.AddRange(missingPlans);
            await context.SaveChangesAsync();
        }
    }

    private static async Task SeedPilotTenantsAsync(ApplicationDbContext context)
    {
        var pilotPlan = await context.SubscriptionPlans
            .SingleAsync(plan => plan.Name == "Pilot");

        var aganangTenant = await GetOrCreateTenantAsync(
            context,
            "Aganang Burial Society",
            "aganang-burial-society");

        var letsGrowTenant = await GetOrCreateTenantAsync(
            context,
            "Let’s Grow Together",
            "lets-grow-together");

        await GetOrCreateStokvelAsync(
            context,
            aganangTenant,
            "Aganang Burial Society",
            StokvelType.BurialSociety);

        await GetOrCreateStokvelAsync(
            context,
            letsGrowTenant,
            "Let’s Grow Together",
            StokvelType.SavingsStokvel);

        await GetOrCreateTenantSubscriptionAsync(context, aganangTenant, pilotPlan);
        await GetOrCreateTenantSubscriptionAsync(context, letsGrowTenant, pilotPlan);

        await SeedFineTypesAsync(
            context,
            aganangTenant,
            [
                new FineTypeSeed("Late Coming Fine", 50),
                new FineTypeSeed("Late Apology Fine", 0),
                new FineTypeSeed("No Apology Fine", 0),
                new FineTypeSeed("Food Contribution Fine", 0),
                new FineTypeSeed("Misconduct Fine", 0),
                new FineTypeSeed("Custom Fine", 0)
            ]);

        await SeedFineTypesAsync(
            context,
            letsGrowTenant,
            [
                new FineTypeSeed("Misconduct Fine", 200),
                new FineTypeSeed("Late Apology Fine", 0),
                new FineTypeSeed("No Apology Fine", 0),
                new FineTypeSeed("Early Leave Fine", 0),
                new FineTypeSeed("Non-Compliance Fine", 0),
                new FineTypeSeed("Custom Fine", 0)
            ]);

        await context.SaveChangesAsync();
    }

    private static async Task<Tenant> GetOrCreateTenantAsync(
        ApplicationDbContext context,
        string name,
        string slug)
    {
        var tenant = await context.Tenants
            .SingleOrDefaultAsync(existingTenant => existingTenant.Slug == slug);

        if (tenant is not null)
        {
            return tenant;
        }

        tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = slug,
            IsActive = true
        };

        context.Tenants.Add(tenant);

        return tenant;
    }

    private static async Task<Stokvel> GetOrCreateStokvelAsync(
        ApplicationDbContext context,
        Tenant tenant,
        string name,
        StokvelType type)
    {
        var stokvel = await context.Stokvels
            .SingleOrDefaultAsync(existingStokvel =>
                existingStokvel.TenantId == tenant.Id &&
                existingStokvel.Name == name);

        if (stokvel is not null)
        {
            return stokvel;
        }

        stokvel = new Stokvel
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Tenant = tenant,
            Name = name,
            Type = type,
            IsActive = true
        };

        context.Stokvels.Add(stokvel);

        return stokvel;
    }

    private static async Task<TenantSubscription> GetOrCreateTenantSubscriptionAsync(
        ApplicationDbContext context,
        Tenant tenant,
        SubscriptionPlan pilotPlan)
    {
        var subscription = await context.TenantSubscriptions
            .SingleOrDefaultAsync(existingSubscription =>
                existingSubscription.TenantId == tenant.Id &&
                existingSubscription.SubscriptionPlanId == pilotPlan.Id);

        if (subscription is not null)
        {
            return subscription;
        }

        subscription = new TenantSubscription
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Tenant = tenant,
            SubscriptionPlanId = pilotPlan.Id,
            SubscriptionPlan = pilotPlan,
            Status = SubscriptionStatus.Active,
            IsTrial = true,
            StartDate = DateTime.UtcNow
        };

        context.TenantSubscriptions.Add(subscription);

        return subscription;
    }

    private static async Task SeedFineTypesAsync(
        ApplicationDbContext context,
        Tenant tenant,
        IReadOnlyCollection<FineTypeSeed> fineTypes)
    {
        var existingFineTypeNames = await context.FineTypes
            .Where(fineType => fineType.TenantId == tenant.Id)
            .Select(fineType => fineType.Name)
            .ToListAsync();

        var missingFineTypes = fineTypes
            .Where(fineType => !existingFineTypeNames.Contains(fineType.Name))
            .Select(fineType => new FineType
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Tenant = tenant,
                Name = fineType.Name,
                DefaultAmount = fineType.DefaultAmount,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            })
            .ToList();

        if (missingFineTypes.Count > 0)
        {
            context.FineTypes.AddRange(missingFineTypes);
        }
    }

    private static async Task SeedQuestionnaireAsync(ApplicationDbContext context)
    {
        var stokvelIdentity = await GetOrCreateQuestionnaireSectionAsync(
            context,
            "Stokvel Identity",
            "Basic information about the stokvel.",
            1);

        var membershipRules = await GetOrCreateQuestionnaireSectionAsync(
            context,
            "Membership Rules",
            "Rules that define who can join and how members are managed.",
            2);

        var contributionsAndFines = await GetOrCreateQuestionnaireSectionAsync(
            context,
            "Contributions and Fines",
            "Rules for payments, due dates, arrears and fines.",
            3);

        var meetingsAndDecisions = await GetOrCreateQuestionnaireSectionAsync(
            context,
            "Meetings and Decisions",
            "Rules for meetings, attendance and decision-making.",
            4);

        var claimsAndPayouts = await GetOrCreateQuestionnaireSectionAsync(
            context,
            "Claims and Payouts",
            "Rules for burial claims, savings payouts and approvals.",
            5);

        await GetOrCreateQuestionnaireQuestionAsync(
            context,
            stokvelIdentity,
            "What is the main purpose of this stokvel?",
            QuestionType.Text,
            true,
            1);

        var stokvelTypeQuestion = await GetOrCreateQuestionnaireQuestionAsync(
            context,
            stokvelIdentity,
            "Which type of stokvel is this?",
            QuestionType.SingleSelect,
            true,
            2);
        await SeedQuestionnaireOptionsAsync(
            context,
            stokvelTypeQuestion,
            ["Burial Society", "Savings Stokvel", "Grocery Stokvel", "Investment Stokvel", "Social Club", "Family Society"]);

        await GetOrCreateQuestionnaireQuestionAsync(
            context,
            membershipRules,
            "Does this stokvel have a joining fee?",
            QuestionType.YesNo,
            true,
            1);

        await GetOrCreateQuestionnaireQuestionAsync(
            context,
            membershipRules,
            "What is the joining fee amount?",
            QuestionType.Currency,
            false,
            2);

        await GetOrCreateQuestionnaireQuestionAsync(
            context,
            membershipRules,
            "Does this stokvel have a cooling period?",
            QuestionType.YesNo,
            true,
            3);

        await GetOrCreateQuestionnaireQuestionAsync(
            context,
            membershipRules,
            "How many months is the cooling period?",
            QuestionType.Number,
            false,
            4);

        await GetOrCreateQuestionnaireQuestionAsync(
            context,
            membershipRules,
            "What is the maximum number of next of kin records allowed per member?",
            QuestionType.Number,
            true,
            5);

        await GetOrCreateQuestionnaireQuestionAsync(
            context,
            membershipRules,
            "What is the maximum number of beneficiaries allowed per member?",
            QuestionType.Number,
            true,
            6);

        await GetOrCreateQuestionnaireQuestionAsync(
            context,
            contributionsAndFines,
            "How much must each member contribute?",
            QuestionType.Currency,
            true,
            1);

        var contributionFrequencyQuestion = await GetOrCreateQuestionnaireQuestionAsync(
            context,
            contributionsAndFines,
            "How often must members contribute?",
            QuestionType.SingleSelect,
            true,
            2);
        await SeedQuestionnaireOptionsAsync(
            context,
            contributionFrequencyQuestion,
            ["Weekly", "Monthly", "Quarterly", "Annually"]);

        await GetOrCreateQuestionnaireQuestionAsync(
            context,
            contributionsAndFines,
            "What day of the month are contributions due?",
            QuestionType.Number,
            true,
            3);

        await GetOrCreateQuestionnaireQuestionAsync(
            context,
            contributionsAndFines,
            "Are partial payments allowed?",
            QuestionType.YesNo,
            true,
            4);

        await GetOrCreateQuestionnaireQuestionAsync(
            context,
            contributionsAndFines,
            "Is there a fine for late payment?",
            QuestionType.YesNo,
            true,
            5);

        await GetOrCreateQuestionnaireQuestionAsync(
            context,
            contributionsAndFines,
            "What is the late payment fine amount?",
            QuestionType.Currency,
            false,
            6);

        var meetingFrequencyQuestion = await GetOrCreateQuestionnaireQuestionAsync(
            context,
            meetingsAndDecisions,
            "How often does the stokvel meet?",
            QuestionType.SingleSelect,
            true,
            1);
        await SeedQuestionnaireOptionsAsync(
            context,
            meetingFrequencyQuestion,
            ["Weekly", "Monthly", "Quarterly", "Annually", "As needed"]);

        await GetOrCreateQuestionnaireQuestionAsync(
            context,
            meetingsAndDecisions,
            "Is meeting attendance compulsory?",
            QuestionType.YesNo,
            true,
            2);

        await GetOrCreateQuestionnaireQuestionAsync(
            context,
            meetingsAndDecisions,
            "What is the fine for late coming to meetings?",
            QuestionType.Currency,
            false,
            3);

        var decisionApprovalQuestion = await GetOrCreateQuestionnaireQuestionAsync(
            context,
            meetingsAndDecisions,
            "How should decisions be approved?",
            QuestionType.SingleSelect,
            true,
            4);
        await SeedQuestionnaireOptionsAsync(
            context,
            decisionApprovalQuestion,
            ["Majority vote", "Committee approval", "Chairperson approval", "Unanimous agreement"]);

        await GetOrCreateQuestionnaireQuestionAsync(
            context,
            claimsAndPayouts,
            "Are claims or payouts allowed?",
            QuestionType.YesNo,
            true,
            1);

        var claimApprovalQuestion = await GetOrCreateQuestionnaireQuestionAsync(
            context,
            claimsAndPayouts,
            "Who approves claims or payouts?",
            QuestionType.SingleSelect,
            true,
            2);
        await SeedQuestionnaireOptionsAsync(
            context,
            claimApprovalQuestion,
            ["Chairperson", "Treasurer", "Committee", "Members vote", "Chairperson and Treasurer"]);

        await GetOrCreateQuestionnaireQuestionAsync(
            context,
            claimsAndPayouts,
            "What documents are required for a claim or payout?",
            QuestionType.Text,
            false,
            3);

        await context.SaveChangesAsync();
    }

    private static async Task<QuestionnaireSection> GetOrCreateQuestionnaireSectionAsync(
        ApplicationDbContext context,
        string name,
        string description,
        int displayOrder)
    {
        var section = await context.QuestionnaireSections
            .SingleOrDefaultAsync(existingSection => existingSection.Name == name);

        if (section is not null)
        {
            return section;
        }

        section = new QuestionnaireSection
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            DisplayOrder = displayOrder,
            IsActive = true
        };

        context.QuestionnaireSections.Add(section);

        return section;
    }

    private static async Task<QuestionnaireQuestion> GetOrCreateQuestionnaireQuestionAsync(
        ApplicationDbContext context,
        QuestionnaireSection section,
        string questionText,
        QuestionType questionType,
        bool isRequired,
        int displayOrder)
    {
        var question = await context.QuestionnaireQuestions
            .SingleOrDefaultAsync(existingQuestion =>
                existingQuestion.QuestionnaireSectionId == section.Id &&
                existingQuestion.QuestionText == questionText);

        if (question is not null)
        {
            return question;
        }

        question = new QuestionnaireQuestion
        {
            Id = Guid.NewGuid(),
            QuestionnaireSectionId = section.Id,
            QuestionnaireSection = section,
            QuestionText = questionText,
            QuestionType = questionType,
            IsRequired = isRequired,
            DisplayOrder = displayOrder,
            IsActive = true
        };

        context.QuestionnaireQuestions.Add(question);

        return question;
    }

    private static async Task SeedQuestionnaireOptionsAsync(
        ApplicationDbContext context,
        QuestionnaireQuestion question,
        IReadOnlyList<string> optionTexts)
    {
        var existingOptionTexts = await context.QuestionnaireOptions
            .Where(option => option.QuestionnaireQuestionId == question.Id)
            .Select(option => option.OptionText)
            .ToListAsync();

        for (var index = 0; index < optionTexts.Count; index++)
        {
            var optionText = optionTexts[index];

            if (existingOptionTexts.Contains(optionText))
            {
                continue;
            }

            context.QuestionnaireOptions.Add(new QuestionnaireOption
            {
                Id = Guid.NewGuid(),
                QuestionnaireQuestionId = question.Id,
                QuestionnaireQuestion = question,
                OptionText = optionText,
                OptionValue = optionText,
                DisplayOrder = index + 1,
                IsActive = true
            });
        }
    }

    private sealed record FineTypeSeed(string Name, decimal DefaultAmount);
}
