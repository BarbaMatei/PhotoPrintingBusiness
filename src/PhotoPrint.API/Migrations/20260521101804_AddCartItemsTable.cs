using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhotoPrint.API.Migrations
{
    /// <inheritdoc />
    public partial class AddCartItemsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CartItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    GuestSessionId = table.Column<Guid>(type: "TEXT", nullable: true),
                    UploadId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    AddedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CartItems", x => x.Id);
                    table.CheckConstraint("CK_CartItems_OneOwner", "(\"UserId\" IS NOT NULL AND \"GuestSessionId\" IS NULL) OR (\"UserId\" IS NULL AND \"GuestSessionId\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_CartItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CartItems_Uploads_UploadId",
                        column: x => x.UploadId,
                        principalTable: "Uploads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CartItems_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_cart_items_guest_added_at",
                table: "CartItems",
                columns: new[] { "GuestSessionId", "AddedAt" });

            migrationBuilder.CreateIndex(
                name: "ix_cart_items_guest_upload",
                table: "CartItems",
                columns: new[] { "GuestSessionId", "UploadId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_cart_items_user_added_at",
                table: "CartItems",
                columns: new[] { "UserId", "AddedAt" });

            migrationBuilder.CreateIndex(
                name: "ix_cart_items_user_upload",
                table: "CartItems",
                columns: new[] { "UserId", "UploadId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_ProductId",
                table: "CartItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_UploadId",
                table: "CartItems",
                column: "UploadId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CartItems");
        }
    }
}
