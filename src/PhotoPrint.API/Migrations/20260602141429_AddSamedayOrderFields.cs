using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhotoPrint.API.Migrations
{
    /// <inheritdoc />
    public partial class AddSamedayOrderFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Migration scaffold ran against SQLite (dev default), producing TEXT/INTEGER.
            // SQLite never executes this — it uses EnsureCreated from the model directly
            // (see Program.cs). Migrations only run on Postgres, so the column types here
            // are written for Postgres: `text` (capped to 500 chars via maxLength), and
            // `timestamp with time zone` for the DateTimeOffset (the SQLite-only Unix-ms
            // converter does NOT apply on Postgres, so the column must accept a real
            // timestamptz, not int8). Same pattern as AddUploadArchiveFields.
            migrationBuilder.AddColumn<string>(
                name: "AwbLabelUrl",
                table: "Orders",
                type: "text",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastTrackingSyncAt",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AwbLabelUrl",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "LastTrackingSyncAt",
                table: "Orders");
        }
    }
}
