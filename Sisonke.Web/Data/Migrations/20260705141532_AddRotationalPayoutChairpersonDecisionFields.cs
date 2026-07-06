using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Sisonke.Web.Data;

#nullable disable

namespace Sisonke.Web.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260705141532_AddRotationalPayoutChairpersonDecisionFields")]
    public partial class AddRotationalPayoutChairpersonDecisionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ChairpersonDecision",
                table: "RotationalPayouts",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ChairpersonReviewedAt",
                table: "RotationalPayouts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChairpersonReviewedByUserId",
                table: "RotationalPayouts",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChairpersonReviewNotes",
                table: "RotationalPayouts",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChairpersonDecision",
                table: "RotationalPayouts");

            migrationBuilder.DropColumn(
                name: "ChairpersonReviewedAt",
                table: "RotationalPayouts");

            migrationBuilder.DropColumn(
                name: "ChairpersonReviewedByUserId",
                table: "RotationalPayouts");

            migrationBuilder.DropColumn(
                name: "ChairpersonReviewNotes",
                table: "RotationalPayouts");
        }
    }
}
