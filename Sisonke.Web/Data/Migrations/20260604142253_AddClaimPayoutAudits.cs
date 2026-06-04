using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sisonke.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddClaimPayoutAudits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClaimPayoutAudits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FuneralClaimId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MemberId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StokvelId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Action = table.Column<string>(type: "TEXT", nullable: false),
                    PreviousPayoutAmount = table.Column<decimal>(type: "TEXT", nullable: true),
                    NewPayoutAmount = table.Column<decimal>(type: "TEXT", nullable: true),
                    PreviousStatus = table.Column<string>(type: "TEXT", nullable: true),
                    NewStatus = table.Column<string>(type: "TEXT", nullable: true),
                    PayoutReference = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CapturedByMemberId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimPayoutAudits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClaimPayoutAudits_FuneralClaims_FuneralClaimId",
                        column: x => x.FuneralClaimId,
                        principalTable: "FuneralClaims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClaimPayoutAudits_Members_CapturedByMemberId",
                        column: x => x.CapturedByMemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClaimPayoutAudits_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClaimPayoutAudits_Stokvels_StokvelId",
                        column: x => x.StokvelId,
                        principalTable: "Stokvels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClaimPayoutAudits_CapturedByMemberId",
                table: "ClaimPayoutAudits",
                column: "CapturedByMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimPayoutAudits_FuneralClaimId",
                table: "ClaimPayoutAudits",
                column: "FuneralClaimId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimPayoutAudits_MemberId",
                table: "ClaimPayoutAudits",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimPayoutAudits_StokvelId",
                table: "ClaimPayoutAudits",
                column: "StokvelId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClaimPayoutAudits");
        }
    }
}
