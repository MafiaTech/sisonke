using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sisonke.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddDeceasedTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeathReportedAt",
                table: "Members",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeceasedDate",
                table: "Members",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeceased",
                table: "Members",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeathReportedAt",
                table: "MemberDependents",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeceasedDate",
                table: "MemberDependents",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeceased",
                table: "MemberDependents",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeathReportedAt",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "DeceasedDate",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "IsDeceased",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "DeathReportedAt",
                table: "MemberDependents");

            migrationBuilder.DropColumn(
                name: "DeceasedDate",
                table: "MemberDependents");

            migrationBuilder.DropColumn(
                name: "IsDeceased",
                table: "MemberDependents");
        }
    }
}
