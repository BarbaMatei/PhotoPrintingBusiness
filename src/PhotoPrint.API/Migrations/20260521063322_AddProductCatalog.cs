using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PhotoPrint.API.Migrations
{
    /// <inheritdoc />
    public partial class AddProductCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ProductType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductFinishes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductFinishes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductFinishes_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductSizes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    WidthMm = table.Column<int>(type: "integer", nullable: false),
                    HeightMm = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductSizes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductSizes_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PricingTiers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductSizeId = table.Column<Guid>(type: "uuid", nullable: false),
                    MinQuantity = table.Column<int>(type: "integer", nullable: false),
                    MaxQuantity = table.Column<int>(type: "integer", nullable: true),
                    UnitPrice = table.Column<decimal>(type: "numeric(10,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PricingTiers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PricingTiers_ProductSizes_ProductSizeId",
                        column: x => x.ProductSizeId,
                        principalTable: "ProductSizes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "CreatedAt", "ImageUrl", "IsActive", "Name", "ProductType", "SortOrder" },
                values: new object[] { new Guid("a1000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "Poze foto", "PhotoPrint", 0 });

            migrationBuilder.InsertData(
                table: "ProductFinishes",
                columns: new[] { "Id", "Name", "ProductId" },
                values: new object[,]
                {
                    { new Guid("c1000000-0000-0000-0000-000000000001"), "Lucioasă", new Guid("a1000000-0000-0000-0000-000000000001") },
                    { new Guid("c1000000-0000-0000-0000-000000000002"), "Mată", new Guid("a1000000-0000-0000-0000-000000000001") }
                });

            migrationBuilder.InsertData(
                table: "ProductSizes",
                columns: new[] { "Id", "HeightMm", "IsActive", "Label", "ProductId", "WidthMm" },
                values: new object[,]
                {
                    { new Guid("b1000000-0000-0000-0000-000000000001"), 150, true, "10×15", new Guid("a1000000-0000-0000-0000-000000000001"), 100 },
                    { new Guid("b1000000-0000-0000-0000-000000000002"), 180, true, "13×18", new Guid("a1000000-0000-0000-0000-000000000001"), 130 },
                    { new Guid("b1000000-0000-0000-0000-000000000003"), 210, true, "15×21", new Guid("a1000000-0000-0000-0000-000000000001"), 150 },
                    { new Guid("b1000000-0000-0000-0000-000000000004"), 300, true, "20×30", new Guid("a1000000-0000-0000-0000-000000000001"), 200 },
                    { new Guid("b1000000-0000-0000-0000-000000000005"), 297, true, "A4", new Guid("a1000000-0000-0000-0000-000000000001"), 210 },
                    { new Guid("b1000000-0000-0000-0000-000000000006"), 420, true, "A3", new Guid("a1000000-0000-0000-0000-000000000001"), 297 }
                });

            migrationBuilder.InsertData(
                table: "PricingTiers",
                columns: new[] { "Id", "MaxQuantity", "MinQuantity", "ProductSizeId", "UnitPrice" },
                values: new object[,]
                {
                    { new Guid("d1000000-0000-0000-0000-000000000001"), 9, 1, new Guid("b1000000-0000-0000-0000-000000000001"), 1.20m },
                    { new Guid("d1000000-0000-0000-0000-000000000002"), 49, 10, new Guid("b1000000-0000-0000-0000-000000000001"), 0.90m },
                    { new Guid("d1000000-0000-0000-0000-000000000003"), null, 50, new Guid("b1000000-0000-0000-0000-000000000001"), 0.70m },
                    { new Guid("d1000000-0000-0000-0000-000000000004"), 9, 1, new Guid("b1000000-0000-0000-0000-000000000002"), 1.80m },
                    { new Guid("d1000000-0000-0000-0000-000000000005"), 49, 10, new Guid("b1000000-0000-0000-0000-000000000002"), 1.40m },
                    { new Guid("d1000000-0000-0000-0000-000000000006"), null, 50, new Guid("b1000000-0000-0000-0000-000000000002"), 1.10m },
                    { new Guid("d1000000-0000-0000-0000-000000000007"), 9, 1, new Guid("b1000000-0000-0000-0000-000000000003"), 2.20m },
                    { new Guid("d1000000-0000-0000-0000-000000000008"), 49, 10, new Guid("b1000000-0000-0000-0000-000000000003"), 1.70m },
                    { new Guid("d1000000-0000-0000-0000-000000000009"), null, 50, new Guid("b1000000-0000-0000-0000-000000000003"), 1.30m },
                    { new Guid("d1000000-0000-0000-0000-000000000010"), 9, 1, new Guid("b1000000-0000-0000-0000-000000000004"), 3.50m },
                    { new Guid("d1000000-0000-0000-0000-000000000011"), 49, 10, new Guid("b1000000-0000-0000-0000-000000000004"), 2.80m },
                    { new Guid("d1000000-0000-0000-0000-000000000012"), null, 50, new Guid("b1000000-0000-0000-0000-000000000004"), 2.20m },
                    { new Guid("d1000000-0000-0000-0000-000000000013"), 9, 1, new Guid("b1000000-0000-0000-0000-000000000005"), 3.00m },
                    { new Guid("d1000000-0000-0000-0000-000000000014"), 49, 10, new Guid("b1000000-0000-0000-0000-000000000005"), 2.40m },
                    { new Guid("d1000000-0000-0000-0000-000000000015"), null, 50, new Guid("b1000000-0000-0000-0000-000000000005"), 1.90m },
                    { new Guid("d1000000-0000-0000-0000-000000000016"), 9, 1, new Guid("b1000000-0000-0000-0000-000000000006"), 5.50m },
                    { new Guid("d1000000-0000-0000-0000-000000000017"), 49, 10, new Guid("b1000000-0000-0000-0000-000000000006"), 4.40m },
                    { new Guid("d1000000-0000-0000-0000-000000000018"), null, 50, new Guid("b1000000-0000-0000-0000-000000000006"), 3.50m }
                });

            migrationBuilder.CreateIndex(
                name: "ix_pricing_tiers_product_size_id",
                table: "PricingTiers",
                column: "ProductSizeId");

            migrationBuilder.CreateIndex(
                name: "ix_product_finishes_product_id",
                table: "ProductFinishes",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "ix_products_is_active_sort_order",
                table: "Products",
                columns: new[] { "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "ix_product_sizes_product_id",
                table: "ProductSizes",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "ix_product_sizes_product_id_label",
                table: "ProductSizes",
                columns: new[] { "ProductId", "Label" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PricingTiers");

            migrationBuilder.DropTable(
                name: "ProductFinishes");

            migrationBuilder.DropTable(
                name: "ProductSizes");

            migrationBuilder.DropTable(
                name: "Products");
        }
    }
}
