using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Sisonke.Web.Data;

#nullable disable

namespace Sisonke.Web.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260709190000_AddRotationalPayoutSecretaryReviewFields")]
    public partial class AddRotationalPayoutSecretaryReviewFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SecretaryReviewedAt",
                table: "RotationalPayouts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecretaryReviewedByUserId",
                table: "RotationalPayouts",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SecretaryRecommendedApproval",
                table: "RotationalPayouts",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecretaryReviewNotes",
                table: "RotationalPayouts",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SecretaryReviewedAt",
                table: "RotationalPayouts");

            migrationBuilder.DropColumn(
                name: "SecretaryReviewedByUserId",
                table: "RotationalPayouts");

            migrationBuilder.DropColumn(
                name: "SecretaryRecommendedApproval",
                table: "RotationalPayouts");

            migrationBuilder.DropColumn(
                name: "SecretaryReviewNotes",
                table: "RotationalPayouts");
        }
    }
}
