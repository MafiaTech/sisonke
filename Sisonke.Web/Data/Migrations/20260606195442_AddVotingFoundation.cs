using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sisonke.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddVotingFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VoteMotions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    StokvelId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MeetingId = table.Column<Guid>(type: "TEXT", nullable: true),
                    AgendaItemId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    VoteType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    IsAnonymous = table.Column<bool>(type: "INTEGER", nullable: false),
                    OpensAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ClosesAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedByMemberId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ClosedByMemberId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ResultSummary = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    DecisionOutcome = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoteMotions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VoteMotions_MeetingAgendaItems_AgendaItemId",
                        column: x => x.AgendaItemId,
                        principalTable: "MeetingAgendaItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VoteMotions_Meetings_MeetingId",
                        column: x => x.MeetingId,
                        principalTable: "Meetings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VoteMotions_Stokvels_StokvelId",
                        column: x => x.StokvelId,
                        principalTable: "Stokvels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VoteOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    VoteMotionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OptionText = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoteOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VoteOptions_VoteMotions_VoteMotionId",
                        column: x => x.VoteMotionId,
                        principalTable: "VoteMotions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MemberVotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    VoteMotionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MemberId = table.Column<Guid>(type: "TEXT", nullable: false),
                    VoteOptionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    VotedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberVotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MemberVotes_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MemberVotes_VoteMotions_VoteMotionId",
                        column: x => x.VoteMotionId,
                        principalTable: "VoteMotions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MemberVotes_VoteOptions_VoteOptionId",
                        column: x => x.VoteOptionId,
                        principalTable: "VoteOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MemberVotes_MemberId",
                table: "MemberVotes",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberVotes_VoteMotionId_MemberId",
                table: "MemberVotes",
                columns: new[] { "VoteMotionId", "MemberId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MemberVotes_VoteOptionId",
                table: "MemberVotes",
                column: "VoteOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_VoteMotions_AgendaItemId",
                table: "VoteMotions",
                column: "AgendaItemId");

            migrationBuilder.CreateIndex(
                name: "IX_VoteMotions_MeetingId",
                table: "VoteMotions",
                column: "MeetingId");

            migrationBuilder.CreateIndex(
                name: "IX_VoteMotions_StokvelId",
                table: "VoteMotions",
                column: "StokvelId");

            migrationBuilder.CreateIndex(
                name: "IX_VoteOptions_VoteMotionId",
                table: "VoteOptions",
                column: "VoteMotionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MemberVotes");

            migrationBuilder.DropTable(
                name: "VoteOptions");

            migrationBuilder.DropTable(
                name: "VoteMotions");
        }
    }
}
