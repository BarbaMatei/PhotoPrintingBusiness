using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PhotoPrint.API.Migrations
{
    /// <inheritdoc />
    public partial class AddUploadsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "PricingTiers",
                keyColumn: "Id",
                keyValue: new Guid("d1000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "PricingTiers",
                keyColumn: "Id",
                keyValue: new Guid("d1000000-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "PricingTiers",
                keyColumn: "Id",
                keyValue: new Guid("d1000000-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "PricingTiers",
                keyColumn: "Id",
                keyValue: new Guid("d1000000-0000-0000-0000-000000000004"));

            migrationBuilder.DeleteData(
                table: "PricingTiers",
                keyColumn: "Id",
                keyValue: new Guid("d1000000-0000-0000-0000-000000000005"));

            migrationBuilder.DeleteData(
                table: "PricingTiers",
                keyColumn: "Id",
                keyValue: new Guid("d1000000-0000-0000-0000-000000000006"));

            migrationBuilder.DeleteData(
                table: "PricingTiers",
                keyColumn: "Id",
                keyValue: new Guid("d1000000-0000-0000-0000-000000000007"));

            migrationBuilder.DeleteData(
                table: "PricingTiers",
                keyColumn: "Id",
                keyValue: new Guid("d1000000-0000-0000-0000-000000000008"));

            migrationBuilder.DeleteData(
                table: "PricingTiers",
                keyColumn: "Id",
                keyValue: new Guid("d1000000-0000-0000-0000-000000000009"));

            migrationBuilder.DeleteData(
                table: "PricingTiers",
                keyColumn: "Id",
                keyValue: new Guid("d1000000-0000-0000-0000-000000000010"));

            migrationBuilder.DeleteData(
                table: "PricingTiers",
                keyColumn: "Id",
                keyValue: new Guid("d1000000-0000-0000-0000-000000000011"));

            migrationBuilder.DeleteData(
                table: "PricingTiers",
                keyColumn: "Id",
                keyValue: new Guid("d1000000-0000-0000-0000-000000000012"));

            migrationBuilder.DeleteData(
                table: "PricingTiers",
                keyColumn: "Id",
                keyValue: new Guid("d1000000-0000-0000-0000-000000000013"));

            migrationBuilder.DeleteData(
                table: "PricingTiers",
                keyColumn: "Id",
                keyValue: new Guid("d1000000-0000-0000-0000-000000000014"));

            migrationBuilder.DeleteData(
                table: "PricingTiers",
                keyColumn: "Id",
                keyValue: new Guid("d1000000-0000-0000-0000-000000000015"));

            migrationBuilder.DeleteData(
                table: "PricingTiers",
                keyColumn: "Id",
                keyValue: new Guid("d1000000-0000-0000-0000-000000000016"));

            migrationBuilder.DeleteData(
                table: "PricingTiers",
                keyColumn: "Id",
                keyValue: new Guid("d1000000-0000-0000-0000-000000000017"));

            migrationBuilder.DeleteData(
                table: "PricingTiers",
                keyColumn: "Id",
                keyValue: new Guid("d1000000-0000-0000-0000-000000000018"));

            migrationBuilder.DeleteData(
                table: "ProductFinishes",
                keyColumn: "Id",
                keyValue: new Guid("c1000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "ProductFinishes",
                keyColumn: "Id",
                keyValue: new Guid("c1000000-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "ProductSizes",
                keyColumn: "Id",
                keyValue: new Guid("b1000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "ProductSizes",
                keyColumn: "Id",
                keyValue: new Guid("b1000000-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "ProductSizes",
                keyColumn: "Id",
                keyValue: new Guid("b1000000-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "ProductSizes",
                keyColumn: "Id",
                keyValue: new Guid("b1000000-0000-0000-0000-000000000004"));

            migrationBuilder.DeleteData(
                table: "ProductSizes",
                keyColumn: "Id",
                keyValue: new Guid("b1000000-0000-0000-0000-000000000005"));

            migrationBuilder.DeleteData(
                table: "ProductSizes",
                keyColumn: "Id",
                keyValue: new Guid("b1000000-0000-0000-0000-000000000006"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-0000-0000-000000000001"));

            migrationBuilder.AlterColumn<string>(
                name: "Role",
                table: "Users",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "Users",
                type: "TEXT",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "Users",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NormalizedEmail",
                table: "Users",
                type: "TEXT",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<long>(
                name: "LockoutEnd",
                table: "Users",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LastName",
                table: "Users",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<bool>(
                name: "IsEmailConfirmed",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<bool>(
                name: "GdprConsentAccepted",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<string>(
                name: "FirstName",
                table: "Users",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<int>(
                name: "FailedLoginCount",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "TEXT",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<long>(
                name: "CreatedAt",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Users",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "RefreshTokens",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "TokenHash",
                table: "RefreshTokens",
                type: "TEXT",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<long>(
                name: "RevokedAt",
                table: "RefreshTokens",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "ExpiresAt",
                table: "RefreshTokens",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<long>(
                name: "CreatedAt",
                table: "RefreshTokens",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "RefreshTokens",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<int>(
                name: "WidthMm",
                table: "ProductSizes",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<Guid>(
                name: "ProductId",
                table: "ProductSizes",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "Label",
                table: "ProductSizes",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "ProductSizes",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<int>(
                name: "HeightMm",
                table: "ProductSizes",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "ProductSizes",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<int>(
                name: "SortOrder",
                table: "Products",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "ProductType",
                table: "Products",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Products",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Products",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<string>(
                name: "ImageUrl",
                table: "Products",
                type: "TEXT",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "CreatedAt",
                table: "Products",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Products",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "ProductId",
                table: "ProductFinishes",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "ProductFinishes",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "ProductFinishes",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<decimal>(
                name: "UnitPrice",
                table: "PricingTiers",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,2)");

            migrationBuilder.AlterColumn<Guid>(
                name: "ProductSizeId",
                table: "PricingTiers",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<int>(
                name: "MinQuantity",
                table: "PricingTiers",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "MaxQuantity",
                table: "PricingTiers",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "PricingTiers",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "PasswordResetTokens",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "TokenHash",
                table: "PasswordResetTokens",
                type: "TEXT",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<long>(
                name: "ExpiresAt",
                table: "PasswordResetTokens",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<long>(
                name: "CreatedAt",
                table: "PasswordResetTokens",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "PasswordResetTokens",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "GuestSessions",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "LastName",
                table: "GuestSessions",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "FirstName",
                table: "GuestSessions",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<long>(
                name: "ExpiresAt",
                table: "GuestSessions",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "GuestSessions",
                type: "TEXT",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<long>(
                name: "CreatedAt",
                table: "GuestSessions",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<Guid>(
                name: "ClaimedByUserId",
                table: "GuestSessions",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "GuestSessions",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "ExternalLogins",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "ProviderKey",
                table: "ExternalLogins",
                type: "TEXT",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "Provider",
                table: "ExternalLogins",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<long>(
                name: "CreatedAt",
                table: "ExternalLogins",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "ExternalLogins",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "To",
                table: "EmailQueue",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Subject",
                table: "EmailQueue",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "EmailQueue",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<long>(
                name: "SentAt",
                table: "EmailQueue",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "NextRetryAt",
                table: "EmailQueue",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<long>(
                name: "CreatedAt",
                table: "EmailQueue",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<int>(
                name: "Attempts",
                table: "EmailQueue",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "EmailQueue",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "EmailConfirmationTokens",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "TokenHash",
                table: "EmailConfirmationTokens",
                type: "TEXT",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<long>(
                name: "ExpiresAt",
                table: "EmailConfirmationTokens",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<long>(
                name: "CreatedAt",
                table: "EmailConfirmationTokens",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "EmailConfirmationTokens",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.CreateTable(
                name: "Uploads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    GuestSessionId = table.Column<Guid>(type: "TEXT", nullable: true),
                    FilePath = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    OriginalFileName = table.Column<string>(type: "TEXT", maxLength: 260, nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    WidthPx = table.Column<int>(type: "INTEGER", nullable: false),
                    HeightPx = table.Column<int>(type: "INTEGER", nullable: false),
                    FileSizeBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    UploadedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<long>(type: "INTEGER", nullable: true)
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Uploads");

            migrationBuilder.AlterColumn<string>(
                name: "Role",
                table: "Users",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "Users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "Users",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NormalizedEmail",
                table: "Users",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "LockoutEnd",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LastName",
                table: "Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<bool>(
                name: "IsEmailConfirmed",
                table: "Users",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<bool>(
                name: "GdprConsentAccepted",
                table: "Users",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<string>(
                name: "FirstName",
                table: "Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<int>(
                name: "FailedLoginCount",
                table: "Users",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Users",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "RefreshTokens",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "TokenHash",
                table: "RefreshTokens",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "RevokedAt",
                table: "RefreshTokens",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "ExpiresAt",
                table: "RefreshTokens",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "RefreshTokens",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "RefreshTokens",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<int>(
                name: "WidthMm",
                table: "ProductSizes",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<Guid>(
                name: "ProductId",
                table: "ProductSizes",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "Label",
                table: "ProductSizes",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "ProductSizes",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<int>(
                name: "HeightMm",
                table: "ProductSizes",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "ProductSizes",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<int>(
                name: "SortOrder",
                table: "Products",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<string>(
                name: "ProductType",
                table: "Products",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Products",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Products",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<string>(
                name: "ImageUrl",
                table: "Products",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "Products",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Products",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<Guid>(
                name: "ProductId",
                table: "ProductFinishes",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "ProductFinishes",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "ProductFinishes",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<decimal>(
                name: "UnitPrice",
                table: "PricingTiers",
                type: "decimal(10,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<Guid>(
                name: "ProductSizeId",
                table: "PricingTiers",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<int>(
                name: "MinQuantity",
                table: "PricingTiers",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<int>(
                name: "MaxQuantity",
                table: "PricingTiers",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "PricingTiers",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "PasswordResetTokens",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "TokenHash",
                table: "PasswordResetTokens",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "ExpiresAt",
                table: "PasswordResetTokens",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "PasswordResetTokens",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "PasswordResetTokens",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "GuestSessions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "LastName",
                table: "GuestSessions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "FirstName",
                table: "GuestSessions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "ExpiresAt",
                table: "GuestSessions",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "GuestSessions",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "GuestSessions",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<Guid>(
                name: "ClaimedByUserId",
                table: "GuestSessions",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "GuestSessions",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "ExternalLogins",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "ProviderKey",
                table: "ExternalLogins",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "Provider",
                table: "ExternalLogins",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "ExternalLogins",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "ExternalLogins",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "To",
                table: "EmailQueue",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "Subject",
                table: "EmailQueue",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "EmailQueue",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "SentAt",
                table: "EmailQueue",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "NextRetryAt",
                table: "EmailQueue",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "EmailQueue",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<int>(
                name: "Attempts",
                table: "EmailQueue",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "EmailQueue",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "EmailConfirmationTokens",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "TokenHash",
                table: "EmailConfirmationTokens",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "ExpiresAt",
                table: "EmailConfirmationTokens",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "EmailConfirmationTokens",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "EmailConfirmationTokens",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

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
        }
    }
}
