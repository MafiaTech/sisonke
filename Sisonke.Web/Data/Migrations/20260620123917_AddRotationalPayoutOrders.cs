using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sisonke.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRotationalPayoutOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RotationalPayoutOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StokvelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    HasReceivedPayout = table.Column<bool>(type: "bit", nullable: false),
                    LastPayoutDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RotationalPayoutOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RotationalPayoutOrders_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RotationalPayoutOrders_Stokvels_StokvelId",
                        column: x => x.StokvelId,
                        principalTable: "Stokvels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RotationalPayoutOrders_MemberId",
                table: "RotationalPayoutOrders",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_RotationalPayoutOrders_StokvelId",
                table: "RotationalPayoutOrders",
                column: "StokvelId");

            migrationBuilder.CreateIndex(
                name: "IX_RotationalPayoutOrders_StokvelId_IsActive",
                table: "RotationalPayoutOrders",
                columns: new[] { "StokvelId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_RotationalPayoutOrders_StokvelId_MemberId",
                table: "RotationalPayoutOrders",
                columns: new[] { "StokvelId", "MemberId" },
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_RotationalPayoutOrders_StokvelId_Position",
                table: "RotationalPayoutOrders",
                columns: new[] { "StokvelId", "Position" },
                unique: true,
                filter: "[IsActive] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RotationalPayoutOrders");
        }
    }
}
