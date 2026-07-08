using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sisonke.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationUserExternalAuthFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
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
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_AspNetUsers_ExternalAuthProvider_ExternalObjectId'
      AND [object_id] = OBJECT_ID(N'[dbo].[AspNetUsers]')
)
BEGIN
    DROP INDEX [IX_AspNetUsers_ExternalAuthProvider_ExternalObjectId]
        ON [AspNetUsers];
END

IF COL_LENGTH('dbo.AspNetUsers', 'ExternalAuthProvider') IS NOT NULL
BEGIN
    ALTER TABLE [AspNetUsers] DROP COLUMN [ExternalAuthProvider];
END

IF COL_LENGTH('dbo.AspNetUsers', 'ExternalEmail') IS NOT NULL
BEGIN
    ALTER TABLE [AspNetUsers] DROP COLUMN [ExternalEmail];
END

IF COL_LENGTH('dbo.AspNetUsers', 'ExternalObjectId') IS NOT NULL
BEGIN
    ALTER TABLE [AspNetUsers] DROP COLUMN [ExternalObjectId];
END

IF COL_LENGTH('dbo.AspNetUsers', 'ExternalTenantId') IS NOT NULL
BEGIN
    ALTER TABLE [AspNetUsers] DROP COLUMN [ExternalTenantId];
END
");
        }
    }
}
