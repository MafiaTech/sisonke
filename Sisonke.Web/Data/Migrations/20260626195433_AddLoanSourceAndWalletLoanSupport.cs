using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sisonke.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLoanSourceAndWalletLoanSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LoanSource",
                table: "MemberLoans",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<Guid>(
                name: "WalletId",
                table: "MemberLoans",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MemberLoans_WalletId",
                table: "MemberLoans",
                column: "WalletId");

            migrationBuilder.AddForeignKey(
                name: "FK_MemberLoans_MemberSurplusWallets_WalletId",
                table: "MemberLoans",
                column: "WalletId",
                principalTable: "MemberSurplusWallets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MemberLoans_MemberSurplusWallets_WalletId",
                table: "MemberLoans");

            migrationBuilder.DropIndex(
                name: "IX_MemberLoans_WalletId",
                table: "MemberLoans");

            migrationBuilder.DropColumn(
                name: "LoanSource",
                table: "MemberLoans");

            migrationBuilder.DropColumn(
                name: "WalletId",
                table: "MemberLoans");
        }
    }
}
