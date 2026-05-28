using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sisonke.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddConstitutionUploadFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "ConstitutionDocuments",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "FileSizeBytes",
                table: "ConstitutionDocuments",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsUploadedDocument",
                table: "ConstitutionDocuments",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OriginalFileName",
                table: "ConstitutionDocuments",
                type: "TEXT",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StoredFilePath",
                table: "ConstitutionDocuments",
                type: "TEXT",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "ConstitutionDocuments");

            migrationBuilder.DropColumn(
                name: "FileSizeBytes",
                table: "ConstitutionDocuments");

            migrationBuilder.DropColumn(
                name: "IsUploadedDocument",
                table: "ConstitutionDocuments");

            migrationBuilder.DropColumn(
                name: "OriginalFileName",
                table: "ConstitutionDocuments");

            migrationBuilder.DropColumn(
                name: "StoredFilePath",
                table: "ConstitutionDocuments");
        }
    }
}
