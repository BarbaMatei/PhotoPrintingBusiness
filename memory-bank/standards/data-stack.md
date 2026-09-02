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
| Relational tests | a pooled PostgreSQL database leased per test class via `PhotoPrint.Tests.Helpers.PostgresTestDatabase`, migrated once per schema and reused across runs |

Local setup: PostgreSQL 16 on `localhost:5432` with a `photoPrint` role and database. The
connection string lives in `appsettings.Development.json` with a placeholder password; the real
credentials go in the git-ignored `appsettings.Development.Local.json` (loaded last, so it wins)
or user-secrets — never in a tracked settings file (ADR-006).

## The migration chain

**One migration, always:** **`20260820133204_InitialPostgres`**, scaffolded under the Npgsql
design-time provider.

Pre-deploy, it is **edited in place** — a schema change edits the baseline and its `.Designer.cs`,
never adds a second file, so the branch that reaches `main` carries one migration rather than a
trail of corrections. `Migrate()` matches ids, not contents, so an edited baseline never reaches a
database that already ran it: bring a stale developer database in line by hand
(`ALTER TABLE ... ADD COLUMN` / `DROP COLUMN` keeps the data) or recreate it. Neither a missing
column nor a stale one registers as a pending migration, so boot will not warn. Once anything is
deployed this reverses — an applied migration is frozen and a change goes in a new migration.

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
  `EmailQueue.Status`, delivery/ANAF enums) — except `Upload.StorageLocation` and
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
- **Sequences:** `OrderNumberService` and `PostgresInvoiceNumberingService` both create their
  per-year sequence on first use through `PostgresSequences.EnsureAsync`, so a new year needs no
  migration. `CREATE SEQUENCE IF NOT EXISTS` is **not** atomic against a concurrent create — the
  loser raises `42P07`, `42710` or `23505` on a catalogue index — so the helper runs it inside a
  `DO` block whose exception handler swallows exactly those three, and only when the name then
  holds a sequence. A client-side catch would not do: it leaves an enclosing transaction aborted.

## Writing a relational test

```csharp
public class MyRelationalTests : IClassFixture<PostgresTestDatabase>
{
    private readonly PostgresTestDatabase _database;

    public MyRelationalTests(PostgresTestDatabase database)
    {
        _database = database;
        database.ResetForTest();
    }

    [Fact]
    public async Task Something()
    {
        using var db = _database.NewContext();   // every context hits the same database
        ...
    }
}
```

**Take the fixture; never build a database per test.** A field initialiser
(`private readonly PostgresTestDatabase _database = new();`) builds one per test *method*, because
xUnit constructs the test class once per test — measured at ~3 s each, and 161 relational tests
took 143 s that way. `IClassFixture` plus `ResetForTest()` in the constructor gets the same clean
slate for ~3 ms, and the same 161 tests run in ~20 s.

Where those seconds went, and why the fixture is shaped the way it is:

- **The databases are pooled and permanent.** The fixture leases `pp_test_<salt>_<schema>_std_<nn>`,
  holding a Postgres advisory lock for its lifetime, and leaves the database in place on dispose.
  The next run reuses it, so `CREATE DATABASE` + `Migrate()` is paid once per schema rather than
  once per run. The lease is what makes concurrent runs safe: two suites at once (two worktrees, or
  two agent sessions) take different slots, and the pool grows on demand.
- **`<salt>` is the test assembly's directory and `<schema>` hashes the generated create script**,
  not just the migration id list — a migration edited in place keeps its id while producing a
  different schema, and a reused database would then be silently wrong. A schema change makes new
  names; the stale set is swept when nothing holds its lease.
- **`ResetForTest()` deletes rows, it does not truncate.** `TRUNCATE` rewrites every table's storage
  and flushes it, which measured 285–556 ms *on empty tables*; the delete-based reset with foreign
  keys switched off for the wipe measured 3.1 ms. It needs a superuser for
  `session_replication_role`; without one it falls back to truncating.
- **Sequences get the same treatment as rows.** A reset rewinds the sequences the migration chain
  ships and **drops** every other one, because `PostgresSequences.EnsureAsync` creates the per-year
  sequences on first use: one left behind turns the next test's create into a duplicate, which is
  exactly what the create-race tests need to be able to provoke. `TRUNCATE … RESTART IDENTITY`
  rewinds neither, since no table column owns them. The shipped set is read out of the migration
  script — the model's create script does not contain the raw `CREATE SEQUENCE` statements.
- **Two escape hatches.** `ForeignKeyFreeTestDatabase` is a separate pool with the constraints
  dropped, for tests that insert a row without its parents — separate so a constraint-free schema is
  never handed to a class that expects constraints. `PostgresTestDatabase.Throwaway()` migrates a
  private database and drops it, for the migration-chain tests and for tests that wreck the schema
  (`BreakOrdersTable()` drops a table, and a pooled database would carry the wreck onward).
  `Execute(sql)` marks the database dirty, so a leased one is dropped instead of handed on.

Extras: `DropAllForeignKeys()` for tests that insert a row without its parents, `Execute(sql)` for
schema surgery, and `BreakOrdersTable()` to simulate an unreachable database. It connects using
`POSTGRES_TEST_CONNECTION` if set, otherwise
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
