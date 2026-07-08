IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] nvarchar(450) NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] nvarchar(450) NOT NULL,
        [FullName] nvarchar(150) NULL,
        [IdNumber] nvarchar(30) NULL,
        [CellphoneNumber] nvarchar(30) NULL,
        [ResidentialArea] nvarchar(250) NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(256) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE TABLE [QuestionnaireSections] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(150) NOT NULL,
        [Description] nvarchar(500) NULL,
        [DisplayOrder] int NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_QuestionnaireSections] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE TABLE [SubscriptionPlans] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [MinMembers] int NOT NULL,
        [MaxMembers] int NULL,
        [MonthlyPrice] decimal(18,2) NOT NULL,
        [AnnualPrice] decimal(18,2) NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_SubscriptionPlans] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE TABLE [Tenants] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(150) NOT NULL,
        [Slug] nvarchar(150) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_Tenants] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(128) NOT NULL,
        [ProviderKey] nvarchar(128) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] nvarchar(450) NOT NULL,
        [RoleId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] nvarchar(450) NOT NULL,
        [LoginProvider] nvarchar(128) NOT NULL,
        [Name] nvarchar(128) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE TABLE [QuestionnaireQuestions] (
        [Id] uniqueidentifier NOT NULL,
        [QuestionnaireSectionId] uniqueidentifier NOT NULL,
        [StokvelType] int NULL,
        [QuestionText] nvarchar(500) NOT NULL,
        [HelpText] nvarchar(500) NULL,
        [QuestionType] int NOT NULL,
        [IsRequired] bit NOT NULL,
        [DisplayOrder] int NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_QuestionnaireQuestions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_QuestionnaireQuestions_QuestionnaireSections_QuestionnaireSectionId] FOREIGN KEY ([QuestionnaireSectionId]) REFERENCES [QuestionnaireSections] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE TABLE [ConstitutionDocuments] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [Content] nvarchar(max) NOT NULL,
        [IsUploadedDocument] bit NOT NULL,
        [OriginalFileName] nvarchar(300) NULL,
        [StoredFilePath] nvarchar(500) NULL,
        [ContentType] nvarchar(100) NULL,
        [FileSizeBytes] bigint NULL,
        [VersionNumber] int NOT NULL,
        [IsApproved] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ApprovedAt] datetime2 NULL,
        CONSTRAINT [PK_ConstitutionDocuments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ConstitutionDocuments_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE TABLE [ConstitutionWizardAnswers] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [QuestionKey] nvarchar(150) NOT NULL,
        [QuestionText] nvarchar(250) NOT NULL,
        [StepNumber] int NOT NULL,
        [AnswerValue] nvarchar(4000) NOT NULL,
        [AnsweredAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ConstitutionWizardAnswers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ConstitutionWizardAnswers_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE TABLE [ContributionCycles] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [PeriodStart] datetime2 NOT NULL,
        [PeriodEnd] datetime2 NOT NULL,
        [DueDate] datetime2 NOT NULL,
        [Status] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ContributionCycles] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ContributionCycles_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE TABLE [ContributionRules] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [Frequency] int NOT NULL,
        [DueDayOfMonth] int NOT NULL,
        [AllowPartialPayments] bit NOT NULL,
        [LatePaymentFineAmount] decimal(18,2) NOT NULL,
        [GracePeriodDays] int NOT NULL,
        [IsActive] bit NOT NULL,
        [EffectiveFrom] datetime2 NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ContributionRules] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ContributionRules_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE TABLE [FineTypes] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(250) NULL,
        [DefaultAmount] decimal(18,2) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_FineTypes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FineTypes_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE TABLE [Meetings] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [MeetingDate] datetime2 NOT NULL,
        [Venue] nvarchar(150) NULL,
        [Status] int NOT NULL,
        [Purpose] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Meetings] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Meetings_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE TABLE [Members] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [ApplicationUserId] nvarchar(450) NULL,
        [MemberNumber] nvarchar(50) NOT NULL,
        [FullName] nvarchar(150) NOT NULL,
        [CellphoneNumber] nvarchar(30) NOT NULL,
        [EmailAddress] nvarchar(150) NULL,
        [IdNumber] nvarchar(30) NULL,
        [JoiningDate] datetime2 NOT NULL,
        [Status] int NOT NULL,
        [GovernanceStatus] int NOT NULL,
        [GovernanceStatusChangedAt] datetime2 NULL,
        [GovernanceStatusReason] nvarchar(500) NULL,
        [LastWarningIssuedAt] datetime2 NULL,
        [SuspendedAt] datetime2 NULL,
        [ExpelledAt] datetime2 NULL,
        [DefaultRole] int NOT NULL,
        [ResidentialArea] nvarchar(150) NULL,
        [IsInCoolingPeriod] bit NOT NULL,
        [CoolingPeriodEndDate] datetime2 NULL,
        [IsDeceased] bit NOT NULL,
        [DeceasedDate] datetime2 NULL,
        [DeathReportedAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Members] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Members_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE TABLE [Stokvels] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [Name] nvarchar(150) NOT NULL,
        [Code] nvarchar(20) NULL,
        [Type] int NOT NULL,
        [Province] nvarchar(100) NULL,
        [TownOrArea] nvarchar(100) NULL,
        [EstablishedDate] datetime2 NULL,
        [ExpectedMemberCount] int NULL,
        [Description] nvarchar(500) NULL,
        [IsActive] bit NOT NULL,
        [IsSetupComplete] bit NOT NULL,
        [SetupCompletedAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Stokvels] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Stokvels_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE TABLE [TenantSubscriptions] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [SubscriptionPlanId] uniqueidentifier NOT NULL,
        [Status] int NOT NULL,
        [StartDate] datetime2 NOT NULL,
        [EndDate] datetime2 NULL,
        [NextBillingDate] datetime2 NULL,
        [IsTrial] bit NOT NULL,
        CONSTRAINT [PK_TenantSubscriptions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TenantSubscriptions_SubscriptionPlans_SubscriptionPlanId] FOREIGN KEY ([SubscriptionPlanId]) REFERENCES [SubscriptionPlans] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_TenantSubscriptions_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE TABLE [QuestionnaireOptions] (
        [Id] uniqueidentifier NOT NULL,
        [QuestionnaireQuestionId] uniqueidentifier NOT NULL,
        [OptionText] nvarchar(150) NOT NULL,
        [OptionValue] nvarchar(150) NULL,
        [DisplayOrder] int NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_QuestionnaireOptions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_QuestionnaireOptions_QuestionnaireQuestions_QuestionnaireQuestionId] FOREIGN KEY ([QuestionnaireQuestionId]) REFERENCES [QuestionnaireQuestions] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE TABLE [StokvelQuestionnaireAnswers] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [QuestionnaireQuestionId] uniqueidentifier NOT NULL,
        [AnswerValue] nvarchar(2000) NOT NULL,
        [AnsweredAt] datetime2 NOT NULL,
        CONSTRAINT [PK_StokvelQuestionnaireAnswers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_StokvelQuestionnaireAnswers_QuestionnaireQuestions_QuestionnaireQuestionId] FOREIGN KEY ([QuestionnaireQuestionId]) REFERENCES [QuestionnaireQuestions] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_StokvelQuestionnaireAnswers_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE TABLE [MeetingAgendaItems] (
        [Id] uniqueidentifier NOT NULL,
        [MeetingId] uniqueidentifier NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [Description] nvarchar(1000) NULL,
        [DisplayOrder] int NOT NULL,
        [IsCompleted] bit NOT NULL,
        [Notes] nvarchar(1000) NULL,
        CONSTRAINT [PK_MeetingAgendaItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MeetingAgendaItems_Meetings_MeetingId] FOREIGN KEY ([MeetingId]) REFERENCES [Meetings] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE TABLE [MeetingVotes] (
        [Id] uniqueidentifier NOT NULL,
        [MeetingId] uniqueidentifier NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [Description] nvarchar(1000) NULL,
        [VotingMethod] int NOT NULL,
        [Status] int NOT NULL,
        [Result] int NOT NULL,
        [OpenedAt] datetime2 NOT NULL,
        [ClosedAt] datetime2 NULL,
        CONSTRAINT [PK_MeetingVotes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MeetingVotes_Meetings_MeetingId] FOREIGN KEY ([MeetingId]) REFERENCES [Meetings] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE TABLE [Beneficiaries] (
        [Id] uniqueidentifier NOT NULL,
        [MemberId] uniqueidentifier NOT NULL,
        [FullName] nvarchar(150) NOT NULL,
        [Relationship] nvarchar(80) NOT NULL,
        [IdNumber] nvarchar(30) NULL,
        [CellphoneNumber] nvarchar(30) NULL,
        [DateOfBirth] datetime2 NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Beneficiaries] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Beneficiaries_Members_MemberId] FOREIGN KEY ([MemberId]) REFERENCES [Members] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE TABLE [MeetingApologies] (
        [Id] uniqueidentifier NOT NULL,
        [MeetingId] uniqueidentifier NOT NULL,
        [MemberId] uniqueidentifier NOT NULL,
        [ApologyType] nvarchar(100) NOT NULL,
        [Reason] nvarchar(1000) NOT NULL,
        [SubmittedAt] datetime2 NOT NULL,
        [Status] nvarchar(30) NOT NULL,
        [ResponseNote] nvarchar(1000) NULL,
        [ReviewedAt] datetime2 NULL,
        [ReviewedByMemberId] uniqueidentifier NULL,
        CONSTRAINT [PK_MeetingApologies] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MeetingApologies_Meetings_MeetingId] FOREIGN KEY ([MeetingId]) REFERENCES [Meetings] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_MeetingApologies_Members_MemberId] FOREIGN KEY ([MemberId]) REFERENCES [Members] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE TABLE [MeetingAttendances] (
        [Id] uniqueidentifier NOT NULL,
        [MeetingId] uniqueidentifier NOT NULL,
        [MemberId] uniqueidentifier NOT NULL,
        [Status] int NOT NULL,
        [IsLate] bit NOT NULL,
        [LeftEarly] bit NOT NULL,
        [Notes] nvarchar(500) NULL,
        [MarkedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_MeetingAttendances] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MeetingAttendances_Meetings_MeetingId] FOREIGN KEY ([MeetingId]) REFERENCES [Meetings] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_MeetingAttendances_Members_MemberId] FOREIGN KEY ([MemberId]) REFERENCES [Members] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE TABLE [MemberContributions] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [ContributionCycleId] uniqueidentifier NOT NULL,
        [MemberId] uniqueidentifier NOT NULL,
        [ExpectedAmount] decimal(18,2) NOT NULL,
        [PaidAmount] decimal(18,2) NOT NULL,
        [OutstandingAmount] decimal(18,2) NOT NULL,
        [Status] int NOT NULL,
        [FullyPaidDate] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_MemberContributions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MemberContributions_ContributionCycles_ContributionCycleId] FOREIGN KEY ([ContributionCycleId]) REFERENCES [ContributionCycles] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_MemberContributions_Members_MemberId] FOREIGN KEY ([MemberId]) REFERENCES [Members] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_MemberContributions_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE TABLE [MemberDependents] (
        [Id] uniqueidentifier NOT NULL,
        [MemberId] uniqueidentifier NOT NULL,
        [FullName] nvarchar(150) NOT NULL,
        [Relationship] nvarchar(50) NOT NULL,
        [DateOfBirth] datetime2 NULL,
        [IdNumber] nvarchar(30) NULL,
        [CellphoneNumber] nvarchar(30) NULL,
        [IsActive] bit NOT NULL,
        [IsDeceased] bit NOT NULL,
        [DeceasedDate] datetime2 NULL,
        [DeathReportedAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_MemberDependents] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MemberDependents_Members_MemberId] FOREIGN KEY ([MemberId]) REFERENCES [Members] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE TABLE [MemberFines] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [MemberId] uniqueidentifier NOT NULL,
        [FineTypeId] uniqueidentifier NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [Reason] nvarchar(250) NOT NULL,
        [FineDate] datetime2 NOT NULL,
        [Status] int NOT NULL,
        [PaidDate] datetime2 NULL,
        [CapturedByUserId] nvarchar(100) NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_MemberFines] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MemberFines_FineTypes_FineTypeId] FOREIGN KEY ([FineTypeId]) REFERENCES [FineTypes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_MemberFines_Members_MemberId] FOREIGN KEY ([MemberId]) REFERENCES [Members] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_MemberFines_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE TABLE [NextOfKinRecords] (
        [Id] uniqueidentifier NOT NULL,
        [MemberId] uniqueidentifier NOT NULL,
        [FullName] nvarchar(150) NOT NULL,
        [Relationship] nvarchar(80) NOT NULL,
        [CellphoneNumber] nvarchar(30) NOT NULL,
        [Address] nvarchar(250) NULL,
        [IsPrimary] bit NOT NULL,
        CONSTRAINT [PK_NextOfKinRecords] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_NextOfKinRecords_Members_MemberId] FOREIGN KEY ([MemberId]) REFERENCES [Members] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE TABLE [MeetingMinutes] (
        [Id] uniqueidentifier NOT NULL,
        [MeetingId] uniqueidentifier NOT NULL,
        [StokvelId] uniqueidentifier NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [OpeningNotes] nvarchar(max) NOT NULL,
        [AttendanceSummary] nvarchar(max) NOT NULL,
        [ApologySummary] nvarchar(max) NOT NULL,
        [MattersArising] nvarchar(max) NOT NULL,
        [DecisionsTaken] nvarchar(max) NOT NULL,
        [ActionItems] nvarchar(max) NOT NULL,
        [ClosingNotes] nvarchar(max) NOT NULL,
        [Status] nvarchar(30) NOT NULL,
        [CreatedByMemberId] uniqueidentifier NULL,
        [UpdatedByMemberId] uniqueidentifier NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [ApprovedAt] datetime2 NULL,
        [ApprovedByMemberId] uniqueidentifier NULL,
        CONSTRAINT [PK_MeetingMinutes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MeetingMinutes_Meetings_MeetingId] FOREIGN KEY ([MeetingId]) REFERENCES [Meetings] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_MeetingMinutes_Stokvels_StokvelId] FOREIGN KEY ([StokvelId]) REFERENCES [Stokvels] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE TABLE [MemberWarnings] (
        [Id] uniqueidentifier NOT NULL,
        [MemberId] uniqueidentifier NOT NULL,
        [StokvelId] uniqueidentifier NOT NULL,
        [MeetingId] uniqueidentifier NULL,
        [WarningType] nvarchar(450) NOT NULL,
        [Reason] nvarchar(max) NOT NULL,
        [AbsenceCount] int NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [AcknowledgedAt] datetime2 NULL,
        [CreatedByMemberId] uniqueidentifier NULL,
        [Notes] nvarchar(max) NULL,
        CONSTRAINT [PK_MemberWarnings] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MemberWarnings_Meetings_MeetingId] FOREIGN KEY ([MeetingId]) REFERENCES [Meetings] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_MemberWarnings_Members_MemberId] FOREIGN KEY ([MemberId]) REFERENCES [Members] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_MemberWarnings_Stokvels_StokvelId] FOREIGN KEY ([StokvelId]) REFERENCES [Stokvels] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE TABLE [StokvelOperatingRules] (
        [Id] uniqueidentifier NOT NULL,
        [StokvelId] uniqueidentifier NOT NULL,
        [StokvelType] nvarchar(100) NOT NULL,
        [MonthlyContributionAmount] decimal(18,2) NOT NULL,
        [ContributionDueDay] int NOT NULL,
        [GracePeriodDays] int NOT NULL,
        [AllowPartialPayments] bit NOT NULL,
        [ChargeLatePaymentFine] bit NOT NULL,
        [LatePaymentFineAmount] decimal(18,2) NOT NULL,
        [EnableDependents] bit NOT NULL,
        [MaximumDependents] int NOT NULL,
        [MemberWaitingPeriodMonths] int NOT NULL,
        [DependentWaitingPeriodMonths] int NOT NULL,
        [RequireDependentIdNumber] bit NOT NULL,
        [EnableClaims] bit NOT NULL,
        [RequireDeathCertificateForClaims] bit NOT NULL,
        [RequireClaimDocuments] bit NOT NULL,
        [BlockClaimsIfMemberInArrears] bit NOT NULL,
        [BlockClaimsIfMemberSuspended] bit NOT NULL,
        [DefaultClaimPayoutAmount] decimal(18,2) NOT NULL,
        [EnableAttendanceTracking] bit NOT NULL,
        [AbsenceReminderThreshold] int NOT NULL,
        [FormalWarningThreshold] int NOT NULL,
        [ExecutiveReviewThreshold] int NOT NULL,
        [ApologyDeadlineHoursBeforeMeeting] int NOT NULL,
        [ChargeLateApologyFine] bit NOT NULL,
        [LateApologyFineAmount] decimal(18,2) NOT NULL,
        [ChargeAbsenceWithoutApologyFine] bit NOT NULL,
        [AbsenceWithoutApologyFineAmount] decimal(18,2) NOT NULL,
        [EnableMeetings] bit NOT NULL,
        [RequireMinutesApproval] bit NOT NULL,
        [QuorumPercentage] decimal(18,2) NOT NULL,
        [EnableVoting] bit NOT NULL,
        [DefaultVotingApprovalThreshold] decimal(18,2) NOT NULL,
        [AllowAnonymousVoting] bit NOT NULL,
        [EnableRotationalPayouts] bit NOT NULL,
        [PayoutFrequency] nvarchar(100) NULL,
        [RequireTreasurerConfirmationForPayouts] bit NOT NULL,
        [EnableGroceryModule] bit NOT NULL,
        [EnableInvestmentModule] bit NOT NULL,
        [EnablePropertyModule] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [CreatedByMemberId] uniqueidentifier NULL,
        [UpdatedByMemberId] uniqueidentifier NULL,
        CONSTRAINT [PK_StokvelOperatingRules] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_StokvelOperatingRules_Stokvels_StokvelId] FOREIGN KEY ([StokvelId]) REFERENCES [Stokvels] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE TABLE [VoteMotions] (
        [Id] uniqueidentifier NOT NULL,
        [StokvelId] uniqueidentifier NOT NULL,
        [MeetingId] uniqueidentifier NULL,
        [AgendaItemId] uniqueidentifier NULL,
        [Title] nvarchar(200) NOT NULL,
        [Description] nvarchar(1000) NOT NULL,
        [VoteType] nvarchar(50) NOT NULL,
        [Status] nvarchar(50) NOT NULL,
        [IsAnonymous] bit NOT NULL,
        [OpensAt] datetime2 NOT NULL,
        [ClosesAt] datetime2 NULL,
        [CreatedByMemberId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ClosedAt] datetime2 NULL,
        [ClosedByMemberId] uniqueidentifier NULL,
        [ResultSummary] nvarchar(1000) NULL,
        [DecisionOutcome] nvarchar(100) NULL,
        CONSTRAINT [PK_VoteMotions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_VoteMotions_MeetingAgendaItems_AgendaItemId] FOREIGN KEY ([AgendaItemId]) REFERENCES [MeetingAgendaItems] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_VoteMotions_Meetings_MeetingId] FOREIGN KEY ([MeetingId]) REFERENCES [Meetings] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_VoteMotions_Stokvels_StokvelId] FOREIGN KEY ([StokvelId]) REFERENCES [Stokvels] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE TABLE [MeetingVoteResponses] (
        [Id] uniqueidentifier NOT NULL,
        [MeetingVoteId] uniqueidentifier NOT NULL,
        [MemberId] uniqueidentifier NOT NULL,
        [Choice] int NOT NULL,
        [VotedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_MeetingVoteResponses] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MeetingVoteResponses_MeetingVotes_MeetingVoteId] FOREIGN KEY ([MeetingVoteId]) REFERENCES [MeetingVotes] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_MeetingVoteResponses_Members_MemberId] FOREIGN KEY ([MemberId]) REFERENCES [Members] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE TABLE [ContributionPaymentAudits] (
        [Id] uniqueidentifier NOT NULL,
        [ContributionPaymentId] uniqueidentifier NOT NULL,
        [MemberId] uniqueidentifier NOT NULL,
        [StokvelId] uniqueidentifier NOT NULL,
        [Action] nvarchar(max) NOT NULL,
        [PreviousAmountPaid] decimal(18,2) NULL,
        [NewAmountPaid] decimal(18,2) NULL,
        [PreviousStatus] nvarchar(max) NULL,
        [NewStatus] nvarchar(max) NULL,
        [PaymentReference] nvarchar(max) NULL,
        [Notes] nvarchar(max) NULL,
        [CapturedByMemberId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ContributionPaymentAudits] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ContributionPaymentAudits_MemberContributions_ContributionPaymentId] FOREIGN KEY ([ContributionPaymentId]) REFERENCES [MemberContributions] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ContributionPaymentAudits_Members_CapturedByMemberId] FOREIGN KEY ([CapturedByMemberId]) REFERENCES [Members] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ContributionPaymentAudits_Members_MemberId] FOREIGN KEY ([MemberId]) REFERENCES [Members] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ContributionPaymentAudits_Stokvels_StokvelId] FOREIGN KEY ([StokvelId]) REFERENCES [Stokvels] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE TABLE [Payments] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [MemberContributionId] uniqueidentifier NOT NULL,
        [MemberId] uniqueidentifier NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [PaymentDate] datetime2 NOT NULL,
        [Reference] nvarchar(100) NULL,
        [PaymentMethod] int NOT NULL,
        [CapturedByUserId] nvarchar(100) NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Payments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Payments_MemberContributions_MemberContributionId] FOREIGN KEY ([MemberContributionId]) REFERENCES [MemberContributions] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Payments_Members_MemberId] FOREIGN KEY ([MemberId]) REFERENCES [Members] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Payments_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE TABLE [FuneralClaims] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [MemberId] uniqueidentifier NOT NULL,
        [SubjectType] int NOT NULL,
        [DependentId] uniqueidentifier NULL,
        [DeceasedFullName] nvarchar(150) NOT NULL,
        [DateOfDeath] datetime2 NULL,
        [Status] int NOT NULL,
        [ClaimReference] nvarchar(50) NULL,
        [ClaimReason] nvarchar(1000) NULL,
        [ReviewNotes] nvarchar(1000) NULL,
        [IsWaitingPeriodSatisfied] bit NOT NULL,
        [IsMemberStatusEligible] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [SubmittedAt] datetime2 NULL,
        [SubmittedByName] nvarchar(150) NULL,
        [SecretaryReviewedAt] datetime2 NULL,
        [SecretaryReviewedByName] nvarchar(150) NULL,
        [SecretaryRecommendedApproval] bit NULL,
        [SecretaryReviewNotes] nvarchar(1000) NULL,
        [ChairpersonDecisionAt] datetime2 NULL,
        [ChairpersonDecisionByName] nvarchar(150) NULL,
        [ChairpersonDecisionNotes] nvarchar(1000) NULL,
        [ApprovedAt] datetime2 NULL,
        [RejectedAt] datetime2 NULL,
        [PayoutAmount] decimal(18,2) NULL,
        [PayoutPaidAt] datetime2 NULL,
        [PayoutReference] nvarchar(100) NULL,
        [PayoutNotes] nvarchar(1000) NULL,
        [PayoutCapturedByMemberId] uniqueidentifier NULL,
        CONSTRAINT [PK_FuneralClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FuneralClaims_MemberDependents_DependentId] FOREIGN KEY ([DependentId]) REFERENCES [MemberDependents] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_FuneralClaims_Members_MemberId] FOREIGN KEY ([MemberId]) REFERENCES [Members] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_FuneralClaims_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE TABLE [VoteOptions] (
        [Id] uniqueidentifier NOT NULL,
        [VoteMotionId] uniqueidentifier NOT NULL,
        [OptionText] nvarchar(150) NOT NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_VoteOptions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_VoteOptions_VoteMotions_VoteMotionId] FOREIGN KEY ([VoteMotionId]) REFERENCES [VoteMotions] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE TABLE [ClaimPayoutAudits] (
        [Id] uniqueidentifier NOT NULL,
        [FuneralClaimId] uniqueidentifier NOT NULL,
        [MemberId] uniqueidentifier NOT NULL,
        [StokvelId] uniqueidentifier NOT NULL,
        [Action] nvarchar(max) NOT NULL,
        [PreviousPayoutAmount] decimal(18,2) NULL,
        [NewPayoutAmount] decimal(18,2) NULL,
        [PreviousStatus] nvarchar(max) NULL,
        [NewStatus] nvarchar(max) NULL,
        [PayoutReference] nvarchar(max) NULL,
        [Notes] nvarchar(max) NULL,
        [CapturedByMemberId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ClaimPayoutAudits] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ClaimPayoutAudits_FuneralClaims_FuneralClaimId] FOREIGN KEY ([FuneralClaimId]) REFERENCES [FuneralClaims] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ClaimPayoutAudits_Members_CapturedByMemberId] FOREIGN KEY ([CapturedByMemberId]) REFERENCES [Members] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ClaimPayoutAudits_Members_MemberId] FOREIGN KEY ([MemberId]) REFERENCES [Members] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ClaimPayoutAudits_Stokvels_StokvelId] FOREIGN KEY ([StokvelId]) REFERENCES [Stokvels] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE TABLE [FuneralClaimDocuments] (
        [Id] uniqueidentifier NOT NULL,
        [FuneralClaimId] uniqueidentifier NOT NULL,
        [DocumentType] int NOT NULL,
        [OriginalFileName] nvarchar(300) NOT NULL,
        [StoredFilePath] nvarchar(500) NOT NULL,
        [ContentType] nvarchar(100) NULL,
        [FileSizeBytes] bigint NOT NULL,
        [UploadedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_FuneralClaimDocuments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FuneralClaimDocuments_FuneralClaims_FuneralClaimId] FOREIGN KEY ([FuneralClaimId]) REFERENCES [FuneralClaims] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE TABLE [MemberVotes] (
        [Id] uniqueidentifier NOT NULL,
        [VoteMotionId] uniqueidentifier NOT NULL,
        [MemberId] uniqueidentifier NOT NULL,
        [VoteOptionId] uniqueidentifier NOT NULL,
        [VotedAt] datetime2 NOT NULL,
        [Notes] nvarchar(1000) NULL,
        CONSTRAINT [PK_MemberVotes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MemberVotes_Members_MemberId] FOREIGN KEY ([MemberId]) REFERENCES [Members] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_MemberVotes_VoteMotions_VoteMotionId] FOREIGN KEY ([VoteMotionId]) REFERENCES [VoteMotions] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_MemberVotes_VoteOptions_VoteOptionId] FOREIGN KEY ([VoteOptionId]) REFERENCES [VoteOptions] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE INDEX [IX_Beneficiaries_MemberId] ON [Beneficiaries] ([MemberId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE INDEX [IX_ClaimPayoutAudits_CapturedByMemberId] ON [ClaimPayoutAudits] ([CapturedByMemberId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE INDEX [IX_ClaimPayoutAudits_FuneralClaimId] ON [ClaimPayoutAudits] ([FuneralClaimId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE INDEX [IX_ClaimPayoutAudits_MemberId] ON [ClaimPayoutAudits] ([MemberId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE INDEX [IX_ClaimPayoutAudits_StokvelId] ON [ClaimPayoutAudits] ([StokvelId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE INDEX [IX_ConstitutionDocuments_TenantId] ON [ConstitutionDocuments] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ConstitutionWizardAnswers_TenantId_QuestionKey] ON [ConstitutionWizardAnswers] ([TenantId], [QuestionKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE INDEX [IX_ContributionCycles_TenantId] ON [ContributionCycles] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE INDEX [IX_ContributionPaymentAudits_CapturedByMemberId] ON [ContributionPaymentAudits] ([CapturedByMemberId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE INDEX [IX_ContributionPaymentAudits_ContributionPaymentId] ON [ContributionPaymentAudits] ([ContributionPaymentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE INDEX [IX_ContributionPaymentAudits_MemberId] ON [ContributionPaymentAudits] ([MemberId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE INDEX [IX_ContributionPaymentAudits_StokvelId] ON [ContributionPaymentAudits] ([StokvelId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE INDEX [IX_ContributionRules_TenantId] ON [ContributionRules] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE INDEX [IX_FineTypes_TenantId] ON [FineTypes] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE INDEX [IX_FuneralClaimDocuments_FuneralClaimId] ON [FuneralClaimDocuments] ([FuneralClaimId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE INDEX [IX_FuneralClaims_ClaimReference] ON [FuneralClaims] ([ClaimReference]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE INDEX [IX_FuneralClaims_DependentId] ON [FuneralClaims] ([DependentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE INDEX [IX_FuneralClaims_MemberId] ON [FuneralClaims] ([MemberId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE INDEX [IX_FuneralClaims_TenantId] ON [FuneralClaims] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE INDEX [IX_MeetingAgendaItems_MeetingId] ON [MeetingAgendaItems] ([MeetingId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE UNIQUE INDEX [IX_MeetingApologies_MeetingId_MemberId] ON [MeetingApologies] ([MeetingId], [MemberId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE INDEX [IX_MeetingApologies_MemberId] ON [MeetingApologies] ([MemberId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE UNIQUE INDEX [IX_MeetingAttendances_MeetingId_MemberId] ON [MeetingAttendances] ([MeetingId], [MemberId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE INDEX [IX_MeetingAttendances_MemberId] ON [MeetingAttendances] ([MemberId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE UNIQUE INDEX [IX_MeetingMinutes_MeetingId] ON [MeetingMinutes] ([MeetingId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE INDEX [IX_MeetingMinutes_StokvelId] ON [MeetingMinutes] ([StokvelId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE INDEX [IX_Meetings_TenantId] ON [Meetings] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE UNIQUE INDEX [IX_MeetingVoteResponses_MeetingVoteId_MemberId] ON [MeetingVoteResponses] ([MeetingVoteId], [MemberId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE INDEX [IX_MeetingVoteResponses_MemberId] ON [MeetingVoteResponses] ([MemberId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE INDEX [IX_MeetingVotes_MeetingId] ON [MeetingVotes] ([MeetingId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE INDEX [IX_MemberContributions_ContributionCycleId] ON [MemberContributions] ([ContributionCycleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE INDEX [IX_MemberContributions_MemberId] ON [MemberContributions] ([MemberId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE INDEX [IX_MemberContributions_TenantId] ON [MemberContributions] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE INDEX [IX_MemberDependents_MemberId] ON [MemberDependents] ([MemberId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE INDEX [IX_MemberFines_FineTypeId] ON [MemberFines] ([FineTypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE INDEX [IX_MemberFines_MemberId] ON [MemberFines] ([MemberId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE INDEX [IX_MemberFines_TenantId] ON [MemberFines] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE INDEX [IX_Members_ApplicationUserId] ON [Members] ([ApplicationUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE INDEX [IX_Members_IdNumber] ON [Members] ([IdNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE INDEX [IX_Members_TenantId] ON [Members] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE INDEX [IX_MemberVotes_MemberId] ON [MemberVotes] ([MemberId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE UNIQUE INDEX [IX_MemberVotes_VoteMotionId_MemberId] ON [MemberVotes] ([VoteMotionId], [MemberId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE INDEX [IX_MemberVotes_VoteOptionId] ON [MemberVotes] ([VoteOptionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE INDEX [IX_MemberWarnings_MeetingId] ON [MemberWarnings] ([MeetingId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_MemberWarnings_MemberId_MeetingId_WarningType] ON [MemberWarnings] ([MemberId], [MeetingId], [WarningType]) WHERE [MeetingId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE INDEX [IX_MemberWarnings_StokvelId] ON [MemberWarnings] ([StokvelId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE INDEX [IX_NextOfKinRecords_MemberId] ON [NextOfKinRecords] ([MemberId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE INDEX [IX_Payments_MemberContributionId] ON [Payments] ([MemberContributionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE INDEX [IX_Payments_MemberId] ON [Payments] ([MemberId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE INDEX [IX_Payments_TenantId] ON [Payments] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE INDEX [IX_QuestionnaireOptions_QuestionnaireQuestionId] ON [QuestionnaireOptions] ([QuestionnaireQuestionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE INDEX [IX_QuestionnaireQuestions_QuestionnaireSectionId] ON [QuestionnaireQuestions] ([QuestionnaireSectionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE UNIQUE INDEX [IX_StokvelOperatingRules_StokvelId] ON [StokvelOperatingRules] ([StokvelId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE INDEX [IX_StokvelQuestionnaireAnswers_QuestionnaireQuestionId] ON [StokvelQuestionnaireAnswers] ([QuestionnaireQuestionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE UNIQUE INDEX [IX_StokvelQuestionnaireAnswers_TenantId_QuestionnaireQuestionId] ON [StokvelQuestionnaireAnswers] ([TenantId], [QuestionnaireQuestionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE INDEX [IX_Stokvels_Code] ON [Stokvels] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE INDEX [IX_Stokvels_TenantId] ON [Stokvels] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Tenants_Slug] ON [Tenants] ([Slug]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE INDEX [IX_TenantSubscriptions_SubscriptionPlanId] ON [TenantSubscriptions] ([SubscriptionPlanId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE INDEX [IX_TenantSubscriptions_TenantId] ON [TenantSubscriptions] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE INDEX [IX_VoteMotions_AgendaItemId] ON [VoteMotions] ([AgendaItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE INDEX [IX_VoteMotions_MeetingId] ON [VoteMotions] ([MeetingId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE INDEX [IX_VoteMotions_StokvelId] ON [VoteMotions] ([StokvelId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    CREATE INDEX [IX_VoteOptions_VoteMotionId] ON [VoteOptions] ([VoteMotionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612131346_AzureSqlInitialSyncClean'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260612131346_AzureSqlInitialSyncClean', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260613081118_AddStokvelArchetypeConfiguration'
)
BEGIN
    ALTER TABLE [Stokvels] ADD [Archetype] int NOT NULL DEFAULT 1;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260613081118_AddStokvelArchetypeConfiguration'
)
BEGIN
    ALTER TABLE [Stokvels] ADD [EnableClaims] bit NOT NULL DEFAULT CAST(1 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260613081118_AddStokvelArchetypeConfiguration'
)
BEGIN
    ALTER TABLE [Stokvels] ADD [EnableDependents] bit NOT NULL DEFAULT CAST(1 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260613081118_AddStokvelArchetypeConfiguration'
)
BEGIN
    ALTER TABLE [Stokvels] ADD [EnableEducationPayouts] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260613081118_AddStokvelArchetypeConfiguration'
)
BEGIN
    ALTER TABLE [Stokvels] ADD [EnableInventory] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260613081118_AddStokvelArchetypeConfiguration'
)
BEGIN
    ALTER TABLE [Stokvels] ADD [EnableInvestmentTracking] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260613081118_AddStokvelArchetypeConfiguration'
)
BEGIN
    ALTER TABLE [Stokvels] ADD [EnableLending] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260613081118_AddStokvelArchetypeConfiguration'
)
BEGIN
    ALTER TABLE [Stokvels] ADD [EnableRotation] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260613081118_AddStokvelArchetypeConfiguration'
)
BEGIN
    ALTER TABLE [Stokvels] ADD [EnableSocialEvents] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260613081118_AddStokvelArchetypeConfiguration'
)
BEGIN
    ALTER TABLE [Stokvels] ADD [EnableTravelPlanning] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260613081118_AddStokvelArchetypeConfiguration'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260613081118_AddStokvelArchetypeConfiguration', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260613190852_Stabilisation_Sprint1_Precision_Indexes'
)
BEGIN
    DROP INDEX [IX_MemberContributions_TenantId] ON [MemberContributions];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260613190852_Stabilisation_Sprint1_Precision_Indexes'
)
BEGIN
    DROP INDEX [IX_Meetings_TenantId] ON [Meetings];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260613190852_Stabilisation_Sprint1_Precision_Indexes'
)
BEGIN
    DROP INDEX [IX_FuneralClaims_TenantId] ON [FuneralClaims];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260613190852_Stabilisation_Sprint1_Precision_Indexes'
)
BEGIN
    CREATE INDEX [IX_MemberContributions_TenantId_MemberId] ON [MemberContributions] ([TenantId], [MemberId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260613190852_Stabilisation_Sprint1_Precision_Indexes'
)
BEGIN
    CREATE INDEX [IX_Meetings_TenantId_MeetingDate] ON [Meetings] ([TenantId], [MeetingDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260613190852_Stabilisation_Sprint1_Precision_Indexes'
)
BEGIN
    CREATE INDEX [IX_FuneralClaims_TenantId_Status] ON [FuneralClaims] ([TenantId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260613190852_Stabilisation_Sprint1_Precision_Indexes'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260613190852_Stabilisation_Sprint1_Precision_Indexes', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615063725_SyncBurialMvpModel'
)
BEGIN
    ALTER TABLE [Meetings] ADD [HostMemberId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615063725_SyncBurialMvpModel'
)
BEGIN
    CREATE INDEX [IX_Meetings_HostMemberId] ON [Meetings] ([HostMemberId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615063725_SyncBurialMvpModel'
)
BEGIN
    ALTER TABLE [Meetings] ADD CONSTRAINT [FK_Meetings_Members_HostMemberId] FOREIGN KEY ([HostMemberId]) REFERENCES [Members] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615063725_SyncBurialMvpModel'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260615063725_SyncBurialMvpModel', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615092409_AddApplicationUserAuditColumns'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260615092409_AddApplicationUserAuditColumns', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615102456_FixAspNetUsersAuditColumns'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260615102456_FixAspNetUsersAuditColumns', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615102644_RepairAspNetUsersAuditColumns'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME());
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615102644_RepairAspNetUsersAuditColumns'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [CreatedBy] nvarchar(256) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615102644_RepairAspNetUsersAuditColumns'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [UpdatedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615102644_RepairAspNetUsersAuditColumns'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [UpdatedBy] nvarchar(256) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615102644_RepairAspNetUsersAuditColumns'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615102644_RepairAspNetUsersAuditColumns'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [DeletedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615102644_RepairAspNetUsersAuditColumns'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [DeletedBy] nvarchar(256) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615102644_RepairAspNetUsersAuditColumns'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [LastLoginAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615102644_RepairAspNetUsersAuditColumns'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [LastLoginIp] nvarchar(64) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615102644_RepairAspNetUsersAuditColumns'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260615102644_RepairAspNetUsersAuditColumns', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260620123917_AddRotationalPayoutOrders'
)
BEGIN
    CREATE TABLE [RotationalPayoutOrders] (
        [Id] uniqueidentifier NOT NULL,
        [StokvelId] uniqueidentifier NOT NULL,
        [MemberId] uniqueidentifier NOT NULL,
        [Position] int NOT NULL,
        [HasReceivedPayout] bit NOT NULL,
        [LastPayoutDate] datetime2 NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(450) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(450) NULL,
        CONSTRAINT [PK_RotationalPayoutOrders] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RotationalPayoutOrders_Members_MemberId] FOREIGN KEY ([MemberId]) REFERENCES [Members] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RotationalPayoutOrders_Stokvels_StokvelId] FOREIGN KEY ([StokvelId]) REFERENCES [Stokvels] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260620123917_AddRotationalPayoutOrders'
)
BEGIN
    CREATE INDEX [IX_RotationalPayoutOrders_MemberId] ON [RotationalPayoutOrders] ([MemberId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260620123917_AddRotationalPayoutOrders'
)
BEGIN
    CREATE INDEX [IX_RotationalPayoutOrders_StokvelId] ON [RotationalPayoutOrders] ([StokvelId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260620123917_AddRotationalPayoutOrders'
)
BEGIN
    CREATE INDEX [IX_RotationalPayoutOrders_StokvelId_IsActive] ON [RotationalPayoutOrders] ([StokvelId], [IsActive]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260620123917_AddRotationalPayoutOrders'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_RotationalPayoutOrders_StokvelId_MemberId] ON [RotationalPayoutOrders] ([StokvelId], [MemberId]) WHERE [IsActive] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260620123917_AddRotationalPayoutOrders'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_RotationalPayoutOrders_StokvelId_Position] ON [RotationalPayoutOrders] ([StokvelId], [Position]) WHERE [IsActive] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260620123917_AddRotationalPayoutOrders'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260620123917_AddRotationalPayoutOrders', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622181440_RestoreRotationalLoanEntities'
)
BEGIN
    CREATE TABLE [MemberLoans] (
        [Id] uniqueidentifier NOT NULL,
        [StokvelId] uniqueidentifier NOT NULL,
        [MemberId] uniqueidentifier NOT NULL,
        [RequestedAmount] decimal(18,2) NOT NULL,
        [ApprovedAmount] decimal(18,2) NULL,
        [RepaymentMonths] int NOT NULL,
        [MonthlyRepaymentAmount] decimal(18,2) NOT NULL,
        [TotalRepayableAmount] decimal(18,2) NOT NULL,
        [OutstandingBalance] decimal(18,2) NOT NULL,
        [LoanStatus] int NOT NULL,
        [RequestReason] nvarchar(1000) NOT NULL,
        [RequestedAt] datetime2 NOT NULL,
        [RequestedBy] nvarchar(450) NULL,
        [ApprovedByChairpersonId] nvarchar(450) NULL,
        [ApprovedAt] datetime2 NULL,
        [RejectedByChairpersonId] nvarchar(450) NULL,
        [RejectedAt] datetime2 NULL,
        [RejectionReason] nvarchar(1000) NULL,
        [DisbursedByTreasurerId] nvarchar(450) NULL,
        [DisbursedAt] datetime2 NULL,
        [DisbursementMethod] int NULL,
        [DisbursementReference] nvarchar(100) NULL,
        [DueStartDate] datetime2 NULL,
        [ExpectedFinalPaymentDate] datetime2 NULL,
        [FullyRepaidAt] datetime2 NULL,
        [NextEligibleLoanDate] datetime2 NULL,
        [Notes] nvarchar(1000) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(450) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(450) NULL,
        CONSTRAINT [PK_MemberLoans] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MemberLoans_Members_MemberId] FOREIGN KEY ([MemberId]) REFERENCES [Members] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622181440_RestoreRotationalLoanEntities'
)
BEGIN
    CREATE INDEX [IX_MemberLoans_MemberId] ON [MemberLoans] ([MemberId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622181440_RestoreRotationalLoanEntities'
)
BEGIN
    CREATE INDEX [IX_MemberLoans_StokvelId] ON [MemberLoans] ([StokvelId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622181440_RestoreRotationalLoanEntities'
)
BEGIN
    CREATE TABLE [MemberLoanRepayments] (
        [Id] uniqueidentifier NOT NULL,
        [LoanId] uniqueidentifier NOT NULL,
        [StokvelId] uniqueidentifier NOT NULL,
        [MemberId] uniqueidentifier NOT NULL,
        [DueDate] datetime2 NOT NULL,
        [ExpectedAmount] decimal(18,2) NOT NULL,
        [PaidAmount] decimal(18,2) NOT NULL,
        [FineAmount] decimal(18,2) NOT NULL,
        [PaymentStatus] int NOT NULL,
        [PaymentDate] datetime2 NULL,
        [PaymentMethod] int NULL,
        [PaymentReference] nvarchar(100) NULL,
        [ConfirmedByTreasurerId] nvarchar(450) NULL,
        [ConfirmedAt] datetime2 NULL,
        [Notes] nvarchar(500) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(450) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(450) NULL,
        CONSTRAINT [PK_MemberLoanRepayments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MemberLoanRepayments_MemberLoans_LoanId] FOREIGN KEY ([LoanId]) REFERENCES [MemberLoans] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622181440_RestoreRotationalLoanEntities'
)
BEGIN
    CREATE INDEX [IX_MemberLoanRepayments_LoanId] ON [MemberLoanRepayments] ([LoanId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622181440_RestoreRotationalLoanEntities'
)
BEGIN
    CREATE TABLE [MemberSurplusWallets] (
        [Id] uniqueidentifier NOT NULL,
        [StokvelId] uniqueidentifier NOT NULL,
        [MemberId] uniqueidentifier NOT NULL,
        [AvailableBalance] decimal(18,2) NOT NULL,
        [TotalCredits] decimal(18,2) NOT NULL,
        [TotalWithdrawals] decimal(18,2) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(450) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(450) NULL,
        CONSTRAINT [PK_MemberSurplusWallets] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MemberSurplusWallets_Members_MemberId] FOREIGN KEY ([MemberId]) REFERENCES [Members] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622181440_RestoreRotationalLoanEntities'
)
BEGIN
    CREATE INDEX [IX_MemberSurplusWallets_MemberId] ON [MemberSurplusWallets] ([MemberId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622181440_RestoreRotationalLoanEntities'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_MemberSurplusWallets_StokvelId_MemberId] ON [MemberSurplusWallets] ([StokvelId], [MemberId]) WHERE [IsActive] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622181440_RestoreRotationalLoanEntities'
)
BEGIN
    CREATE TABLE [MemberSurplusWalletTransactions] (
        [Id] uniqueidentifier NOT NULL,
        [WalletId] uniqueidentifier NOT NULL,
        [StokvelId] uniqueidentifier NOT NULL,
        [MemberId] uniqueidentifier NOT NULL,
        [TransactionType] int NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [BalanceAfterTransaction] decimal(18,2) NOT NULL,
        [SourceType] int NOT NULL,
        [SourceReferenceId] uniqueidentifier NULL,
        [Description] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(450) NULL,
        CONSTRAINT [PK_MemberSurplusWalletTransactions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MemberSurplusWalletTransactions_MemberSurplusWallets_WalletId] FOREIGN KEY ([WalletId]) REFERENCES [MemberSurplusWallets] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622181440_RestoreRotationalLoanEntities'
)
BEGIN
    CREATE INDEX [IX_MemberSurplusWalletTransactions_WalletId] ON [MemberSurplusWalletTransactions] ([WalletId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622181440_RestoreRotationalLoanEntities'
)
BEGIN
    CREATE TABLE [MemberSurplusWithdrawalRequests] (
        [Id] uniqueidentifier NOT NULL,
        [WalletId] uniqueidentifier NOT NULL,
        [StokvelId] uniqueidentifier NOT NULL,
        [MemberId] uniqueidentifier NOT NULL,
        [RequestedAmount] decimal(18,2) NOT NULL,
        [WithdrawalStatus] int NOT NULL,
        [RequestReason] nvarchar(1000) NOT NULL,
        [RequestedAt] datetime2 NOT NULL,
        [RequestedBy] nvarchar(450) NULL,
        [ApprovedByChairpersonId] nvarchar(450) NULL,
        [ApprovedAt] datetime2 NULL,
        [RejectedByChairpersonId] nvarchar(450) NULL,
        [RejectedAt] datetime2 NULL,
        [RejectionReason] nvarchar(1000) NULL,
        [PaidByTreasurerId] nvarchar(450) NULL,
        [PaidAt] datetime2 NULL,
        [PaymentMethod] int NULL,
        [PaymentReference] nvarchar(100) NULL,
        [Notes] nvarchar(1000) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(450) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(450) NULL,
        CONSTRAINT [PK_MemberSurplusWithdrawalRequests] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MemberSurplusWithdrawalRequests_Members_MemberId] FOREIGN KEY ([MemberId]) REFERENCES [Members] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_MemberSurplusWithdrawalRequests_MemberSurplusWallets_WalletId] FOREIGN KEY ([WalletId]) REFERENCES [MemberSurplusWallets] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622181440_RestoreRotationalLoanEntities'
)
BEGIN
    CREATE INDEX [IX_MemberSurplusWithdrawalRequests_MemberId] ON [MemberSurplusWithdrawalRequests] ([MemberId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622181440_RestoreRotationalLoanEntities'
)
BEGIN
    CREATE INDEX [IX_MemberSurplusWithdrawalRequests_StokvelId] ON [MemberSurplusWithdrawalRequests] ([StokvelId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622181440_RestoreRotationalLoanEntities'
)
BEGIN
    CREATE INDEX [IX_MemberSurplusWithdrawalRequests_WalletId] ON [MemberSurplusWithdrawalRequests] ([WalletId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622181440_RestoreRotationalLoanEntities'
)
BEGIN
    CREATE TABLE [RotationalStokvelConfigurations] (
        [Id] uniqueidentifier NOT NULL,
        [StokvelId] uniqueidentifier NOT NULL,
        [ContributionAmount] decimal(18,2) NOT NULL,
        [ContributionFrequency] int NOT NULL,
        [ContributionDueDay] int NOT NULL,
        [PayoutAmount] decimal(18,2) NOT NULL,
        [PayoutFrequency] int NOT NULL,
        [RotationStartDate] datetime2 NOT NULL,
        [RotationOrderMethod] int NOT NULL,
        [AllowPayoutTurnSwap] bit NOT NULL,
        [LatePenaltyType] int NOT NULL,
        [LatePenaltyAmount] decimal(18,2) NULL,
        [GracePeriodDays] int NOT NULL,
        [MinimumBalanceBeforePayout] decimal(18,2) NOT NULL,
        [MissedContributionBlocksPayout] bit NOT NULL,
        [TreasurerConfirmationRequired] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [CreatedBy] nvarchar(450) NULL,
        [UpdatedBy] nvarchar(450) NULL,
        CONSTRAINT [PK_RotationalStokvelConfigurations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RotationalStokvelConfigurations_Stokvels_StokvelId] FOREIGN KEY ([StokvelId]) REFERENCES [Stokvels] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622181440_RestoreRotationalLoanEntities'
)
BEGIN
    CREATE INDEX [IX_RotationalStokvelConfigurations_StokvelId] ON [RotationalStokvelConfigurations] ([StokvelId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622181440_RestoreRotationalLoanEntities'
)
BEGIN
    CREATE INDEX [IX_RotationalStokvelConfigurations_StokvelId_IsActive] ON [RotationalStokvelConfigurations] ([StokvelId], [IsActive]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622181440_RestoreRotationalLoanEntities'
)
BEGIN
    CREATE TABLE [RotationalContributionCycles] (
        [Id] uniqueidentifier NOT NULL,
        [StokvelId] uniqueidentifier NOT NULL,
        [ConfigurationId] uniqueidentifier NOT NULL,
        [CycleNumber] int NOT NULL,
        [CycleName] nvarchar(200) NOT NULL,
        [CycleStartDate] datetime2 NOT NULL,
        [CycleEndDate] datetime2 NOT NULL,
        [ContributionDueDate] datetime2 NOT NULL,
        [ContributionAmountPerMember] decimal(18,2) NOT NULL,
        [ExpectedTotalContributionAmount] decimal(18,2) NOT NULL,
        [PayoutOrderId] uniqueidentifier NOT NULL,
        [PayoutMemberId] uniqueidentifier NOT NULL,
        [ExpectedPayoutAmount] decimal(18,2) NOT NULL,
        [ScheduledPayoutDate] datetime2 NOT NULL,
        [Status] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(450) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(450) NULL,
        CONSTRAINT [PK_RotationalContributionCycles] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RotationalContributionCycles_RotationalStokvelConfigurations_ConfigurationId] FOREIGN KEY ([ConfigurationId]) REFERENCES [RotationalStokvelConfigurations] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RotationalContributionCycles_Members_PayoutMemberId] FOREIGN KEY ([PayoutMemberId]) REFERENCES [Members] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RotationalContributionCycles_Stokvels_StokvelId] FOREIGN KEY ([StokvelId]) REFERENCES [Stokvels] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622181440_RestoreRotationalLoanEntities'
)
BEGIN
    CREATE INDEX [IX_RotationalContributionCycles_ConfigurationId] ON [RotationalContributionCycles] ([ConfigurationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622181440_RestoreRotationalLoanEntities'
)
BEGIN
    CREATE INDEX [IX_RotationalContributionCycles_PayoutMemberId] ON [RotationalContributionCycles] ([PayoutMemberId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622181440_RestoreRotationalLoanEntities'
)
BEGIN
    CREATE INDEX [IX_RotationalContributionCycles_StokvelId] ON [RotationalContributionCycles] ([StokvelId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622181440_RestoreRotationalLoanEntities'
)
BEGIN
    CREATE TABLE [RotationalContributionPayments] (
        [Id] uniqueidentifier NOT NULL,
        [CycleId] uniqueidentifier NOT NULL,
        [StokvelId] uniqueidentifier NOT NULL,
        [MemberId] uniqueidentifier NOT NULL,
        [ExpectedAmount] decimal(18,2) NOT NULL,
        [PaidAmount] decimal(18,2) NOT NULL,
        [PenaltyAmount] decimal(18,2) NOT NULL,
        [PaymentStatus] int NOT NULL,
        [PaymentDate] datetime2 NULL,
        [PaymentMethod] int NULL,
        [ReferenceNumber] nvarchar(100) NULL,
        [ConfirmedByTreasurerId] nvarchar(450) NULL,
        [ConfirmedAt] datetime2 NULL,
        [Notes] nvarchar(500) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(450) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(450) NULL,
        CONSTRAINT [PK_RotationalContributionPayments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RotationalContributionPayments_RotationalContributionCycles_CycleId] FOREIGN KEY ([CycleId]) REFERENCES [RotationalContributionCycles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_RotationalContributionPayments_Members_MemberId] FOREIGN KEY ([MemberId]) REFERENCES [Members] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622181440_RestoreRotationalLoanEntities'
)
BEGIN
    CREATE INDEX [IX_RotationalContributionPayments_CycleId] ON [RotationalContributionPayments] ([CycleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622181440_RestoreRotationalLoanEntities'
)
BEGIN
    CREATE INDEX [IX_RotationalContributionPayments_MemberId] ON [RotationalContributionPayments] ([MemberId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622181440_RestoreRotationalLoanEntities'
)
BEGIN
    CREATE TABLE [RotationalPayouts] (
        [Id] uniqueidentifier NOT NULL,
        [CycleId] uniqueidentifier NOT NULL,
        [StokvelId] uniqueidentifier NOT NULL,
        [PayoutMemberId] uniqueidentifier NOT NULL,
        [PayoutAmount] decimal(18,2) NOT NULL,
        [PayoutStatus] int NOT NULL,
        [RequestedAt] datetime2 NOT NULL,
        [RequestedBy] nvarchar(450) NULL,
        [ApprovedByChairpersonId] nvarchar(450) NULL,
        [ApprovedAt] datetime2 NULL,
        [RejectedByChairpersonId] nvarchar(450) NULL,
        [RejectedAt] datetime2 NULL,
        [RejectionReason] nvarchar(1000) NULL,
        [PaidByTreasurerId] nvarchar(450) NULL,
        [PaidAt] datetime2 NULL,
        [PaymentMethod] int NULL,
        [PaymentReference] nvarchar(100) NULL,
        [Notes] nvarchar(1000) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(450) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(450) NULL,
        CONSTRAINT [PK_RotationalPayouts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RotationalPayouts_RotationalContributionCycles_CycleId] FOREIGN KEY ([CycleId]) REFERENCES [RotationalContributionCycles] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RotationalPayouts_Members_PayoutMemberId] FOREIGN KEY ([PayoutMemberId]) REFERENCES [Members] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622181440_RestoreRotationalLoanEntities'
)
BEGIN
    CREATE INDEX [IX_RotationalPayouts_CycleId] ON [RotationalPayouts] ([CycleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622181440_RestoreRotationalLoanEntities'
)
BEGIN
    CREATE INDEX [IX_RotationalPayouts_PayoutMemberId] ON [RotationalPayouts] ([PayoutMemberId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622181440_RestoreRotationalLoanEntities'
)
BEGIN
    CREATE INDEX [IX_RotationalPayouts_StokvelId] ON [RotationalPayouts] ([StokvelId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622181440_RestoreRotationalLoanEntities'
)
BEGIN
    CREATE TABLE [StokvelBankingDetails] (
        [Id] uniqueidentifier NOT NULL,
        [StokvelId] uniqueidentifier NOT NULL,
        [BankName] nvarchar(100) NOT NULL,
        [AccountHolderName] nvarchar(150) NOT NULL,
        [AccountNumber] nvarchar(50) NOT NULL,
        [AccountType] int NOT NULL,
        [BranchCode] nvarchar(20) NULL,
        [BranchName] nvarchar(100) NULL,
        [PaymentReferenceFormat] nvarchar(200) NULL,
        [IsPrimary] bit NOT NULL,
        [Notes] nvarchar(500) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(450) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(450) NULL,
        CONSTRAINT [PK_StokvelBankingDetails] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_StokvelBankingDetails_Stokvels_StokvelId] FOREIGN KEY ([StokvelId]) REFERENCES [Stokvels] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622181440_RestoreRotationalLoanEntities'
)
BEGIN
    CREATE INDEX [IX_StokvelBankingDetails_StokvelId] ON [StokvelBankingDetails] ([StokvelId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622181440_RestoreRotationalLoanEntities'
)
BEGIN
    CREATE TABLE [StokvelLoanConfigurations] (
        [Id] uniqueidentifier NOT NULL,
        [StokvelId] uniqueidentifier NOT NULL,
        [LoansEnabled] bit NOT NULL,
        [MinLoanAmount] decimal(18,2) NOT NULL,
        [MaxLoanAmount] decimal(18,2) NOT NULL,
        [MaxRepaymentMonths] int NOT NULL,
        [DefaultRepaymentMonths] int NOT NULL,
        [LoanInterestType] int NOT NULL,
        [LoanInterestRate] decimal(18,4) NOT NULL,
        [LateRepaymentFineType] int NOT NULL,
        [LateRepaymentFineAmount] decimal(18,2) NULL,
        [GracePeriodDays] int NOT NULL,
        [FreezePeriodAfterFullRepaymentDays] int NOT NULL,
        [RequireChairpersonApproval] bit NOT NULL,
        [RequireTreasurerDisbursementConfirmation] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(450) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(450) NULL,
        CONSTRAINT [PK_StokvelLoanConfigurations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_StokvelLoanConfigurations_Stokvels_StokvelId] FOREIGN KEY ([StokvelId]) REFERENCES [Stokvels] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622181440_RestoreRotationalLoanEntities'
)
BEGIN
    CREATE INDEX [IX_StokvelLoanConfigurations_StokvelId] ON [StokvelLoanConfigurations] ([StokvelId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622181440_RestoreRotationalLoanEntities'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260622181440_RestoreRotationalLoanEntities', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704181543_AddRotationalLoanMechanics'
)
BEGIN
    ALTER TABLE [StokvelLoanConfigurations] ADD [EarlyPayoutDiscountRatePercent] decimal(18,4) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704181543_AddRotationalLoanMechanics'
)
BEGIN
    ALTER TABLE [StokvelLoanConfigurations] ADD [EarlyPayoutLoansEnabled] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704181543_AddRotationalLoanMechanics'
)
BEGIN
    ALTER TABLE [StokvelLoanConfigurations] ADD [RequiredGuarantorCount] int NOT NULL DEFAULT 2;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704181543_AddRotationalLoanMechanics'
)
BEGIN
    ALTER TABLE [StokvelLoanConfigurations] ADD [SurplusBackedLoansEnabled] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704181543_AddRotationalLoanMechanics'
)
BEGIN
    ALTER TABLE [StokvelLoanConfigurations] ADD [SurplusEquityLoanMultiplier] decimal(18,4) NOT NULL DEFAULT 1.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704181543_AddRotationalLoanMechanics'
)
BEGIN
    ALTER TABLE [MemberSurplusWallets] ADD [CoreSavingsBalance] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704181543_AddRotationalLoanMechanics'
)
BEGIN
    ALTER TABLE [MemberSurplusWallets] ADD [LockedSurplusEquityBalance] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704181543_AddRotationalLoanMechanics'
)
BEGIN
    ALTER TABLE [MemberSurplusWallets] ADD [SurplusEquityBalance] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704181543_AddRotationalLoanMechanics'
)
BEGIN
    ALTER TABLE [MemberLoans] ADD [CollateralLockedAmount] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704181543_AddRotationalLoanMechanics'
)
BEGIN
    ALTER TABLE [MemberLoans] ADD [CollateralLockedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704181543_AddRotationalLoanMechanics'
)
BEGIN
    ALTER TABLE [MemberLoans] ADD [CollateralUnlockedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704181543_AddRotationalLoanMechanics'
)
BEGIN
    ALTER TABLE [MemberLoans] ADD [CollateralWalletId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704181543_AddRotationalLoanMechanics'
)
BEGIN
    ALTER TABLE [MemberLoans] ADD [EarlyPayoutDiscountAmount] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704181543_AddRotationalLoanMechanics'
)
BEGIN
    ALTER TABLE [MemberLoans] ADD [EarlyPayoutDiscountRatePercent] decimal(18,4) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704181543_AddRotationalLoanMechanics'
)
BEGIN
    ALTER TABLE [MemberLoans] ADD [EarlyPayoutGrossAmount] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704181543_AddRotationalLoanMechanics'
)
BEGIN
    ALTER TABLE [MemberLoans] ADD [EarlyPayoutNetDisbursedAmount] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704181543_AddRotationalLoanMechanics'
)
BEGIN
    ALTER TABLE [MemberLoans] ADD [LoanType] int NOT NULL DEFAULT 1;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704181543_AddRotationalLoanMechanics'
)
BEGIN
    ALTER TABLE [MemberLoans] ADD [OriginalContributionCycleId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704181543_AddRotationalLoanMechanics'
)
BEGIN
    ALTER TABLE [MemberLoans] ADD [OriginalPayoutOrderId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704181543_AddRotationalLoanMechanics'
)
BEGIN
    CREATE TABLE [MemberLoanGuarantors] (
        [Id] uniqueidentifier NOT NULL,
        [LoanId] uniqueidentifier NOT NULL,
        [GuarantorMemberId] uniqueidentifier NOT NULL,
        [Status] int NOT NULL,
        [RequestedAt] datetime2 NOT NULL,
        [RespondedAt] datetime2 NULL,
        [RequestedByUserId] nvarchar(450) NULL,
        [RespondedByUserId] nvarchar(450) NULL,
        [Notes] nvarchar(1000) NULL,
        [Reason] nvarchar(1000) NULL,
        CONSTRAINT [PK_MemberLoanGuarantors] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MemberLoanGuarantors_MemberLoans_LoanId] FOREIGN KEY ([LoanId]) REFERENCES [MemberLoans] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_MemberLoanGuarantors_Members_GuarantorMemberId] FOREIGN KEY ([GuarantorMemberId]) REFERENCES [Members] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704181543_AddRotationalLoanMechanics'
)
BEGIN
    CREATE TABLE [StokvelReserveTransactions] (
        [Id] uniqueidentifier NOT NULL,
        [StokvelId] uniqueidentifier NOT NULL,
        [MemberLoanId] uniqueidentifier NULL,
        [Amount] decimal(18,2) NOT NULL,
        [TransactionType] int NOT NULL,
        [Description] nvarchar(1000) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByUserId] nvarchar(450) NULL,
        CONSTRAINT [PK_StokvelReserveTransactions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_StokvelReserveTransactions_MemberLoans_MemberLoanId] FOREIGN KEY ([MemberLoanId]) REFERENCES [MemberLoans] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_StokvelReserveTransactions_Stokvels_StokvelId] FOREIGN KEY ([StokvelId]) REFERENCES [Stokvels] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704181543_AddRotationalLoanMechanics'
)
BEGIN
    CREATE INDEX [IX_MemberLoans_CollateralWalletId] ON [MemberLoans] ([CollateralWalletId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704181543_AddRotationalLoanMechanics'
)
BEGIN
    CREATE INDEX [IX_MemberLoans_OriginalContributionCycleId] ON [MemberLoans] ([OriginalContributionCycleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704181543_AddRotationalLoanMechanics'
)
BEGIN
    CREATE INDEX [IX_MemberLoans_OriginalPayoutOrderId] ON [MemberLoans] ([OriginalPayoutOrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704181543_AddRotationalLoanMechanics'
)
BEGIN
    CREATE INDEX [IX_MemberLoanGuarantors_GuarantorMemberId] ON [MemberLoanGuarantors] ([GuarantorMemberId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704181543_AddRotationalLoanMechanics'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_MemberLoanGuarantors_LoanId_GuarantorMemberId] ON [MemberLoanGuarantors] ([LoanId], [GuarantorMemberId]) WHERE [LoanId] IS NOT NULL AND [GuarantorMemberId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704181543_AddRotationalLoanMechanics'
)
BEGIN
    CREATE INDEX [IX_StokvelReserveTransactions_MemberLoanId] ON [StokvelReserveTransactions] ([MemberLoanId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704181543_AddRotationalLoanMechanics'
)
BEGIN
    CREATE INDEX [IX_StokvelReserveTransactions_StokvelId] ON [StokvelReserveTransactions] ([StokvelId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704181543_AddRotationalLoanMechanics'
)
BEGIN
    ALTER TABLE [MemberLoans] ADD CONSTRAINT [FK_MemberLoans_MemberSurplusWallets_CollateralWalletId] FOREIGN KEY ([CollateralWalletId]) REFERENCES [MemberSurplusWallets] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704181543_AddRotationalLoanMechanics'
)
BEGIN
    ALTER TABLE [MemberLoans] ADD CONSTRAINT [FK_MemberLoans_RotationalContributionCycles_OriginalContributionCycleId] FOREIGN KEY ([OriginalContributionCycleId]) REFERENCES [RotationalContributionCycles] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704181543_AddRotationalLoanMechanics'
)
BEGIN
    ALTER TABLE [MemberLoans] ADD CONSTRAINT [FK_MemberLoans_RotationalPayoutOrders_OriginalPayoutOrderId] FOREIGN KEY ([OriginalPayoutOrderId]) REFERENCES [RotationalPayoutOrders] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260704181543_AddRotationalLoanMechanics'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260704181543_AddRotationalLoanMechanics', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705131042_AddSurplusWithdrawalGovernanceReview'
)
BEGIN
    ALTER TABLE [MemberSurplusWithdrawalRequests] ADD [ChairpersonNotes] nvarchar(1000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705131042_AddSurplusWithdrawalGovernanceReview'
)
BEGIN
    ALTER TABLE [MemberSurplusWithdrawalRequests] ADD [ChairpersonReviewedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705131042_AddSurplusWithdrawalGovernanceReview'
)
BEGIN
    ALTER TABLE [MemberSurplusWithdrawalRequests] ADD [ChairpersonReviewedByUserId] nvarchar(450) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705131042_AddSurplusWithdrawalGovernanceReview'
)
BEGIN
    ALTER TABLE [MemberSurplusWithdrawalRequests] ADD [PaymentNotes] nvarchar(1000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705131042_AddSurplusWithdrawalGovernanceReview'
)
BEGIN
    ALTER TABLE [MemberSurplusWithdrawalRequests] ADD [RequestReasonNotes] nvarchar(1000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705131042_AddSurplusWithdrawalGovernanceReview'
)
BEGIN
    ALTER TABLE [MemberSurplusWithdrawalRequests] ADD [SecretaryRecommendedApproval] bit NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705131042_AddSurplusWithdrawalGovernanceReview'
)
BEGIN
    ALTER TABLE [MemberSurplusWithdrawalRequests] ADD [SecretaryReviewNotes] nvarchar(1000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705131042_AddSurplusWithdrawalGovernanceReview'
)
BEGIN
    ALTER TABLE [MemberSurplusWithdrawalRequests] ADD [SecretaryReviewedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705131042_AddSurplusWithdrawalGovernanceReview'
)
BEGIN
    ALTER TABLE [MemberSurplusWithdrawalRequests] ADD [SecretaryReviewedByUserId] nvarchar(450) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705131042_AddSurplusWithdrawalGovernanceReview'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260705131042_AddSurplusWithdrawalGovernanceReview', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705141532_AddRotationalPayoutChairpersonDecisionFields'
)
BEGIN
    ALTER TABLE [RotationalPayouts] ADD [ChairpersonDecision] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705141532_AddRotationalPayoutChairpersonDecisionFields'
)
BEGIN
    ALTER TABLE [RotationalPayouts] ADD [ChairpersonReviewedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705141532_AddRotationalPayoutChairpersonDecisionFields'
)
BEGIN
    ALTER TABLE [RotationalPayouts] ADD [ChairpersonReviewedByUserId] nvarchar(450) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705141532_AddRotationalPayoutChairpersonDecisionFields'
)
BEGIN
    ALTER TABLE [RotationalPayouts] ADD [ChairpersonReviewNotes] nvarchar(1000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705141532_AddRotationalPayoutChairpersonDecisionFields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260705141532_AddRotationalPayoutChairpersonDecisionFields', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260708152903_AddApplicationUserExternalAuthFields'
)
BEGIN

    IF COL_LENGTH('dbo.AspNetUsers', 'ExternalAuthProvider') IS NULL
    BEGIN
        ALTER TABLE [AspNetUsers] ADD [ExternalAuthProvider] nvarchar(50) NULL;
    END

    IF COL_LENGTH('dbo.AspNetUsers', 'ExternalEmail') IS NULL
    BEGIN
        ALTER TABLE [AspNetUsers] ADD [ExternalEmail] nvarchar(256) NULL;
    END

    IF COL_LENGTH('dbo.AspNetUsers', 'ExternalObjectId') IS NULL
    BEGIN
        ALTER TABLE [AspNetUsers] ADD [ExternalObjectId] nvarchar(150) NULL;
    END

    IF COL_LENGTH('dbo.AspNetUsers', 'ExternalTenantId') IS NULL
    BEGIN
        ALTER TABLE [AspNetUsers] ADD [ExternalTenantId] nvarchar(150) NULL;
    END

    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE [name] = N'IX_AspNetUsers_ExternalAuthProvider_ExternalObjectId'
          AND [object_id] = OBJECT_ID(N'[dbo].[AspNetUsers]')
    )
    BEGIN
        CREATE INDEX [IX_AspNetUsers_ExternalAuthProvider_ExternalObjectId]
            ON [AspNetUsers] ([ExternalAuthProvider], [ExternalObjectId]);
    END

END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260708152903_AddApplicationUserExternalAuthFields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260708152903_AddApplicationUserExternalAuthFields', N'10.0.8');
END;

COMMIT;
GO

