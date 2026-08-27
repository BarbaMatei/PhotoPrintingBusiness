using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhotoPrint.API.Migrations
{
    /// <inheritdoc />
    public partial class DropEuPlatescColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EuPlatescRedirectUrl",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "EuPlatescTransactionId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PaymentProcessor",
                table: "Orders");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EuPlatescRedirectUrl",
                table: "Orders",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EuPlatescTransactionId",
                table: "Orders",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentProcessor",
                table: "Orders",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
