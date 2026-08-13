---
type: review
target: 035-payment-idempotency
version: 1
supersedes: null
commit: 691e23d
branch: feat/bolt-035-payment-idempotency
pass-type: discovery
date: 2026-06-18
lenses: [correctness, security, pr-requirements, quality-altitude, tests-verification]
lenses-not-run: —
verdict: request-changes
blockers: [PPW-1, PPW-2]
findings: { high: 1, medium: 6, low: 5, cleanup: 3, refuted: 3 }
tests: { dotnet: "8/8 targeted idempotency tests; the commit claims 457/457 overall", frontend: "not recorded" }
---

# Review v1 — 035-payment-idempotency

## Findings

| ID | Sev | Title | File | Fix now? |
|---|---|---|---|---|
| PPW-1 | 🔴 | Two concurrent requests with the same key both insert, and the loser returns 500 instead of a replay | `Services/OrderService.cs:392` | yes |
| PPW-2 | 🟠 | The idempotency lookup is not scoped to the caller, so another tenant's order and its Stripe secret can be handed over | `Services/OrderService.cs:437` | yes |
| PPW-3 | 🟠 | The divergence check ignores cart contents, so a reused key can replay the wrong photos at the same total | `Services/OrderService.cs:449` | yes |
| PPW-4 | 🟠 | Nothing tracks the planned change that makes the key header required | `Controllers/PaymentsController.cs:105` | no |
| PPW-5 | 🟠 | A second database round-trip looks up the stale row the first query already covered | `Services/OrderService.cs:408` | no |
| PPW-6 | 🟠 | Header extraction and the missing-key warning are copied into both payment endpoints | `Controllers/PaymentsController.cs:49` | no |
| PPW-7 | 🟠 | The replay, compute and persist sequence is duplicated between the two processor branches | `Controllers/PaymentsController.cs:51` | no |
| PPW-8 | 🟡 | A freed and reused key is forwarded to Stripe with a possibly different amount | `Services/StripePaymentGateway.cs:31` | no |
| PPW-9 | 🟡 | The migration writes an unfiltered `TEXT` index while the runtime model filters it on Postgres | `Migrations/20260527075359_AddOrderIdempotencyKey.cs:34` | no |
| PPW-10 | 🟡 | A comment credits the per-statement constraint check to Postgres alone, though SQLite enforces it too | `Services/OrderService.cs:405` | no |
| PPW-11 | 🟡 | The unique-index null comment reads as if duplicate nulls were forbidden | `Data/PhotoPrintDbContext.cs:146` | no |
| PPW-12 | 🟡 | The ddd-02 design sketch puts conflict resolution in the controller, the code puts it in the order service | `memory-bank/…/ddd-02` | no |
| PPW-13 | ⚪ | A second conflict exception type exists only to carry the divergent-field payload | `Exceptions/IdempotencyConflictException.cs` | no |
| PPW-14 | ⚪ | The correlation id is read out of the request items bag by a raw string key in two places | `Controllers/PaymentsController.cs:109` | no |
| PPW-15 | ⚪ | Each create saves twice, once in the service and once in the controller | `Controllers/PaymentsController.cs:65` | no |

## Refuted

| Suspicion | Why it is not real |
|---|---|
| The blank-key check is a redundant conditional | It normalises a whitespace-only key to null so such keys cannot collide on the unique index. Removing it changes behaviour. |
| A replay whose stored secret is null falls through and calls Stripe again | Deliberate crash recovery: the order exists but the secret was never stored, and the forwarded key makes the gateway return the same payment intent, so no second charge. It owes a test, not a fix. |
| Freeing a stale key can null another tenant's key | That branch runs only when no order holds the key inside the window, so it can only clear a key that already expired. Security confidence 3 out of 10. |

## Notes for the fixer

- Fix PPW-1 and PPW-2 before merge; PPW-3 is strongly recommended in the same change, because a replay of the
  wrong photos is cheap to close and specific to this shop.
- PPW-1 needs a test that runs on a real database engine. The default in-memory provider does not enforce
  unique indexes, so it cannot reproduce the race at all.
- PPW-2 is the only new query that ignores the ownership pattern the rest of the repository follows. Apply
  the same owner filter to the stale-key free, not just the lookup.
- Decide once whether uniqueness stays global or moves to owner-plus-key. PPW-2's fix, PPW-9 and the shape of
  PPW-1's recovery all turn on that answer.
- PPW-5, PPW-6, PPW-7, PPW-13, PPW-14 and PPW-15 carry no behaviour change. PPW-6, PPW-7 and PPW-14 are one theme: idempotency
  handling is copied per endpoint instead of living in one place.
- Four untested behaviours were listed with no finding id and are still work this round owes: divergence
  on delivery type, locker id and total; the EuPlatesc conflict path; a replay whose stored value is
  null; and a whitespace-only key.
- Every test in the suite passes today and none of them covers a second caller, a second thread or a
  second database engine. A green suite here proves the single-caller happy path only.
- This is one discovery pass, so it cannot call the feature clean. Closing it wants a later blinded pass
  that comes back quiet.
