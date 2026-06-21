using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Sisonke.Web.Data;

#nullable disable

namespace Sisonke.Web.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260620200000_AddStokvelSoftDeleteAndAudit")]
    public partial class AddStokvelSoftDeleteAndAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Stokvels",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Stokvels",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Stokvels",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeleteReason",
                table: "Stokvels",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Stokvels",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Stokvels",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "IsDeleted",    table: "Stokvels");
            migrationBuilder.DropColumn(name: "DeletedAt",    table: "Stokvels");
            migrationBuilder.DropColumn(name: "DeletedBy",    table: "Stokvels");
            migrationBuilder.DropColumn(name: "DeleteReason", table: "Stokvels");
            migrationBuilder.DropColumn(name: "UpdatedAt",    table: "Stokvels");
            migrationBuilder.DropColumn(name: "UpdatedBy",    table: "Stokvels");
        }
    }
}
