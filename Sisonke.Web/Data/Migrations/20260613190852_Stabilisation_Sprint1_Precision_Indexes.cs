using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sisonke.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class Stabilisation_Sprint1_Precision_Indexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MemberContributions_TenantId",
                table: "MemberContributions");

            migrationBuilder.DropIndex(
                name: "IX_Meetings_TenantId",
                table: "Meetings");

            migrationBuilder.DropIndex(
                name: "IX_FuneralClaims_TenantId",
                table: "FuneralClaims");

            migrationBuilder.CreateIndex(
                name: "IX_MemberContributions_TenantId_MemberId",
                table: "MemberContributions",
                columns: new[] { "TenantId", "MemberId" });

            migrationBuilder.CreateIndex(
                name: "IX_Meetings_TenantId_MeetingDate",
                table: "Meetings",
                columns: new[] { "TenantId", "MeetingDate" });

            migrationBuilder.CreateIndex(
                name: "IX_FuneralClaims_TenantId_Status",
                table: "FuneralClaims",
                columns: new[] { "TenantId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MemberContributions_TenantId_MemberId",
                table: "MemberContributions");

            migrationBuilder.DropIndex(
                name: "IX_Meetings_TenantId_MeetingDate",
                table: "Meetings");

            migrationBuilder.DropIndex(
                name: "IX_FuneralClaims_TenantId_Status",
                table: "FuneralClaims");

            migrationBuilder.CreateIndex(
                name: "IX_MemberContributions_TenantId",
                table: "MemberContributions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Meetings_TenantId",
                table: "Meetings",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_FuneralClaims_TenantId",
                table: "FuneralClaims",
                column: "TenantId");
        }
    }
}
