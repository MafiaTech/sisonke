using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sisonke.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddMeetingVoting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MeetingVotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MeetingId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    VotingMethod = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Result = table.Column<int>(type: "INTEGER", nullable: false),
                    OpenedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingVotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MeetingVotes_Meetings_MeetingId",
                        column: x => x.MeetingId,
                        principalTable: "Meetings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MeetingVoteResponses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MeetingVoteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MemberId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Choice = table.Column<int>(type: "INTEGER", nullable: false),
                    VotedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingVoteResponses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MeetingVoteResponses_MeetingVotes_MeetingVoteId",
                        column: x => x.MeetingVoteId,
                        principalTable: "MeetingVotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MeetingVoteResponses_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingVoteResponses_MeetingVoteId_MemberId",
                table: "MeetingVoteResponses",
                columns: new[] { "MeetingVoteId", "MemberId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MeetingVoteResponses_MemberId",
                table: "MeetingVoteResponses",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingVotes_MeetingId",
                table: "MeetingVotes",
                column: "MeetingId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MeetingVoteResponses");

            migrationBuilder.DropTable(
                name: "MeetingVotes");
        }
    }
}
