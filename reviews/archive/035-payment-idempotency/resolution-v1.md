---
type: resolution
target: 035-payment-idempotency
version: 1
answers: review-v1.md
status: resolved
fixed_commit: 650f615
closed: 2026-06-19
---

# Resolution v1 — 035-payment-idempotency

## Findings

| ID | Status | Commit | Note |
|---|---|---|---|
| PPW-1 | fixed | `2093302` | Catch the unique-index violation on insert: the same owner replays, another caller gets a clean 409, anything else is rethrown. The regression tests run on real SQLite, because the in-memory provider does not enforce the index. |
| PPW-2 | fixed | `2093302` | The keyed lookup and the stale-key free both filter on user id or guest session id. Unit, controller and integration tests prove a second caller receives neither the order nor its Stripe secret. |
| PPW-3 | fixed | `2093302` | An order-independent signature of product, upload and quantity joins the divergence comparison, so a same-total different-items request is refused and names the items. |
| PPW-4 | fixed | `b52f4b6` | A searchable follow-up marker sits on the missing-key warning, tracking the later change to a 400. |
| PPW-5 | fixed | `2093302` | The two lookups became one owner-scoped round-trip, with the freshness branch decided in memory. |
| PPW-6 | fixed | `0b0fa04` | An action filter reads the header once, normalises a blank key to none, warns when it is missing and stashes it. Both endpoints read it back through one accessor. |
| PPW-7 | fixed | `0b0fa04` | One generic method holds resolve, replay, compute, persist and respond; both processors are thin adapters over it. |
| PPW-8 | fixed | `b52f4b6` | Stripe is keyed by the order id instead of the caller's key, so retries of one order still share a key while distinct orders never do. |
| PPW-9 | fixed | `2f1872c` | The migration branches on the active provider: Postgres gets sized text and the filtered index, and the SQLite output is byte-identical. The snapshot half stays open; it is PPW-20. |
| PPW-10 | fixed | `2093302` | The comment now names both database engines. |
| PPW-11 | fixed | `b52f4b6` | Reworded to say that multiple nulls are permitted. |
| PPW-12 | deferred | — | Historical design sketch. The deviation is already written up in the walkthrough, so correcting the sketch is low-value churn; batched into a later documents pass. |
| PPW-13 | wont-fix | — | Kept. The type carries the divergent-field payload, and the PPW-1 fix now also throws the plain conflict type, so the two coexist meaningfully. |
| PPW-14 | fixed | `b52f4b6` | One accessor and one shared key constant replace the raw reads in the controller and the exception middleware, and the setter in the correlation middleware. |
| PPW-15 | wont-fix | — | Kept. The order must exist before the gateway call so a crash during it is recoverable, and the PPW-1 fix preserves that ordering. |
| PPW-16 | fixed | `b6198b6` | A real SQLite factory builds the schema and drives the cross-caller scenario over HTTP, reaching the refusal the in-memory provider cannot produce. |
| PPW-17 | wont-fix | — | Kept. An expired key blocked for a second caller is the accepted price of one global key index; it is safe and discloses nothing. It resolves only with owner-plus-key uniqueness. |
| PPW-18 | fixed | `3415ec7` | SQLite joins the count-based branch and the Postgres sequence path is byte-identical. A new test drives the real service against a real SQLite database, and the test fake is gone. |
| PPW-19 | fixed | `650f615` | The searchable marker and the document pointer are back in the filter's missing-key branch; the log call is untouched. |

## Scope

| Cluster | Findings | Files | Approach-check |
|---|---|---|---|
| A — The two required fixes, the divergence check and the folded lookup (`2093302`) | PPW-1, PPW-2, PPW-3, PPW-5, PPW-10 | `Services/OrderService.cs`, `Tests/…/OrderServiceIdempotencyConcurrencyTests.cs` | not recorded (predates approach-checks) |
| B — Gateway keying, the tracking marker, the correlation accessor and two comments (`b52f4b6`) | PPW-4, PPW-8, PPW-11, PPW-14 | `Controllers/PaymentsController.cs`, `Middleware/…`, `Data/PhotoPrintDbContext.cs` | not recorded (predates approach-checks) |
| C — The key filter and the shared replay method (`0b0fa04`) | PPW-6, PPW-7 | `Filters/IdempotencyKeyFilter.cs`, `Controllers/PaymentsController.cs` | not recorded (predates approach-checks) |
| D — Provider-aware migration (`2f1872c`) | PPW-9 | `Migrations/20260527075359_AddOrderIdempotencyKey.cs` | not recorded (predates approach-checks) |
| E — The real-database cross-caller test (`b6198b6`) | PPW-16 | `Tests/…/SqlitePaymentFactory.cs`, `Tests/…/PaymentIdempotencyRelationalTests.cs` | not recorded (predates approach-checks) |
| F — Order numbers on SQLite (`3415ec7`) | PPW-18 | `Services/OrderNumberService.cs`, `Tests/…/OrderNumberServiceSqliteTests.cs` | not recorded (predates approach-checks) |
| G — The restored tracking marker (`650f615`) | PPW-19 | `Filters/IdempotencyKeyFilter.cs` | not recorded (predates approach-checks) |
| H — Left undone this round | PPW-12, PPW-13, PPW-15, PPW-17 | — | not needed (no code changed) |

## Decisions

### One file records the work answering three passes (PPW-16, PPW-17, PPW-18, PPW-19)

Clusters A and B answer this review. Clusters C, D and E answer two items that passes v2 and v3 raised
while checking those fixes, and clusters F and G answer two more that pass v3 raised. Those passes wrote
no review file of their own, so all of it is recorded here rather than split across files that would each
hold two rows. The ledger's history lines carry which pass raised which row.

### Uniqueness stayed global rather than moving to owner and key (PPW-1, PPW-2)

The review offered per-caller key namespaces as an option. One global index was kept, and a key held by
another caller now returns a plain refusal, because re-resolution is owner-scoped and can never replay
somebody else's order. Per-owner uniqueness would need two filtered partial indexes over two nullable
columns, which is the same dual-database complexity PPW-9 is about, for a collision that is vanishingly
unlikely with random keys. The price of that choice is recorded as PPW-17.

### The concurrency tests run on real SQLite rather than the in-memory provider

The in-memory provider does not enforce unique indexes, so it cannot reproduce this race at all. The
tests use a shared-connection SQLite database and a one-shot save interceptor to inject the winning
request deterministically. That is why a SQLite test package was added to the test project.

### The stale-key free became owner-scoped, and one existing test was retargeted

A caller now frees only their own expired key, never an ownerless or another caller's row. One existing
test seeded an ownerless stale order freed by a different user; it was retargeted to the owner-scoped
scenario. That tightens the test to the secure behaviour rather than weakening it.

### The migration was edited in place instead of adding a new one

Safe here: the SQLite output is byte-identical, development databases are created directly rather than
migrated, and no Postgres database has applied it before deployment. The model snapshot stays
SQLite-flavoured, so scaffold-time drift is reduced rather than removed.

### Two rows were kept rather than fixed (PPW-13, PPW-15)

Both are shapes the review itself suspected were deliberate, and both turned out to be. The second
conflict type earns its place now that the PPW-1 fix throws the plain one as well, and the two saves are
what makes a crash during the gateway call recoverable.

### The test fake that made PPW-16 pass was hiding a real defect (PPW-16, PPW-18)

The new real-database factory could not run without faking the order-number service, because that service
had no SQLite branch. The fake made the test green while papering over a break in the documented local
development path. Removing it is what turned PPW-18 into a finding, and the factory now drives the real
service end to end.
