using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhotoPrint.API.Migrations
{
    public partial class InitialPostgres : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EasyboxLockers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SamedayId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Address = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    County = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Lat = table.Column<double>(type: "double precision", nullable: false),
                    Lng = table.Column<double>(type: "double precision", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EasyboxLockers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmailConfirmationTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailConfirmationTokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmailQueue",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    To = table.Column<string>(type: "text", nullable: false),
                    Subject = table.Column<string>(type: "text", nullable: false),
                    HtmlBody = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    NextRetryAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastError = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailQueue", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PasswordResetTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordResetTokens", x => x.Id);
                });

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
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Role = table.Column<string>(type: "text", nullable: false),
                    IsEmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    GdprConsentAccepted = table.Column<bool>(type: "boolean", nullable: false),
                    FailedLoginCount = table.Column<int>(type: "integer", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletionRequestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
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
                name: "ExternalLogins",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProviderKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalLogins", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExternalLogins_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GuestSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ClaimedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuestSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuestSessions_Users_ClaimedByUserId",
                        column: x => x.ClaimedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    GuestSessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    PaymentIntentId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ShippingAddress = table.Column<string>(type: "jsonb", nullable: false),
                    DeliveryType = table.Column<string>(type: "text", nullable: false),
                    EasyboxLockerId = table.Column<Guid>(type: "uuid", nullable: true),
                    ShippingCostRon = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    SubtotalRon = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    TotalRon = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    NetTotalRon = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    VatRon = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    VatRate = table.Column<decimal>(type: "numeric(5,4)", nullable: false),
                    AwbNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TrackingUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AwbLabelUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    LastTrackingSyncAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AwbClaimedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IdempotencyKey = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    StripeClientSecret = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    GuestEmail = table.Column<string>(type: "text", nullable: true),
                    InternalNotes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PaidAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ShippedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeliveredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
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
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SavedAddresses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AddressLine = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    County = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PostalCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedAddresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SavedAddresses_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Uploads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    GuestSessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    FilePath = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    ThumbnailPath = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    LargePreviewPath = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    OriginalPurgedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    StorageLocation = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    OriginalFileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    WidthPx = table.Column<int>(type: "integer", nullable: false),
                    HeightPx = table.Column<int>(type: "integer", nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    UploadedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Uploads", x => x.Id);
                    table.CheckConstraint("CK_Uploads_OneOwner", "(\"UserId\" IS NOT NULL AND \"GuestSessionId\" IS NULL) OR (\"UserId\" IS NULL AND \"GuestSessionId\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_Uploads_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
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

            migrationBuilder.CreateTable(
                name: "Invoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Series = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: false),
                    IssuedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    NetTotalRon = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    VatRon = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalRon = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    XmlPayload = table.Column<string>(type: "text", nullable: true),
                    PdfStoragePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AnafUploadId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AnafStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    LastError = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ClaimedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    StorageLocation = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    UnknownUploadOutcomes = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Invoices_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CartItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    GuestSessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    UploadId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    SizeId = table.Column<Guid>(type: "uuid", nullable: false),
                    FinishName = table.Column<string>(type: "text", nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    AddedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CartItems", x => x.Id);
                    table.CheckConstraint("CK_CartItems_OneOwner", "(\"UserId\" IS NOT NULL AND \"GuestSessionId\" IS NULL) OR (\"UserId\" IS NULL AND \"GuestSessionId\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_CartItems_ProductSizes_SizeId",
                        column: x => x.SizeId,
                        principalTable: "ProductSizes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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

            migrationBuilder.CreateTable(
                name: "OrderItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    UploadId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitPriceRon = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    LineTotalRon = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    ProductSnapshot = table.Column<string>(type: "jsonb", nullable: false)
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
                name: "IX_CartItems_SizeId",
                table: "CartItems",
                column: "SizeId");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_UploadId",
                table: "CartItems",
                column: "UploadId");

            migrationBuilder.CreateIndex(
                name: "ix_easybox_lockers_city",
                table: "EasyboxLockers",
                column: "City");

            migrationBuilder.CreateIndex(
                name: "ix_email_confirmation_tokens_user_id",
                table: "EmailConfirmationTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "ix_email_queue_status_next_retry",
                table: "EmailQueue",
                columns: new[] { "Status", "NextRetryAt" });

            migrationBuilder.CreateIndex(
                name: "ix_external_logins_provider_key",
                table: "ExternalLogins",
                columns: new[] { "Provider", "ProviderKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_external_logins_user_provider",
                table: "ExternalLogins",
                columns: new[] { "UserId", "Provider" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_guest_sessions_claimed_by_user",
                table: "GuestSessions",
                column: "ClaimedByUserId");

            migrationBuilder.CreateIndex(
                name: "ix_guest_sessions_expires_at",
                table: "GuestSessions",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "ix_invoices_anaf_status",
                table: "Invoices",
                column: "AnafStatus");

            migrationBuilder.CreateIndex(
                name: "ix_invoices_invoice_number",
                table: "Invoices",
                column: "InvoiceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_invoices_order_id",
                table: "Invoices",
                column: "OrderId",
                unique: true);

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
                name: "ix_orders_idempotency_key",
                table: "Orders",
                column: "IdempotencyKey",
                unique: true,
                filter: "\"IdempotencyKey\" IS NOT NULL");

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

            migrationBuilder.CreateIndex(
                name: "ix_password_reset_tokens_user_id",
                table: "PasswordResetTokens",
                column: "UserId");

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

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_hash",
                table: "RefreshTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "ix_saved_addresses_user_id",
                table: "SavedAddresses",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Uploads_DeletedAt",
                table: "Uploads",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Uploads_GuestSessionId",
                table: "Uploads",
                column: "GuestSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Uploads_UploadedAt",
                table: "Uploads",
                column: "UploadedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Uploads_UserId",
                table: "Uploads",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "ix_users_normalized_email",
                table: "Users",
                column: "NormalizedEmail",
                unique: true);

            migrationBuilder.InsertData(
                table: "EasyboxLockers",
                columns: new[] { "Id", "SamedayId", "Name", "Address", "City", "County", "Lat", "Lng", "IsActive" },
                values: new object[,]
                {
                    { new Guid("a1000001-0000-0000-0000-000000000001"), "SMD-B-001", "Kaufland Militari", "Bd. Iuliu Maniu 1", "București", "Ilfov", 44.4388, 26.0074, true },
                    { new Guid("a1000001-0000-0000-0000-000000000002"), "SMD-B-002", "Mega Image Unirii", "Bd. Unirii 11", "București", "Ilfov", 44.4258, 26.1025, true },
                    { new Guid("a1000001-0000-0000-0000-000000000003"), "SMD-B-003", "AFI Cotroceni", "Bd. Vasile Milea 4", "București", "Ilfov", 44.4315, 26.0660, true },
                    { new Guid("a1000001-0000-0000-0000-000000000004"), "SMD-B-004", "Promenada Mall", "Calea Floreasca 246", "București", "Ilfov", 44.4679, 26.1075, true },
                    { new Guid("a1000001-0000-0000-0000-000000000005"), "SMD-B-005", "Sun Plaza", "Calea Văcărești 391", "București", "Ilfov", 44.3994, 26.0985, true },
                    { new Guid("a1000001-0000-0000-0000-000000000006"), "SMD-B-006", "Plaza Romania", "Bd. Timișoara 26", "București", "Ilfov", 44.4401, 25.9976, true },
                    { new Guid("a1000001-0000-0000-0000-000000000007"), "SMD-B-007", "Baneasa Shopping City", "Șoseaua București-Ploiești 42", "București", "Ilfov", 44.5030, 26.0824, true },
                    { new Guid("a1000001-0000-0000-0000-000000000008"), "SMD-B-008", "Carrefour Colentina", "Str. Fundeni 117", "București", "Ilfov", 44.4705, 26.1450, true },
                    { new Guid("a1000002-0000-0000-0000-000000000001"), "SMD-CJ-001", "Mega Image Mărăști", "Calea Dorobanților 2", "Cluj-Napoca", "Cluj", 46.7712, 23.5836, true },
                    { new Guid("a1000002-0000-0000-0000-000000000002"), "SMD-CJ-002", "Iulius Mall Cluj", "Str. Alexandru Vaida Voievod 53", "Cluj-Napoca", "Cluj", 46.7535, 23.5991, true },
                    { new Guid("a1000002-0000-0000-0000-000000000003"), "SMD-CJ-003", "Kaufland Mănăștur", "Calea Florești 2", "Cluj-Napoca", "Cluj", 46.7592, 23.5540, true },
                    { new Guid("a1000002-0000-0000-0000-000000000004"), "SMD-CJ-004", "Vivo Cluj", "Str. Taietura Turcului 47", "Cluj-Napoca", "Cluj", 46.7450, 23.5680, true },
                    { new Guid("a1000003-0000-0000-0000-000000000001"), "SMD-TM-001", "Iulius Mall Timișoara", "Str. Aristide Demetriade 1", "Timișoara", "Timiș", 45.7489, 21.2087, true },
                    { new Guid("a1000003-0000-0000-0000-000000000002"), "SMD-TM-002", "Kaufland Calea Torontalului", "Calea Torontalului 39", "Timișoara", "Timiș", 45.7553, 21.2124, true },
                    { new Guid("a1000003-0000-0000-0000-000000000003"), "SMD-TM-003", "Carrefour Timișoara", "Bd. Cetății 1", "Timișoara", "Timiș", 45.7698, 21.2266, true },
                    { new Guid("a1000004-0000-0000-0000-000000000001"), "SMD-IS-001", "Palas Mall Iași", "Str. Palas 7", "Iași", "Iași", 47.1576, 27.5893, true },
                    { new Guid("a1000004-0000-0000-0000-000000000002"), "SMD-IS-002", "Iulius Mall Iași", "Bd. Tudor Vladimirescu 12", "Iași", "Iași", 47.1669, 27.5792, true },
                    { new Guid("a1000004-0000-0000-0000-000000000003"), "SMD-IS-003", "Mega Image Copou", "Str. Universității 5", "Iași", "Iași", 47.1880, 27.5630, true },
                    { new Guid("a1000005-0000-0000-0000-000000000001"), "SMD-BV-001", "AFI Brașov", "Str. Lungă 161", "Brașov", "Brașov", 45.6429, 25.5887, true },
                    { new Guid("a1000005-0000-0000-0000-000000000002"), "SMD-BV-002", "Coresi Shopping Resort", "Calea Feldioarei 19", "Brașov", "Brașov", 45.6344, 25.6195, true },
                    { new Guid("a1000005-0000-0000-0000-000000000003"), "SMD-BV-003", "Kaufland Noua", "Str. Hărmanului 2", "Brașov", "Brașov", 45.6271, 25.6323, true },
                    { new Guid("a1000006-0000-0000-0000-000000000001"), "SMD-CT-001", "City Park Mall", "Bd. Alexandru Lăpușneanu 116", "Constanța", "Constanța", 44.1598, 28.6278, true },
                    { new Guid("a1000006-0000-0000-0000-000000000002"), "SMD-CT-002", "Kaufland Mamaia", "Șoseaua Mamaia 211", "Constanța", "Constanța", 44.1968, 28.6373, true },
                    { new Guid("a1000007-0000-0000-0000-000000000001"), "SMD-GL-001", "Galați Mall", "Str. Brăilei 190", "Galați", "Galați", 45.4391, 28.0328, true },
                    { new Guid("a1000007-0000-0000-0000-000000000002"), "SMD-GL-002", "Kaufland Galați", "Str. Tecuci 244", "Galați", "Galați", 45.4463, 28.0495, true },
                    { new Guid("a1000008-0000-0000-0000-000000000001"), "SMD-DJ-001", "Electroputere Mall", "Calea București 32", "Craiova", "Dolj", 44.3302, 23.7945, true },
                    { new Guid("a1000008-0000-0000-0000-000000000002"), "SMD-DJ-002", "Kaufland Craiova", "Str. 1 Mai 8", "Craiova", "Dolj", 44.3183, 23.8218, true },
                    { new Guid("a1000009-0000-0000-0000-000000000001"), "SMD-PH-001", "AFI Ploiești", "Bd. Republicii 1", "Ploiești", "Prahova", 44.9438, 26.0310, true },
                    { new Guid("a1000009-0000-0000-0000-000000000002"), "SMD-PH-002", "Winmarkt Ploiești", "Str. Mihai Bravu 9", "Ploiești", "Prahova", 44.9521, 26.0228, true },
                    { new Guid("a1000010-0000-0000-0000-000000000001"), "SMD-BH-001", "Lotus Center Oradea", "Str. Cantemir 2", "Oradea", "Bihor", 47.0722, 21.9217, true },
                    { new Guid("a1000010-0000-0000-0000-000000000002"), "SMD-BH-002", "Oradea Value Centre", "Calea Borșului 35", "Oradea", "Bihor", 47.0817, 21.9401, true },
                    { new Guid("a1000011-0000-0000-0000-000000000001"), "SMD-SB-001", "Shopping City Sibiu", "Calea Șurii Mici 1", "Sibiu", "Sibiu", 45.7956, 24.1495, true },
                    { new Guid("a1000011-0000-0000-0000-000000000002"), "SMD-SB-002", "Kaufland Sibiu", "Str. Lucian Blaga 2", "Sibiu", "Sibiu", 45.7885, 24.1652, true },
                    { new Guid("a1000012-0000-0000-0000-000000000001"), "SMD-AR-001", "Atrium Mall Arad", "Str. Poetului 4", "Arad", "Arad", 46.1826, 21.3154, true },
                    { new Guid("a1000013-0000-0000-0000-000000000001"), "SMD-AG-001", "Trivale Mall Pitești", "Str. Exercițiu 1", "Pitești", "Argeș", 44.8524, 24.8688, true },
                    { new Guid("a1000014-0000-0000-0000-000000000001"), "SMD-BC-001", "Arena Mall Bacău", "Calea Mărășești 1", "Bacău", "Bacău", 46.5730, 26.9143, true },
                    { new Guid("a1000015-0000-0000-0000-000000000001"), "SMD-MS-001", "Promenada Mureș", "Bd. 1 Decembrie 1918 244", "Târgu Mureș", "Mureș", 46.5452, 24.5611, true },
                    { new Guid("a1000016-0000-0000-0000-000000000001"), "SMD-MM-001", "Shopping City Baia Mare", "Str. Victoriei 135", "Baia Mare", "Maramureș", 47.6567, 23.5763, true },
                    { new Guid("a1000017-0000-0000-0000-000000000001"), "SMD-SV-001", "Shopping City Suceava", "Str. Armenească 2", "Suceava", "Suceava", 47.6505, 26.2553, true },
                    { new Guid("a1000018-0000-0000-0000-000000000001"), "SMD-HD-001", "Kaufland Deva", "Bd. Nicolae Titulescu 1", "Deva", "Hunedoara", 45.8832, 22.9095, true },
                    { new Guid("a1000019-0000-0000-0000-000000000001"), "SMD-VN-001", "Shopping City Focșani", "Bd. Gării 10", "Focșani", "Vrancea", 45.6949, 27.1873, true },
                    { new Guid("a1000020-0000-0000-0000-000000000001"), "SMD-VL-001", "Shopping City Vâlcea", "Calea lui Traian 178", "Râmnicu Vâlcea", "Vâlcea", 45.0994, 24.3700, true },
                });

            // EXTRACT on a bare timestamptz is only STABLE; an index expression must be IMMUTABLE.
            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX \"uq_invoices_series_year_number\" " +
                "ON \"Invoices\"(\"Series\", " +
                "(EXTRACT(YEAR FROM (\"IssuedAt\" AT TIME ZONE 'UTC'))::int), \"Number\");");

            migrationBuilder.Sql(
                "CREATE SEQUENCE IF NOT EXISTS \"invoice_seq_ft_2026\" START 1 INCREMENT 1;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DROP SEQUENCE IF EXISTS \"invoice_seq_ft_2026\";");

            migrationBuilder.DropTable(
                name: "CartItems");

            migrationBuilder.DropTable(
                name: "EmailConfirmationTokens");

            migrationBuilder.DropTable(
                name: "EmailQueue");

            migrationBuilder.DropTable(
                name: "ExternalLogins");

            migrationBuilder.DropTable(
                name: "GuestSessions");

            migrationBuilder.DropTable(
                name: "Invoices");

            migrationBuilder.DropTable(
                name: "OrderItems");

            migrationBuilder.DropTable(
                name: "PasswordResetTokens");

            migrationBuilder.DropTable(
                name: "PricingTiers");

            migrationBuilder.DropTable(
                name: "ProductFinishes");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "SavedAddresses");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "Uploads");

            migrationBuilder.DropTable(
                name: "ProductSizes");

            migrationBuilder.DropTable(
                name: "EasyboxLockers");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Products");
        }
    }
}
