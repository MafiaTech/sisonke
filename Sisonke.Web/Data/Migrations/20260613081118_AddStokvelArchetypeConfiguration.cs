using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sisonke.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStokvelArchetypeConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Archetype",
                table: "Stokvels",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<bool>(
                name: "EnableClaims",
                table: "Stokvels",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "EnableDependents",
                table: "Stokvels",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "EnableEducationPayouts",
                table: "Stokvels",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EnableInventory",
                table: "Stokvels",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EnableInvestmentTracking",
                table: "Stokvels",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EnableLending",
                table: "Stokvels",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EnableRotation",
                table: "Stokvels",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EnableSocialEvents",
                table: "Stokvels",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EnableTravelPlanning",
                table: "Stokvels",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Archetype",
                table: "Stokvels");

            migrationBuilder.DropColumn(
                name: "EnableClaims",
                table: "Stokvels");

            migrationBuilder.DropColumn(
                name: "EnableDependents",
                table: "Stokvels");

            migrationBuilder.DropColumn(
                name: "EnableEducationPayouts",
                table: "Stokvels");

            migrationBuilder.DropColumn(
                name: "EnableInventory",
                table: "Stokvels");

            migrationBuilder.DropColumn(
                name: "EnableInvestmentTracking",
                table: "Stokvels");

            migrationBuilder.DropColumn(
                name: "EnableLending",
                table: "Stokvels");

            migrationBuilder.DropColumn(
                name: "EnableRotation",
                table: "Stokvels");

            migrationBuilder.DropColumn(
                name: "EnableSocialEvents",
                table: "Stokvels");

            migrationBuilder.DropColumn(
                name: "EnableTravelPlanning",
                table: "Stokvels");
        }
    }
}
