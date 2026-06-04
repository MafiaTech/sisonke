using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sisonke.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddContributionPaymentAudits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContributionPaymentAudits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContributionPaymentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MemberId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StokvelId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Action = table.Column<string>(type: "TEXT", nullable: false),
                    PreviousAmountPaid = table.Column<decimal>(type: "TEXT", nullable: true),
                    NewAmountPaid = table.Column<decimal>(type: "TEXT", nullable: true),
                    PreviousStatus = table.Column<string>(type: "TEXT", nullable: true),
                    NewStatus = table.Column<string>(type: "TEXT", nullable: true),
                    PaymentReference = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CapturedByMemberId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContributionPaymentAudits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContributionPaymentAudits_MemberContributions_ContributionPaymentId",
                        column: x => x.ContributionPaymentId,
                        principalTable: "MemberContributions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContributionPaymentAudits_Members_CapturedByMemberId",
                        column: x => x.CapturedByMemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContributionPaymentAudits_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContributionPaymentAudits_Stokvels_StokvelId",
                        column: x => x.StokvelId,
                        principalTable: "Stokvels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContributionPaymentAudits_CapturedByMemberId",
                table: "ContributionPaymentAudits",
                column: "CapturedByMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_ContributionPaymentAudits_ContributionPaymentId",
                table: "ContributionPaymentAudits",
                column: "ContributionPaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_ContributionPaymentAudits_MemberId",
                table: "ContributionPaymentAudits",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_ContributionPaymentAudits_StokvelId",
                table: "ContributionPaymentAudits",
                column: "StokvelId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContributionPaymentAudits");
        }
    }
}
