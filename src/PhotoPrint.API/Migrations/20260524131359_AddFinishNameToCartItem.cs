using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhotoPrint.API.Migrations
{
    /// <inheritdoc />
    public partial class AddFinishNameToCartItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FinishName",
                table: "CartItems",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FinishName",
                table: "CartItems");
        }
    }
}
