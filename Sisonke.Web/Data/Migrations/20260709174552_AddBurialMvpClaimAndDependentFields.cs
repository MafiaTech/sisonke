using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Sisonke.Web.Data;

#nullable disable

namespace Sisonke.Web.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260709174552_AddBurialMvpClaimAndDependentFields")]
    public partial class AddBurialMvpClaimAndDependentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CoverageStatus",
                table: "MemberDependents",
                type: "int",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<int>(
                name: "ClaimType",
                table: "FuneralClaims",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<Guid>(
                name: "StokvelId",
                table: "FuneralClaims",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FuneralClaims_StokvelId",
                table: "FuneralClaims",
                column: "StokvelId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FuneralClaims_StokvelId",
                table: "FuneralClaims");

            migrationBuilder.DropColumn(
                name: "CoverageStatus",
                table: "MemberDependents");

            migrationBuilder.DropColumn(
                name: "ClaimType",
                table: "FuneralClaims");

            migrationBuilder.DropColumn(
                name: "StokvelId",
                table: "FuneralClaims");
        }
    }
}
