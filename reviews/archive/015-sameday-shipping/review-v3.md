---
type: review
target: 015-sameday-shipping
version: 3
supersedes: 2
commit: 8584572
branch: feat/bolt-036-sameday-api-client
pass-type: discovery
date: 2026-07-27
lenses: [correctness, security, requirements, quality, tests-coverage, race, db-parity, input-validation, observability, frontend-ux, completeness-critic]
lenses-not-run: []
verdict: request-changes
blockers: [D43, D44, D45]
findings: { high: 3, medium: 8, low: 1, cleanup: 0, refuted: 1 }
tests: { dotnet: "893/893 (+10 skipped MinIO)", frontend: "451/451" }
---

# Review v3 — 015-sameday-shipping

## Findings

| F# | D# | Sev | Title | File | Fix now? |
|---|---|---|---|---|---|
| F1 | D43 | 🔴 | Easybox `Continue` never re-enables after typing contact (`canContinue` cannot see `form.valid`) | `UI/…/delivery-step.ts:326` | yes |
| F2 | D44 | 🔴 | Slow-Sameday `OperationCanceledException` treated as shutdown → tracking poll loop exits | `BackgroundJobs/ShipmentTrackingJob.cs:54` | yes |
| F3 | D45 | 🔴 | No per-order guard before the vendor AWB call; DB CAS blocks only the 2nd DB write | `Services/Sameday/AwbCreator.cs:69` | yes |
| F4 | D46 | 🟠 | `isDeliveryComplete()` Easybox gate ignores mandatory contact → stepper skip to payment → 400 | `UI/…/checkout-state.service.ts:51` | yes |
| F5 | D47 | 🟠 | Same OCE-as-shutdown bug drops an AWB dispatch job silently | `BackgroundJobs/AwbDispatcher.cs:69` | yes |
| F6 | D48 | 🟠 | `LastTrackingSyncAt=UtcNow` fallback + monotonic guard can strand a Shipped order | `BackgroundJobs/ShipmentTrackingJob.cs:139` | yes |
| F7 | D49 | 🟠 | EuPlatesc webhook→AWB enqueue untested (Stripe-only from D6) | `Tests/…/PaymentControllerIntegrationTests.cs` | yes |
| F8 | D50 | 🟠 | `AwbDispatcher` outcome routing + re-enqueue untested | `BackgroundJobs/AwbDispatcher.cs:83` | yes |
| F9 | D51 | 🟠 | `Status != Cancelled` persist guard has no test | `Services/Sameday/AwbCreator.cs:107` | yes |
| F10 | D52 | 🟠 | A `429` surviving retries → permanent GiveUp instead of transient | `Services/Sameday/SamedayClient.cs:139` | yes |
| F11 | D54 | 🟠 | Paid→Cancelled orphan billable AWB — no compensating void or operator alert | `Services/Sameday/AwbCreator.cs:141` | yes |
| F12 | D31 | 🟠 | `GenerateAwbAsync` returns stale "generate manually" + pre-037 comment | `Services/SamedayShippingService.cs:52` | yes |
| F13 | D53 | 🟡 | ADR-015 + 037 domain model name `awbPayment` as the idempotency key — doc trap | `memory-bank/…/adr-015-*.md` | yes |
| — | D21 | 🟡 | Raw vendor error body in exception + logged at Error (conditional PII) | `Services/Sameday/SamedayClient.cs:140` | no |
| — | D23 | 🟡 | Dual-DB parity: migrations + `timestamptz` CAS never run on Postgres | `Tests/…/OrderSamedayFieldsTests.cs:21` | no |

## Refuted

| Suspicion | Why it is not real |
|---|---|
| The ADRs name the wrong wire field, so the vendor reference breaks silently (cert-B) | The code sends the right field and a wire test asserts it; only the document is wrong, which is kept as D53. |

## Notes for the fixer

- This pass ran as a certification pair: two independent blinded full-manifest passes, cert-A and cert-B, over the frozen commit, each seeded with the 21 already-decided backlog and false-positive items. It did not certify — three new 🔴 re-arm the loop.
- Both passes independently raised D43, D45, D46, D49, D21 and the ADR-015 drift. D44 came from cert-A alone and was confirmed by hand against the code.
- D43 and D46 are regressions from the round-1 D4 fix, on live checkout. The two flags do not shield them. Do them first.
- D45 needs an owner decision before code: accept the interim residual with orphan logging, or add a durable per-order claim before the vendor call. It is the D2 residual, not a new defect.
- D44 and D47 are one bug in two places: a vendor timeout raises a cancellation that both loops read as shutdown. Fix both, and sweep the retry job for the same shape.
- D50, D51 and D49 are missing tests on guards that already work. Each new test must go red when its guard is removed.
- D21 and D23 were re-raised with their prior decisions attached and stand as deferred; the multi-lens re-raise on D21 is worth noting when the round touches that file.
