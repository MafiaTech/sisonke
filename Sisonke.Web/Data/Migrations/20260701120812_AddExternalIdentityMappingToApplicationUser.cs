using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sisonke.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalIdentityMappingToApplicationUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalAuthProvider",
                table: "AspNetUsers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalEmail",
                table: "AspNetUsers",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalObjectId",
                table: "AspNetUsers",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalTenantId",
                table: "AspNetUsers",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_ExternalAuthProvider_ExternalTenantId_ExternalObjectId",
                table: "AspNetUsers",
                columns: new[] { "ExternalAuthProvider", "ExternalTenantId", "ExternalObjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_ExternalEmail",
                table: "AspNetUsers",
                column: "ExternalEmail");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_ExternalAuthProvider_ExternalTenantId_ExternalObjectId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_ExternalEmail",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ExternalAuthProvider",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ExternalEmail",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ExternalObjectId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ExternalTenantId",
                table: "AspNetUsers");
        }
    }
}
