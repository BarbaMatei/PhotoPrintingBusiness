using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhotoPrint.API.Migrations
{
    /// <inheritdoc />
    public partial class AddUploadThumbnailPath : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Provider-aware, mirroring the sibling AddOrderIdempotencyKey
            // migration. On Postgres "TEXT" is unbounded (ignores maxLength), so the column would
            // diverge from the runtime Npgsql model (character varying(512)) and the next
            // `ef migrations add` under Npgsql would scaffold a phantom AlterColumn. Emit
            // varchar(512) on Postgres, plain TEXT on SQLite (dev/test — unchanged). Safe to edit
            // in place: no Postgres DB has applied this migration yet, and SQLite ignores maxLength.
            var isNpgsql = migrationBuilder.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL";

            migrationBuilder.AddColumn<string>(
                name: "ThumbnailPath",
                table: "Uploads",
                type: isNpgsql ? "character varying(512)" : "TEXT",
                maxLength: 512,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ThumbnailPath",
                table: "Uploads");
        }
    }
}
