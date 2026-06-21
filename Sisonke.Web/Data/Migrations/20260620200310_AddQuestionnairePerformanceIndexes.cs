using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Sisonke.Web.Data;

#nullable disable

namespace Sisonke.Web.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260620200310_AddQuestionnairePerformanceIndexes")]
    public partial class AddQuestionnairePerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_QuestionnaireQuestions_StokvelType",
                table: "QuestionnaireQuestions",
                column: "StokvelType");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionnaireQuestions_IsActive",
                table: "QuestionnaireQuestions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionnaireQuestions_QuestionnaireSectionId_StokvelType_IsActive",
                table: "QuestionnaireQuestions",
                columns: new[] { "QuestionnaireSectionId", "StokvelType", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_QuestionnaireQuestions_QuestionnaireSectionId_StokvelType_IsActive",
                table: "QuestionnaireQuestions");

            migrationBuilder.DropIndex(
                name: "IX_QuestionnaireQuestions_IsActive",
                table: "QuestionnaireQuestions");

            migrationBuilder.DropIndex(
                name: "IX_QuestionnaireQuestions_StokvelType",
                table: "QuestionnaireQuestions");
        }
    }
}
