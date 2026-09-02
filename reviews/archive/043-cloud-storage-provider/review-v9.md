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

| ID | Sev | Title | File | Fix now? |
|---|---|---|---|---|
| PPW-232 | 🟠 | "Photos no longer available" is shown for a just-paid order and for pending orders | `UI/…/order-detail-page.ts` | yes |
| PPW-233 | 🟠 | No test asserts that the legacy processor payment notification enqueues promotion | `Tests/…/PaymentControllerIntegrationTests.cs` | yes |
| PPW-234 | 🟠 | The backfill command was outside the review file list, and backfill against the live worker is untested | `Cli/BackfillCommand.cs` | no |
| PPW-199 | 🟠 | Purge and retention destroy a photo a second still-active order needs | `Services/OriginalPurger.cs:103` | no |
| PPW-211 | 🟡 | A read failure part-way through the admin ZIP truncates the archive after the headers are sent | `Services/AdminOrderService.cs` | no |
| PPW-184 | 🟡 | Periodic promotion sweep has no in-flight dedup, so it can start a second promotion of one order | `BackgroundJobs/PromotionRecoveryScanner.cs` | no |
| PPW-195 | 🟡 | The periodic sweep re-enqueues permanently failed promotions forever | `BackgroundJobs/PromotionRecoveryScanner.cs` | no |
| PPW-159 | 🟡 | 403 rather than 404 for another user's order tells an attacker which order ids exist | `Services/OrderService.cs:468` | no |
| PPW-169 | 🟡 | The `FilePath` NOT NULL drop is verified on SQLite only, never on Postgres | `Tests/…/UploadThumbnailPathMigrationTests.cs:48` | no |
| PPW-235 | 🟡 | Retention deletes the blobs before it persists the null keys → a broken-URL window | `BackgroundJobs/ArchiveRetentionJob.cs:146` | no |
| PPW-236 | 🟡 | The retention sweep query has no soft-delete filter, so it reprocesses deleted rows | `BackgroundJobs/ArchiveRetentionJob.cs:96` | no |
| PPW-237 | 🟡 | Promoter tests assert the cloud keys written but never the bytes | `Tests/…/OrderPhotoPromoterTests.cs` | no |
| PPW-239 | 🟡 | Closing the lightbox during a refresh has no spec; only closing before the error is tested | `UI/…/order-detail-page.spec.ts` | no |
| PPW-191 | 🟡 | The lightbox tells the user to reload while the app is already fetching a fresh URL | `UI/…/photo-lightbox.component.ts` | no |
| PPW-218 | 🟡 | Originals of orders that never reach production-complete or Cancelled escape the retention window | `BackgroundJobs/ArchiveRetentionJob.cs:92` | no |
| PPW-223 | 🟡 | The promotable-status set is written out three times under a false single-source comment | `Cli/BackfillCommand.cs:43` | no |
| PPW-224 | 🟡 | The S3 retry classification, re-upload and presign protocol are untested | `Services/S3StorageService.cs:60` | no |
| PPW-229 | ⚪ | Local preview cache header disagrees with the documented one | `Controllers/UploadsController.cs:26` | no |
| PPW-238 | ⚪ | Code comments cite finding, decision and design-record ids, which the repo rule bans | codebase-wide | no |

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
- Only eight identities are new here — PPW-232 to PPW-239. The other rows are already-decided items raised again, folds into deferred work, or the accepted residual of the PPW-199 fix, which an independent look re-found and so corroborated.
- The counts above are the pass's 30 canonical findings. The table lists the 19 the pass named one by one; the remaining Lows and Cleanups were counted as totals and never itemised, so they have no identity to carry.
- Three items need an owner decision, not a fix by the fixer: PPW-232, PPW-233 and PPW-234.
- PPW-211's new trigger, a concurrent promotion moving the original while the ZIP streams, was rated Medium by the finder. The ledger row stays 🟡 because it is the same defect as the one recorded at v7, now with a second way in.
- PPW-159's decision stands, but the bolt-053 implementation plan says the endpoint returns 404 while the code returns 403. That document needs reconciling.
- The steady-state storage cost and poison-order amplification flag overlaps the deferred PPW-195 and the fixed PPW-200; it is noted rather than raised as new work.
- Record note: the metrics line for this pass counts 5 new Mediums, 6 new Lows and 2 new Cleanups, while its own note and the ledger name 3 new Mediums (PPW-232, PPW-233, PPW-234), 4 new Lows (PPW-235, PPW-236, PPW-237, PPW-239) and 1 new Cleanup. The ledger identities are the ones to trust; the metrics file is append-only and was not corrected.
