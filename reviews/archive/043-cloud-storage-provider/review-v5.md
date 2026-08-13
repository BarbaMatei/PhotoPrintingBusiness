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

| ID | Sev | Title | File | Fix now? |
|---|---|---|---|---|
| PPW-185 | 🟠 | A stale lightbox photo id re-opens a closed lightbox when a thumbnail URL expires | `UI/…/order-detail-page.ts` | yes |
| PPW-187 | 🟠 | Unroutable Cloud rows are skipped after the fetch, starving local-orphan cleanup | `BackgroundJobs/UploadCleanupJob.cs` | yes |
| PPW-186 | 🟠 | The periodic re-scan is untested and untestable — the interval has no test seam | `BackgroundJobs/PromotionRecoveryScanner.cs` | no |
| PPW-184 | 🟡 | Periodic promotion sweep has no in-flight dedup, so it can start a second promotion of one order | `BackgroundJobs/PromotionRecoveryScanner.cs` | no |
| PPW-195 | 🟡 | The periodic sweep re-enqueues permanently failed promotions forever | `BackgroundJobs/PromotionRecoveryScanner.cs` | no |
| PPW-194 | 🟡 | The ZIP pre-flight throws an unmapped exception → a generic 500 logged as unhandled | `Services/AdminOrderService.cs` | no |
| PPW-196 | 🟡 | The cloud-enabled flag is fixed at boot, so switching provider at runtime needs a restart | `BackgroundJobs/PromotionRecoveryScanner.cs` | no |
| PPW-192 | 🟡 | A 401 for a non-authenticated user leaves a blank order body, with no error and no redirect | `UI/…/order-detail-page.ts` | no |
| PPW-191 | 🟡 | The lightbox tells the user to reload while the app is already fetching a fresh URL | `UI/…/photo-lightbox.component.ts` | no |
| PPW-197 | 🟡 | The lightbox failure flag is reset only on a changed URL, so an identical refreshed URL stays stuck | `UI/…/photo-lightbox.component.ts` | no |
| PPW-189 | 🟡 | The anti-refresh-loop guard has no test | `UI/…/order-detail-page.ts` | no |
| PPW-190 | 🟡 | The lightbox focus trap has no spec | `UI/…/photo-lightbox.component.ts` | no |
| PPW-188 | 🟡 | Renamed guard tests seed an empty database, so they pass for the wrong reason | `Tests/…/PromotionRecoveryScannerTests.cs` | no |
| PPW-193 | ⚪ | Order retries and init subscriptions have no in-flight dedup or teardown | `UI/…/order-detail-page.ts` | no |

## Refuted

| Suspicion | Why it is not real |
|---|---|
| Nothing was refuted. The 19 raw findings reduced to 14 canonical ones and no skeptic dropped any of them. | — |

## Notes for the fixer

- This pass is scoped to the v3 fix round and is blinded, with the nine terminal ledger decisions passed in so they are not re-litigated. None of them re-surfaced.
- Every finding traces to a v3 fix. That is the second fix-generative round in a row.
- Two clusters dominate. The first is the promotion sweep the PPW-168 fix converted from boot-only to periodic: it shipped without dedup, without a test that can fail, without a give-up marker and without runtime configuration awareness. The second is the URL refresh the PPW-154 fix added: it caused a regression and left three loose ends (PPW-189, PPW-191, PPW-197).
- Fix PPW-185 and PPW-187 now. Both are self-contained, both are real, and each has a named regression test.
- Treat the sweep cluster as one design item rather than four patches, and fold it into the concurrency-token work that already carries PPW-158 and PPW-176. Patching it here would re-seed the same fix-generativity.
- PPW-185 was the strongest signal of the pass: four independent lenses raised it and no skeptic could find a guard.
- PPW-184 was deliberately left out of the decided list so its re-find counts as independent agreement rather than a suppressed match.
- Four findings are "the suite is green but this behaviour has no test that can fail": PPW-186, PPW-188, PPW-189 and PPW-190.
- Certification is still out of reach. It is gated behind a quiet delta pass, and this one is not quiet.
