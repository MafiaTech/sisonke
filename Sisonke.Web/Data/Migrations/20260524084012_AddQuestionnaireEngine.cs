using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sisonke.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestionnaireEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConstitutionDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    VersionNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    IsApproved = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConstitutionDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConstitutionDocuments_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QuestionnaireSections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionnaireSections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QuestionnaireQuestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    QuestionnaireSectionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StokvelType = table.Column<int>(type: "INTEGER", nullable: true),
                    QuestionText = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    HelpText = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    QuestionType = table.Column<int>(type: "INTEGER", nullable: false),
                    IsRequired = table.Column<bool>(type: "INTEGER", nullable: false),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionnaireQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestionnaireQuestions_QuestionnaireSections_QuestionnaireSectionId",
                        column: x => x.QuestionnaireSectionId,
                        principalTable: "QuestionnaireSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QuestionnaireOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    QuestionnaireQuestionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OptionText = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    OptionValue = table.Column<string>(type: "TEXT", maxLength: 150, nullable: true),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionnaireOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestionnaireOptions_QuestionnaireQuestions_QuestionnaireQuestionId",
                        column: x => x.QuestionnaireQuestionId,
                        principalTable: "QuestionnaireQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StokvelQuestionnaireAnswers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    QuestionnaireQuestionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AnswerValue = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    AnsweredAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StokvelQuestionnaireAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StokvelQuestionnaireAnswers_QuestionnaireQuestions_QuestionnaireQuestionId",
                        column: x => x.QuestionnaireQuestionId,
                        principalTable: "QuestionnaireQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StokvelQuestionnaireAnswers_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConstitutionDocuments_TenantId",
                table: "ConstitutionDocuments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionnaireOptions_QuestionnaireQuestionId",
                table: "QuestionnaireOptions",
                column: "QuestionnaireQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionnaireQuestions_QuestionnaireSectionId",
                table: "QuestionnaireQuestions",
                column: "QuestionnaireSectionId");

            migrationBuilder.CreateIndex(
                name: "IX_StokvelQuestionnaireAnswers_QuestionnaireQuestionId",
                table: "StokvelQuestionnaireAnswers",
                column: "QuestionnaireQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_StokvelQuestionnaireAnswers_TenantId_QuestionnaireQuestionId",
                table: "StokvelQuestionnaireAnswers",
                columns: new[] { "TenantId", "QuestionnaireQuestionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConstitutionDocuments");

            migrationBuilder.DropTable(
                name: "QuestionnaireOptions");

            migrationBuilder.DropTable(
                name: "StokvelQuestionnaireAnswers");

            migrationBuilder.DropTable(
                name: "QuestionnaireQuestions");

            migrationBuilder.DropTable(
                name: "QuestionnaireSections");
        }
    }
}
