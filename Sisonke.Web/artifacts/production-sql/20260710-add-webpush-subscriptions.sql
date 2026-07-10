/*
Production-safe, additive-only patch adding Web Push support.

Adds:
  - dbo.Members.WebPushEnabled (bit, default 1) — member notification preference,
    mirrors the existing EmailEnabled column.
  - dbo.PushSubscriptions — one row per browser/device push subscription, keyed by
    the AspNetUsers Id (UserId), with a unique index on Endpoint.

Safety:
  - No table drop, no column drop, no row delete.
  - Every block is guarded so this script is safe to run more than once and safe to
    run whether or not `dotnet ef database update` has already applied
    20260710060244_AddWebPushSubscriptions.
  - Mirrors that EF migration exactly; keep both in sync if either changes.
*/

BEGIN TRANSACTION;

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

COMMIT;
GO
