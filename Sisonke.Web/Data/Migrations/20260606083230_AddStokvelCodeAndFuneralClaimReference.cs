using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sisonke.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddStokvelCodeAndFuneralClaimReference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Stokvels",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClaimReference",
                table: "FuneralClaims",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Stokvels_Code",
                table: "Stokvels",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_FuneralClaims_ClaimReference",
                table: "FuneralClaims",
                column: "ClaimReference");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Stokvels_Code",
                table: "Stokvels");

            migrationBuilder.DropIndex(
                name: "IX_FuneralClaims_ClaimReference",
                table: "FuneralClaims");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Stokvels");

            migrationBuilder.DropColumn(
                name: "ClaimReference",
                table: "FuneralClaims");
        }
    }
}
