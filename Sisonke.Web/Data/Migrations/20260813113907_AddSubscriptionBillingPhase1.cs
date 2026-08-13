using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sisonke.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionBillingPhase1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ChargePurpose",
                table: "SubscriptionPayments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProcessedAt",
                table: "SubscriptionPayments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Provider",
                table: "SubscriptionPayments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderRequestReference",
                table: "SubscriptionPayments",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SettledAt",
                table: "SubscriptionPayments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ProviderAuthorizationCode",
                table: "SubscriptionPaymentMethods",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<DateTime>(
                name: "MandateActivatedAt",
                table: "SubscriptionPaymentMethods",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "MandateCreatedAt",
                table: "SubscriptionPaymentMethods",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MandateStatus",
                table: "SubscriptionPaymentMethods",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "MaskedDisplay",
                table: "SubscriptionPaymentMethods",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentMethodType",
                table: "SubscriptionPaymentMethods",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ProviderMandateReference",
                table: "SubscriptionPaymentMethods",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderPaymentMethodReference",
                table: "SubscriptionPaymentMethods",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TrialOptedIn",
                table: "OrganisationSubscriptions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPayments_Provider_ProviderRequestReference",
                table: "SubscriptionPayments",
                columns: new[] { "Provider", "ProviderRequestReference" });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPayments_Provider_ProviderTransactionId",
                table: "SubscriptionPayments",
                columns: new[] { "Provider", "ProviderTransactionId" });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPaymentMethods_Provider_ProviderMandateReference",
                table: "SubscriptionPaymentMethods",
                columns: new[] { "Provider", "ProviderMandateReference" });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPaymentMethods_Provider_ProviderPaymentMethodReference",
                table: "SubscriptionPaymentMethods",
                columns: new[] { "Provider", "ProviderPaymentMethodReference" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SubscriptionPayments_Provider_ProviderRequestReference",
                table: "SubscriptionPayments");

            migrationBuilder.DropIndex(
                name: "IX_SubscriptionPayments_Provider_ProviderTransactionId",
                table: "SubscriptionPayments");

            migrationBuilder.DropIndex(
                name: "IX_SubscriptionPaymentMethods_Provider_ProviderMandateReference",
                table: "SubscriptionPaymentMethods");

            migrationBuilder.DropIndex(
                name: "IX_SubscriptionPaymentMethods_Provider_ProviderPaymentMethodReference",
                table: "SubscriptionPaymentMethods");

            migrationBuilder.DropColumn(
                name: "ChargePurpose",
                table: "SubscriptionPayments");

            migrationBuilder.DropColumn(
                name: "ProcessedAt",
                table: "SubscriptionPayments");

            migrationBuilder.DropColumn(
                name: "Provider",
                table: "SubscriptionPayments");

            migrationBuilder.DropColumn(
                name: "ProviderRequestReference",
                table: "SubscriptionPayments");

            migrationBuilder.DropColumn(
                name: "SettledAt",
                table: "SubscriptionPayments");

            migrationBuilder.DropColumn(
                name: "MandateActivatedAt",
                table: "SubscriptionPaymentMethods");

            migrationBuilder.DropColumn(
                name: "MandateCreatedAt",
                table: "SubscriptionPaymentMethods");

            migrationBuilder.DropColumn(
                name: "MandateStatus",
                table: "SubscriptionPaymentMethods");

            migrationBuilder.DropColumn(
                name: "MaskedDisplay",
                table: "SubscriptionPaymentMethods");

            migrationBuilder.DropColumn(
                name: "PaymentMethodType",
                table: "SubscriptionPaymentMethods");

            migrationBuilder.DropColumn(
                name: "ProviderMandateReference",
                table: "SubscriptionPaymentMethods");

            migrationBuilder.DropColumn(
                name: "ProviderPaymentMethodReference",
                table: "SubscriptionPaymentMethods");

            migrationBuilder.DropColumn(
                name: "TrialOptedIn",
                table: "OrganisationSubscriptions");

            migrationBuilder.AlterColumn<string>(
                name: "ProviderAuthorizationCode",
                table: "SubscriptionPaymentMethods",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);
        }
    }
}
