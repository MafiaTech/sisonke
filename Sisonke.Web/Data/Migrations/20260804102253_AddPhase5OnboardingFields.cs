using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sisonke.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase5OnboardingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RegistrationType",
                table: "Stokvels",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BillingAddress",
                table: "OrganisationSubscriptions",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CardholderName",
                table: "OrganisationSubscriptions",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TermsAcceptedIpAddress",
                table: "OrganisationSubscriptions",
                type: "nvarchar(45)",
                maxLength: 45,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RegistrationType",
                table: "Stokvels");

            migrationBuilder.DropColumn(
                name: "BillingAddress",
                table: "OrganisationSubscriptions");

            migrationBuilder.DropColumn(
                name: "CardholderName",
                table: "OrganisationSubscriptions");

            migrationBuilder.DropColumn(
                name: "TermsAcceptedIpAddress",
                table: "OrganisationSubscriptions");
        }
    }
}
