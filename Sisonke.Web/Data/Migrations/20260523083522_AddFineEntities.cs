using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sisonke.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddFineEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FineTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 250, nullable: true),
                    DefaultAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FineTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FineTypes_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MemberFines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MemberId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FineTypeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                    FineDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    PaidDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CapturedByUserId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberFines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MemberFines_FineTypes_FineTypeId",
                        column: x => x.FineTypeId,
                        principalTable: "FineTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MemberFines_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MemberFines_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FineTypes_TenantId",
                table: "FineTypes",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberFines_FineTypeId",
                table: "MemberFines",
                column: "FineTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberFines_MemberId",
                table: "MemberFines",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberFines_TenantId",
                table: "MemberFines",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MemberFines");

            migrationBuilder.DropTable(
                name: "FineTypes");
        }
    }
}
