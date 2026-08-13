---
type: review
target: 035-payment-idempotency
version: 8
supersedes: 7
commit: 50fc692
branch: feat/bolt-035-payment-idempotency
pass-type: discovery
date: 2026-06-19
lenses: [7 lenses; composition not recorded]
lenses-not-run: —
verdict: approve-with-followups
blockers: []
findings: { high: 0, medium: 2, low: 9, cleanup: 7, refuted: 1 }
tests: { dotnet: "474/474", frontend: "not recorded" }
---

# Review v8 — 035-payment-idempotency

## Findings

| ID | Sev | Title | File | Fix now? |
|---|---|---|---|---|
| PPW-36 | 🟠 | No test touches the Postgres path: not the constraint match, not the filtered index, not the migration | `Services/OrderService.cs:197` | yes |
| PPW-37 | 🟠 | The cross-tenant key collision has no distinct log event, so key probing is invisible | `Services/OrderService.cs:178` | yes |
| PPW-38 | 🟡 | On SQLite the key violation is recognised by a phrase in the error message, not a structured code | `Services/OrderService.cs:194` | yes |
| PPW-39 | 🟡 | One global key index tells an attacker whether a guessed key is in use, and lets them reserve it first | `Data/PhotoPrintDbContext.cs:308` | no |
| PPW-40 | 🟡 | The EuPlatesc recovery replay builds a fresh redirect URL instead of returning the stored one | `Controllers/PaymentsController.cs:131` | no |
| PPW-41 | 🟡 | The key is never trimmed, so a padded copy of the same key creates a second order and a second charge | `Filters/IdempotencyKeyFilter.cs:23` | no |
| PPW-42 | 🟡 | Freeing the stale key and inserting the new order are two separate saves with nothing to roll them back together | `Services/OrderService.cs:99` | no |
| PPW-43 | 🟡 | On SQLite two racing requests can collide on the order-number index first, which the recovery does not handle | `Services/OrderService.cs:162` | no |
| PPW-17 | 🟡 | An expired key stays reserved for its first owner, so a second caller is refused a key the contract calls free | `Services/OrderService.cs:184` | no |
| PPW-20 | 🟡 | The model snapshot is SQLite-flavoured, so the next Postgres scaffold emits a phantom migration | `Migrations/PhotoPrintDbContextModelSnapshot.cs` | no |
| PPW-44 | 🟡 | The order service's price-tier lookup is a comment-claimed copy of the cart service's, but reads a different quantity | `Services/OrderService.cs:413` | no |
| PPW-45 | ⚪ | The public key lookup has no production caller left | `Services/OrderService.cs:220` | no |
| PPW-46 | ⚪ | The cart seed graph is rebuilt in three test fixtures and has already drifted apart | `Tests/…/OrderServiceTests.cs:43` | no |
| PPW-47 | ⚪ | The concurrency test hand-builds the winning order with copied number totals | `Tests/…/OrderServiceIdempotencyConcurrencyTests.cs:166` | no |
| PPW-48 | ⚪ | The two replay-logging branches repeat the event shape and test the replay flag twice | `Controllers/PaymentsController.cs:115` | no |
| PPW-49 | ⚪ | The exception middleware reads the hosting environment two different ways in one request | `Middleware/ExceptionHandlerMiddleware.cs:88` | no |
| PPW-50 | ⚪ | The declared 409 response has no body type, so generated clients never see the divergent-field list | `Controllers/PaymentsController.cs:45` | no |
| PPW-51 | ⚪ | The transitional missing-key event logs at warning level on every payment request | `Filters/IdempotencyKeyFilter.cs:49` | no |

## Refuted

| Suspicion | Why it is not real |
|---|---|
| Two writers reusing a stale key on Postgres interleave into an unpredictable replay-or-refusal | Both skeptics traced the post-collision path back to one clean refusal, with exactly one order stored either way. The atomicity gap itself is real and is kept as PPW-42. |

## Notes for the fixer

- Nothing here blocks merge. Close PPW-36 and PPW-37 before relying on this in production, and harden PPW-38
  next: five lenses reached it on their own, the strongest agreement in this pass.
- PPW-36 is the one that makes the rest hard to trust. The suite is green on SQLite and in memory, and
  production is Postgres, so the whole Postgres arm of this feature is proven by nothing. The literal
  constraint name the code matches on does equal the configured index name today; that was checked.
- PPW-39 and PPW-17 share one root: the key index is global while the lookup is per caller. Deciding that
  question once settles both, and it is a schema change, not a code change.
- PPW-42 and PPW-43 both sit inside the create path's recovery. Read them together before touching it, and
  keep them apart from PPW-38, which is about how a violation is recognised rather than what is retried.
- PPW-40 is the only place where the two payment processors behave differently under retry. Stripe is
  deduplicated by the gateway, EuPlatesc is not.
- Six of the eighteen are test or duplication work: PPW-45, PPW-46, PPW-47, PPW-48, PPW-49 and PPW-50.
- Every lens was barred from reading earlier passes, so two of these are problems earlier passes had
  already named and decided: PPW-17 and PPW-20. Their prior rulings are on the ledger and must be read before
  they are re-decided.
- This is a discovery pass on one commit, not a check of the earlier fixes, and it cannot call the
  feature clean.
