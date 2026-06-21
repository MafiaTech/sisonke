using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sisonke.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBankingAndRotationalPayouts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RotationalPayouts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StokvelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CycleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PayoutMemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PayoutAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
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
                    PaymentReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PaymentMethod = table.Column<int>(type: "int", nullable: true),
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
                        name: "FK_RotationalPayouts_Members_PayoutMemberId",
                        column: x => x.PayoutMemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RotationalPayouts_RotationalContributionCycles_CycleId",
                        column: x => x.CycleId,
                        principalTable: "RotationalContributionCycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RotationalPayouts_Stokvels_StokvelId",
                        column: x => x.StokvelId,
                        principalTable: "Stokvels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StokvelBankingDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StokvelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BankName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    AccountHolderName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    AccountNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    AccountType = table.Column<int>(type: "int", nullable: false),
                    BranchCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    BranchName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    PaymentReferenceFormat = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
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
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RotationalPayouts_CycleId",
                table: "RotationalPayouts",
                column: "CycleId");

            migrationBuilder.CreateIndex(
                name: "IX_RotationalPayouts_CycleId_IsActive",
                table: "RotationalPayouts",
                columns: new[] { "CycleId", "IsActive" },
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_RotationalPayouts_PayoutMemberId",
                table: "RotationalPayouts",
                column: "PayoutMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_RotationalPayouts_StokvelId",
                table: "RotationalPayouts",
                column: "StokvelId");

            migrationBuilder.CreateIndex(
                name: "IX_RotationalPayouts_StokvelId_PayoutStatus",
                table: "RotationalPayouts",
                columns: new[] { "StokvelId", "PayoutStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_StokvelBankingDetails_StokvelId",
                table: "StokvelBankingDetails",
                column: "StokvelId");

            migrationBuilder.CreateIndex(
                name: "IX_StokvelBankingDetails_StokvelId_IsActive_IsPrimary",
                table: "StokvelBankingDetails",
                columns: new[] { "StokvelId", "IsActive", "IsPrimary" },
                unique: true,
                filter: "[IsActive] = 1 AND [IsPrimary] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RotationalPayouts");

            migrationBuilder.DropTable(
                name: "StokvelBankingDetails");

        }
    }
}
