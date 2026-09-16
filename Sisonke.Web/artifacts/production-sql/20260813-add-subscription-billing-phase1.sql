BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813113907_AddSubscriptionBillingPhase1'
)
BEGIN
    ALTER TABLE [SubscriptionPayments] ADD [ChargePurpose] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813113907_AddSubscriptionBillingPhase1'
)
BEGIN
    ALTER TABLE [SubscriptionPayments] ADD [ProcessedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813113907_AddSubscriptionBillingPhase1'
)
BEGIN
    ALTER TABLE [SubscriptionPayments] ADD [Provider] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813113907_AddSubscriptionBillingPhase1'
)
BEGIN
    ALTER TABLE [SubscriptionPayments] ADD [ProviderRequestReference] nvarchar(150) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813113907_AddSubscriptionBillingPhase1'
)
BEGIN
    ALTER TABLE [SubscriptionPayments] ADD [SettledAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813113907_AddSubscriptionBillingPhase1'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SubscriptionPaymentMethods]') AND [c].[name] = N'ProviderAuthorizationCode');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [SubscriptionPaymentMethods] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [SubscriptionPaymentMethods] ALTER COLUMN [ProviderAuthorizationCode] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813113907_AddSubscriptionBillingPhase1'
)
BEGIN
    ALTER TABLE [SubscriptionPaymentMethods] ADD [MandateActivatedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813113907_AddSubscriptionBillingPhase1'
)
BEGIN
    ALTER TABLE [SubscriptionPaymentMethods] ADD [MandateCreatedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813113907_AddSubscriptionBillingPhase1'
)
BEGIN
    ALTER TABLE [SubscriptionPaymentMethods] ADD [MandateStatus] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813113907_AddSubscriptionBillingPhase1'
)
BEGIN
    ALTER TABLE [SubscriptionPaymentMethods] ADD [MaskedDisplay] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813113907_AddSubscriptionBillingPhase1'
)
BEGIN
    ALTER TABLE [SubscriptionPaymentMethods] ADD [PaymentMethodType] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813113907_AddSubscriptionBillingPhase1'
)
BEGIN
    ALTER TABLE [SubscriptionPaymentMethods] ADD [ProviderMandateReference] nvarchar(150) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813113907_AddSubscriptionBillingPhase1'
)
BEGIN
    ALTER TABLE [SubscriptionPaymentMethods] ADD [ProviderPaymentMethodReference] nvarchar(150) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813113907_AddSubscriptionBillingPhase1'
)
BEGIN
    ALTER TABLE [OrganisationSubscriptions] ADD [TrialOptedIn] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813113907_AddSubscriptionBillingPhase1'
)
BEGIN
    CREATE INDEX [IX_SubscriptionPayments_Provider_ProviderRequestReference] ON [SubscriptionPayments] ([Provider], [ProviderRequestReference]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813113907_AddSubscriptionBillingPhase1'
)
BEGIN
    CREATE INDEX [IX_SubscriptionPayments_Provider_ProviderTransactionId] ON [SubscriptionPayments] ([Provider], [ProviderTransactionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813113907_AddSubscriptionBillingPhase1'
)
BEGIN
    CREATE INDEX [IX_SubscriptionPaymentMethods_Provider_ProviderMandateReference] ON [SubscriptionPaymentMethods] ([Provider], [ProviderMandateReference]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813113907_AddSubscriptionBillingPhase1'
)
BEGIN
    CREATE INDEX [IX_SubscriptionPaymentMethods_Provider_ProviderPaymentMethodReference] ON [SubscriptionPaymentMethods] ([Provider], [ProviderPaymentMethodReference]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813113907_AddSubscriptionBillingPhase1'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260813113907_AddSubscriptionBillingPhase1', N'10.0.8');
END;

COMMIT;
GO

