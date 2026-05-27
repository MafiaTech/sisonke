using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sisonke.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddConstitutionWizardAnswers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConstitutionWizardAnswers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    QuestionKey = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    AnswerValue = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    AnsweredAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConstitutionWizardAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConstitutionWizardAnswers_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConstitutionWizardAnswers_TenantId_QuestionKey",
                table: "ConstitutionWizardAnswers",
                columns: new[] { "TenantId", "QuestionKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConstitutionWizardAnswers");
        }
    }
}
