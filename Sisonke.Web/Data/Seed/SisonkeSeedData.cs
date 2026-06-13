using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Data.Seed;

public static class SisonkeSeedData
{
    public static async Task SeedAsync(ApplicationDbContext context, ILogger? logger = null)
    {
        await SeedSubscriptionPlansAsync(context, logger);
        await SeedPilotTenantsAsync(context, logger);
        await SeedQuestionnaireAsync(context, logger);
    }

    private static async Task SeedSubscriptionPlansAsync(ApplicationDbContext context, ILogger? logger)
    {
        await UpsertSubscriptionPlanAsync(context, logger, "Pilot", null, 1, null, 0, 0);
        await UpsertSubscriptionPlanAsync(context, logger, "Basic", "Starter", 1, 30, 149, 1490);
        await UpsertSubscriptionPlanAsync(context, logger, "Standard", "Growth", 31, 50, 279, 2790);
        await UpsertSubscriptionPlanAsync(context, logger, "Premium", null, 51, null, 459, 4590);

        await context.SaveChangesAsync();
    }

    private static async Task UpsertSubscriptionPlanAsync(
        ApplicationDbContext context,
        ILogger? logger,
        string name,
        string? legacyName,
        int minMembers,
        int? maxMembers,
        decimal monthlyPrice,
        decimal annualPrice)
    {
        var matchingPlans = await context.SubscriptionPlans
            .Where(existingPlan =>
                existingPlan.Name == name ||
                (legacyName != null && existingPlan.Name == legacyName))
            .ToListAsync();

        var plan = matchingPlans.FirstOrDefault(existingPlan => existingPlan.Name == name)
            ?? matchingPlans.FirstOrDefault();

        if (plan is null)
        {
            plan = new SubscriptionPlan
            {
                Id = Guid.NewGuid()
            };

            context.SubscriptionPlans.Add(plan);
        }
        else if (matchingPlans.Count > 1)
        {
            logger?.LogWarning(
                "Multiple subscription plans found during seed for {PlanName}. Using first existing record {PlanId}.",
                name,
                plan.Id);
        }

        plan.Name = name;
        plan.MinMembers = minMembers;
        plan.MaxMembers = maxMembers;
        plan.MonthlyPrice = monthlyPrice;
        plan.AnnualPrice = annualPrice;
        plan.IsActive = true;
    }

    private static async Task SeedPilotTenantsAsync(ApplicationDbContext context, ILogger? logger)
    {
        var pilotPlan = await context.SubscriptionPlans
            .Where(plan => plan.Name == "Pilot")
            .OrderBy(plan => plan.Name)
            .ThenBy(plan => plan.Id)
            .FirstAsync();

        var aganangTenant = await GetOrCreateTenantAsync(
            context,
            logger,
            "Aganang Burial Society",
            "aganang-burial-society");

        var letsGrowTenant = await GetOrCreateTenantAsync(
            context,
            logger,
            "Let’s Grow Together",
            "lets-grow-together");

        await GetOrCreateStokvelAsync(
            context,
            logger,
            aganangTenant,
            "Aganang Burial Society",
            StokvelType.BurialSociety);

        await GetOrCreateStokvelAsync(
            context,
            logger,
            letsGrowTenant,
            "Let’s Grow Together",
            StokvelType.SavingsStokvel);

        await GetOrCreateTenantSubscriptionAsync(context, logger, aganangTenant, pilotPlan);
        await GetOrCreateTenantSubscriptionAsync(context, logger, letsGrowTenant, pilotPlan);

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
        ILogger? logger,
        string name,
        string slug)
    {
        var tenants = await context.Tenants
            .Where(existingTenant => existingTenant.Slug == slug)
            .OrderBy(existingTenant => existingTenant.CreatedAt)
            .ThenBy(existingTenant => existingTenant.Id)
            .ToListAsync();

        var tenant = tenants.FirstOrDefault();

        if (tenant is not null)
        {
            if (tenants.Count > 1)
            {
                logger?.LogWarning(
                    "Multiple tenant records found for slug {Slug} during seed. Using first existing record {TenantId}.",
                    slug,
                    tenant.Id);
            }

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
        ILogger? logger,
        Tenant tenant,
        string name,
        StokvelType type)
    {
        var stokvels = await context.Stokvels
            .Where(existingStokvel =>
                existingStokvel.TenantId == tenant.Id &&
                existingStokvel.Name == name)
            .OrderBy(existingStokvel => existingStokvel.CreatedAt)
            .ThenBy(existingStokvel => existingStokvel.Id)
            .ToListAsync();

        var stokvel = stokvels.FirstOrDefault();

        if (stokvel is not null)
        {
            if (stokvels.Count > 1)
            {
                logger?.LogWarning(
                    "Multiple stokvel records found for tenant/name during seed. Using first existing record. TenantId={TenantId}, Name={StokvelName}, StokvelId={StokvelId}",
                    tenant.Id,
                    name,
                    stokvel.Id);
            }

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
        ILogger? logger,
        Tenant tenant,
        SubscriptionPlan pilotPlan)
    {
        var subscriptions = await context.TenantSubscriptions
            .Where(existingSubscription =>
                existingSubscription.TenantId == tenant.Id &&
                existingSubscription.SubscriptionPlanId == pilotPlan.Id)
            .OrderBy(existingSubscription => existingSubscription.StartDate)
            .ThenBy(existingSubscription => existingSubscription.Id)
            .ToListAsync();

        var subscription = subscriptions.FirstOrDefault();

        if (subscription is not null)
        {
            if (subscriptions.Count > 1)
            {
                logger?.LogWarning(
                    "Multiple tenant subscription records found during seed. Using first existing record. TenantId={TenantId}, PlanId={PlanId}, SubscriptionId={SubscriptionId}",
                    tenant.Id,
                    pilotPlan.Id,
                    subscription.Id);
            }

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

    private static async Task SeedQuestionnaireAsync(ApplicationDbContext context, ILogger? logger)
    {
        // Clean up any duplicate active sections from earlier seed runs (keep first per name)
        var allSections = await context.QuestionnaireSections.ToListAsync();
        var hadDuplicates = false;
        foreach (var group in allSections.GroupBy(s => s.Name).Where(g => g.Count() > 1))
        {
            foreach (var dupe in group.OrderBy(s => s.DisplayOrder).ThenBy(s => s.Id).Skip(1).Where(s => s.IsActive))
            {
                dupe.IsActive = false;
                hadDuplicates = true;
            }
        }
        if (hadDuplicates) await context.SaveChangesAsync();

        var stokvelIdentity = await GetOrCreateQuestionnaireSectionAsync(
            context,
            logger,
            "Stokvel Identity",
            "Basic information about the stokvel.",
            1);

        var membershipRules = await GetOrCreateQuestionnaireSectionAsync(
            context,
            logger,
            "Membership Rules",
            "Rules that define who can join and how members are managed.",
            2);

        var contributionsAndFines = await GetOrCreateQuestionnaireSectionAsync(
            context,
            logger,
            "Contributions and Fines",
            "Rules for payments, due dates, arrears and fines.",
            3);

        var meetingsAndDecisions = await GetOrCreateQuestionnaireSectionAsync(
            context,
            logger,
            "Meetings and Decisions",
            "Rules for meetings, attendance and decision-making.",
            4);

        var claimsAndPayouts = await GetOrCreateQuestionnaireSectionAsync(
            context,
            logger,
            "Claims and Payouts",
            "Rules for burial claims, savings payouts and approvals.",
            5);

        await GetOrCreateQuestionnaireQuestionAsync(
            context,
            logger,
            stokvelIdentity,
            "What is the main purpose of this stokvel?",
            QuestionType.Text,
            true,
            1);

        var stokvelTypeQuestion = await GetOrCreateQuestionnaireQuestionAsync(
            context,
            logger,
            stokvelIdentity,
            "Which type of stokvel is this?",
            QuestionType.SingleSelect,
            true,
            2);
        await SeedQuestionnaireOptionsAsync(
            context,
            stokvelTypeQuestion,
            ["Burial Society", "Savings Stokvel", "Grocery Stokvel", "Investment Stokvel", "Social Club", "Family Society"]);

        var existingConstitutionQuestion = await GetOrCreateQuestionnaireQuestionAsync(
            context,
            logger,
            stokvelIdentity,
            "Does this stokvel already have a constitution?",
            QuestionType.SingleSelect,
            true,
            3);
        await SeedQuestionnaireOptionsAsync(
            context,
            existingConstitutionQuestion,
            ["Yes", "No", "Not sure"]);

        await GetOrCreateQuestionnaireQuestionAsync(
            context,
            logger,
            membershipRules,
            "Does this stokvel have a joining fee?",
            QuestionType.YesNo,
            true,
            1);

        await GetOrCreateQuestionnaireQuestionAsync(
            context,
            logger,
            membershipRules,
            "What is the joining fee amount?",
            QuestionType.Currency,
            false,
            2);

        await GetOrCreateQuestionnaireQuestionAsync(
            context,
            logger,
            membershipRules,
            "Does this stokvel have a cooling period?",
            QuestionType.YesNo,
            true,
            3);

        await GetOrCreateQuestionnaireQuestionAsync(
            context,
            logger,
            membershipRules,
            "How many months is the cooling period?",
            QuestionType.Number,
            false,
            4);

        await GetOrCreateQuestionnaireQuestionAsync(
            context,
            logger,
            membershipRules,
            "What is the maximum number of next of kin records allowed per member?",
            QuestionType.Number,
            true,
            5);

        await GetOrCreateQuestionnaireQuestionAsync(
            context,
            logger,
            membershipRules,
            "What is the maximum number of beneficiaries allowed per member?",
            QuestionType.Number,
            true,
            6);

        await GetOrCreateQuestionnaireQuestionAsync(
            context,
            logger,
            membershipRules,
            "What is the maximum number of dependents allowed per member?",
            QuestionType.Number,
            true,
            7,
            "This applies mainly to burial societies where members may register covered dependents.");

        await GetOrCreateQuestionnaireQuestionAsync(
            context,
            logger,
            contributionsAndFines,
            "How much must each member contribute?",
            QuestionType.Currency,
            true,
            1);

        var contributionFrequencyQuestion = await GetOrCreateQuestionnaireQuestionAsync(
            context,
            logger,
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
            logger,
            contributionsAndFines,
            "What day of the month are contributions due?",
            QuestionType.Number,
            true,
            3);

        await GetOrCreateQuestionnaireQuestionAsync(
            context,
            logger,
            contributionsAndFines,
            "Are partial payments allowed?",
            QuestionType.YesNo,
            true,
            4);

        await GetOrCreateQuestionnaireQuestionAsync(
            context,
            logger,
            contributionsAndFines,
            "Is there a fine for late payment?",
            QuestionType.YesNo,
            true,
            5);

        await GetOrCreateQuestionnaireQuestionAsync(
            context,
            logger,
            contributionsAndFines,
            "What is the late payment fine amount?",
            QuestionType.Currency,
            false,
            6);

        var meetingFrequencyQuestion = await GetOrCreateQuestionnaireQuestionAsync(
            context,
            logger,
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
            logger,
            meetingsAndDecisions,
            "Is meeting attendance compulsory?",
            QuestionType.YesNo,
            true,
            2);

        await GetOrCreateQuestionnaireQuestionAsync(
            context,
            logger,
            meetingsAndDecisions,
            "What is the fine for late coming to meetings?",
            QuestionType.Currency,
            false,
            3);

        var decisionApprovalQuestion = await GetOrCreateQuestionnaireQuestionAsync(
            context,
            logger,
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
            logger,
            claimsAndPayouts,
            "Are claims or payouts allowed?",
            QuestionType.YesNo,
            true,
            1);

        var claimApprovalQuestion = await GetOrCreateQuestionnaireQuestionAsync(
            context,
            logger,
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
            logger,
            claimsAndPayouts,
            "What is the waiting period in months before a new member or dependent can claim?",
            QuestionType.Number,
            true,
            3,
            "For burial societies, this is commonly used before new members or covered lives become eligible for funeral benefits.");

        await GetOrCreateQuestionnaireQuestionAsync(
            context,
            logger,
            claimsAndPayouts,
            "What documents are required for a claim or payout?",
            QuestionType.Text,
            false,
            4);

        await context.SaveChangesAsync();
    }

    private static async Task<QuestionnaireSection> GetOrCreateQuestionnaireSectionAsync(
        ApplicationDbContext context,
        ILogger? logger,
        string name,
        string description,
        int displayOrder)
    {
        var sections = await context.QuestionnaireSections
            .Where(existingSection => existingSection.Name == name)
            .OrderBy(existingSection => existingSection.DisplayOrder)
            .ThenBy(existingSection => existingSection.Id)
            .ToListAsync();

        var section = sections.FirstOrDefault();

        if (section is not null)
        {
            if (sections.Count > 1)
            {
                logger?.LogWarning(
                    "Multiple questionnaire sections found during seed for {SectionName}. Using first existing record {SectionId}.",
                    name,
                    section.Id);
            }

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
        ILogger? logger,
        QuestionnaireSection section,
        string questionText,
        QuestionType questionType,
        bool isRequired,
        int displayOrder,
        string? helpText = null)
    {
        var questions = await context.QuestionnaireQuestions
            .Where(existingQuestion =>
                existingQuestion.QuestionnaireSectionId == section.Id &&
                existingQuestion.QuestionText == questionText)
            .OrderBy(existingQuestion => existingQuestion.DisplayOrder)
            .ThenBy(existingQuestion => existingQuestion.Id)
            .ToListAsync();

        var question = questions.FirstOrDefault();

        if (question is not null)
        {
            if (questions.Count > 1)
            {
                logger?.LogWarning(
                    "Multiple questionnaire questions found during seed for section/question. Using first existing record. SectionId={SectionId}, QuestionText={QuestionText}, QuestionId={QuestionId}",
                    section.Id,
                    questionText,
                    question.Id);
            }

            return question;
        }

        question = new QuestionnaireQuestion
        {
            Id = Guid.NewGuid(),
            QuestionnaireSectionId = section.Id,
            QuestionnaireSection = section,
            QuestionText = questionText,
            HelpText = helpText,
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
