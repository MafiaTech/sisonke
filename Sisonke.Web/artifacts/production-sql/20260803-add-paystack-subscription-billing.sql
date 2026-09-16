/*
Adds Paystack subscription integration schema
(20260803180305_AddPaystackSubscriptionBilling) to a database that is behind on migrations.

Covers, in order:
  1. SubscriptionPlans.ProviderPlanCode — the Paystack plan code, created once per Sisonke plan
     and shared across every subscriber (not per-organisation).
  2. OrganisationSubscriptions — ProviderEmailToken (required alongside ProviderSubscriptionCode
     to disable a Paystack subscription), PendingPlanChangePlanId/PendingPlanChangeEffectiveAt
     (deferred downgrade support).
  3. New table InvoiceNumberCounters — one row per calendar year, backs the gapless
     SIS-INV-{year}-{seq} invoice numbering. Year is an app-supplied value, NOT an identity
     column — do not add an IDENTITY constraint here.

Safety:
  - No table drop, no column drop, no row delete, no column type change anywhere in this script.
  - Every block is guarded on __EFMigrationsHistory, so this script is safe to run more than
    once and safe regardless of environment.
  - Mirrors the corresponding EF migration's Up() method exactly; keep this script and that
    migration file in sync if either changes.
*/

PRINT N'Running against database: ' + DB_NAME() + N'. Confirm this is the intended target before proceeding.';

BEGIN TRANSACTION;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803180305_AddPaystackSubscriptionBilling'
)
BEGIN
    IF COL_LENGTH('dbo.SubscriptionPlans', 'ProviderPlanCode') IS NULL
        ALTER TABLE [SubscriptionPlans] ADD [ProviderPlanCode] nvarchar(100) NULL;

    IF COL_LENGTH('dbo.OrganisationSubscriptions', 'PendingPlanChangeEffectiveAt') IS NULL
        ALTER TABLE [OrganisationSubscriptions] ADD [PendingPlanChangeEffectiveAt] datetime2 NULL;

    IF COL_LENGTH('dbo.OrganisationSubscriptions', 'PendingPlanChangePlanId') IS NULL
        ALTER TABLE [OrganisationSubscriptions] ADD [PendingPlanChangePlanId] uniqueidentifier NULL;

    IF COL_LENGTH('dbo.OrganisationSubscriptions', 'ProviderEmailToken') IS NULL
        ALTER TABLE [OrganisationSubscriptions] ADD [ProviderEmailToken] nvarchar(200) NULL;

    IF OBJECT_ID(N'[InvoiceNumberCounters]') IS NULL
    BEGIN
        CREATE TABLE [InvoiceNumberCounters] (
            [Year] int NOT NULL,
            [NextNumber] int NOT NULL,
            CONSTRAINT [PK_InvoiceNumberCounters] PRIMARY KEY ([Year])
        );
    END

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE [name] = N'IX_OrganisationSubscriptions_PendingPlanChangePlanId'
          AND [object_id] = OBJECT_ID(N'[dbo].[OrganisationSubscriptions]')
    )
        CREATE INDEX [IX_OrganisationSubscriptions_PendingPlanChangePlanId] ON [OrganisationSubscriptions] ([PendingPlanChangePlanId]);

    IF NOT EXISTS (
        SELECT 1 FROM sys.foreign_keys
        WHERE [name] = N'FK_OrganisationSubscriptions_SubscriptionPlans_PendingPlanChangePlanId'
    )
        ALTER TABLE [OrganisationSubscriptions]
        ADD CONSTRAINT [FK_OrganisationSubscriptions_SubscriptionPlans_PendingPlanChangePlanId]
        FOREIGN KEY ([PendingPlanChangePlanId]) REFERENCES [SubscriptionPlans] ([Id]) ON DELETE NO ACTION;

    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260803180305_AddPaystackSubscriptionBilling', N'10.0.8');
END;

COMMIT;
GO
