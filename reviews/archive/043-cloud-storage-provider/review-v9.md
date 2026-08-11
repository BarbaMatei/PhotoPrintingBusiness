---
type: review
target: 043-cloud-storage-provider
version: 9
supersedes: 8
commit: ac97e42
branch: feat/bolt-043-cloud-storage-provider
pass-type: certification
date: 2026-07-22
lenses: [correctness, security, race, db-parity, observability, requirements, quality, input-validation, frontend-ux, tests-coverage, completeness-critic]
lenses-not-run: []
verdict: approve-with-followups
blockers: []
findings: { high: 0, medium: 10, low: 14, cleanup: 6, refuted: 3 }
tests: { dotnet: "719/719 (+10 skipped MinIO)", frontend: "439/439" }
---

# Review v9 — 043-cloud-storage-provider

## Findings

| F# | D# | Sev | Title | File | Fix now? |
|---|---|---|---|---|---|
| — | D83 | 🟠 | "Photos no longer available" is shown for a just-paid order and for pending orders | `UI/…/order-detail-page.ts` | yes |
| — | D84 | 🟠 | No test asserts that the EuPlatesc payment notification enqueues promotion | `Tests/…/PaymentControllerIntegrationTests.cs` | yes |
| — | D85 | 🟠 | The backfill command was outside the review file list, and backfill against the live worker is untested | `Cli/BackfillCommand.cs` | no |
| — | D50 | 🟠 | Purge and retention destroy a photo a second still-active order needs | `Services/OriginalPurger.cs:103` | no |
| — | D62 | 🟡 | A read failure part-way through the admin ZIP truncates the archive after the headers are sent | `Services/AdminOrderService.cs` | no |
| — | D35 | 🟡 | Periodic promotion sweep has no in-flight dedup, so it can start a second promotion of one order | `BackgroundJobs/PromotionRecoveryScanner.cs` | no |
| — | D46 | 🟡 | The periodic sweep re-enqueues permanently failed promotions forever | `BackgroundJobs/PromotionRecoveryScanner.cs` | no |
| — | D10 | 🟡 | 403 rather than 404 for another user's order tells an attacker which order ids exist | `Services/OrderService.cs:468` | no |
| — | D20 | 🟡 | The `FilePath` NOT NULL drop is verified on SQLite only, never on Postgres | `Tests/…/UploadThumbnailPathMigrationTests.cs:48` | no |
| — | D86 | 🟡 | Retention deletes the blobs before it persists the null keys → a broken-URL window | `BackgroundJobs/ArchiveRetentionJob.cs:146` | no |
| — | D87 | 🟡 | The retention sweep query has no soft-delete filter, so it reprocesses deleted rows | `BackgroundJobs/ArchiveRetentionJob.cs:96` | no |
| — | D88 | 🟡 | Promoter tests assert the cloud keys written but never the bytes | `Tests/…/OrderPhotoPromoterTests.cs` | no |
| — | D90 | 🟡 | Closing the lightbox during a refresh has no spec; only closing before the error is tested | `UI/…/order-detail-page.spec.ts` | no |
| — | D42 | 🟡 | The lightbox tells the user to reload while the app is already fetching a fresh URL | `UI/…/photo-lightbox.component.ts` | no |
| — | D69 | 🟡 | Originals of orders that never reach production-complete or Cancelled escape the retention window | `BackgroundJobs/ArchiveRetentionJob.cs:92` | no |
| — | D74 | 🟡 | The promotable-status set is written out three times under a false single-source comment | `Cli/BackfillCommand.cs:43` | no |
| — | D75 | 🟡 | The S3 retry classification, re-upload and presign protocol are untested | `Services/S3StorageService.cs:60` | no |
| — | D80 | ⚪ | Local preview cache header disagrees with the documented one | `Controllers/UploadsController.cs:26` | no |
| — | D89 | ⚪ | Code comments cite finding, decision and design-record ids, which the repo rule bans | codebase-wide | no |

## Refuted

| Suspicion | Why it is not real |
|---|---|
| The frontend file list skipped the upload thumbnail component | The file was reviewed. Dropped. |
| The image processor fails open on a bad image | It fails closed; the skeptic confirmed it. Dropped. |
| The type validator reads only part of the file before deciding | Refuted on the code. Dropped. |

## Notes for the fixer

- This pass certified the feature clean of serious defects: 0 High and 0 regression caused by the previous round's fixes. Under the severity-based stop rule the loop does not re-arm.
- It ran as one blinded full-list pass rather than the usual two, an owner-approved deviation, because the pair had audited near-identical code hours earlier and the round between them moved only eight files, all independently verified. 45 agents, 2.87M tokens, 31 raw findings reduced to 30.
- The certificate says no serious defect survives. It does not say zero defects remain: a second pass would surface a different set of Medium items.
- Only eight identities are new here — D83 to D90. The other rows are already-decided items raised again, folds into deferred work, or the accepted residual of the D50 fix, which an independent look re-found and so corroborated.
- The counts above are the pass's 30 canonical findings. The table lists the 19 the pass named one by one; the remaining Lows and Cleanups were counted as totals and never itemised, so they have no identity to carry.
- Three items need an owner decision, not a fix by the fixer: D83, D84 and D85.
- D62's new trigger, a concurrent promotion moving the original while the ZIP streams, was rated Medium by the finder. The ledger row stays 🟡 because it is the same defect as the one recorded at v7, now with a second way in.
- D10's decision stands, but the bolt-053 implementation plan says the endpoint returns 404 while the code returns 403. That document needs reconciling.
- The steady-state storage cost and poison-order amplification flag overlaps the deferred D46 and the fixed D51; it is noted rather than raised as new work.
- Record note: the metrics line for this pass counts 5 new Mediums, 6 new Lows and 2 new Cleanups, while its own note and the ledger name 3 new Mediums (D83, D84, D85), 4 new Lows (D86, D87, D88, D90) and 1 new Cleanup (D89). The ledger identities are the ones to trust; the metrics file is append-only and was not corrected.
