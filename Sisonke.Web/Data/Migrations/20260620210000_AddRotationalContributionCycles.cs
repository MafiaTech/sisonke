using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sisonke.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRotationalContributionCycles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RotationalContributionCycles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StokvelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConfigurationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PayoutOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PayoutMemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CycleNumber = table.Column<int>(type: "int", nullable: false),
                    CycleName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CycleStartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CycleEndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ContributionDueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ScheduledPayoutDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ContributionAmountPerMember = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ExpectedTotalContributionAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ExpectedPayoutAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
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
                        name: "FK_RotationalContributionCycles_Members_PayoutMemberId",
                        column: x => x.PayoutMemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RotationalContributionCycles_RotationalPayoutOrders_PayoutOrderId",
                        column: x => x.PayoutOrderId,
                        principalTable: "RotationalPayoutOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RotationalContributionCycles_RotationalStokvelConfigurations_ConfigurationId",
                        column: x => x.ConfigurationId,
                        principalTable: "RotationalStokvelConfigurations",
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
                name: "IX_RotationalContributionCycles_PayoutOrderId",
                table: "RotationalContributionCycles",
                column: "PayoutOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_RotationalContributionCycles_StokvelId",
                table: "RotationalContributionCycles",
                column: "StokvelId");

            migrationBuilder.CreateIndex(
                name: "IX_RotationalContributionCycles_StokvelId_CycleNumber",
                table: "RotationalContributionCycles",
                columns: new[] { "StokvelId", "CycleNumber" },
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_RotationalContributionCycles_StokvelId_IsActive",
                table: "RotationalContributionCycles",
                columns: new[] { "StokvelId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_RotationalContributionCycles_StokvelId_Status",
                table: "RotationalContributionCycles",
                columns: new[] { "StokvelId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "RotationalContributionCycles");
        }
    }
}
