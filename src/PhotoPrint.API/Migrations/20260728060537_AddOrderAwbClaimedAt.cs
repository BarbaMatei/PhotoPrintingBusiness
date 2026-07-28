using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhotoPrint.API.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderAwbClaimedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Scaffolded against SQLite (dev default) as INTEGER via the Unix-ms converter, but
            // SQLite never runs migrations (EnsureCreated). Migrations only run on Postgres, where
            // the DateTimeOffset must be a real timestamptz — same pattern as AddSamedayOrderFields.
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AwbClaimedAt",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AwbClaimedAt",
                table: "Orders");
        }
    }
}
