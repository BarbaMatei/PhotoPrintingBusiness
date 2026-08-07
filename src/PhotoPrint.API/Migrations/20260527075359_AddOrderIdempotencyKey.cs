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
            // Provider-aware so the production Postgres schema is
            // correct, while SQLite (dev/test) output is unchanged. On Postgres, "TEXT"
            // is unbounded (ignores maxLength) and the index needs an explicit NULL
            // filter to match the runtime model (PhotoPrintDbContext); on SQLite, plain
            // TEXT + a plain unique index (NULLs are distinct) is exactly what we had.
            //
            // Breadcrumb for the NEXT author. The model snapshot
            // (PhotoPrintDbContextModelSnapshot.cs) is SQLite-flavored: it records these
            // columns as TEXT and the idempotency index as UNFILTERED. The runtime Npgsql
            // model is character varying(N) + a filtered index. EF diffs the next migration
            // against that snapshot, so `dotnet ef migrations add` under the Npgsql provider
            // will scaffold a PHANTOM diff for these columns — AlterColumn (TEXT→varchar) +
            // Drop/CreateIndex (to add `"IdempotencyKey" IS NOT NULL`). For an already-correct
            // deployed schema that diff is spurious: review it and discard the idempotency-
            // column operations. Fully eliminating the drift needs per-provider migration
            // assemblies — a deferred follow-up, not done here.
            //
            // Re-affirmed deferred. Two related gaps, same home:
            //   • The SQLite-flavored snapshot phantom diff described above.
            //   • NO test exercises the Postgres arm at all — every fixture uses
            //     EnsureCreated (schema from the model), never Migrate, so this migration's
            //     DDL, the filtered index, and the Npgsql `IsIdempotencyKeyViolation` branch run
            //     in tests NOWHERE. A Testcontainers-Postgres regression that applies this
            //     migration and drives the concurrent double-submit is the durable fix.
            // Both belong to the migration/deploy (3-env) phase of the roadmap, not a bolt-035
            // fix pass — the app has no migration-based deployment yet. Deferral upheld v5→v8.
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
                // 512, not Stripe's exact 255 ceiling — headroom so a
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
