using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Sisonke.Web.Data;

#nullable disable

namespace Sisonke.Web.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260704181543_AddRotationalLoanMechanics")]
    public partial class AddRotationalLoanMechanics : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "EarlyPayoutDiscountRatePercent",
                table: "StokvelLoanConfigurations",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "EarlyPayoutLoansEnabled",
                table: "StokvelLoanConfigurations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "RequiredGuarantorCount",
                table: "StokvelLoanConfigurations",
                type: "int",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<bool>(
                name: "SurplusBackedLoansEnabled",
                table: "StokvelLoanConfigurations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "SurplusEquityLoanMultiplier",
                table: "StokvelLoanConfigurations",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<decimal>(
                name: "CoreSavingsBalance",
                table: "MemberSurplusWallets",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "LockedSurplusEquityBalance",
                table: "MemberSurplusWallets",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SurplusEquityBalance",
                table: "MemberSurplusWallets",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CollateralLockedAmount",
                table: "MemberLoans",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "CollateralLockedAt",
                table: "MemberLoans",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CollateralUnlockedAt",
                table: "MemberLoans",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CollateralWalletId",
                table: "MemberLoans",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EarlyPayoutDiscountAmount",
                table: "MemberLoans",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "EarlyPayoutDiscountRatePercent",
                table: "MemberLoans",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "EarlyPayoutGrossAmount",
                table: "MemberLoans",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "EarlyPayoutNetDisbursedAmount",
                table: "MemberLoans",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "LoanType",
                table: "MemberLoans",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<Guid>(
                name: "OriginalContributionCycleId",
                table: "MemberLoans",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OriginalPayoutOrderId",
                table: "MemberLoans",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MemberLoanGuarantors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LoanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GuarantorMemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RespondedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RequestedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    RespondedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(1000)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberLoanGuarantors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MemberLoanGuarantors_MemberLoans_LoanId",
                        column: x => x.LoanId,
                        principalTable: "MemberLoans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MemberLoanGuarantors_Members_GuarantorMemberId",
                        column: x => x.GuarantorMemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StokvelReserveTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StokvelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberLoanId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TransactionType = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StokvelReserveTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StokvelReserveTransactions_MemberLoans_MemberLoanId",
                        column: x => x.MemberLoanId,
                        principalTable: "MemberLoans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StokvelReserveTransactions_Stokvels_StokvelId",
                        column: x => x.StokvelId,
                        principalTable: "Stokvels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MemberLoans_CollateralWalletId",
                table: "MemberLoans",
                column: "CollateralWalletId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberLoans_OriginalContributionCycleId",
                table: "MemberLoans",
                column: "OriginalContributionCycleId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberLoans_OriginalPayoutOrderId",
                table: "MemberLoans",
                column: "OriginalPayoutOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberLoanGuarantors_GuarantorMemberId",
                table: "MemberLoanGuarantors",
                column: "GuarantorMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberLoanGuarantors_LoanId_GuarantorMemberId",
                table: "MemberLoanGuarantors",
                columns: ["LoanId", "GuarantorMemberId"],
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StokvelReserveTransactions_MemberLoanId",
                table: "StokvelReserveTransactions",
                column: "MemberLoanId");

            migrationBuilder.CreateIndex(
                name: "IX_StokvelReserveTransactions_StokvelId",
                table: "StokvelReserveTransactions",
                column: "StokvelId");

            migrationBuilder.AddForeignKey(
                name: "FK_MemberLoans_MemberSurplusWallets_CollateralWalletId",
                table: "MemberLoans",
                column: "CollateralWalletId",
                principalTable: "MemberSurplusWallets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MemberLoans_RotationalContributionCycles_OriginalContributionCycleId",
                table: "MemberLoans",
                column: "OriginalContributionCycleId",
                principalTable: "RotationalContributionCycles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MemberLoans_RotationalPayoutOrders_OriginalPayoutOrderId",
                table: "MemberLoans",
                column: "OriginalPayoutOrderId",
                principalTable: "RotationalPayoutOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MemberLoans_MemberSurplusWallets_CollateralWalletId",
                table: "MemberLoans");

            migrationBuilder.DropForeignKey(
                name: "FK_MemberLoans_RotationalContributionCycles_OriginalContributionCycleId",
                table: "MemberLoans");

            migrationBuilder.DropForeignKey(
                name: "FK_MemberLoans_RotationalPayoutOrders_OriginalPayoutOrderId",
                table: "MemberLoans");

            migrationBuilder.DropTable(
                name: "MemberLoanGuarantors");

            migrationBuilder.DropTable(
                name: "StokvelReserveTransactions");

            migrationBuilder.DropIndex(
                name: "IX_MemberLoans_CollateralWalletId",
                table: "MemberLoans");

            migrationBuilder.DropIndex(
                name: "IX_MemberLoans_OriginalContributionCycleId",
                table: "MemberLoans");

            migrationBuilder.DropIndex(
                name: "IX_MemberLoans_OriginalPayoutOrderId",
                table: "MemberLoans");

            migrationBuilder.DropColumn(
                name: "EarlyPayoutDiscountRatePercent",
                table: "StokvelLoanConfigurations");

            migrationBuilder.DropColumn(
                name: "EarlyPayoutLoansEnabled",
                table: "StokvelLoanConfigurations");

            migrationBuilder.DropColumn(
                name: "RequiredGuarantorCount",
                table: "StokvelLoanConfigurations");

            migrationBuilder.DropColumn(
                name: "SurplusBackedLoansEnabled",
                table: "StokvelLoanConfigurations");

            migrationBuilder.DropColumn(
                name: "SurplusEquityLoanMultiplier",
                table: "StokvelLoanConfigurations");

            migrationBuilder.DropColumn(
                name: "CoreSavingsBalance",
                table: "MemberSurplusWallets");

            migrationBuilder.DropColumn(
                name: "LockedSurplusEquityBalance",
                table: "MemberSurplusWallets");

            migrationBuilder.DropColumn(
                name: "SurplusEquityBalance",
                table: "MemberSurplusWallets");

            migrationBuilder.DropColumn(
                name: "CollateralLockedAmount",
                table: "MemberLoans");

            migrationBuilder.DropColumn(
                name: "CollateralLockedAt",
                table: "MemberLoans");

            migrationBuilder.DropColumn(
                name: "CollateralUnlockedAt",
                table: "MemberLoans");

            migrationBuilder.DropColumn(
                name: "CollateralWalletId",
                table: "MemberLoans");

            migrationBuilder.DropColumn(
                name: "EarlyPayoutDiscountAmount",
                table: "MemberLoans");

            migrationBuilder.DropColumn(
                name: "EarlyPayoutDiscountRatePercent",
                table: "MemberLoans");

            migrationBuilder.DropColumn(
                name: "EarlyPayoutGrossAmount",
                table: "MemberLoans");

            migrationBuilder.DropColumn(
                name: "EarlyPayoutNetDisbursedAmount",
                table: "MemberLoans");

            migrationBuilder.DropColumn(
                name: "LoanType",
                table: "MemberLoans");

            migrationBuilder.DropColumn(
                name: "OriginalContributionCycleId",
                table: "MemberLoans");

            migrationBuilder.DropColumn(
                name: "OriginalPayoutOrderId",
                table: "MemberLoans");
        }
    }
}
