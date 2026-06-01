using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sisonke.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIdNumberAndMemberUserLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId",
                table: "Members",
                type: "TEXT",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CellphoneNumber",
                table: "AspNetUsers",
                type: "TEXT",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdNumber",
                table: "AspNetUsers",
                type: "TEXT",
                maxLength: 30,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Members_ApplicationUserId",
                table: "Members",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Members_IdNumber",
                table: "Members",
                column: "IdNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Members_ApplicationUserId",
                table: "Members");

            migrationBuilder.DropIndex(
                name: "IX_Members_IdNumber",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "CellphoneNumber",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "IdNumber",
                table: "AspNetUsers");
        }
    }
}
