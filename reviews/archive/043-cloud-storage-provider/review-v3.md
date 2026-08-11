---
type: review
target: 043-cloud-storage-provider
version: 3
supersedes: 2
commit: 2be8ab8
branch: feat/bolt-043-cloud-storage-provider
pass-type: delta-discovery
date: 2026-07-14
lenses: [correctness, race, security, requirements, tests-coverage, completeness-critic, frontend-ux]
lenses-not-run: [db-parity, observability, input-validation, quality]
verdict: approve-with-followups
blockers: []
findings: { high: 0, medium: 8, low: 10, cleanup: 0, refuted: 1 }
tests: { dotnet: "686/686 (+10 skipped MinIO, run in CI)", frontend: "423/423" }
---

# Review v3 — 043-cloud-storage-provider

## Findings

| ID | Sev | Title | File | Fix now? |
|---|---|---|---|---|
| PPW-168 | 🟠 | Promotion recovery left boot-only while its purge sibling was made periodic | `BackgroundJobs/PromotionRecoveryScanner.cs` | yes |
| PPW-173 | 🟠 | Tier resolve throws with cloud disabled and wedges every cleanup batch | `BackgroundJobs/UploadCleanupJob.cs:92` | yes |
| PPW-170 | 🟠 | Purge sweep untested — tests call the sweep by reflection and `ExecuteAsync` is never driven | `BackgroundJobs/OriginalPurgeRecoveryScanner.cs:60` | yes |
| PPW-172 | 🟠 | Production-complete purge lacks the cancel path's try/catch → 500 after the transition committed | `Services/AdminOrderService.cs:135` | yes |
| PPW-171 | 🟠 | Cancel-purge try/catch untested — a throwing purger is never exercised | `Services/AdminOrderService.cs:235` | yes |
| PPW-162 | 🟠 | Empty-state copy collapses four causes into one permanent "no longer available", with no retry | `UI/…/order-detail-page.ts` | yes |
| PPW-153 | 🟠 | Lightbox large URL is minted at list fetch and expires after its 1h lifetime, with no refresh | `UI/…/order-detail-page.ts` | yes |
| PPW-180 | 🟠 | Upload thumbnail mints an unrevoked blob URL on every change-detection cycle | `UI/…/photo-thumbnail.component.ts:86` | yes |
| PPW-174 | 🟡 | ZIP tier resolve throws mid-stream with cloud disabled → truncated admin ZIP | `Services/AdminOrderService.cs:171` | yes |
| PPW-175 | 🟡 | Cleanup routes by the row's tier, so a failed promotion's cloud litter is never reclaimed | `BackgroundJobs/UploadCleanupJob.cs:92` | no |
| PPW-176 | 🟡 | Duplicate concurrent promotion re-creates a just-purged cloud original as an orphan | `Services/OrderPhotoPromoter.cs:168` | no |
| PPW-177 | 🟡 | New sweep-interval validator untested → an interval of 0 boots, then crashes the host | `Configuration/ArchiveSettings.cs:86` | yes |
| PPW-179 | 🟡 | Backfill filter test never crosses the exclusion boundary | `Tests/…/BackfillCommandTests.cs:40` | yes |
| PPW-178 | 🟡 | Preview re-resolve-to-local success branch untested | `Controllers/UploadsController.cs:200` | yes |
| PPW-169 | 🟡 | The `FilePath` NOT NULL drop is verified on SQLite only, never on Postgres | `Tests/…/UploadThumbnailPathMigrationTests.cs:48` | no |
| PPW-181 | 🟡 | Order detail redirects on any fetch error, so a transient failure bounces the user with no retry | `UI/…/order-detail-page.ts:357` | yes |
| PPW-182 | 🟡 | Lightbox modal has no focus trap, dialog role, modal flag or focus restore | `UI/…/photo-lightbox.component.ts` | yes |
| PPW-183 | 🟡 | Order detail loads only in `ngOnInit` despite a route-bound id → latent staleness | `UI/…/order-detail-page.ts:357` | no |

## Refuted

| Suspicion | Why it is not real |
|---|---|
| The worker drain added for PPW-155 can still abandon a promotion mid-write, if a long cloud upload outlives the host shutdown timeout | The promoter writes to cloud first and flips the row only after every write succeeds, the same-key upload is idempotent, and the recovery scan re-runs it. A process killed mid-write leaves the row on local storage, which is harmless and repeatable whatever the timeout. Do not raise it again. |

## Notes for the fixer

- This pass is scoped to the fix round since the last full pass, plus one full-surface frontend review that the lean v1 pass owed. It cannot certify.
- The theme is fix-generativity: almost every backend finding traces to a v1 fix, not to the original feature. PPW-168 and PPW-172 are the PPW-152 and PPW-166 fixes applied to one site instead of the whole class. PPW-173 and PPW-174 are new faults the PPW-149 and PPW-150 routing fixes introduced. PPW-170, PPW-171, PPW-177, PPW-178 and PPW-179 are mechanisms that shipped without a test that can fail.
- Do PPW-168 and PPW-172 with the class sweep they were the victims of. PPW-173 and PPW-174 are one class and want a single guarded-resolve fix.
- The frontend findings are the opposite: first-time discovery of a surface v1 never reviewed. PPW-162 and the second half of PPW-153 were the two items v1 deferred to this lens; both are confirmed and PPW-162 is now wider than v1 recorded.
- F7 is that second half of PPW-153. The ledger carries it as its own row, `PPW-154`, an identifier from before the current numbering rule, so the table above shows PPW-153.
- PPW-169 belongs with the three-environment work, not this round. PPW-176's precondition is PPW-158, so it belongs with the concurrency-token work that already carries PPW-158.
- Five findings are exactly "the suite is green but this behaviour has no test that can fail": PPW-170, PPW-171, PPW-177, PPW-178 and PPW-179.
- Still owed before closure: the four lenses v1 skipped beyond the slices this delta touched, and the certification pass that is the only instrument allowed to approve.
