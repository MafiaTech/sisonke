using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sisonke.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddFuneralClaimReviewWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ChairpersonDecisionAt",
                table: "FuneralClaims",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChairpersonDecisionByName",
                table: "FuneralClaims",
                type: "TEXT",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChairpersonDecisionNotes",
                table: "FuneralClaims",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SecretaryRecommendedApproval",
                table: "FuneralClaims",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecretaryReviewNotes",
                table: "FuneralClaims",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SecretaryReviewedAt",
                table: "FuneralClaims",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecretaryReviewedByName",
                table: "FuneralClaims",
                type: "TEXT",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubmittedByName",
                table: "FuneralClaims",
                type: "TEXT",
                maxLength: 150,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChairpersonDecisionAt",
                table: "FuneralClaims");

            migrationBuilder.DropColumn(
                name: "ChairpersonDecisionByName",
                table: "FuneralClaims");

            migrationBuilder.DropColumn(
                name: "ChairpersonDecisionNotes",
                table: "FuneralClaims");

            migrationBuilder.DropColumn(
                name: "SecretaryRecommendedApproval",
                table: "FuneralClaims");

            migrationBuilder.DropColumn(
                name: "SecretaryReviewNotes",
                table: "FuneralClaims");

            migrationBuilder.DropColumn(
                name: "SecretaryReviewedAt",
                table: "FuneralClaims");

            migrationBuilder.DropColumn(
                name: "SecretaryReviewedByName",
                table: "FuneralClaims");

            migrationBuilder.DropColumn(
                name: "SubmittedByName",
                table: "FuneralClaims");
        }
    }
}
