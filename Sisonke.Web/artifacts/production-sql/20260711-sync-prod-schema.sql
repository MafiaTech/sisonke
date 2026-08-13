/*
Production schema sync — brings prod up to date with 4 migrations that were
never applied there. Confirmed missing by comparing prod's __EFMigrationsHistory
export (stops at 20260709174552_AddBurialMvpClaimAndDependentFields) against the
current codebase's Data/Migrations folder.

Covers, in order:
  1. 20260709190000_AddRotationalPayoutSecretaryReviewFields
       - 4 nullable columns on dbo.RotationalPayouts (table already exists in prod)
  2. 20260709210007_AddNotificationSystem
       - dbo.Members.EmailEnabled (bit, default 1)
       - new dbo.NotificationMessages table + 3 indexes
  3. 20260710060244_AddWebPushSubscriptions
       - dbo.Members.WebPushEnabled (bit, default 1)
       - new dbo.PushSubscriptions table + 2 indexes
  4. 20260710174805_AddMemberDocuments
       - new dbo.MemberDocuments table + 1 index

Safety:
  - No table drop, no column drop, no row delete anywhere in this script.
  - Every block is guarded on __EFMigrationsHistory so this script is safe to
    run more than once, and safe regardless of which of the 4 migrations (if
    any) have already been partially applied by another route.
  - Mirrors the corresponding EF migrations' Up() methods exactly; keep this
    script and those migration files in sync if either changes.
  - This does NOT touch the ~11 older migration IDs present in prod's history
    but absent as files in the current codebase (e.g. AddRotationalStokvelMvp,
    the Checkpoint_RotationalMvp_* ones) — those were squashed into consolidated
    migrations at some point; their tables already exist in prod, so there is
    nothing to sync there.
*/

BEGIN TRANSACTION;

-- 1. AddRotationalPayoutSecretaryReviewFields ------------------------------
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709190000_AddRotationalPayoutSecretaryReviewFields'
)
BEGIN
    IF COL_LENGTH('dbo.RotationalPayouts', 'SecretaryReviewedAt') IS NULL
    BEGIN
        ALTER TABLE [RotationalPayouts] ADD [SecretaryReviewedAt] datetime2 NULL;
    END

    IF COL_LENGTH('dbo.RotationalPayouts', 'SecretaryReviewedByUserId') IS NULL
    BEGIN
        ALTER TABLE [RotationalPayouts] ADD [SecretaryReviewedByUserId] nvarchar(450) NULL;
    END

    IF COL_LENGTH('dbo.RotationalPayouts', 'SecretaryRecommendedApproval') IS NULL
    BEGIN
        ALTER TABLE [RotationalPayouts] ADD [SecretaryRecommendedApproval] bit NULL;
    END

    IF COL_LENGTH('dbo.RotationalPayouts', 'SecretaryReviewNotes') IS NULL
    BEGIN
        ALTER TABLE [RotationalPayouts] ADD [SecretaryReviewNotes] nvarchar(1000) NULL;
    END
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709190000_AddRotationalPayoutSecretaryReviewFields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260709190000_AddRotationalPayoutSecretaryReviewFields', N'10.0.8');
END;

-- 2. AddNotificationSystem --------------------------------------------------
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709210007_AddNotificationSystem'
)
BEGIN
    IF COL_LENGTH('dbo.Members', 'EmailEnabled') IS NULL
    BEGIN
        ALTER TABLE [Members] ADD [EmailEnabled] bit NOT NULL DEFAULT CAST(1 AS bit);
    END

    IF OBJECT_ID(N'[NotificationMessages]') IS NULL
    BEGIN
        CREATE TABLE [NotificationMessages] (
            [Id] uniqueidentifier NOT NULL,
            [StokvelId] uniqueidentifier NULL,
            [RecipientMemberId] uniqueidentifier NOT NULL,
            [EntityType] nvarchar(100) NOT NULL,
            [EntityId] uniqueidentifier NOT NULL,
            [Channel] int NOT NULL,
            [Type] int NOT NULL,
            [DedupeKey] nvarchar(200) NOT NULL,
            [Subject] nvarchar(200) NULL,
            [Body] nvarchar(max) NOT NULL,
            [Status] int NOT NULL,
            [AttemptCount] int NOT NULL,
            [LastAttemptAt] datetime2 NULL,
            [LastError] nvarchar(1000) NULL,
            [SentAt] datetime2 NULL,
            [CreatedAt] datetime2 NOT NULL,
            CONSTRAINT [PK_NotificationMessages] PRIMARY KEY ([Id]),
            CONSTRAINT [FK_NotificationMessages_Members_RecipientMemberId] FOREIGN KEY ([RecipientMemberId]) REFERENCES [Members] ([Id]) ON DELETE NO ACTION
        );
    END

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE [name] = N'IX_NotificationMessages_DedupeKey'
          AND [object_id] = OBJECT_ID(N'[dbo].[NotificationMessages]')
    )
    BEGIN
        CREATE UNIQUE INDEX [IX_NotificationMessages_DedupeKey] ON [NotificationMessages] ([DedupeKey]);
    END

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE [name] = N'IX_NotificationMessages_RecipientMemberId'
          AND [object_id] = OBJECT_ID(N'[dbo].[NotificationMessages]')
    )
    BEGIN
        CREATE INDEX [IX_NotificationMessages_RecipientMemberId] ON [NotificationMessages] ([RecipientMemberId]);
    END

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE [name] = N'IX_NotificationMessages_Status_CreatedAt'
          AND [object_id] = OBJECT_ID(N'[dbo].[NotificationMessages]')
    )
    BEGIN
        CREATE INDEX [IX_NotificationMessages_Status_CreatedAt] ON [NotificationMessages] ([Status], [CreatedAt]);
    END
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709210007_AddNotificationSystem'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260709210007_AddNotificationSystem', N'10.0.8');
END;

-- 3. AddWebPushSubscriptions -------------------------------------------------
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710060244_AddWebPushSubscriptions'
)
BEGIN
    IF COL_LENGTH('dbo.Members', 'WebPushEnabled') IS NULL
    BEGIN
        ALTER TABLE [Members] ADD [WebPushEnabled] bit NOT NULL DEFAULT CAST(1 AS bit);
    END

    IF OBJECT_ID(N'[PushSubscriptions]') IS NULL
    BEGIN
        CREATE TABLE [PushSubscriptions] (
            [Id] uniqueidentifier NOT NULL,
            [UserId] nvarchar(450) NOT NULL,
            [Endpoint] nvarchar(500) NOT NULL,
            [P256dh] nvarchar(200) NOT NULL,
            [Auth] nvarchar(200) NOT NULL,
            [CreatedAt] datetime2 NOT NULL,
            CONSTRAINT [PK_PushSubscriptions] PRIMARY KEY ([Id])
        );
    END

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE [name] = N'IX_PushSubscriptions_Endpoint'
          AND [object_id] = OBJECT_ID(N'[dbo].[PushSubscriptions]')
    )
    BEGIN
        CREATE UNIQUE INDEX [IX_PushSubscriptions_Endpoint] ON [PushSubscriptions] ([Endpoint]);
    END

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE [name] = N'IX_PushSubscriptions_UserId'
          AND [object_id] = OBJECT_ID(N'[dbo].[PushSubscriptions]')
    )
    BEGIN
        CREATE INDEX [IX_PushSubscriptions_UserId] ON [PushSubscriptions] ([UserId]);
    END
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710060244_AddWebPushSubscriptions'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260710060244_AddWebPushSubscriptions', N'10.0.8');
END;

-- 4. AddMemberDocuments ------------------------------------------------------
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710174805_AddMemberDocuments'
)
BEGIN
    IF OBJECT_ID(N'[MemberDocuments]') IS NULL
    BEGIN
        CREATE TABLE [MemberDocuments] (
            [Id] uniqueidentifier NOT NULL,
            [MemberId] uniqueidentifier NOT NULL,
            [DocumentType] nvarchar(80) NOT NULL,
            [OriginalFileName] nvarchar(300) NOT NULL,
            [StoredFilePath] nvarchar(500) NOT NULL,
            [ContentType] nvarchar(100) NULL,
            [FileSizeBytes] bigint NOT NULL,
            [UploadedAt] datetime2 NOT NULL,
            CONSTRAINT [PK_MemberDocuments] PRIMARY KEY ([Id]),
            CONSTRAINT [FK_MemberDocuments_Members_MemberId] FOREIGN KEY ([MemberId]) REFERENCES [Members] ([Id]) ON DELETE CASCADE
        );
    END

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE [name] = N'IX_MemberDocuments_MemberId_UploadedAt'
          AND [object_id] = OBJECT_ID(N'[dbo].[MemberDocuments]')
    )
    BEGIN
        CREATE INDEX [IX_MemberDocuments_MemberId_UploadedAt] ON [MemberDocuments] ([MemberId], [UploadedAt]);
    END
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710174805_AddMemberDocuments'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260710174805_AddMemberDocuments', N'10.0.8');
END;

COMMIT;
GO
