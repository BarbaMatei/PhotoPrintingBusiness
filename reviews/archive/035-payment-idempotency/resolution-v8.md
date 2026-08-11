---
type: resolution
target: 035-payment-idempotency
version: 8
answers: review-v8.md
status: resolved
fixed_commit: 065a516
closed: 2026-07-04
---

# Resolution v8 — 035-payment-idempotency

## Findings

| D# | Status | Commit | Note |
|---|---|---|---|
| D36 | deferred | `01b5264` | No test runs the Postgres path, because every fixture creates its schema directly and none applies the migration. A container-backed regression belongs with the migration and deployment work. The migration breadcrumb was refreshed. |
| D37 | fixed | `21a295a` | A distinct exception type for a key held by another caller, still mapped to 409, plus the reserved cross-caller log event. A regression test asserts both. |
| D38 | fixed | `6a370e0` | The index name is now one shared constant referenced by the index and by the match, so a rename breaks the build. SQLite matches the structured extended code and the column name rather than a message phrase. |
| D39 | deferred | `01b5264` | A per-caller composite index is a schema and migration change, so it rides with D36 and D20. An accepted-residual note sits at the index. Exploitability is low: keys are random and the probe creates a real charge. |
| D40 | deferred | `01b5264` | The row-lock fix needs the Postgres path that does not exist yet. Did the review's stated minimum: the difference between the two gateways under retry is documented at the build path and in ddd-02. |
| D41 | fixed | `b76eede` | The key is trimmed before the blank and length checks. The filter tests set the raw header directly, because HTTP strips the padding on the way in, and prove a padded key now matches a plain one. |
| D42 | fixed | `0d5a721` | The free and the insert share one save, so a failing insert rolls the free back. The framework orders the update before the insert, so there is no collision inside the batch. |
| D43 | fixed | `f71041f` | A bounded retry when the order-number index is the one violated, capped at three so a persistent clash still surfaces. The test drives the real number generator. |
| D17 | fixed | `6de2e58` | Documented rather than changed: reclamation is owner-scoped, and for anyone else the global index keeps the key reserved. The contract text, the entity and the service interface now agree with the code. |
| D20 | deferred | `01b5264` | Re-raise of the standing deferral from the earlier passes. Same home as D36: the migration and deployment work. |
| D44 | fixed | `f7a314a` | One shared tier resolver; each caller keeps its own quantity source, so behaviour is unchanged. The comment that claimed the two were identical is corrected. |
| D45 | fixed | `b8238a9` | The dead lookup is removed and its tests retargeted at the create path, so the coverage they gave is kept. |
| D46 | fixed | `71255b1` | One shared cart seed used by all three fixtures, with the drifted size link now set everywhere. |
| D47 | fixed | `4bbd9c5` | The winning order is built by calling the real create path, so no copied totals remain. Each test pins the collision it means to exercise. |
| D48 | fixed | `694788a` | One branch over the replay flag and the cached value replaces the two repeated blocks. |
| D49 | fixed | `cfa23af` | The writer is an instance method using the injected environment; the service-locator hop is gone. |
| D50 | fixed | `b37d322` | A typed problem-details body is declared on both endpoints, with the field list nullable so it matches what the runtime actually sends. |
| D51 | fixed | `065a516` | The event logs at information level. The code half landed first; the checking pass reopened the row because four documents still described the old level, and the alignment was finished in the same round. |

## Scope

| Cluster | Findings | Files | Approach-check |
|---|---|---|---|
| A — Cross-caller conflict as its own type and event (`21a295a`) | D37 | `Exceptions/…`, `Services/OrderService.cs`, `Middleware/ExceptionHandlerMiddleware.cs` | not recorded (predates approach-checks) |
| B — Constraint matching and the order-number retry (`6a370e0`, `f71041f`) | D38, D43 | `Services/OrderService.cs`, `Data/PhotoPrintDbContext.cs` | not recorded (predates approach-checks) |
| C — Key trimming and the atomic free-and-insert (`b76eede`, `0d5a721`) | D41, D42 | `Filters/IdempotencyKeyFilter.cs`, `Services/OrderService.cs` | not recorded (predates approach-checks) |
| D — Contract documents (`6de2e58`, `065a516`) | D17, D51 | `memory-bank/…/ddd-01`, `memory-bank/…/ddd-02`, `Models/Order.cs`, `Services/IOrderService.cs` | not recorded (predates approach-checks) |
| E — Shared pricing, dead code and the response type (`f7a314a`, `b8238a9`, `b37d322`) | D44, D45, D50 | `Services/OrderService.cs`, `Services/CartService.cs`, `Controllers/PaymentsController.cs` | not recorded (predates approach-checks) |
| F — Test seed and concurrency-test rebuild (`71255b1`, `4bbd9c5`) | D46, D47 | `Tests/…/OrderServiceTests.cs`, `Tests/…/OrderServiceIdempotencyConcurrencyTests.cs` | not recorded (predates approach-checks) |
| G — Controller and middleware tidying (`694788a`, `cfa23af`) | D48, D49 | `Controllers/PaymentsController.cs`, `Middleware/ExceptionHandlerMiddleware.cs` | not recorded (predates approach-checks) |
| H — Breadcrumbs and threat notes for the deferred cluster (`01b5264`) | D36, D39, D40, D20 | `Migrations/…`, `Data/PhotoPrintDbContext.cs` | not needed (notes only) |

## Decisions

### The owner set the scope: fix everything tractable, keep the database cluster deferred

Agreed on 2026-07-04. Every finding that could be closed with a regression test or a behaviour-preserving
refactor was closed in this round. The four that need a schema change, a migration run or a real Postgres
database were kept deferred to the migration and deployment phase, which is the same ruling the earlier
passes made. That is D36, D39, D40 and D20.

### Two committed tests were changed as documented consequences of fixes (D37, D38)

The cross-caller concurrency test now expects the new exception type D37 introduced. The "an unrelated
failure still propagates" test was re-pointed at a foreign-key violation, because after D43 an
order-number collision is a handled retry rather than an unrelated failure. Both changes are recorded
here rather than left to be discovered in the diff.

### The atomic fix relies on the framework's command ordering (D42)

Putting the free and the insert in one save works because the framework issues the update before the
insert, so the two never collide inside the batch. That contradicts the assumption recorded when D28 was
resolved, which held that one transaction would collide on the per-statement check. That assumption was
right for hand-written statements and wrong for this path. Verified on SQLite.

### One re-raised row upheld its earlier ruling and one overturned it (D17, D43)

D17 was ruled acceptable in an earlier pass and re-raised here. The fix chose the option the earlier
ruling already implied, writing the owner-scoped behaviour into the contract rather than changing it, so
the ruling stands. D43 is the opposite: the same behaviour was raised in an earlier pass, dismissed as
not a finding, and is fixed here.

### Two record errors are left standing rather than corrected

The pass counts eighteen new findings, while its own metrics note names two of them as problems earlier
passes had already found and decided, D17 and D20. The pass is also recorded as running seven lenses,
while the agreement section names eight. Both are kept as written.
