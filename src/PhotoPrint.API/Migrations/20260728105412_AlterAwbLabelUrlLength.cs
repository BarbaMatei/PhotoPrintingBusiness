using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhotoPrint.API.Migrations
{
    /// <inheritdoc />
    public partial class AlterAwbLabelUrlLength : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SQLite (dev) ignores string length and never runs migrations — the scaffold produced
            // an empty body. This alter only executes on Postgres, so the type is written for
            // Postgres (Npgsql ignores maxLength when an explicit type is set, so the cap lives in
            // the type). Widen AwbLabelUrl 500→2048 to hold long vendor signed URLs; 500 rejected
            // them on the column write after the AWB was billed, looping the retry into a re-bill.
            migrationBuilder.AlterColumn<string>(
                name: "AwbLabelUrl",
                table: "Orders",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "AwbLabelUrl",
                table: "Orders",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2048)",
                oldMaxLength: 2048,
                oldNullable: true);
        }
    }
}
