using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sisonke.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class SyncBurialMvpModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "HostMemberId",
                table: "Meetings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Meetings_HostMemberId",
                table: "Meetings",
                column: "HostMemberId");

            migrationBuilder.AddForeignKey(
                name: "FK_Meetings_Members_HostMemberId",
                table: "Meetings",
                column: "HostMemberId",
                principalTable: "Members",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Meetings_Members_HostMemberId",
                table: "Meetings");

            migrationBuilder.DropIndex(
                name: "IX_Meetings_HostMemberId",
                table: "Meetings");

            migrationBuilder.DropColumn(
                name: "HostMemberId",
                table: "Meetings");
        }
    }
}
