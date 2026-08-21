# Data Stack

*(Rewritten 2026-08-20 from the code. Descriptive — this states what IS, not what is planned.)*

## Overview — one provider

**PostgreSQL 16 is the database in every environment.** `Program.cs` registers exactly one
provider:

```csharp
builder.Services.AddDbContext<PhotoPrintDbContext>(options =>
    options.UseNpgsql(connStr, o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));
```

| Environment | Schema creation |
|---|---|
| Production | `Database.Migrate()` at boot, guarded by `IsNpgsql()` |
| Local development | the same `Database.Migrate()` at boot — dev and prod are schema-identical |
| Integration tests (default) | **EF InMemory**; model-built, and relational features (unique indexes, check constraints, `jsonb`, decimal precision) are **not** enforced |
| Relational tests | a throwaway PostgreSQL database per test class via `PhotoPrint.Tests.Helpers.PostgresTestDatabase`, with the migration chain applied |

Local setup: PostgreSQL 16 on `localhost:5432` with a `photoPrint` role and database. The
connection string lives in `appsettings.Development.json` with a placeholder password; the real
credentials go in the git-ignored `appsettings.Development.Local.json` (loaded last, so it wins)
or user-secrets — never in a tracked settings file (ADR-006).

## The migration chain

Three migrations, all scaffolded under the Npgsql design-time provider: the squashed baseline
**`20260820133204_InitialPostgres`**, then `20260821054658_AddInvoiceStorageLocation` and
`20260821110018_AddInvoiceUnknownUploadOutcomes`, each a single `AddColumn`.

The baseline's snapshot contains only Postgres store types — `uuid`, `timestamp with time zone`,
`jsonb`, `numeric`, `character varying(n)`, `double precision`, `boolean` — and **zero**
`TEXT`/`INTEGER`/`REAL`. It has been applied against a real PostgreSQL 16 server from an empty
database, so the chain is proven, not merely assumed.

Three things in it are **not** derived from the model and must survive any future squash:

1. the 42 `EasyboxLocker` seed rows (`InsertData`) — nothing else seeds lockers, so dropping
   them leaves production with no Easybox pickup points;
2. `uq_invoices_series_year_number`, a unique index over
   `(Series, EXTRACT(YEAR FROM (IssuedAt AT TIME ZONE 'UTC'))::int, Number)` — raw SQL because
   EF cannot express an expression column, and `AT TIME ZONE` is required because `EXTRACT` on a
   bare `timestamptz` is only `STABLE` while an index expression must be `IMMUTABLE`;
3. the `invoice_seq_ft_2026` sequence.

`EnsureCreated()` is not used anywhere and would silently skip all three.

### Rules

- New migrations are scaffolded from the Npgsql snapshot; check the generated DDL before
  committing.
- Scaffold with a **fresh build**. `dotnet ef migrations add --no-build` reads the migrations
  compiled into a stale assembly and emits an `AlterColumn` diff against the old snapshot
  instead of a baseline.
- Production applies pending migrations at boot. Never drop tables; data-preserving migrations
  only.
- Seeding: `--seed` (product catalog + admin user, idempotent) and `--seed-dev` (adds fake
  users/uploads/orders). `docs/DEPLOYMENT.md` runs `--seed` on first deploy.

## Model configuration (read before touching OnModelCreating)

`PhotoPrintDbContext.OnModelCreating` no longer branches per provider for column types — money
is unconditionally `decimal(10,2)` / `decimal(18,2)` / `decimal(5,4)`, and there is no
`DateTimeOffset` value converter, because Npgsql maps `DateTimeOffset` to `timestamptz`
natively. Three provider-conditional pieces remain:

- **Postgres-only:** `jsonb` column type for `Order.ShippingAddress` and
  `OrderItem.ProductSnapshot` (both serialized through a camelCase `JsonSerializer` converter),
  and the `HasFilter("\"IdempotencyKey\" IS NOT NULL")` partial-index filter.
- **Skipped on InMemory:** the check constraints `CK_Uploads_OneOwner` and
  `CK_CartItems_OneOwner` (exactly one of `UserId`/`GuestSessionId`) — relational-only API.
- **InMemory-only:** `OrderNumberService` falls back to a count-based number, and
  `StaticShippingService` falls back to `ToLower().Contains` because `EF.Functions.ILike` is
  Postgres-specific.

Consequence for tests: behaviour resting on unique indexes, check constraints, `jsonb`, decimal
precision, or `ExecuteUpdateAsync` is **invisible to the InMemory default** — use
`PostgresTestDatabase`.

**Npgsql gotcha:** writing a `DateTimeOffset` whose offset is not zero to a `timestamptz`
throws. Construct from `DateTimeOffset.UtcNow` or an explicit `TimeSpan.Zero` offset.

## Entities (18 DbSets)

`EmailQueue`, `User`, `RefreshToken`, `EmailConfirmationToken`, `PasswordResetToken`,
`ExternalLogin`, `GuestSession`, `Product`, `ProductSize`, `ProductFinish`, `PricingTier`,
`Upload`, `CartItem`, `EasyboxLocker`, `Order`, `OrderItem`, `Invoice`, `SavedAddress`.

### Key conventions (as implemented)

- **PKs:** `Guid Id` everywhere, defaulted by property initializer (`Guid.NewGuid()`), not by
  the database.
- **Timestamps:** no EF interceptor/automation. `CreatedAt` is set by property initializer
  (`DateTimeOffset.UtcNow`); only `Order` and `SavedAddress` carry `UpdatedAt` (nullable,
  maintained manually in services). `Upload` uses `UploadedAt`; `CartItem` uses `AddedAt`.
  Don't assume an `UpdatedAt` exists.
- **Soft delete:** `Upload` only (`DeletedAt`), enforced **manually per query** — there is no
  global query filter. Every new Upload query must filter `DeletedAt == null` itself.
- **Enums:** stored as strings via `HasConversion<string>()` (`Order.Status`, `User.Role`,
  `EmailQueue.Status`, payment/delivery/ANAF enums) — except `Upload.StorageLocation` and
  `Invoice.StorageLocation`, both stored as `int` with default `Local`.
- **Concurrency:** there are **no concurrency tokens** (no RowVersion/xmin anywhere).
  Correctness under concurrency comes from unique indexes + violation detection + retry. The
  named index constants `IdempotencyKeyIndexName`, `OrderNumberIndexName`,
  `InvoiceOrderIdIndexName`, and `InvoiceNumberIndexName` are exposed on the DbContext and
  compared against `PostgresException.ConstraintName`, so a rename is a compile break rather
  than a silent fall-through. A change needing optimistic concurrency is a schema decision (see
  ledger D9 / bolt-035 deferral), not a quick add.
- **Idempotency:** `Order.IdempotencyKey`, globally-unique partial index filtered on
  `IS NOT NULL`. Known accepted residual: not tenant-scoped.
- **Sequences:** `OrderNumberService` and `PostgresInvoiceNumberingService` both
  `CREATE SEQUENCE IF NOT EXISTS` on first use for the current year, so a new year needs no
  migration.

## Writing a relational test

```csharp
public class MyRelationalTests : IDisposable
{
    private readonly PostgresTestDatabase _database = new();
    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Something()
    {
        using var db = _database.NewContext();   // every context hits the same database
        ...
    }
}
```

The fixture creates `pp_test_<guid>`, applies the migration chain, and drops the database on
`Dispose`. Extras: `DropAllForeignKeys()` for tests that insert a row without its parents,
`Execute(sql)` for schema surgery, and `BreakOrdersTable()` to simulate an unreachable
database. It connects using `POSTGRES_TEST_CONNECTION` if set, otherwise
`Host=localhost;Port=5432;Username=postgres;Password=postgres;Database=postgres`; the role must
be allowed to `CREATE DATABASE`. If no server is reachable the constructor throws with that
instruction rather than skipping — a silently-skipped relational test proves nothing.

## What the test matrix proves

| Layer | Provider | Proves |
|---|---|---|
| Integration default (WebApplicationFactory family) | InMemory | wiring, auth pipeline, contract shapes — **not** unique indexes, constraints, or SQL semantics |
| `PostgresPaymentFactory` (HTTP-level) | PostgreSQL | the cross-tenant idempotency-key collision → 409 path against the real unique index |
| `PostgresTestDatabase` classes | PostgreSQL | unique-index and check-constraint behaviour, `ExecuteUpdateAsync` CAS transitions, SQL translatability, sequence-based numbering under concurrency, and the migration chain itself |
| MinIO `SkippableFact` suite (CI-gated via `STORAGE_TEST_*` env) | — | real S3 protocol behaviour |

CI starts a `postgres:16-alpine` service container and exports `POSTGRES_TEST_CONNECTION`, so
the PostgreSQL-backed tests run on every push.
