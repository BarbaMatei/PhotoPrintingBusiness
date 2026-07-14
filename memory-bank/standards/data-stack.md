# Data Stack

*(Rewritten 2026-07-14 from the code. Descriptive — this states what IS, not what is planned.)*

## Overview — the dual-provider reality

There is no single database. The app runs on **two providers plus one test double**, selected by
the `DatabaseProvider` config key (default `"Postgres"`; constants in
`src/PhotoPrint.API/Data/DbProviders.cs`):

| Environment | Provider | Schema creation |
|---|---|---|
| Production / default | **PostgreSQL 16** (`UseNpgsql`) | `Database.Migrate()` at boot (Program.cs, Npgsql branch only) |
| Development (`appsettings.Development.json`) | **SQLite file** (`photoPrint-dev.db`) | `EnsureCreated()` + a self-heal that drops/recreates if core tables are missing — **migrations never run here** |
| Integration tests (default) | **EF InMemory** | model-built; relational features (unique indexes, check constraints) not enforced |
| Specific relational tests | **SQLite in-memory** (shared open connection) | `EnsureCreated()`, or `Database.Migrate()` in the migration-chain test |

**The standing parity warning (recurring review finding, D23-class):** the single migration set
under `Migrations/` was scaffolded against SQLite — the snapshot carries `TEXT/INTEGER/REAL`
store types and no Npgsql annotations — yet those migrations execute **only against Postgres**
(at prod boot). Dev never runs them; the InMemory test default can't validate them; only one
unit test (`UploadThumbnailPathMigrationTests`) applies the chain, on SQLite. Nothing in the
repo proves the DDL against Postgres today (deferred to the Testcontainers/3-env work). Any
bolt touching a migration must read this paragraph and D-o-D class 2.

## Provider-conditional model configuration (read before touching OnModelCreating)

`PhotoPrintDbContext.OnModelCreating` branches on `Database.ProviderName`:

- **SQLite only:** every `DateTimeOffset` property gets a value converter to Unix-epoch
  milliseconds (`long`) — SQLite has no native DateTimeOffset. Ordering/precision behavior
  therefore differs from Postgres.
- **Non-SQLite only:** `decimal(10,2)` column types on money fields (`PricingTier.UnitPrice`,
  `Order.*Ron`, `OrderItem.*Ron`). On SQLite, decimals are TEXT.
- **Postgres only:** `jsonb` column type for `Order.ShippingAddress` and
  `OrderItem.ProductSnapshot` (both serialized via a camelCase `JsonSerializer` value
  converter; TEXT elsewhere).
- **Skipped on InMemory:** check constraints `CK_Uploads_OneOwner`, `CK_CartItems_OneOwner`
  (exactly one of `UserId`/`GuestSessionId`) — relational-only API.

Consequence for tests: a behavior resting on unique indexes, check constraints, `jsonb`, or
decimal precision is **invisible to the InMemory default** — use the SQLite-in-memory pattern
(`SqlitePaymentFactory` is the model) and note the remaining Postgres gap.

## Entities (17 DbSets)

`User`, `RefreshToken`, `EmailConfirmationToken`, `PasswordResetToken`, `ExternalLogin`,
`GuestSession`, `Product`, `ProductSize`, `ProductFinish`, `PricingTier`, `Upload`, `CartItem`,
`EasyboxLocker`, `Order`, `OrderItem`, `SavedAddress`, `EmailQueue`.

### Key conventions (as implemented)

- **PKs:** `Guid Id` everywhere, defaulted by property initializer (`Guid.NewGuid()`), not by
  the database.
- **Timestamps:** no EF interceptor/automation. `CreatedAt` is set by property initializer
  (`DateTimeOffset.UtcNow`); only `Order` and `SavedAddress` carry `UpdatedAt`
  (nullable, maintained manually in services). `Upload` uses `UploadedAt`; `CartItem` uses
  `AddedAt`. Don't assume an `UpdatedAt` exists.
- **Soft delete:** `Upload` only (`DeletedAt`), enforced **manually per query** — there is no
  global query filter. Every new Upload query must filter `DeletedAt == null` itself.
- **Enums:** stored as strings via `HasConversion<string>()` (`Order.Status`,
  `User.Role`, `EmailQueue.Status`, payment/delivery enums) — except `Upload.StorageLocation`,
  stored as `int` with default `Local`.
- **Concurrency:** there are **no concurrency tokens** (no RowVersion/xmin anywhere).
  Correctness under concurrency is achieved by unique indexes + violation detection + retry —
  the named index constants `ix_orders_idempotency_key` / `ix_orders_order_number` are exposed
  on the DbContext and string-matched by `OrderService`. A change that needs optimistic
  concurrency is a schema decision (see ledger D9 / bolt-035 deferral), not a quick add.
- **Idempotency:** `Order.IdempotencyKey`, globally-unique index (partial on Postgres,
  `IS NOT NULL` filter; plain unique on SQLite). Known accepted residual: not tenant-scoped.

## Migration rules (as they actually work today)

- One shared migration set, currently SQLite-typed. New migrations are scaffolded from the
  SQLite-flavored snapshot — check what the generated DDL means on Postgres before committing.
- Dev does not run migrations at all (`EnsureCreated`) — do not rely on dev boot to validate a
  migration; run the migration-chain unit test pattern instead.
- Production applies pending migrations at boot. Never drop tables; data-preserving migrations
  only.
- Seeding: `--seed` / `--seed-dev` boot modes; one migration (`AddShippingAndOrderTables`)
  seeds ~50 `EasyboxLocker` rows via `InsertData`.

## What the test matrix can and cannot prove

| Layer | Provider | Proves |
|---|---|---|
| Integration default (WebApplicationFactory family) | InMemory | wiring, auth pipeline, contract shapes — **not** unique indexes, constraints, SQL semantics |
| `SqlitePaymentFactory` + SQLite `:memory:` unit tests | SQLite | unique-index 409 paths, migration chain (SQLite arm), relational behavior |
| MinIO `SkippableFact` suite (CI-gated via `STORAGE_TEST_*` env) | — | real S3 protocol behavior |
| *(nothing)* | Postgres | **nothing is proven against Postgres today** — CI starts a postgres:16 service container that no test uses. The Npgsql arm is the standing gap (D23), planned via Testcontainers. |
