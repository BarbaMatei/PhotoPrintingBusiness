using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhotoPrint.API.Migrations
{
    /// <inheritdoc />
    public partial class AddVatAndInvoices : Migration
    {
        // Bolt 038 — intent 016 (Romanian VAT + e-Factura).
        // Scaffold ran against SQLite (dev default) and produced TEXT/INTEGER
        // column types. The migration is only ever applied to Postgres
        // (Program.cs guards with IsNpgsql); SQLite uses EnsureCreated from
        // the model. Same convention as 20260602141429_AddSamedayOrderFields.
        //
        // Story 001 (VAT fields) — 3 new columns on Orders.
        // Story 002 (Invoice entity + numbering) — Invoices table + the FT-2026
        // sequence seed + the (Series, year, Number) composite uniqueness
        // constraint (defence-in-depth per ADR-020).
        //
        // Backfill posture: pre-existing Orders rows carry NetTotalRon=0,
        // VatRon=0, VatRate=0.19 after the migration. Those orders predate
        // the VAT feature and have no invoice; they will NOT be re-invoiced
        // retroactively. An auditor reading the DB sees the breakdown
        // populated only for orders created on/after this migration.

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Orders columns ──────────────────────────────────────────
            migrationBuilder.AddColumn<decimal>(
                name: "NetTotalRon",
                table: "Orders",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "VatRon",
                table: "Orders",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "VatRate",
                table: "Orders",
                type: "numeric(5,4)",
                nullable: false,
                defaultValue: 0.19m);

            // ── Invoices table ───────────────────────────────────────────
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

            migrationBuilder.CreateIndex(
                name: "ix_invoices_invoice_number",
                table: "Invoices",
                column: "InvoiceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_invoices_order_id",
                table: "Invoices",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "ix_invoices_anaf_status",
                table: "Invoices",
                column: "AnafStatus");

            // EXTRACT and SEQUENCE are Postgres-only; SQLite dev/test numbers via MAX+1.
            if (migrationBuilder.ActiveProvider != "Npgsql.EntityFrameworkCore.PostgreSQL")
                return;

            // Composite uniqueness over (Series, fiscal year of IssuedAt, Number) —
            // the last-line-of-defence guarantee from ADR-020 against any race the
            // SEQUENCE primitive might lose (e.g. operator error during restore).
            // Expression index — EF Core's CreateIndex doesn't support expression
            // columns, so this lives as raw SQL.
            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX \"uq_invoices_series_year_number\" " +
                "ON \"Invoices\"(\"Series\", " +
                "(EXTRACT(YEAR FROM \"IssuedAt\")::int), \"Number\");");

            // Seed the current-year sequence per ADR-020. Subsequent years are
            // auto-created by PostgresInvoiceNumberingService on first call via
            // the same IF NOT EXISTS clause — no migration needed for 2027 etc.
            // Series lower-cased to match the service's seqName convention.
            migrationBuilder.Sql(
                "CREATE SEQUENCE IF NOT EXISTS \"invoice_seq_ft_2026\" START 1 INCREMENT 1;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
                migrationBuilder.Sql(
                    "DROP SEQUENCE IF EXISTS \"invoice_seq_ft_2026\";");

            migrationBuilder.DropTable(
                name: "Invoices");

            migrationBuilder.DropColumn(
                name: "NetTotalRon",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "VatRon",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "VatRate",
                table: "Orders");
        }
    }
}
