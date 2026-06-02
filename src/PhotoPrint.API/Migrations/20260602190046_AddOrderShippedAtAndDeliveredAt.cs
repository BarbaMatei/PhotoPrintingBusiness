using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhotoPrint.API.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderShippedAtAndDeliveredAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Migration scaffold ran against SQLite (dev default), producing
            // INTEGER (Unix-ms via the SQLite-only converter). SQLite never
            // executes this — it uses EnsureCreated from the model directly
            // (see Program.cs). Migrations only run on Postgres, so the column
            // types here are written for Postgres: `timestamp with time zone`
            // for the real DateTimeOffset columns. Same pattern as
            // AddUploadArchiveFields and AddSamedayOrderFields.
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ShippedAt",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeliveredAt",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveredAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippedAt",
                table: "Orders");
        }
    }
}
