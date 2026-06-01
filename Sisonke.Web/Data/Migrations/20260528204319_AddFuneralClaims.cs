using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sisonke.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddFuneralClaims : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FuneralClaims",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MemberId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SubjectType = table.Column<int>(type: "INTEGER", nullable: false),
                    DependentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    DeceasedFullName = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    DateOfDeath = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ClaimReason = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ReviewNotes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    IsWaitingPeriodSatisfied = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsMemberStatusEligible = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RejectedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FuneralClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FuneralClaims_MemberDependents_DependentId",
                        column: x => x.DependentId,
                        principalTable: "MemberDependents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FuneralClaims_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FuneralClaims_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FuneralClaimDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FuneralClaimId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DocumentType = table.Column<int>(type: "INTEGER", nullable: false),
                    OriginalFileName = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    StoredFilePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    FileSizeBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FuneralClaimDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FuneralClaimDocuments_FuneralClaims_FuneralClaimId",
                        column: x => x.FuneralClaimId,
                        principalTable: "FuneralClaims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FuneralClaimDocuments_FuneralClaimId",
                table: "FuneralClaimDocuments",
                column: "FuneralClaimId");

            migrationBuilder.CreateIndex(
                name: "IX_FuneralClaims_DependentId",
                table: "FuneralClaims",
                column: "DependentId");

            migrationBuilder.CreateIndex(
                name: "IX_FuneralClaims_MemberId",
                table: "FuneralClaims",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_FuneralClaims_TenantId",
                table: "FuneralClaims",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FuneralClaimDocuments");

            migrationBuilder.DropTable(
                name: "FuneralClaims");
        }
    }
}
