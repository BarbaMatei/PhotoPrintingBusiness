---
type: resolution
target: 038-039-invoicing
version: 10
answers: pass v10 (verification — index row)
status: resolved
fixed_commit: 4d6bc6d
closed: 2026-08-21
---

# Resolution v10 — 038-039-invoicing

## Findings

| ID | Status | Commit | Note |
|---|---|---|---|
| PPW-578 | fixed | `de1a4cb`, `4d6bc6d` | both sequence sites create through `PostgresSequences.EnsureAsync`, whose `DO` block swallows `42P07`, `42710` and `23505` only when the name then holds a sequence. New surface: the helper and its name guard |
| PPW-569 | fixed | `de1a4cb` | the same defect at the invoice site, fixed by the same helper; the comment claiming a concurrent `IF NOT EXISTS` is safe is deleted, because a race test reddens on it |

## Scope

| Cluster | Findings | Files | Approach-check |
|---|---|---|---|
| A — lazy sequence creation race | PPW-578, PPW-569 | `Data/PostgresSequences.cs`, `Services/OrderNumberService.cs`, `Services/Invoicing/PostgresInvoiceNumberingService.cs`, `Tests/Helpers/UncommittedRelationCreator.cs`, `Tests/Unit/Services/OrderNumberServicePostgresTests.cs`, `Tests/Integration/PostgresInvoiceNumberingServiceIntegrationTests.cs`, `Tests/Integration/PostgresSequencesTests.cs`, `memory-bank/standards/data-stack.md`, `memory-bank/bolts/038-vat-calculation/adr-020-postgres-sequence-for-invoice-numbering-accept-gap-on-rollback.md` | new check run (`revised`, folded in) |

## Decisions

### The duplicate is swallowed inside PL/pgSQL, not caught in C# (PPW-578)

PostgreSQL documents `CREATE ... IF NOT EXISTS` as not atomic against a concurrent create, so
the remedy is to treat the duplicate as success. Two ways to do that were compared.

- An advisory lock was rejected. Transaction-scoped, it is released at statement end where no
  caller opens a transaction, so it serialises nothing; where a caller does open one it is held
  until that transaction commits, serialising every invoice of the series and year. Session-scoped
  needs a guaranteed unlock across a pooled connection.
- A client-side `catch` on the SQLSTATE was rejected because a failed statement aborts an
  enclosing transaction, so the following `nextval` would fail `25P02`. A PL/pgSQL exception
  handler runs in its own subtransaction, so it is correct whether or not a caller holds a
  transaction. One test locks that property.

`IF NOT EXISTS` stays inside the block so the steady state writes nothing and raises nothing.

### The race was made deterministic before anything was changed (PPW-578)

The suite's own 20-caller test exposed this once in 1457 tests, which is too rare to prove a fix.
So a rival connection holds an uncommitted `CREATE SEQUENCE` of the same name; the caller's create
is invisible to it, tries to insert the same catalogue row, and blocks; the rival then commits and
the caller's create fails. All three new tests were red this way on
`23505: duplicate key value violates unique constraint "pg_class_relname_nsp_index"` — the order
site naming `CREATE SEQUENCE "order_number_seq_2026"` as the failing statement — and pass after
the fix. The handshake polls `pg_stat_activity` from a separate connection, because that view is
snapshotted per transaction and a poll inside the rival's transaction never sees the block.

### What the approach-check asked for, and what was declined (PPW-578)

Adopted: `duplicate_object` (42710), which the catalogue's type-name pre-check can raise ahead of
any unique violation and would otherwise escape the handler; the `relkind = 'S'` re-check, so a
same-named table re-raises instead of surfacing later as a confusing `nextval` error; a name guard
in the helper, so a series carrying a quote throws instead of reaching the DDL.

Declined, with reasons: schema-qualifying the create and `nextval` would repoint any deployment
whose `search_path` is not `public` at a different sequence, which on invoice numbers is worse
than the drift it fixes; a `lock_timeout` plus `55P03` handling would need a retry loop to be
correct, and both sites run the create in autocommit, where the wait is one `CREATE SEQUENCE`
statement long; pre-creating next year's sequences removes DDL from the request path but is a new
boot-time mechanism and still needs the lazy create to be safe for a new series. The counter-table
alternative is forbidden by ADR-020 without re-opening its trade-off.

### The class's third site is a migration, and one line of it cannot be fixed (PPW-578)

The seed at `Migrations/20260820133204_InitialPostgres.cs:746` runs the same statement. Migrations
apply at boot with no migration lock, but two instances booting together collide on the first
`CREATE TABLE` long before the sequence line, and each migration runs in its own transaction, so
the racing unit is the whole migration rather than this statement. Guarding the one line would
prove nothing. Adjacent to PPW-560, which owns the boot path.

### Parked: the invoice numbering's documented transaction contract has no caller (PPW-569)

The class doc requires callers to allocate the number inside the transaction that persists the
`Invoice` row. `BeginTransactionAsync` appears only in `Services/CartService.cs`, so neither the
webhook path nor the admin path opens one, and the contract is unhonoured. The round is
unattended, so this is parked rather than guessed: either the callers or the doc is wrong, and
which one is the owner's ruling. The fix here is correct under both.

### Parked: PPW-569 was reused rather than a second id minted (PPW-569)

The driver's instruction was to mint the next id for this defect. Minting it for the order site
was right, but the invoice site already had a row from v9 — same statement, same race, and its
suggested fix is the one taken here. A second id for one defect would corrupt the ledger, so the
row was pulled out of the backlog instead, following this target's own precedent with PPW-568.
Routing a backlog row is the owner's call, so the choice is parked for the run-end report.

### The micro-review found five real gaps in this round's own work (PPW-578)

Adopted, in `4d6bc6d`: the `EF1002` suppression is back, because dropping it added a build warning
and lost the note that a DDL identifier cannot be a parameter — the name guard is now named as the
mitigation; the race helper no longer leaks its connection when its own `CREATE` fails, and its
dispose is idempotent; each race test now releases the rival and observes the call in flight before
disposing the context, so a handshake that times out cannot tear a connection down mid-command;
and the helper's own failure modes are covered — four unusable names throw before any SQL, and a
name a concurrent transaction gives to a table re-raises, which was the only branch of the helper
no test executed.

Declined: a log line on the swallow, because nothing here reads Npgsql notices and a
`RAISE WARNING` would be invisible; and a comment stating that the `relkind` probe needs
READ COMMITTED — true, but no caller sets an isolation level, and under a stricter one the helper
re-raises, which is exactly the behaviour before this fix.

### The bolt's stage documents were left as written (PPW-569)

`ddd-02-technical-design.md` and `ddd-03-test-report.md` still show the bare `IF NOT EXISTS`
create and call it safe. They record what bolt 038 designed and tested, not what the code does
now, so rewriting them would falsify that record. The two documents that do state current
behaviour — the data-stack standard and ADR-020 — are corrected in `de1a4cb`.

### Two neighbouring warts were left alone (PPW-578)

`OrderNumberService` opens the EF connection by hand and never closes it, and draws `nextval` on a
raw command with no transaction set. Both predate this defect and neither takes part in the race,
so touching them would widen a concurrency fix into unrelated connection handling.
