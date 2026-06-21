using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sisonke.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRotationalContributionPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RotationalContributionPayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StokvelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CycleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpectedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaymentStatus = table.Column<int>(type: "int", nullable: false),
                    PaymentMethod = table.Column<int>(type: "int", nullable: true),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ConfirmedByTreasurerId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ConfirmedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PenaltyAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
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
                        name: "FK_RotationalContributionPayments_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RotationalContributionPayments_RotationalContributionCycles_CycleId",
                        column: x => x.CycleId,
                        principalTable: "RotationalContributionCycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RotationalContributionPayments_Stokvels_StokvelId",
                        column: x => x.StokvelId,
                        principalTable: "Stokvels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RotationalContributionPayments_ConfirmedByTreasurerId",
                table: "RotationalContributionPayments",
                column: "ConfirmedByTreasurerId");

            migrationBuilder.CreateIndex(
                name: "IX_RotationalContributionPayments_CycleId",
                table: "RotationalContributionPayments",
                column: "CycleId");

            migrationBuilder.CreateIndex(
                name: "IX_RotationalContributionPayments_CycleId_MemberId",
                table: "RotationalContributionPayments",
                columns: new[] { "CycleId", "MemberId" },
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_RotationalContributionPayments_MemberId",
                table: "RotationalContributionPayments",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_RotationalContributionPayments_StokvelId",
                table: "RotationalContributionPayments",
                column: "StokvelId");

            migrationBuilder.CreateIndex(
                name: "IX_RotationalContributionPayments_StokvelId_CycleId",
                table: "RotationalContributionPayments",
                columns: new[] { "StokvelId", "CycleId" });

            migrationBuilder.CreateIndex(
                name: "IX_RotationalContributionPayments_StokvelId_PaymentStatus",
                table: "RotationalContributionPayments",
                columns: new[] { "StokvelId", "PaymentStatus" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "RotationalContributionPayments");
        }
    }
}
