using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sisonke.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTrialDunningAndJobLocks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DunningStartedAt",
                table: "OrganisationSubscriptions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "JobExecutionLocks",
                columns: table => new
                {
                    JobName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LockToken = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LockedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LockExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobExecutionLocks", x => x.JobName);
                });

            migrationBuilder.CreateTable(
                name: "TrialReminderSents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganisationSubscriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Bucket = table.Column<int>(type: "int", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrialReminderSents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrialReminderSents_OrganisationSubscriptions_OrganisationSubscriptionId",
                        column: x => x.OrganisationSubscriptionId,
                        principalTable: "OrganisationSubscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrialReminderSents_OrganisationSubscriptionId_Bucket",
                table: "TrialReminderSents",
                columns: new[] { "OrganisationSubscriptionId", "Bucket" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JobExecutionLocks");

            migrationBuilder.DropTable(
                name: "TrialReminderSents");

            migrationBuilder.DropColumn(
                name: "DunningStartedAt",
                table: "OrganisationSubscriptions");
        }
    }
}
