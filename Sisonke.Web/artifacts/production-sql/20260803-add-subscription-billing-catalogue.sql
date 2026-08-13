/*
Adds the subscription billing & plan entitlement schema
(20260803170428_AddSubscriptionBillingCatalogue) to a database that is behind on migrations.

Covers, in order:
  1. SubscriptionPlans — AnnualPrice widened to nullable; new columns Code, Description,
     Currency, MaximumMembers, MaximumAdministrators, MaximumSchemes, StorageLimitMb,
     IsCustomPricing, DisplayOrder, CreatedAt, UpdatedAt. Existing legacy plan rows
     (Pilot/Basic/Standard/Premium) are left untouched — Code stays NULL for them.
  2. New tables: FeatureDefinitions, PlanFeatures, OrganisationSubscriptions,
     SubscriptionPaymentMethods, SubscriptionInvoices, SubscriptionInvoiceLines,
     SubscriptionPayments, SubscriptionEvents, SubscriptionUsages, BillingWebhookEvents,
     PromotionalTrials.

Safety:
  - No table drop, no column drop, no row delete anywhere in this script.
  - The only column change is AnnualPrice going NOT NULL -> NULL (widening, not narrowing);
    existing values are preserved.
  - The old Tenant/TenantSubscription/legacy SubscriptionPlan rows are not touched or renamed.
  - Every block is guarded on __EFMigrationsHistory, so this script is safe to run more than
    once and safe regardless of environment.
  - Mirrors the corresponding EF migration's Up() method exactly; keep this script and that
    migration file in sync if either changes.
*/

PRINT N'Running against database: ' + DB_NAME() + N'. Confirm this is the intended target before proceeding.';

BEGIN TRANSACTION;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803170428_AddSubscriptionBillingCatalogue'
)
BEGIN
    -- ── SubscriptionPlans: widen + additive columns ────────────────────────
    IF EXISTS (
        SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID('dbo.SubscriptionPlans') AND name = 'AnnualPrice' AND is_nullable = 0
    )
    BEGIN
        ALTER TABLE [SubscriptionPlans] ALTER COLUMN [AnnualPrice] decimal(18,2) NULL;
    END

    IF COL_LENGTH('dbo.SubscriptionPlans', 'Code') IS NULL
        ALTER TABLE [SubscriptionPlans] ADD [Code] nvarchar(30) NULL;

    IF COL_LENGTH('dbo.SubscriptionPlans', 'CreatedAt') IS NULL
        ALTER TABLE [SubscriptionPlans] ADD [CreatedAt] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.000';

    IF COL_LENGTH('dbo.SubscriptionPlans', 'Currency') IS NULL
        ALTER TABLE [SubscriptionPlans] ADD [Currency] nvarchar(3) NOT NULL DEFAULT '';

    IF COL_LENGTH('dbo.SubscriptionPlans', 'Description') IS NULL
        ALTER TABLE [SubscriptionPlans] ADD [Description] nvarchar(500) NULL;

    IF COL_LENGTH('dbo.SubscriptionPlans', 'DisplayOrder') IS NULL
        ALTER TABLE [SubscriptionPlans] ADD [DisplayOrder] int NOT NULL DEFAULT 0;

    IF COL_LENGTH('dbo.SubscriptionPlans', 'IsCustomPricing') IS NULL
        ALTER TABLE [SubscriptionPlans] ADD [IsCustomPricing] bit NOT NULL DEFAULT CAST(0 AS bit);

    IF COL_LENGTH('dbo.SubscriptionPlans', 'MaximumAdministrators') IS NULL
        ALTER TABLE [SubscriptionPlans] ADD [MaximumAdministrators] int NULL;

    IF COL_LENGTH('dbo.SubscriptionPlans', 'MaximumMembers') IS NULL
        ALTER TABLE [SubscriptionPlans] ADD [MaximumMembers] int NULL;

    IF COL_LENGTH('dbo.SubscriptionPlans', 'MaximumSchemes') IS NULL
        ALTER TABLE [SubscriptionPlans] ADD [MaximumSchemes] int NULL;

    IF COL_LENGTH('dbo.SubscriptionPlans', 'StorageLimitMb') IS NULL
        ALTER TABLE [SubscriptionPlans] ADD [StorageLimitMb] int NULL;

    IF COL_LENGTH('dbo.SubscriptionPlans', 'UpdatedAt') IS NULL
        ALTER TABLE [SubscriptionPlans] ADD [UpdatedAt] datetime2 NULL;

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE [name] = N'IX_SubscriptionPlans_Code' AND [object_id] = OBJECT_ID(N'[dbo].[SubscriptionPlans]')
    )
        CREATE UNIQUE INDEX [IX_SubscriptionPlans_Code] ON [SubscriptionPlans] ([Code]) WHERE [Code] IS NOT NULL;

    -- ── New tables ──────────────────────────────────────────────────────────
    IF OBJECT_ID(N'[BillingWebhookEvents]') IS NULL
    BEGIN
        CREATE TABLE [BillingWebhookEvents] (
            [Id] uniqueidentifier NOT NULL,
            [Provider] int NOT NULL,
            [ProviderEventId] nvarchar(150) NOT NULL,
            [EventType] nvarchar(100) NOT NULL,
            [RawPayload] nvarchar(max) NOT NULL,
            [SignatureValid] bit NOT NULL,
            [ProcessedAt] datetime2 NULL,
            [ProcessingStatus] int NOT NULL,
            [ErrorMessage] nvarchar(1000) NULL,
            [AttemptCount] int NOT NULL,
            [ReceivedAt] datetime2 NOT NULL,
            CONSTRAINT [PK_BillingWebhookEvents] PRIMARY KEY ([Id])
        );
        CREATE UNIQUE INDEX [IX_BillingWebhookEvents_ProviderEventId] ON [BillingWebhookEvents] ([ProviderEventId]);
    END

    IF OBJECT_ID(N'[FeatureDefinitions]') IS NULL
    BEGIN
        CREATE TABLE [FeatureDefinitions] (
            [Id] uniqueidentifier NOT NULL,
            [Code] nvarchar(60) NOT NULL,
            [Name] nvarchar(150) NOT NULL,
            [Description] nvarchar(500) NULL,
            [Category] nvarchar(50) NOT NULL,
            [DataType] int NOT NULL,
            [DefaultValue] nvarchar(50) NOT NULL,
            [IsActive] bit NOT NULL,
            CONSTRAINT [PK_FeatureDefinitions] PRIMARY KEY ([Id])
        );
        CREATE UNIQUE INDEX [IX_FeatureDefinitions_Code] ON [FeatureDefinitions] ([Code]);
    END

    IF OBJECT_ID(N'[OrganisationSubscriptions]') IS NULL
    BEGIN
        CREATE TABLE [OrganisationSubscriptions] (
            [Id] uniqueidentifier NOT NULL,
            [StokvelId] uniqueidentifier NOT NULL,
            [SubscriptionPlanId] uniqueidentifier NULL,
            [Status] int NOT NULL,
            [TrialStartedAt] datetime2 NULL,
            [TrialEndsAt] datetime2 NULL,
            [CurrentPeriodStartedAt] datetime2 NULL,
            [CurrentPeriodEndsAt] datetime2 NULL,
            [CancelAtPeriodEnd] bit NOT NULL,
            [CancelledAt] datetime2 NULL,
            [SuspendedAt] datetime2 NULL,
            [Provider] int NULL,
            [ProviderCustomerCode] nvarchar(100) NULL,
            [ProviderSubscriptionCode] nvarchar(100) NULL,
            [ProviderPlanCode] nvarchar(100) NULL,
            [LastSuccessfulPaymentAt] datetime2 NULL,
            [NextBillingAt] datetime2 NULL,
            [GracePeriodEndsAt] datetime2 NULL,
            [BillingEmail] nvarchar(256) NULL,
            [TermsAcceptedAt] datetime2 NULL,
            [TermsAcceptedByUserId] nvarchar(450) NULL,
            [TermsVersion] nvarchar(30) NULL,
            [CreatedAt] datetime2 NOT NULL,
            [UpdatedAt] datetime2 NULL,
            [RowVersion] varbinary(max) NOT NULL,
            CONSTRAINT [PK_OrganisationSubscriptions] PRIMARY KEY ([Id]),
            CONSTRAINT [FK_OrganisationSubscriptions_Stokvels_StokvelId] FOREIGN KEY ([StokvelId]) REFERENCES [Stokvels] ([Id]) ON DELETE NO ACTION,
            CONSTRAINT [FK_OrganisationSubscriptions_SubscriptionPlans_SubscriptionPlanId] FOREIGN KEY ([SubscriptionPlanId]) REFERENCES [SubscriptionPlans] ([Id]) ON DELETE NO ACTION
        );
        CREATE UNIQUE INDEX [IX_OrganisationSubscriptions_StokvelId] ON [OrganisationSubscriptions] ([StokvelId]) WHERE [Status] IN (0, 1, 2, 3, 6, 7, 8);
        CREATE INDEX [IX_OrganisationSubscriptions_SubscriptionPlanId] ON [OrganisationSubscriptions] ([SubscriptionPlanId]);
    END

    IF OBJECT_ID(N'[PromotionalTrials]') IS NULL
    BEGIN
        CREATE TABLE [PromotionalTrials] (
            [Id] uniqueidentifier NOT NULL,
            [Code] nvarchar(30) NOT NULL,
            [Description] nvarchar(300) NULL,
            [TrialDays] int NOT NULL,
            [AppliesToPlanId] uniqueidentifier NULL,
            [ValidFrom] datetime2 NULL,
            [ValidTo] datetime2 NULL,
            [MaxRedemptions] int NULL,
            [RedemptionCount] int NOT NULL,
            [IsActive] bit NOT NULL,
            [CreatedAt] datetime2 NOT NULL,
            CONSTRAINT [PK_PromotionalTrials] PRIMARY KEY ([Id]),
            CONSTRAINT [FK_PromotionalTrials_SubscriptionPlans_AppliesToPlanId] FOREIGN KEY ([AppliesToPlanId]) REFERENCES [SubscriptionPlans] ([Id]) ON DELETE NO ACTION
        );
        CREATE INDEX [IX_PromotionalTrials_AppliesToPlanId] ON [PromotionalTrials] ([AppliesToPlanId]);
        CREATE UNIQUE INDEX [IX_PromotionalTrials_Code] ON [PromotionalTrials] ([Code]);
    END

    IF OBJECT_ID(N'[PlanFeatures]') IS NULL
    BEGIN
        CREATE TABLE [PlanFeatures] (
            [Id] uniqueidentifier NOT NULL,
            [SubscriptionPlanId] uniqueidentifier NOT NULL,
            [FeatureDefinitionId] uniqueidentifier NOT NULL,
            [IsEnabled] bit NOT NULL,
            [LimitValue] int NULL,
            [ConfigurationJson] nvarchar(max) NULL,
            CONSTRAINT [PK_PlanFeatures] PRIMARY KEY ([Id]),
            CONSTRAINT [FK_PlanFeatures_FeatureDefinitions_FeatureDefinitionId] FOREIGN KEY ([FeatureDefinitionId]) REFERENCES [FeatureDefinitions] ([Id]) ON DELETE NO ACTION,
            CONSTRAINT [FK_PlanFeatures_SubscriptionPlans_SubscriptionPlanId] FOREIGN KEY ([SubscriptionPlanId]) REFERENCES [SubscriptionPlans] ([Id]) ON DELETE CASCADE
        );
        CREATE INDEX [IX_PlanFeatures_FeatureDefinitionId] ON [PlanFeatures] ([FeatureDefinitionId]);
        CREATE UNIQUE INDEX [IX_PlanFeatures_SubscriptionPlanId_FeatureDefinitionId] ON [PlanFeatures] ([SubscriptionPlanId], [FeatureDefinitionId]);
    END

    IF OBJECT_ID(N'[SubscriptionEvents]') IS NULL
    BEGIN
        CREATE TABLE [SubscriptionEvents] (
            [Id] uniqueidentifier NOT NULL,
            [OrganisationSubscriptionId] uniqueidentifier NOT NULL,
            [EventType] int NOT NULL,
            [FromStatus] int NULL,
            [ToStatus] int NULL,
            [ActorUserId] nvarchar(450) NULL,
            [Notes] nvarchar(1000) NULL,
            [PayloadJson] nvarchar(max) NULL,
            [OccurredAt] datetime2 NOT NULL,
            CONSTRAINT [PK_SubscriptionEvents] PRIMARY KEY ([Id]),
            CONSTRAINT [FK_SubscriptionEvents_OrganisationSubscriptions_OrganisationSubscriptionId] FOREIGN KEY ([OrganisationSubscriptionId]) REFERENCES [OrganisationSubscriptions] ([Id]) ON DELETE CASCADE
        );
        CREATE INDEX [IX_SubscriptionEvents_OrganisationSubscriptionId_OccurredAt] ON [SubscriptionEvents] ([OrganisationSubscriptionId], [OccurredAt]);
    END

    IF OBJECT_ID(N'[SubscriptionInvoices]') IS NULL
    BEGIN
        CREATE TABLE [SubscriptionInvoices] (
            [Id] uniqueidentifier NOT NULL,
            [OrganisationSubscriptionId] uniqueidentifier NOT NULL,
            [InvoiceNumber] nvarchar(40) NOT NULL,
            [Status] int NOT NULL,
            [PeriodStart] datetime2 NOT NULL,
            [PeriodEnd] datetime2 NOT NULL,
            [SubTotal] decimal(18,2) NOT NULL,
            [VatAmount] decimal(18,2) NOT NULL,
            [Total] decimal(18,2) NOT NULL,
            [Currency] nvarchar(3) NOT NULL,
            [IssuedAt] datetime2 NULL,
            [DueAt] datetime2 NULL,
            [PaidAt] datetime2 NULL,
            [ProviderInvoiceCode] nvarchar(100) NULL,
            [CreatedAt] datetime2 NOT NULL,
            [UpdatedAt] datetime2 NULL,
            CONSTRAINT [PK_SubscriptionInvoices] PRIMARY KEY ([Id]),
            CONSTRAINT [FK_SubscriptionInvoices_OrganisationSubscriptions_OrganisationSubscriptionId] FOREIGN KEY ([OrganisationSubscriptionId]) REFERENCES [OrganisationSubscriptions] ([Id]) ON DELETE CASCADE
        );
        CREATE UNIQUE INDEX [IX_SubscriptionInvoices_InvoiceNumber] ON [SubscriptionInvoices] ([InvoiceNumber]);
        CREATE INDEX [IX_SubscriptionInvoices_OrganisationSubscriptionId] ON [SubscriptionInvoices] ([OrganisationSubscriptionId]);
    END

    IF OBJECT_ID(N'[SubscriptionPaymentMethods]') IS NULL
    BEGIN
        CREATE TABLE [SubscriptionPaymentMethods] (
            [Id] uniqueidentifier NOT NULL,
            [OrganisationSubscriptionId] uniqueidentifier NOT NULL,
            [Provider] int NOT NULL,
            [ProviderAuthorizationCode] nvarchar(100) NOT NULL,
            [CardBrand] nvarchar(30) NULL,
            [Last4] nvarchar(4) NULL,
            [ExpiryMonth] int NULL,
            [ExpiryYear] int NULL,
            [Bank] nvarchar(100) NULL,
            [CardholderName] nvarchar(150) NULL,
            [IsDefault] bit NOT NULL,
            [IsReusable] bit NOT NULL,
            [AuthorisedAt] datetime2 NOT NULL,
            [RemovedAt] datetime2 NULL,
            [CreatedAt] datetime2 NOT NULL,
            CONSTRAINT [PK_SubscriptionPaymentMethods] PRIMARY KEY ([Id]),
            CONSTRAINT [FK_SubscriptionPaymentMethods_OrganisationSubscriptions_OrganisationSubscriptionId] FOREIGN KEY ([OrganisationSubscriptionId]) REFERENCES [OrganisationSubscriptions] ([Id]) ON DELETE CASCADE
        );
        CREATE INDEX [IX_SubscriptionPaymentMethods_OrganisationSubscriptionId] ON [SubscriptionPaymentMethods] ([OrganisationSubscriptionId]);
    END

    IF OBJECT_ID(N'[SubscriptionUsages]') IS NULL
    BEGIN
        CREATE TABLE [SubscriptionUsages] (
            [Id] uniqueidentifier NOT NULL,
            [OrganisationSubscriptionId] uniqueidentifier NOT NULL,
            [FeatureCode] nvarchar(60) NOT NULL,
            [PeriodStart] datetime2 NOT NULL,
            [PeriodEnd] datetime2 NOT NULL,
            [UsedValue] int NOT NULL,
            [LimitValue] int NULL,
            [LastCalculatedAt] datetime2 NOT NULL,
            CONSTRAINT [PK_SubscriptionUsages] PRIMARY KEY ([Id]),
            CONSTRAINT [FK_SubscriptionUsages_OrganisationSubscriptions_OrganisationSubscriptionId] FOREIGN KEY ([OrganisationSubscriptionId]) REFERENCES [OrganisationSubscriptions] ([Id]) ON DELETE CASCADE
        );
        CREATE UNIQUE INDEX [IX_SubscriptionUsages_OrganisationSubscriptionId_FeatureCode_PeriodStart] ON [SubscriptionUsages] ([OrganisationSubscriptionId], [FeatureCode], [PeriodStart]);
    END

    IF OBJECT_ID(N'[SubscriptionInvoiceLines]') IS NULL
    BEGIN
        CREATE TABLE [SubscriptionInvoiceLines] (
            [Id] uniqueidentifier NOT NULL,
            [SubscriptionInvoiceId] uniqueidentifier NOT NULL,
            [Description] nvarchar(300) NOT NULL,
            [Quantity] int NOT NULL,
            [UnitPrice] decimal(18,2) NOT NULL,
            [LineTotal] decimal(18,2) NOT NULL,
            [FeatureCode] nvarchar(60) NULL,
            [SortOrder] int NOT NULL,
            CONSTRAINT [PK_SubscriptionInvoiceLines] PRIMARY KEY ([Id]),
            CONSTRAINT [FK_SubscriptionInvoiceLines_SubscriptionInvoices_SubscriptionInvoiceId] FOREIGN KEY ([SubscriptionInvoiceId]) REFERENCES [SubscriptionInvoices] ([Id]) ON DELETE CASCADE
        );
        CREATE INDEX [IX_SubscriptionInvoiceLines_SubscriptionInvoiceId] ON [SubscriptionInvoiceLines] ([SubscriptionInvoiceId]);
    END

    IF OBJECT_ID(N'[SubscriptionPayments]') IS NULL
    BEGIN
        CREATE TABLE [SubscriptionPayments] (
            [Id] uniqueidentifier NOT NULL,
            [OrganisationSubscriptionId] uniqueidentifier NOT NULL,
            [SubscriptionInvoiceId] uniqueidentifier NULL,
            [Amount] decimal(18,2) NOT NULL,
            [Currency] nvarchar(3) NOT NULL,
            [Status] int NOT NULL,
            [ProviderReference] nvarchar(100) NOT NULL,
            [ProviderTransactionId] nvarchar(100) NULL,
            [AttemptNumber] int NOT NULL,
            [FailureCode] nvarchar(50) NULL,
            [FailureMessage] nvarchar(500) NULL,
            [PaidAt] datetime2 NULL,
            [CreatedAt] datetime2 NOT NULL,
            CONSTRAINT [PK_SubscriptionPayments] PRIMARY KEY ([Id]),
            CONSTRAINT [FK_SubscriptionPayments_OrganisationSubscriptions_OrganisationSubscriptionId] FOREIGN KEY ([OrganisationSubscriptionId]) REFERENCES [OrganisationSubscriptions] ([Id]) ON DELETE CASCADE,
            CONSTRAINT [FK_SubscriptionPayments_SubscriptionInvoices_SubscriptionInvoiceId] FOREIGN KEY ([SubscriptionInvoiceId]) REFERENCES [SubscriptionInvoices] ([Id]) ON DELETE NO ACTION
        );
        CREATE INDEX [IX_SubscriptionPayments_OrganisationSubscriptionId] ON [SubscriptionPayments] ([OrganisationSubscriptionId]);
        CREATE UNIQUE INDEX [IX_SubscriptionPayments_ProviderReference] ON [SubscriptionPayments] ([ProviderReference]);
        CREATE INDEX [IX_SubscriptionPayments_SubscriptionInvoiceId] ON [SubscriptionPayments] ([SubscriptionInvoiceId]);
    END

    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260803170428_AddSubscriptionBillingCatalogue', N'10.0.8');
END;

COMMIT;
GO
