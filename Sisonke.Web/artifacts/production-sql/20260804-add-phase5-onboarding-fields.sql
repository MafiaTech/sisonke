/*
Adds Phase 5 onboarding/billing-page schema (20260804102253_AddPhase5OnboardingFields) to a
database that is behind on migrations.

Covers, in order:
  1. Stokvels.RegistrationType — nullable int (StokvelRegistrationType enum), captured on the
     Phase 5 onboarding wizard's Step 1. Null for stokvels created before this field existed.
  2. OrganisationSubscriptions.BillingAddress — nullable display-only billing identity captured
     on the card-authorisation step. Never card data.
  3. OrganisationSubscriptions.CardholderName — nullable display-only billing identity. Never
     card data.
  4. OrganisationSubscriptions.TermsAcceptedIpAddress — nullable, part of the legacy-migration /
     onboarding evidence trail (who accepted, when, from what IP, which terms version).

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
    WHERE [MigrationId] = N'20260804102253_AddPhase5OnboardingFields'
)
BEGIN
    IF COL_LENGTH('dbo.Stokvels', 'RegistrationType') IS NULL
        ALTER TABLE [Stokvels] ADD [RegistrationType] int NULL;

    IF COL_LENGTH('dbo.OrganisationSubscriptions', 'BillingAddress') IS NULL
        ALTER TABLE [OrganisationSubscriptions] ADD [BillingAddress] nvarchar(400) NULL;

    IF COL_LENGTH('dbo.OrganisationSubscriptions', 'CardholderName') IS NULL
        ALTER TABLE [OrganisationSubscriptions] ADD [CardholderName] nvarchar(150) NULL;

    IF COL_LENGTH('dbo.OrganisationSubscriptions', 'TermsAcceptedIpAddress') IS NULL
        ALTER TABLE [OrganisationSubscriptions] ADD [TermsAcceptedIpAddress] nvarchar(45) NULL;

    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260804102253_AddPhase5OnboardingFields', N'10.0.8');
END;

COMMIT;
GO
