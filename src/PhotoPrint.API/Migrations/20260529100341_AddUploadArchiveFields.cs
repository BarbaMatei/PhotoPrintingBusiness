using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhotoPrint.API.Migrations
{
    /// <inheritdoc />
    public partial class AddUploadArchiveFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Migration scaffold ran against SQLite (dev default), producing TEXT/INTEGER.
            // SQLite never executes this — it uses EnsureCreated from the model directly
            // (see Program.cs). Migrations only run on Postgres, so the column types here
            // are written for Postgres: TEXT → Postgres `text`; the DateTimeOffset column
            // needs `timestamp with time zone` (the SQLite-only Unix-ms converter does NOT
            // apply on Postgres, so the column must accept a real timestamptz, not int8).
            migrationBuilder.AddColumn<string>(
                name: "LargePreviewPath",
                table: "Uploads",
                type: "text",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "OriginalPurgedAt",
                table: "Uploads",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LargePreviewPath",
                table: "Uploads");

            migrationBuilder.DropColumn(
                name: "OriginalPurgedAt",
                table: "Uploads");
        }
    }
}
