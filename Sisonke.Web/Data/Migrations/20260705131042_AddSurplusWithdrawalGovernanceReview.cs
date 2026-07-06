using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sisonke.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSurplusWithdrawalGovernanceReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ChairpersonNotes",
                table: "MemberSurplusWithdrawalRequests",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ChairpersonReviewedAt",
                table: "MemberSurplusWithdrawalRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChairpersonReviewedByUserId",
                table: "MemberSurplusWithdrawalRequests",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentNotes",
                table: "MemberSurplusWithdrawalRequests",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestReasonNotes",
                table: "MemberSurplusWithdrawalRequests",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SecretaryRecommendedApproval",
                table: "MemberSurplusWithdrawalRequests",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecretaryReviewNotes",
                table: "MemberSurplusWithdrawalRequests",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SecretaryReviewedAt",
                table: "MemberSurplusWithdrawalRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecretaryReviewedByUserId",
                table: "MemberSurplusWithdrawalRequests",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChairpersonNotes",
                table: "MemberSurplusWithdrawalRequests");

            migrationBuilder.DropColumn(
                name: "ChairpersonReviewedAt",
                table: "MemberSurplusWithdrawalRequests");

            migrationBuilder.DropColumn(
                name: "ChairpersonReviewedByUserId",
                table: "MemberSurplusWithdrawalRequests");

            migrationBuilder.DropColumn(
                name: "PaymentNotes",
                table: "MemberSurplusWithdrawalRequests");

            migrationBuilder.DropColumn(
                name: "RequestReasonNotes",
                table: "MemberSurplusWithdrawalRequests");

            migrationBuilder.DropColumn(
                name: "SecretaryRecommendedApproval",
                table: "MemberSurplusWithdrawalRequests");

            migrationBuilder.DropColumn(
                name: "SecretaryReviewNotes",
                table: "MemberSurplusWithdrawalRequests");

            migrationBuilder.DropColumn(
                name: "SecretaryReviewedAt",
                table: "MemberSurplusWithdrawalRequests");

            migrationBuilder.DropColumn(
                name: "SecretaryReviewedByUserId",
                table: "MemberSurplusWithdrawalRequests");
        }
    }
}
