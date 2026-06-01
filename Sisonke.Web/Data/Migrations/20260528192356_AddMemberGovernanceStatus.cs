using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sisonke.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddMemberGovernanceStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExpelledAt",
                table: "Members",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GovernanceStatus",
                table: "Members",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "GovernanceStatusChangedAt",
                table: "Members",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GovernanceStatusReason",
                table: "Members",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastWarningIssuedAt",
                table: "Members",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SuspendedAt",
                table: "Members",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpelledAt",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "GovernanceStatus",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "GovernanceStatusChangedAt",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "GovernanceStatusReason",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "LastWarningIssuedAt",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "SuspendedAt",
                table: "Members");
        }
    }
}
