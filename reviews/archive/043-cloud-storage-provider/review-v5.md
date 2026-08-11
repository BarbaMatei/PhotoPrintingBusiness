---
type: review
target: 043-cloud-storage-provider
version: 5
supersedes: 4
commit: 972a8b4
branch: feat/bolt-043-cloud-storage-provider
pass-type: delta-discovery
date: 2026-07-20
lenses: [correctness, race, tests-coverage, frontend-ux, completeness-critic]
lenses-not-run: [security, db-parity, observability, requirements, input-validation, quality]
verdict: approve-with-followups
blockers: []
findings: { high: 0, medium: 3, low: 10, cleanup: 1, refuted: 0 }
tests: { dotnet: "701/701 (+10 skipped MinIO)", frontend: "438/438" }
---

# Review v5 — 043-cloud-storage-provider

## Findings

| F# | D# | Sev | Title | File | Fix now? |
|---|---|---|---|---|---|
| F1 | D36 | 🟠 | A stale lightbox photo id re-opens a closed lightbox when a thumbnail URL expires | `UI/…/order-detail-page.ts` | yes |
| F2 | D38 | 🟠 | Unroutable Cloud rows are skipped after the fetch, starving local-orphan cleanup | `BackgroundJobs/UploadCleanupJob.cs` | yes |
| F3 | D37 | 🟠 | The periodic re-scan is untested and untestable — the interval has no test seam | `BackgroundJobs/PromotionRecoveryScanner.cs` | no |
| F4 | D35 | 🟡 | Periodic promotion sweep has no in-flight dedup, so it can start a second promotion of one order | `BackgroundJobs/PromotionRecoveryScanner.cs` | no |
| F5 | D46 | 🟡 | The periodic sweep re-enqueues permanently failed promotions forever | `BackgroundJobs/PromotionRecoveryScanner.cs` | no |
| F6 | D45 | 🟡 | The ZIP pre-flight throws an unmapped exception → a generic 500 logged as unhandled | `Services/AdminOrderService.cs` | no |
| F7 | D47 | 🟡 | The cloud-enabled flag is fixed at boot, so switching provider at runtime needs a restart | `BackgroundJobs/PromotionRecoveryScanner.cs` | no |
| F8 | D43 | 🟡 | A 401 for a non-authenticated user leaves a blank order body, with no error and no redirect | `UI/…/order-detail-page.ts` | no |
| F9 | D42 | 🟡 | The lightbox tells the user to reload while the app is already fetching a fresh URL | `UI/…/photo-lightbox.component.ts` | no |
| F10 | D48 | 🟡 | The lightbox failure flag is reset only on a changed URL, so an identical refreshed URL stays stuck | `UI/…/photo-lightbox.component.ts` | no |
| F11 | D40 | 🟡 | The anti-refresh-loop guard has no test | `UI/…/order-detail-page.ts` | no |
| F12 | D41 | 🟡 | The lightbox focus trap has no spec | `UI/…/photo-lightbox.component.ts` | no |
| F13 | D39 | 🟡 | Renamed guard tests seed an empty database, so they pass for the wrong reason | `Tests/…/PromotionRecoveryScannerTests.cs` | no |
| F14 | D44 | ⚪ | Order retries and init subscriptions have no in-flight dedup or teardown | `UI/…/order-detail-page.ts` | no |

## Refuted

| Suspicion | Why it is not real |
|---|---|
| Nothing was refuted. The 19 raw findings reduced to 14 canonical ones and no skeptic dropped any of them. | — |

## Notes for the fixer

- This pass is scoped to the v3 fix round and is blinded, with the nine terminal ledger decisions passed in so they are not re-litigated. None of them re-surfaced.
- Every finding traces to a v3 fix. That is the second fix-generative round in a row.
- Two clusters dominate. The first is the promotion sweep the D19 fix converted from boot-only to periodic: it shipped without dedup (D35), without a test that can fail (D37), without a give-up marker (D46) and without runtime configuration awareness (D47). The second is the URL refresh the D5b fix added: it caused a regression (D36) and left three loose ends (D40, D42, D48).
- Fix D36 and D38 now. Both are self-contained, both are real, and each has a named regression test.
- Treat the sweep cluster as one design item rather than four patches, and fold it into the concurrency-token work that already carries D9 and D27. Patching it here would re-seed the same fix-generativity.
- D36 was the strongest signal of the pass: four independent lenses raised it and no skeptic could find a guard.
- D35 was deliberately left out of the decided list so its re-find counts as independent agreement rather than a suppressed match.
- Four findings are "the suite is green but this behaviour has no test that can fail": D37, D39, D40 and D41.
- Certification is still out of reach. It is gated behind a quiet delta pass, and this one is not quiet.
