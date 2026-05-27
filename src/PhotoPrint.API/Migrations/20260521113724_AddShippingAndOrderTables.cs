using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhotoPrint.API.Migrations
{
    /// <inheritdoc />
    public partial class AddShippingAndOrderTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EasyboxLockers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SamedayId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Address = table.Column<string>(type: "TEXT", maxLength: 400, nullable: false),
                    City = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    County = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Lat = table.Column<double>(type: "REAL", nullable: false),
                    Lng = table.Column<double>(type: "REAL", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EasyboxLockers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OrderNumber = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    GuestSessionId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    PaymentProcessor = table.Column<string>(type: "TEXT", nullable: false),
                    PaymentIntentId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    EuPlatescTransactionId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ShippingAddress = table.Column<string>(type: "TEXT", nullable: false),
                    DeliveryType = table.Column<string>(type: "TEXT", nullable: false),
                    EasyboxLockerId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ShippingCostRon = table.Column<decimal>(type: "TEXT", nullable: false),
                    SubtotalRon = table.Column<decimal>(type: "TEXT", nullable: false),
                    TotalRon = table.Column<decimal>(type: "TEXT", nullable: false),
                    AwbNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    TrackingUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    PaidAt = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Orders_EasyboxLockers_EasyboxLockerId",
                        column: x => x.EasyboxLockerId,
                        principalTable: "EasyboxLockers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Orders_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "OrderItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OrderId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UploadId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    UnitPriceRon = table.Column<decimal>(type: "TEXT", nullable: false),
                    LineTotalRon = table.Column<decimal>(type: "TEXT", nullable: false),
                    ProductSnapshot = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderItems_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderItems_Uploads_UploadId",
                        column: x => x.UploadId,
                        principalTable: "Uploads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_easybox_lockers_city",
                table: "EasyboxLockers",
                column: "City");

            // ── Seed: representative Sameday Easybox locker locations ──────────────
            migrationBuilder.InsertData(
                table: "EasyboxLockers",
                columns: new[] { "Id", "SamedayId", "Name", "Address", "City", "County", "Lat", "Lng", "IsActive" },
                values: new object[,]
                {
                    // București
                    { new Guid("a1000001-0000-0000-0000-000000000001"), "SMD-B-001", "Kaufland Militari", "Bd. Iuliu Maniu 1", "București", "Ilfov", 44.4388, 26.0074, true },
                    { new Guid("a1000001-0000-0000-0000-000000000002"), "SMD-B-002", "Mega Image Unirii", "Bd. Unirii 11", "București", "Ilfov", 44.4258, 26.1025, true },
                    { new Guid("a1000001-0000-0000-0000-000000000003"), "SMD-B-003", "AFI Cotroceni", "Bd. Vasile Milea 4", "București", "Ilfov", 44.4315, 26.0660, true },
                    { new Guid("a1000001-0000-0000-0000-000000000004"), "SMD-B-004", "Promenada Mall", "Calea Floreasca 246", "București", "Ilfov", 44.4679, 26.1075, true },
                    { new Guid("a1000001-0000-0000-0000-000000000005"), "SMD-B-005", "Sun Plaza", "Calea Văcărești 391", "București", "Ilfov", 44.3994, 26.0985, true },
                    { new Guid("a1000001-0000-0000-0000-000000000006"), "SMD-B-006", "Plaza Romania", "Bd. Timișoara 26", "București", "Ilfov", 44.4401, 25.9976, true },
                    { new Guid("a1000001-0000-0000-0000-000000000007"), "SMD-B-007", "Baneasa Shopping City", "Șoseaua București-Ploiești 42", "București", "Ilfov", 44.5030, 26.0824, true },
                    { new Guid("a1000001-0000-0000-0000-000000000008"), "SMD-B-008", "Carrefour Colentina", "Str. Fundeni 117", "București", "Ilfov", 44.4705, 26.1450, true },
                    // Cluj-Napoca
                    { new Guid("a1000002-0000-0000-0000-000000000001"), "SMD-CJ-001", "Mega Image Mărăști", "Calea Dorobanților 2", "Cluj-Napoca", "Cluj", 46.7712, 23.5836, true },
                    { new Guid("a1000002-0000-0000-0000-000000000002"), "SMD-CJ-002", "Iulius Mall Cluj", "Str. Alexandru Vaida Voievod 53", "Cluj-Napoca", "Cluj", 46.7535, 23.5991, true },
                    { new Guid("a1000002-0000-0000-0000-000000000003"), "SMD-CJ-003", "Kaufland Mănăștur", "Calea Florești 2", "Cluj-Napoca", "Cluj", 46.7592, 23.5540, true },
                    { new Guid("a1000002-0000-0000-0000-000000000004"), "SMD-CJ-004", "Vivo Cluj", "Str. Taietura Turcului 47", "Cluj-Napoca", "Cluj", 46.7450, 23.5680, true },
                    // Timișoara
                    { new Guid("a1000003-0000-0000-0000-000000000001"), "SMD-TM-001", "Iulius Mall Timișoara", "Str. Aristide Demetriade 1", "Timișoara", "Timiș", 45.7489, 21.2087, true },
                    { new Guid("a1000003-0000-0000-0000-000000000002"), "SMD-TM-002", "Kaufland Calea Torontalului", "Calea Torontalului 39", "Timișoara", "Timiș", 45.7553, 21.2124, true },
                    { new Guid("a1000003-0000-0000-0000-000000000003"), "SMD-TM-003", "Carrefour Timișoara", "Bd. Cetății 1", "Timișoara", "Timiș", 45.7698, 21.2266, true },
                    // Iași
                    { new Guid("a1000004-0000-0000-0000-000000000001"), "SMD-IS-001", "Palas Mall Iași", "Str. Palas 7", "Iași", "Iași", 47.1576, 27.5893, true },
                    { new Guid("a1000004-0000-0000-0000-000000000002"), "SMD-IS-002", "Iulius Mall Iași", "Bd. Tudor Vladimirescu 12", "Iași", "Iași", 47.1669, 27.5792, true },
                    { new Guid("a1000004-0000-0000-0000-000000000003"), "SMD-IS-003", "Mega Image Copou", "Str. Universității 5", "Iași", "Iași", 47.1880, 27.5630, true },
                    // Brașov
                    { new Guid("a1000005-0000-0000-0000-000000000001"), "SMD-BV-001", "AFI Brașov", "Str. Lungă 161", "Brașov", "Brașov", 45.6429, 25.5887, true },
                    { new Guid("a1000005-0000-0000-0000-000000000002"), "SMD-BV-002", "Coresi Shopping Resort", "Calea Feldioarei 19", "Brașov", "Brașov", 45.6344, 25.6195, true },
                    { new Guid("a1000005-0000-0000-0000-000000000003"), "SMD-BV-003", "Kaufland Noua", "Str. Hărmanului 2", "Brașov", "Brașov", 45.6271, 25.6323, true },
                    // Constanța
                    { new Guid("a1000006-0000-0000-0000-000000000001"), "SMD-CT-001", "City Park Mall", "Bd. Alexandru Lăpușneanu 116", "Constanța", "Constanța", 44.1598, 28.6278, true },
                    { new Guid("a1000006-0000-0000-0000-000000000002"), "SMD-CT-002", "Kaufland Mamaia", "Șoseaua Mamaia 211", "Constanța", "Constanța", 44.1968, 28.6373, true },
                    // Galați
                    { new Guid("a1000007-0000-0000-0000-000000000001"), "SMD-GL-001", "Galați Mall", "Str. Brăilei 190", "Galați", "Galați", 45.4391, 28.0328, true },
                    { new Guid("a1000007-0000-0000-0000-000000000002"), "SMD-GL-002", "Kaufland Galați", "Str. Tecuci 244", "Galați", "Galați", 45.4463, 28.0495, true },
                    // Craiova
                    { new Guid("a1000008-0000-0000-0000-000000000001"), "SMD-DJ-001", "Electroputere Mall", "Calea București 32", "Craiova", "Dolj", 44.3302, 23.7945, true },
                    { new Guid("a1000008-0000-0000-0000-000000000002"), "SMD-DJ-002", "Kaufland Craiova", "Str. 1 Mai 8", "Craiova", "Dolj", 44.3183, 23.8218, true },
                    // Ploiești
                    { new Guid("a1000009-0000-0000-0000-000000000001"), "SMD-PH-001", "AFI Ploiești", "Bd. Republicii 1", "Ploiești", "Prahova", 44.9438, 26.0310, true },
                    { new Guid("a1000009-0000-0000-0000-000000000002"), "SMD-PH-002", "Winmarkt Ploiești", "Str. Mihai Bravu 9", "Ploiești", "Prahova", 44.9521, 26.0228, true },
                    // Oradea
                    { new Guid("a1000010-0000-0000-0000-000000000001"), "SMD-BH-001", "Lotus Center Oradea", "Str. Cantemir 2", "Oradea", "Bihor", 47.0722, 21.9217, true },
                    { new Guid("a1000010-0000-0000-0000-000000000002"), "SMD-BH-002", "Oradea Value Centre", "Calea Borșului 35", "Oradea", "Bihor", 47.0817, 21.9401, true },
                    // Sibiu
                    { new Guid("a1000011-0000-0000-0000-000000000001"), "SMD-SB-001", "Shopping City Sibiu", "Calea Șurii Mici 1", "Sibiu", "Sibiu", 45.7956, 24.1495, true },
                    { new Guid("a1000011-0000-0000-0000-000000000002"), "SMD-SB-002", "Kaufland Sibiu", "Str. Lucian Blaga 2", "Sibiu", "Sibiu", 45.7885, 24.1652, true },
                    // Arad
                    { new Guid("a1000012-0000-0000-0000-000000000001"), "SMD-AR-001", "Atrium Mall Arad", "Str. Poetului 4", "Arad", "Arad", 46.1826, 21.3154, true },
                    // Pitești
                    { new Guid("a1000013-0000-0000-0000-000000000001"), "SMD-AG-001", "Trivale Mall Pitești", "Str. Exercițiu 1", "Pitești", "Argeș", 44.8524, 24.8688, true },
                    // Bacău
                    { new Guid("a1000014-0000-0000-0000-000000000001"), "SMD-BC-001", "Arena Mall Bacău", "Calea Mărășești 1", "Bacău", "Bacău", 46.5730, 26.9143, true },
                    // Târgu Mureș
                    { new Guid("a1000015-0000-0000-0000-000000000001"), "SMD-MS-001", "Promenada Mureș", "Bd. 1 Decembrie 1918 244", "Târgu Mureș", "Mureș", 46.5452, 24.5611, true },
                    // Baia Mare
                    { new Guid("a1000016-0000-0000-0000-000000000001"), "SMD-MM-001", "Shopping City Baia Mare", "Str. Victoriei 135", "Baia Mare", "Maramureș", 47.6567, 23.5763, true },
                    // Suceava
                    { new Guid("a1000017-0000-0000-0000-000000000001"), "SMD-SV-001", "Shopping City Suceava", "Str. Armenească 2", "Suceava", "Suceava", 47.6505, 26.2553, true },
                    // Deva
                    { new Guid("a1000018-0000-0000-0000-000000000001"), "SMD-HD-001", "Kaufland Deva", "Bd. Nicolae Titulescu 1", "Deva", "Hunedoara", 45.8832, 22.9095, true },
                    // Focșani
                    { new Guid("a1000019-0000-0000-0000-000000000001"), "SMD-VN-001", "Shopping City Focșani", "Bd. Gării 10", "Focșani", "Vrancea", 45.6949, 27.1873, true },
                    // Râmnicu Vâlcea
                    { new Guid("a1000020-0000-0000-0000-000000000001"), "SMD-VL-001", "Shopping City Vâlcea", "Calea lui Traian 178", "Râmnicu Vâlcea", "Vâlcea", 45.0994, 24.3700, true },
                });


            migrationBuilder.CreateIndex(
                name: "ix_order_items_order_id",
                table: "OrderItems",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_ProductId",
                table: "OrderItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_UploadId",
                table: "OrderItems",
                column: "UploadId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_EasyboxLockerId",
                table: "Orders",
                column: "EasyboxLockerId");

            migrationBuilder.CreateIndex(
                name: "ix_orders_order_number",
                table: "Orders",
                column: "OrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_orders_payment_intent_id",
                table: "Orders",
                column: "PaymentIntentId");

            migrationBuilder.CreateIndex(
                name: "ix_orders_status_created_at",
                table: "Orders",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_UserId",
                table: "Orders",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderItems");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "EasyboxLockers");
        }
    }
}
