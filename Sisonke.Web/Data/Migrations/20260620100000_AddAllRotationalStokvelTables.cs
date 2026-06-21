using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sisonke.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAllRotationalStokvelTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // RotationalStokvelSettings, RotationOrders, RotationCycles, CycleContributions,
            // and CyclePayouts were created by AddRotationalStokvelMvp.
            // This migration adds the RotationalStokvelConfigurations table.

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "RotationalStokvelConfigurations");
        }
    }
}
