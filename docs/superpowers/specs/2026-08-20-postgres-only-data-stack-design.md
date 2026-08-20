# Postgres-only data stack — drop SQLite

*Design approved 2026-08-20. Supersedes the dual-provider reality described in
`memory-bank/standards/data-stack.md`; that standard is rewritten in Phase 2.*

## Goal

One database provider everywhere: PostgreSQL 16. Dev, prod, and (eventually) the
relational tests all run the same engine with the same schema, applied by the same
migration chain. SQLite disappears from the runtime and from the test suite.

Sequencing constraint set by the owner: **wire dev to Postgres first, drop SQLite only
after that is green.**

## Starting state (verified 2026-08-20)

| Fact | Evidence |
|---|---|
| No Postgres server on the dev machine | no `PostgreSQL` service, no `C:\Program Files\PostgreSQL`, port 5432 closed. pgAdmin 4 is installed but is a client only (`C:\Program Files\pgAdmin 4\runtime\psql.exe`) |
| No Docker | `docker` not on PATH — Testcontainers is unavailable for now |
| Dev runs SQLite | `appsettings.Development.json` sets `DatabaseProvider` to `Sqlite`, `Data Source=photoPrint-dev.db` |
| Provider selected by one config key | `Program.cs` reads `DatabaseProvider` (default `Postgres`), then calls `UseSqlite` or `UseNpgsql` |
| Migration chain is SQLite-typed | 51 files under `Migrations/`; snapshot carries 116 `HasColumnType("TEXT")` plus `INTEGER`/`REAL` |
| Prod applies that chain at boot | `Program.cs` else-branch calls `db.Database.Migrate()`, guarded by `IsNpgsql()` |
| Every Postgres runtime arm already exists | `PostgresInvoiceNumberingService`, the `nextval` path in `OrderNumberService`, the `decimal(10,2)`/`decimal(18,2)`/`jsonb` model config |

### The defect this work fixes

The one shared migration chain was scaffolded against SQLite but executes **only** against
Postgres, at prod boot. The generated DDL succeeds — `TEXT`, `INTEGER`, and `REAL` are all
legal Postgres types — but the Npgsql-configured model expects `uuid` for `Guid`,
`timestamptz` for `DateTimeOffset` (the SQLite Unix-ms converter does not apply off SQLite),
`jsonb` for `Order.ShippingAddress` and `OrderItem.ProductSnapshot`, and `numeric` for money.
First queries would fail on type mismatch. `data-stack.md` records this as
deploy-blocking-if-true and unproven; Phase 1b is the experiment that settles it.

## Phase 0 — install the server

Install PostgreSQL 16, matching the documented prod version:

```
winget install --id PostgreSQL.PostgreSQL.16 --accept-package-agreements --accept-source-agreements
```

`PostgreSQL.PostgreSQL.16` resolves to 16.15-1 in the winget catalog. Then create the role and
database that the connection string expects — via pgAdmin or `psql`:

```sql
CREATE ROLE "photoPrint" LOGIN PASSWORD 'dev-password-here';
CREATE DATABASE "photoPrint" OWNER "photoPrint";
```

The dev password goes in `appsettings.Development.Local.json` (git-ignored, already used for
`JwtSettings:PrivateKeyPem`) or user-secrets — never in `appsettings.Development.json`, per
ADR-006.

## Phase 1a — dev talks to Postgres, schema from the model

Fully reversible; no code deleted.

1. `appsettings.Development.json`: set `DatabaseProvider` to `Postgres` and point the
   connection string at the local server with a placeholder password; the real password
   overrides from `appsettings.Development.Local.json`.
2. Schema creation for this phase only: `EnsureCreated()` against Npgsql. It builds from the
   **model**, so column types are correct (`uuid`, `timestamptz`, `jsonb`, `numeric`) and the
   broken migration chain is bypassed. `Program.cs` currently gates `EnsureCreated` on the
   provider being `Sqlite`; widen that gate for the phase, then remove it in 1b.
3. Seed: `dotnet run --project src/PhotoPrint.API --seed-dev`.

Exit criterion: API boots, `/health` passes, and the SPA can register a user, add to cart, and
place an order end to end — exercising the Postgres arms of `OrderNumberService` and
`PostgresInvoiceNumberingService`, both of which create their sequences on first use.

Phase 1a proves the *model* against Postgres. It does not prove the migrations — that is 1b.

## Phase 1b — one Npgsql-native migration baseline

Approach chosen: **squash**, not re-scaffold-51-steps. Nothing is deployed, so no schema
history needs preserving and no data needs migrating.

1. Delete `src/PhotoPrint.API/Migrations/` in full (51 files plus the snapshot).
2. Regenerate one baseline with Npgsql as the design-time provider:
   `dotnet ef migrations add InitialPostgres`, with `DatabaseProvider=Postgres` in the
   design-time environment. Verify the new snapshot contains `uuid`,
   `timestamp with time zone`, `jsonb`, and `numeric` store types and **zero**
   `TEXT`/`INTEGER`/`REAL`.
3. Carry forward the two things that lived in the deleted chain rather than in the model:
   - the ~50 `EasyboxLocker` rows seeded by `InsertData` in `AddShippingAndOrderTables`;
   - the `CREATE SEQUENCE IF NOT EXISTS "invoice_seq_ft_2026"` raw SQL from
     `AddVatAndInvoices`. Both `PostgresInvoiceNumberingService` and `OrderNumberService`
     create their sequences on demand, so this one is belt-and-braces rather than
     load-bearing — but it must not silently vanish.
4. Drop the Phase-1a `EnsureCreated` widening. Dev now boots through `Database.Migrate()`,
   the same path as prod, starting from an empty database.

Exit criterion: dropping and recreating the dev database, then booting, yields a working app
whose schema came from the migration chain. That is the evidence `data-stack.md` says is
missing.

## Phase 2 — remove SQLite

### Runtime

- Drop `Microsoft.EntityFrameworkCore.Sqlite` from `PhotoPrint.API.csproj` and
  `PhotoPrint.Tests.csproj`.
- `PhotoPrintDbContext`: delete the `DateTimeOffset`-to-Unix-ms converter block and the five
  provider branches (the money-type guards near lines 208, 354, and 393, plus the
  string-literal provider comparison near 432). The money and `jsonb` column types become
  unconditional; the check constraints stay skipped on InMemory only.
- Delete `SqliteInvoiceNumberingService` and the provider branch in `Program.cs` that
  registers it; register `PostgresInvoiceNumberingService` unconditionally.
- `OrderNumberService`: drop `DbProviders.Sqlite` from the count-based branch. `InMemory`
  stays until the integration-test default changes.
- Delete the `Microsoft.Data.Sqlite.SqliteException` / extended-error-2067 arms in
  `OrderService` (near 259 and 279) and `WebhooksController` (near 451 and 463); keep the
  Npgsql unique-violation detection.
- Delete `DbProviders.Sqlite` and the whole SQLite `EnsureCreated` plus self-heal block in
  `Program.cs`.

### Tests

14 source files use `UseSqlite` — including `SqlitePaymentFactory`, the invoicing suites, and
`UploadThumbnailPathMigrationTests` (the only test that applies the migration chain). They use
SQLite because it is the only *relational* provider available: the EF InMemory default cannot
enforce unique indexes or check constraints, so the 409-on-duplicate and one-owner paths would
go unproven on InMemory.

Replacement: point these at a real local Postgres test database with per-test isolation.
Testcontainers is the better answer but needs Docker, which is not installed — so the isolation
strategy (schema-per-test vs Respawn vs database-per-collection) is decided at the start of
Phase 2 with Phase 1 in hand, not now. Tests are renamed off the `Sqlite` prefix
(`SqliteInvoiceNumberingServiceTests`, `OrderNumberServiceSqliteTests`, `SqlitePaymentFactory`).

Note: CI already starts a `postgres:16` service container that no test currently uses. Phase 2
makes it load-bearing.

### Standards

`memory-bank/standards/data-stack.md` is rewritten in the same change — the dual-provider
table, the parity warning, the provider-conditional `OnModelCreating` section, and the
"nothing is proven against Postgres" row all become false. Standards are descriptive.

## Risks

- **Non-UTC `DateTimeOffset`.** Npgsql rejects a `DateTimeOffset` whose offset is not zero
  when writing `timestamptz`. Current code constructs from `DateTimeOffset.UtcNow`, but the
  codebase needs a sweep for any other construction — this is a write-time throw, not a
  compile error.
- **Decimal precision changes behaviour.** On SQLite, money is TEXT with no rounding; on
  Postgres it is `numeric(10,2)` or `numeric(18,2)`. Totals that previously kept extra digits
  now round at the database. VAT rounding is `MidpointRounding.AwayFromZero` in application
  code (ADR-019), so this should be inert — but order and invoice total assertions are where it
  would surface.
- **Squash loses replayability of the old chain.** Acceptable only because nothing is
  deployed. If that changes before Phase 1b lands, this design needs revisiting.
- **Test-side conversion is the bulk of the effort** and is the least specified part of this
  design, by intent — see Phase 2.

## Out of scope

Deployment, the 3-environment setup, and Docker/Testcontainers adoption. This is a local plus
migration-chain correctness change only.
