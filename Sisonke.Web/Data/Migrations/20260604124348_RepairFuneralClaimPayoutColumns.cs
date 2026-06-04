using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace Sisonke.Web.Migrations
{
    /// <inheritdoc />
    public partial class RepairFuneralClaimPayoutColumns : Migration
    {
        /// <inheritdoc />
       protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.AddColumn<decimal>(
        name: "PayoutAmount",
        table: "FuneralClaims",
        type: "TEXT",
        nullable: true);

    migrationBuilder.AddColumn<DateTime>(
        name: "PayoutPaidAt",
        table: "FuneralClaims",
        type: "TEXT",
        nullable: true);

    migrationBuilder.AddColumn<string>(
        name: "PayoutReference",
        table: "FuneralClaims",
        type: "TEXT",
        nullable: true);

    migrationBuilder.AddColumn<string>(
        name: "PayoutNotes",
        table: "FuneralClaims",
        type: "TEXT",
        nullable: true);

    migrationBuilder.AddColumn<Guid>(
        name: "PayoutCapturedByMemberId",
        table: "FuneralClaims",
        type: "TEXT",
        nullable: true);
}

protected override void Down(MigrationBuilder migrationBuilder)
{
    migrationBuilder.DropColumn(
        name: "PayoutAmount",
        table: "FuneralClaims");

    migrationBuilder.DropColumn(
        name: "PayoutPaidAt",
        table: "FuneralClaims");

    migrationBuilder.DropColumn(
        name: "PayoutReference",
        table: "FuneralClaims");

    migrationBuilder.DropColumn(
        name: "PayoutNotes",
        table: "FuneralClaims");

    migrationBuilder.DropColumn(
        name: "PayoutCapturedByMemberId",
        table: "FuneralClaims");
}
    }
}
