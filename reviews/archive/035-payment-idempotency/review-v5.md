---
type: review
target: 035-payment-idempotency
version: 5
supersedes: 4
commit: 224c711
branch: feat/bolt-035-payment-idempotency
pass-type: discovery
date: 2026-06-19
lenses: [correctness-finder-1, correctness-finder-2, security, pr-requirements, quality, db-parity]
lenses-not-run: —
verdict: approved
blockers: []
findings: { high: 0, medium: 3, low: 6, cleanup: 6, refuted: 13 }
tests: { dotnet: "466/466", frontend: "not recorded" }
---

# Review v5 — 035-payment-idempotency

## Findings

| ID | Sev | Title | File | Fix now? |
|---|---|---|---|---|
| PPW-20 | 🟠 | The model snapshot is SQLite-flavoured, so the next Postgres scaffold emits a phantom migration | `Migrations/PhotoPrintDbContextModelSnapshot.cs` | no |
| PPW-21 | 🟠 | The Stripe secret column is sized at exactly the vendor ceiling, so a longer secret fails on Postgres after the charge | `Data/PhotoPrintDbContext.cs:296` | yes |
| PPW-22 | 🟠 | The 409 body names the divergent fields only outside Development, and no test reads the body | `Middleware/ExceptionHandlerMiddleware.cs:103` | yes |
| PPW-23 | 🟡 | The recovery catch infers that any database write failure was the key collision | `Services/OrderService.cs:161` | no |
| PPW-24 | 🟡 | With both owner ids null the scope test collapses to "any order without a guest id" | `Services/OrderService.cs:235` | no |
| PPW-25 | 🟡 | Key length is never checked, so an over-long key fails on Postgres instead of being refused | `Filters/IdempotencyKeyFilter.cs:22` | no |
| PPW-26 | 🟡 | The reserved conflict log event is never emitted | `Middleware/ExceptionHandlerMiddleware.cs` | no |
| PPW-27 | 🟡 | The recovery replay calls the gateway again and writes no replay log, so it reads as a fresh request | `Controllers/PaymentsController.cs:117` | no |
| PPW-28 | 🟡 | The design document says the stale key is freed inside the insert's transaction; the code uses a separate save | `Services/OrderService.cs:108` | no |
| PPW-29 | ⚪ | No document states that the gateway is keyed by the order id rather than the caller's key | `memory-bank/…/ddd-02` | no |
| PPW-30 | ⚪ | The pre-insert and post-collision resolution blocks are near duplicates | `Services/OrderService.cs` | no |
| PPW-31 | ⚪ | Provider names are written out as literal strings in four places | `Data/PhotoPrintDbContext.cs` | no |
| PPW-32 | ⚪ | The controller saves through the database context itself rather than through the order service | `Controllers/PaymentsController.cs:125` | no |
| PPW-33 | ⚪ | The payment request builders and the SQLite fixture setup are duplicated across test files | `Tests/…` | no |
| PPW-34 | ⚪ | The order-number query raises a compiler warning and its Postgres branch has no test | `Services/OrderNumberService.cs:33` | no |

## Refuted

| Suspicion | Why it is not real |
|---|---|
| Reading a missing key from the request items bag throws | The items indexer returns null for a missing key, and the existing fallback in the middleware relies on exactly that. |
| A borrowed key can reach another caller's order on one of the read paths | All three reads are owner-scoped: the first lookup, the stale-key free and the post-collision re-resolve. Proven by a real-database test. |
| The 409 body leaks field values or another caller's data | It carries field names only, and only for the caller's own earlier order; the cross-caller path throws a fixed message. |
| A secret is logged or returned to the wrong caller | The only log lines carry processor, order id and correlation id; secrets come only from the owner-scoped order. |
| Stripe is keyed by the caller's header, so a recycled key collides at the gateway | The controller keys Stripe by the order id, asserted by an existing test. |
| Duplicate and null key handling differs between the two database engines | Both allow any number of key-less orders and both refuse a duplicate non-null key. |
| The freshness window is inverted or off by one | The comparison is exactly the specification's, rearranged. |
| Two different carts can produce the same item signature | The signature is built from identifier pairs and quantities in a fixed order, so distinct carts cannot collide. |
| Two same-owner same-key requests double-create or return 500 | The loser catches the violation, re-resolves the winner and replays; one row survives. Proven by a real-database test. |
| The divergence check still misses the cart items | Items take part through the signature, and a same-total different-items request is refused. |
| The legacy processor redirect URL overflows, or key namespaces can be griefed | The URL fits its column with wide margin, and global keys are inherent to any globally scoped key. |
| A the legacy processor body at the Stripe endpoint replays across processors | A mismatched body is refused on the processor field; the remaining edge only ever builds a value for the caller's own order. |

## Notes for the fixer

- Nothing here blocks merge. Two items are cheap and matter before deployment: PPW-21 and PPW-22.
- PPW-21 is the clearest case of the local database hiding a production failure. Length limits are ignored
  locally and enforced on Postgres, and the failure lands after the charge already exists.
- PPW-23 and PPW-24 are both residuals of earlier fixes to this same recovery path, not new behaviour. Read
  them next to PPW-1 and PPW-2 on the ledger before changing the catch.
- PPW-20 is scaffold-time only. There is no startup drift, because the runtime model and this migration
  agree; the trap is for whoever scaffolds the next migration.
- PPW-26, PPW-27 and PPW-22 are one theme: the feature's documented signals are reserved but not emitted, so a
  conflict, a recovery replay and a divergent field are all invisible in operation.
- The lenses ran blind to the earlier passes and to the finding ids written into the source comments,
  which were handed over as claims to check rather than as facts.
- This is a discovery pass over the whole change, not a check of the earlier fixes. Landing on the same
  bottom line as the pass before it is corroboration, not agreement.
