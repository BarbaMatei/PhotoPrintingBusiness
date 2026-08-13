using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhotoPrint.API.Migrations
{
    /// <inheritdoc />
    public partial class MakeInvoiceOrderIdUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_invoices_order_id",
                table: "Invoices");

            migrationBuilder.CreateIndex(
                name: "ix_invoices_order_id",
                table: "Invoices",
                column: "OrderId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_invoices_order_id",
                table: "Invoices");

            migrationBuilder.CreateIndex(
                name: "ix_invoices_order_id",
                table: "Invoices",
                column: "OrderId");
        }
    }
}
