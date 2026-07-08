using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sisonke.Web.Data.Migrations
{
    public partial class RestoreRotationalLoanEntities : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MemberLoans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StokvelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ApprovedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    RepaymentMonths = table.Column<int>(type: "int", nullable: false),
                    MonthlyRepaymentAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalRepayableAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OutstandingBalance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
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
                    DisbursementMethod = table.Column<int>(type: "int", nullable: true),
                    DisbursementReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
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
                });

            migrationBuilder.CreateIndex(
                name: "IX_MemberLoans_MemberId",
                table: "MemberLoans",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberLoans_StokvelId",
                table: "MemberLoans",
                column: "StokvelId");

            migrationBuilder.CreateTable(
                name: "MemberLoanRepayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LoanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StokvelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpectedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FineAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaymentStatus = table.Column<int>(type: "int", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaymentMethod = table.Column<int>(type: "int", nullable: true),
                    PaymentReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ConfirmedByTreasurerId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ConfirmedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MemberLoanRepayments_LoanId",
                table: "MemberLoanRepayments",
                column: "LoanId");

            migrationBuilder.CreateTable(
                name: "MemberSurplusWallets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StokvelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AvailableBalance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalCredits = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalWithdrawals = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
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

            migrationBuilder.CreateIndex(
                name: "IX_MemberSurplusWallets_MemberId",
                table: "MemberSurplusWallets",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberSurplusWallets_StokvelId_MemberId",
                table: "MemberSurplusWallets",
                columns: new[] { "StokvelId", "MemberId" },
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateTable(
                name: "MemberSurplusWalletTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WalletId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StokvelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransactionType = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BalanceAfterTransaction = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
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
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MemberSurplusWalletTransactions_WalletId",
                table: "MemberSurplusWalletTransactions",
                column: "WalletId");

            migrationBuilder.CreateTable(
                name: "MemberSurplusWithdrawalRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WalletId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StokvelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
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
                    PaymentReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
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
                        name: "FK_MemberSurplusWithdrawalRequests_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MemberSurplusWithdrawalRequests_MemberSurplusWallets_WalletId",
                        column: x => x.WalletId,
                        principalTable: "MemberSurplusWallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MemberSurplusWithdrawalRequests_MemberId",
                table: "MemberSurplusWithdrawalRequests",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberSurplusWithdrawalRequests_StokvelId",
                table: "MemberSurplusWithdrawalRequests",
                column: "StokvelId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberSurplusWithdrawalRequests_WalletId",
                table: "MemberSurplusWithdrawalRequests",
                column: "WalletId");

            migrationBuilder.CreateTable(
                name: "RotationalStokvelConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StokvelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContributionAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ContributionFrequency = table.Column<int>(type: "int", nullable: false),
                    ContributionDueDay = table.Column<int>(type: "int", nullable: false),
                    PayoutAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PayoutFrequency = table.Column<int>(type: "int", nullable: false),
                    RotationStartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RotationOrderMethod = table.Column<int>(type: "int", nullable: false),
                    AllowPayoutTurnSwap = table.Column<bool>(type: "bit", nullable: false),
                    LatePenaltyType = table.Column<int>(type: "int", nullable: false),
                    LatePenaltyAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    GracePeriodDays = table.Column<int>(type: "int", nullable: false),
                    MinimumBalanceBeforePayout = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MissedContributionBlocksPayout = table.Column<bool>(type: "bit", nullable: false),
                    TreasurerConfirmationRequired = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RotationalStokvelConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RotationalStokvelConfigurations_Stokvels_StokvelId",
                        column: x => x.StokvelId,
                        principalTable: "Stokvels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RotationalStokvelConfigurations_StokvelId",
                table: "RotationalStokvelConfigurations",
                column: "StokvelId");

            migrationBuilder.CreateIndex(
                name: "IX_RotationalStokvelConfigurations_StokvelId_IsActive",
                table: "RotationalStokvelConfigurations",
                columns: new[] { "StokvelId", "IsActive" });

            migrationBuilder.CreateTable(
                name: "RotationalContributionCycles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StokvelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConfigurationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CycleNumber = table.Column<int>(type: "int", nullable: false),
                    CycleName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CycleStartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CycleEndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ContributionDueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ContributionAmountPerMember = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ExpectedTotalContributionAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PayoutOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PayoutMemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpectedPayoutAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ScheduledPayoutDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RotationalContributionCycles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RotationalContributionCycles_RotationalStokvelConfigurations_ConfigurationId",
                        column: x => x.ConfigurationId,
                        principalTable: "RotationalStokvelConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RotationalContributionCycles_Members_PayoutMemberId",
                        column: x => x.PayoutMemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RotationalContributionCycles_Stokvels_StokvelId",
                        column: x => x.StokvelId,
                        principalTable: "Stokvels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RotationalContributionCycles_ConfigurationId",
                table: "RotationalContributionCycles",
                column: "ConfigurationId");

            migrationBuilder.CreateIndex(
                name: "IX_RotationalContributionCycles_PayoutMemberId",
                table: "RotationalContributionCycles",
                column: "PayoutMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_RotationalContributionCycles_StokvelId",
                table: "RotationalContributionCycles",
                column: "StokvelId");

            migrationBuilder.CreateTable(
                name: "RotationalContributionPayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CycleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StokvelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpectedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PenaltyAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaymentStatus = table.Column<int>(type: "int", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaymentMethod = table.Column<int>(type: "int", nullable: true),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ConfirmedByTreasurerId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ConfirmedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RotationalContributionPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RotationalContributionPayments_RotationalContributionCycles_CycleId",
                        column: x => x.CycleId,
                        principalTable: "RotationalContributionCycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RotationalContributionPayments_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RotationalContributionPayments_CycleId",
                table: "RotationalContributionPayments",
                column: "CycleId");

            migrationBuilder.CreateIndex(
                name: "IX_RotationalContributionPayments_MemberId",
                table: "RotationalContributionPayments",
                column: "MemberId");

            migrationBuilder.CreateTable(
                name: "RotationalPayouts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CycleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StokvelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PayoutMemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PayoutAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PayoutStatus = table.Column<int>(type: "int", nullable: false),
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
                    PaymentReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RotationalPayouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RotationalPayouts_RotationalContributionCycles_CycleId",
                        column: x => x.CycleId,
                        principalTable: "RotationalContributionCycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RotationalPayouts_Members_PayoutMemberId",
                        column: x => x.PayoutMemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RotationalPayouts_CycleId",
                table: "RotationalPayouts",
                column: "CycleId");

            migrationBuilder.CreateIndex(
                name: "IX_RotationalPayouts_PayoutMemberId",
                table: "RotationalPayouts",
                column: "PayoutMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_RotationalPayouts_StokvelId",
                table: "RotationalPayouts",
                column: "StokvelId");

            migrationBuilder.CreateTable(
                name: "StokvelBankingDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StokvelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BankName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AccountHolderName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    AccountNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AccountType = table.Column<int>(type: "int", nullable: false),
                    BranchCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    BranchName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PaymentReferenceFormat = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StokvelBankingDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StokvelBankingDetails_Stokvels_StokvelId",
                        column: x => x.StokvelId,
                        principalTable: "Stokvels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StokvelBankingDetails_StokvelId",
                table: "StokvelBankingDetails",
                column: "StokvelId");

            migrationBuilder.CreateTable(
                name: "StokvelLoanConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StokvelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LoansEnabled = table.Column<bool>(type: "bit", nullable: false),
                    MinLoanAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MaxLoanAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MaxRepaymentMonths = table.Column<int>(type: "int", nullable: false),
                    DefaultRepaymentMonths = table.Column<int>(type: "int", nullable: false),
                    LoanInterestType = table.Column<int>(type: "int", nullable: false),
                    LoanInterestRate = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    LateRepaymentFineType = table.Column<int>(type: "int", nullable: false),
                    LateRepaymentFineAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
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
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StokvelLoanConfigurations_StokvelId",
                table: "StokvelLoanConfigurations",
                column: "StokvelId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StokvelLoanConfigurations");

            migrationBuilder.DropTable(
                name: "StokvelBankingDetails");

            migrationBuilder.DropTable(
                name: "RotationalPayouts");

            migrationBuilder.DropTable(
                name: "RotationalContributionPayments");

            migrationBuilder.DropTable(
                name: "RotationalContributionCycles");

            migrationBuilder.DropTable(
                name: "RotationalStokvelConfigurations");

            migrationBuilder.DropTable(
                name: "MemberSurplusWithdrawalRequests");

            migrationBuilder.DropTable(
                name: "MemberSurplusWalletTransactions");

            migrationBuilder.DropTable(
                name: "MemberSurplusWallets");

            migrationBuilder.DropTable(
                name: "MemberLoanRepayments");

            migrationBuilder.DropTable(
                name: "MemberLoans");
        }
    }
}
