using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhotoPrint.API.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderIdempotencyKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // BUG-5 (review 035-v1): provider-aware so the production Postgres schema is
            // correct, while SQLite (dev/test) output is unchanged. On Postgres, "TEXT"
            // is unbounded (ignores maxLength) and the index needs an explicit NULL
            // filter to match the runtime model (PhotoPrintDbContext); on SQLite, plain
            // TEXT + a plain unique index (NULLs are distinct) is exactly what we had.
            // NOTE: the model snapshot is still SQLite-flavored — fully eliminating
            // scaffold-time drift needs per-provider migration assemblies (deferred).
            var isNpgsql = migrationBuilder.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL";

            migrationBuilder.AddColumn<string>(
                name: "EuPlatescRedirectUrl",
                table: "Orders",
                type: isNpgsql ? "character varying(1000)" : "TEXT",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "Orders",
                type: isNpgsql ? "character varying(80)" : "TEXT",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeClientSecret",
                table: "Orders",
                // DB-2 (review 035-v5): 512, not Stripe's exact 255 ceiling — headroom so a
                // longer client secret can't throw "value too long" on prod Postgres after
                // the charge already exists. Safe to widen this not-yet-deployed migration
                // in place (no Postgres DB has applied it; SQLite is unaffected by maxLength).
                type: isNpgsql ? "character varying(512)" : "TEXT",
                maxLength: 512,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_orders_idempotency_key",
                table: "Orders",
                column: "IdempotencyKey",
                unique: true,
                filter: isNpgsql ? "\"IdempotencyKey\" IS NOT NULL" : null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_orders_idempotency_key",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "EuPlatescRedirectUrl",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "StripeClientSecret",
                table: "Orders");
        }
    }
}
