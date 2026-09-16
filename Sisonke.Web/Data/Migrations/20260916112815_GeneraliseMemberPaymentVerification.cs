using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sisonke.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class GeneraliseMemberPaymentVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ContributionPaymentSubmissions_MemberContributionId",
                table: "ContributionPaymentSubmissions");

            migrationBuilder.AlterColumn<Guid>(
                name: "MemberContributionId",
                table: "ContributionPaymentSubmissions",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<Guid>(
                name: "MemberFineId",
                table: "ContributionPaymentSubmissions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ObligationType",
                table: "ContributionPaymentSubmissions",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_ContributionPaymentSubmissions_MemberContributionId",
                table: "ContributionPaymentSubmissions",
                column: "MemberContributionId",
                unique: true,
                filter: "[Status] = 1 AND [MemberContributionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ContributionPaymentSubmissions_MemberFineId",
                table: "ContributionPaymentSubmissions",
                column: "MemberFineId",
                unique: true,
                filter: "[Status] = 1 AND [MemberFineId] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PaymentSubmission_Obligation",
                table: "ContributionPaymentSubmissions",
                sql: "([ObligationType] = 1 AND [MemberContributionId] IS NOT NULL AND [MemberFineId] IS NULL) OR ([ObligationType] = 2 AND [MemberFineId] IS NOT NULL AND [MemberContributionId] IS NULL AND [PaymentId] IS NULL)");

            migrationBuilder.AddForeignKey(
                name: "FK_ContributionPaymentSubmissions_MemberFines_MemberFineId",
                table: "ContributionPaymentSubmissions",
                column: "MemberFineId",
                principalTable: "MemberFines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContributionPaymentSubmissions_MemberFines_MemberFineId",
                table: "ContributionPaymentSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_ContributionPaymentSubmissions_MemberContributionId",
                table: "ContributionPaymentSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_ContributionPaymentSubmissions_MemberFineId",
                table: "ContributionPaymentSubmissions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PaymentSubmission_Obligation",
                table: "ContributionPaymentSubmissions");

            migrationBuilder.DropColumn(
                name: "MemberFineId",
                table: "ContributionPaymentSubmissions");

            migrationBuilder.DropColumn(
                name: "ObligationType",
                table: "ContributionPaymentSubmissions");

            migrationBuilder.AlterColumn<Guid>(
                name: "MemberContributionId",
                table: "ContributionPaymentSubmissions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContributionPaymentSubmissions_MemberContributionId",
                table: "ContributionPaymentSubmissions",
                column: "MemberContributionId",
                unique: true,
                filter: "[Status] = 1");
        }
    }
}
