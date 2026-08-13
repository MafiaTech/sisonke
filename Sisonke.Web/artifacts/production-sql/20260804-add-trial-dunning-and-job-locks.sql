/*
Adds Phase 4 trial-lifecycle/dunning/job-locking schema
(20260804051930_AddTrialDunningAndJobLocks) to a database that is behind on migrations.

Covers, in order:
  1. OrganisationSubscriptions.DunningStartedAt — set on day 0 of a failed-debit episode;
     DunningJob computes every dunning action from days-since-this-value.
  2. New table JobExecutionLocks — portable distributed lock backing IDistributedJobLock
     (no sp_getapplock, no Hangfire — see Phase 4 report). JobName is a plain string PK, not an
     identity column.
  3. New table TrialReminderSents — one row per (subscription, days-remaining bucket), the
     idempotency marker for TrialReminderJob.

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
    WHERE [MigrationId] = N'20260804051930_AddTrialDunningAndJobLocks'
)
BEGIN
    IF COL_LENGTH('dbo.OrganisationSubscriptions', 'DunningStartedAt') IS NULL
        ALTER TABLE [OrganisationSubscriptions] ADD [DunningStartedAt] datetime2 NULL;

    IF OBJECT_ID(N'[JobExecutionLocks]') IS NULL
    BEGIN
        CREATE TABLE [JobExecutionLocks] (
            [JobName] nvarchar(100) NOT NULL,
            [LockToken] uniqueidentifier NULL,
            [LockedAt] datetime2 NULL,
            [LockExpiresAt] datetime2 NULL,
            CONSTRAINT [PK_JobExecutionLocks] PRIMARY KEY ([JobName])
        );
    END

    IF OBJECT_ID(N'[TrialReminderSents]') IS NULL
    BEGIN
        CREATE TABLE [TrialReminderSents] (
            [Id] uniqueidentifier NOT NULL,
            [OrganisationSubscriptionId] uniqueidentifier NOT NULL,
            [Bucket] int NOT NULL,
            [SentAt] datetime2 NOT NULL,
            CONSTRAINT [PK_TrialReminderSents] PRIMARY KEY ([Id]),
            CONSTRAINT [FK_TrialReminderSents_OrganisationSubscriptions_OrganisationSubscriptionId] FOREIGN KEY ([OrganisationSubscriptionId]) REFERENCES [OrganisationSubscriptions] ([Id]) ON DELETE CASCADE
        );
        CREATE UNIQUE INDEX [IX_TrialReminderSents_OrganisationSubscriptionId_Bucket] ON [TrialReminderSents] ([OrganisationSubscriptionId], [Bucket]);
    END

    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260804051930_AddTrialDunningAndJobLocks', N'10.0.8');
END;

COMMIT;
GO
