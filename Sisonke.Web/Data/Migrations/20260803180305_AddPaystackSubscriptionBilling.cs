using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sisonke.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPaystackSubscriptionBilling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProviderPlanCode",
                table: "SubscriptionPlans",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PendingPlanChangeEffectiveAt",
                table: "OrganisationSubscriptions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PendingPlanChangePlanId",
                table: "OrganisationSubscriptions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderEmailToken",
                table: "OrganisationSubscriptions",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "InvoiceNumberCounters",
                columns: table => new
                {
                    Year = table.Column<int>(type: "int", nullable: false),
                    NextNumber = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceNumberCounters", x => x.Year);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationSubscriptions_PendingPlanChangePlanId",
                table: "OrganisationSubscriptions",
                column: "PendingPlanChangePlanId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrganisationSubscriptions_SubscriptionPlans_PendingPlanChangePlanId",
                table: "OrganisationSubscriptions",
                column: "PendingPlanChangePlanId",
                principalTable: "SubscriptionPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrganisationSubscriptions_SubscriptionPlans_PendingPlanChangePlanId",
                table: "OrganisationSubscriptions");

            migrationBuilder.DropTable(
                name: "InvoiceNumberCounters");

            migrationBuilder.DropIndex(
                name: "IX_OrganisationSubscriptions_PendingPlanChangePlanId",
                table: "OrganisationSubscriptions");

            migrationBuilder.DropColumn(
                name: "ProviderPlanCode",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "PendingPlanChangeEffectiveAt",
                table: "OrganisationSubscriptions");

            migrationBuilder.DropColumn(
                name: "PendingPlanChangePlanId",
                table: "OrganisationSubscriptions");

            migrationBuilder.DropColumn(
                name: "ProviderEmailToken",
                table: "OrganisationSubscriptions");
        }
    }
}
