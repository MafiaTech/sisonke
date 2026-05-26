using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sisonke.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddMemberEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Members",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MemberNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    FullName = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    CellphoneNumber = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    EmailAddress = table.Column<string>(type: "TEXT", maxLength: 150, nullable: true),
                    IdNumber = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    JoiningDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    DefaultRole = table.Column<int>(type: "INTEGER", nullable: false),
                    ResidentialArea = table.Column<string>(type: "TEXT", maxLength: 150, nullable: true),
                    IsInCoolingPeriod = table.Column<bool>(type: "INTEGER", nullable: false),
                    CoolingPeriodEndDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Members", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Members_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Beneficiaries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MemberId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FullName = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Relationship = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    IdNumber = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    CellphoneNumber = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    DateOfBirth = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Beneficiaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Beneficiaries_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NextOfKinRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MemberId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FullName = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Relationship = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    CellphoneNumber = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Address = table.Column<string>(type: "TEXT", maxLength: 250, nullable: true),
                    IsPrimary = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NextOfKinRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NextOfKinRecords_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Beneficiaries_MemberId",
                table: "Beneficiaries",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_Members_TenantId",
                table: "Members",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_NextOfKinRecords_MemberId",
                table: "NextOfKinRecords",
                column: "MemberId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Beneficiaries");

            migrationBuilder.DropTable(
                name: "NextOfKinRecords");

            migrationBuilder.DropTable(
                name: "Members");
        }
    }
}
