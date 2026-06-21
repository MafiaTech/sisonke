using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sisonke.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLoansAndSurplusWallets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MemberLoans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StokvelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ApprovedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    RepaymentMonths = table.Column<int>(type: "int", nullable: false),
                    MonthlyRepaymentAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalRepayableAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OutstandingBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LoanStatus = table.Column<int>(type: "int", nullable: false),
                    RequestReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RequestedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ApprovedByChairpersonId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectedByChairpersonId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    RejectedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DisbursedByTreasurerId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    DisbursedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DisbursementReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DisbursementMethod = table.Column<int>(type: "int", nullable: true),
                    DueStartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpectedFinalPaymentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FullyRepaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NextEligibleLoanDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberLoans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MemberLoans_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MemberLoans_Stokvels_StokvelId",
                        column: x => x.StokvelId,
                        principalTable: "Stokvels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MemberSurplusWallets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StokvelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AvailableBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalCredits = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalWithdrawals = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberSurplusWallets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MemberSurplusWallets_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StokvelLoanConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StokvelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LoansEnabled = table.Column<bool>(type: "bit", nullable: false),
                    MaxLoanAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MinLoanAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MaxRepaymentMonths = table.Column<int>(type: "int", nullable: false),
                    DefaultRepaymentMonths = table.Column<int>(type: "int", nullable: false),
                    LoanInterestType = table.Column<int>(type: "int", nullable: false),
                    LoanInterestRate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LateRepaymentFineType = table.Column<int>(type: "int", nullable: false),
                    LateRepaymentFineAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    GracePeriodDays = table.Column<int>(type: "int", nullable: false),
                    FreezePeriodAfterFullRepaymentDays = table.Column<int>(type: "int", nullable: false),
                    RequireChairpersonApproval = table.Column<bool>(type: "bit", nullable: false),
                    RequireTreasurerDisbursementConfirmation = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StokvelLoanConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StokvelLoanConfigurations_Stokvels_StokvelId",
                        column: x => x.StokvelId,
                        principalTable: "Stokvels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MemberLoanRepayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StokvelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LoanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpectedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaymentStatus = table.Column<int>(type: "int", nullable: false),
                    PaymentMethod = table.Column<int>(type: "int", nullable: true),
                    PaymentReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FineAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ConfirmedByTreasurerId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ConfirmedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberLoanRepayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MemberLoanRepayments_MemberLoans_LoanId",
                        column: x => x.LoanId,
                        principalTable: "MemberLoans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MemberLoanRepayments_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MemberSurplusWalletTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StokvelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WalletId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransactionType = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BalanceAfterTransaction = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SourceType = table.Column<int>(type: "int", nullable: false),
                    SourceReferenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberSurplusWalletTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MemberSurplusWalletTransactions_MemberSurplusWallets_WalletId",
                        column: x => x.WalletId,
                        principalTable: "MemberSurplusWallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MemberSurplusWithdrawalRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StokvelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WalletId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    WithdrawalStatus = table.Column<int>(type: "int", nullable: false),
                    RequestReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RequestedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ApprovedByChairpersonId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectedByChairpersonId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    RejectedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PaidByTreasurerId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaymentMethod = table.Column<int>(type: "int", nullable: true),
                    PaymentReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberSurplusWithdrawalRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MemberSurplusWithdrawalRequests_MemberSurplusWallets_WalletId",
                        column: x => x.WalletId,
                        principalTable: "MemberSurplusWallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MemberSurplusWithdrawalRequests_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MemberLoanRepayments_LoanId",
                table: "MemberLoanRepayments",
                column: "LoanId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberLoanRepayments_LoanId_DueDate",
                table: "MemberLoanRepayments",
                columns: new[] { "LoanId", "DueDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MemberLoanRepayments_MemberId",
                table: "MemberLoanRepayments",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberLoanRepayments_StokvelId",
                table: "MemberLoanRepayments",
                column: "StokvelId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberLoanRepayments_StokvelId_PaymentStatus",
                table: "MemberLoanRepayments",
                columns: new[] { "StokvelId", "PaymentStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_MemberLoans_MemberId",
                table: "MemberLoans",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberLoans_StokvelId",
                table: "MemberLoans",
                column: "StokvelId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberLoans_StokvelId_LoanStatus",
                table: "MemberLoans",
                columns: new[] { "StokvelId", "LoanStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_MemberLoans_StokvelId_MemberId",
                table: "MemberLoans",
                columns: new[] { "StokvelId", "MemberId" },
                unique: true,
                filter: "[IsActive] = 1 AND [LoanStatus] IN (2, 3, 4, 6, 7, 9)");

            migrationBuilder.CreateIndex(
                name: "IX_MemberLoans_StokvelId_MemberId_IsActive",
                table: "MemberLoans",
                columns: new[] { "StokvelId", "MemberId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_MemberSurplusWallets_MemberId",
                table: "MemberSurplusWallets",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberSurplusWallets_StokvelId_MemberId_IsActive",
                table: "MemberSurplusWallets",
                columns: new[] { "StokvelId", "MemberId", "IsActive" },
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_MemberSurplusWalletTransactions_StokvelId_MemberId_CreatedAt",
                table: "MemberSurplusWalletTransactions",
                columns: new[] { "StokvelId", "MemberId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MemberSurplusWalletTransactions_WalletId",
                table: "MemberSurplusWalletTransactions",
                column: "WalletId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberSurplusWalletTransactions_WalletId_SourceType_SourceReferenceId_TransactionType",
                table: "MemberSurplusWalletTransactions",
                columns: new[] { "WalletId", "SourceType", "SourceReferenceId", "TransactionType" },
                unique: true,
                filter: "[SourceReferenceId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MemberSurplusWithdrawalRequests_MemberId",
                table: "MemberSurplusWithdrawalRequests",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberSurplusWithdrawalRequests_StokvelId",
                table: "MemberSurplusWithdrawalRequests",
                column: "StokvelId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberSurplusWithdrawalRequests_StokvelId_MemberId",
                table: "MemberSurplusWithdrawalRequests",
                columns: new[] { "StokvelId", "MemberId" },
                unique: true,
                filter: "[IsActive] = 1 AND [WithdrawalStatus] IN (1, 2, 3, 5)");

            migrationBuilder.CreateIndex(
                name: "IX_MemberSurplusWithdrawalRequests_StokvelId_MemberId_IsActive",
                table: "MemberSurplusWithdrawalRequests",
                columns: new[] { "StokvelId", "MemberId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_MemberSurplusWithdrawalRequests_StokvelId_WithdrawalStatus",
                table: "MemberSurplusWithdrawalRequests",
                columns: new[] { "StokvelId", "WithdrawalStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_MemberSurplusWithdrawalRequests_WalletId",
                table: "MemberSurplusWithdrawalRequests",
                column: "WalletId");

            migrationBuilder.CreateIndex(
                name: "IX_StokvelLoanConfigurations_StokvelId",
                table: "StokvelLoanConfigurations",
                column: "StokvelId");

            migrationBuilder.CreateIndex(
                name: "IX_StokvelLoanConfigurations_StokvelId_IsActive",
                table: "StokvelLoanConfigurations",
                columns: new[] { "StokvelId", "IsActive" },
                unique: true,
                filter: "[IsActive] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MemberLoanRepayments");

            migrationBuilder.DropTable(
                name: "MemberSurplusWalletTransactions");

            migrationBuilder.DropTable(
                name: "MemberSurplusWithdrawalRequests");

            migrationBuilder.DropTable(
                name: "StokvelLoanConfigurations");

            migrationBuilder.DropTable(
                name: "MemberLoans");

            migrationBuilder.DropTable(
                name: "MemberSurplusWallets");
        }
    }
}
