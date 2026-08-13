---
type: review
target: 043-cloud-storage-provider
version: 7
supersedes: 6
commit: 2d02b13
branch: feat/bolt-043-cloud-storage-provider
pass-type: certification
date: 2026-07-22
lenses: [correctness, security, race, db-parity, observability, requirements, quality, input-validation, frontend-ux, tests-coverage, completeness-critic]
lenses-not-run: []
verdict: request-changes
blockers: [PPW-198]
findings: { high: 1, medium: 11, low: 17, cleanup: 5, refuted: 0 }
tests: { dotnet: "702/702 (+10 skipped MinIO)", frontend: "439/439" }
---

# Review v7 — 043-cloud-storage-provider

## Findings

| ID | Sev | Title | File | Fix now? |
|---|---|---|---|---|
| PPW-198 | 🔴 | The S3 upload rewinds the stream outside the retry loop → a retried upload silently loses the photo | `Services/S3StorageService.cs:63` | yes |
| PPW-199 | 🟠 | Purge and retention destroy a photo a second still-active order needs | `Services/OriginalPurger.cs:103` | yes |
| PPW-200 | 🟠 | The promotion worker holds its concurrency slot through the whole retry backoff | `BackgroundJobs/OrderPhotoPromotionWorker.cs:107` | yes |
| PPW-201 | 🟠 | The photos query has no soft-delete filter → presigned URLs for uploads whose blobs are gone | `Services/OrderService.cs:460` | yes |
| PPW-202 | 🟠 | The webhook paid transition is an unguarded check-then-act → duplicate confirmation emails | `Controllers/WebhooksController.cs:215` | no |
| PPW-203 | 🟠 | Upload service re-advertises HEIC while the validator and the UI still reject it | `Services/UploadService.cs:52` | yes |
| PPW-204 | 🟠 | Client filename is not truncated to the column width → Postgres rejects it and returns 500 | `Services/UploadService.cs:113` | yes |
| PPW-205 | 🟠 | The retention audit event is written before the save, so a failed save leaves false audit records | `BackgroundJobs/ArchiveRetentionJob.cs:123` | yes |
| PPW-206 | 🟠 | Cloud-off purge refusal is logged at Error on every ship in the default configuration | `Services/OriginalPurger.cs:43` | yes |
| PPW-207 | 🟠 | The promotion worker's retry, backoff and re-enqueue path is entirely untested | `BackgroundJobs/OrderPhotoPromotionWorker.cs:130` | yes |
| PPW-208 | 🟠 | No test asserts that the payment webhook enqueues promotion | `Controllers/WebhooksController.cs:183` | yes |
| PPW-209 | 🟠 | A real cloud provider is never exercised — only a skip-gated MinIO suite and fakes | `Tests/…/S3StorageServiceIntegrationTests.cs:18` | no |
| PPW-210 | 🟡 | Retention's fixed candidate window is starved by rows whose delete keeps failing | `BackgroundJobs/ArchiveRetentionJob.cs:98` | no |
| PPW-211 | 🟡 | A read failure part-way through the admin ZIP truncates the archive after the headers are sent | `Services/AdminOrderService.cs:197` | no |
| PPW-212 | 🟡 | Preview cache-fill regeneration races the retention delete → an orphaned blob and a null reference | `Services/UploadService.cs:203` | no |
| PPW-213 | 🟡 | A failed best-effort local delete in the promoter leaks local bytes nothing reclaims | `Services/OrderPhotoPromoter.cs:212` | no |
| PPW-214 | 🟡 | The local storage root re-anchor uses a prefix match with no separator boundary | `Services/LocalStorageService.cs:99` | no |
| PPW-215 | 🟡 | The ZIP entry extension is taken from the untrusted client filename, not the validated type | `Services/AdminOrderService.cs:190` | no |
| PPW-216 | 🟡 | Batch upload caps total bytes but not the number of files | `Controllers/UploadsController.cs:102` | no |
| PPW-217 | 🟡 | A broken grid thumbnail has no fallback or retry after the one presigned-URL refresh | `UI/…/order-detail-page.ts:472` | no |
| PPW-218 | 🟡 | Originals of orders that never reach production-complete or Cancelled escape the retention window | `BackgroundJobs/ArchiveRetentionJob.cs:92` | no |
| PPW-219 | 🟡 | The documented 502 for a persistent storage failure is not implemented; it surfaces as 500 | `Services/S3StorageService.cs:145` | no |
| PPW-220 | 🟡 | Idempotent-skip reasons are logged at Debug and never emit under the Information floor | `Services/OrderPhotoPromoter.cs:120` | no |
| PPW-221 | 🟡 | Transient and permanent cloud-write failures collapse into one warning, so poison is retried | `Services/OrderPhotoPromoter.cs:182` | no |
| PPW-222 | 🟡 | The preview cache-hit path lost its no-tracking read | `Services/UploadService.cs:139` | no |
| PPW-223 | 🟡 | The promotable-status set is written out three times under a false single-source comment | `Cli/BackfillCommand.cs:43` | no |
| PPW-224 | 🟡 | The S3 retry classification, re-upload and presign protocol are untested | `Services/S3StorageService.cs:60` | no |
| PPW-225 | 🟡 | Storage wiring, configuration and the CLI sat outside the lens list; the region setting is a trap | `Extensions/StorageExtensions.cs:56` | no |
| PPW-226 | 🟡 | Recovery and retention sweeps run unindexed full scans every six hours | `Data/Configurations/UploadConfiguration.cs:30` | no |
| PPW-227 | ⚪ | The promoter reads the whole original into an array and leaves memory streams undisposed | `Services/OrderPhotoPromoter.cs:138` | no |
| PPW-228 | ⚪ | The best-effort orphan-thumbnail delete swallows its exception with no log | `Services/UploadService.cs:222` | no |
| PPW-229 | ⚪ | Local preview cache header disagrees with the documented one | `Controllers/UploadsController.cs:26` | no |
| PPW-230 | ⚪ | A freshly generated local thumbnail is re-read from disk on a cache miss | `Services/UploadService.cs:240` | no |
| PPW-231 | ⚪ | Order detail shows both the interceptor toast and an inline error for one failure | `UI/…/order-detail-page.ts:403` | no |
| PPW-158 | 🟡 | Duplicate payment webhooks race `Order.Status`; there is no concurrency token | `Controllers/WebhooksController.cs:218` | no |
| PPW-163 | 🟡 | Cloud preview regeneration branch never runs — every test presets the thumbnail path | `Tests/…/CloudPreviewIntegrationTests.cs:225` | no |
| PPW-166 | 🟡 | Paid-then-cancelled originals: purge behaviour undecided and untested | `BackgroundJobs/OriginalPurgeRecoveryScanner.cs:54` | no |
| PPW-169 | 🟡 | The `FilePath` NOT NULL drop is verified on SQLite only, never on Postgres | `Tests/…/UploadThumbnailPathMigrationTests.cs:48` | no |
| PPW-176 | 🟡 | Duplicate concurrent promotion re-creates a just-purged cloud original as an orphan | `Services/OrderPhotoPromoter.cs:168` | no |
| PPW-184 | 🟡 | Periodic promotion sweep has no in-flight dedup, so it can start a second promotion of one order | `BackgroundJobs/PromotionRecoveryScanner.cs` | no |
| PPW-189 | 🟡 | The anti-refresh-loop guard has no test | `UI/…/order-detail-page.ts` | no |
| PPW-191 | 🟡 | The lightbox tells the user to reload while the app is already fetching a fresh URL | `UI/…/photo-lightbox.component.ts` | no |
| PPW-192 | 🟡 | A 401 for a non-authenticated user leaves a blank order body, with no error and no redirect | `UI/…/order-detail-page.ts` | no |

## Refuted

| Suspicion | Why it is not real |
|---|---|
| The migration adding the archive columns hardcodes an unbounded text type, which is a per-column defect | The skeptic showed it is the known dual-database gap that PPW-169 already carries, not a new defect. It was matched to PPW-169 as a re-raise, which is why this pass records nothing refuted. |

## Notes for the fixer

- This was the certification step: two independent blinded passes over the same frozen commit, with the full lens list. It is the first time the full list has ever run on this feature — v1 was lean, v3 and v5 were deltas, and v2, v4 and v6 were verifications.
- It did not certify. A High and a data-loss class re-arm the loop, so the feature is not merge-ready.
- Pass A ran correctness, security, race, database parity, observability, requirements and quality: 33 agents, 1.83M tokens, 27 raw findings reduced to 25. Pass B ran correctness, security, race, input validation, frontend behaviour, test coverage and the completeness critic: 40 agents, 2.13M tokens, 30 raw reduced to 28. Combined about 3.96M tokens across 73 agents.
- Fix PPW-198 first, whatever else happens. Move the rewind inside the retry attempt and reject a stream that cannot be rewound, with a test that proves a retried upload sends the full bytes.
- PPW-199 is a design decision, not a patch. Run the adversarial design check before writing code, and decide how a photo shared by more than one order is counted before anything destroys it.
- The rows with no F# are the Lows and Cleanups this pass recorded without a pass-local identifier, and the nine already-decided items it raised again. Their prior decisions stand; nothing needs re-litigating.
- The two passes agreed on PPW-199, PPW-200, PPW-211 and PPW-212 and disagreed everywhere else, so the search is not exhausted and the feature cannot be certified on this evidence.
- Nine already-decided items were raised again across eleven raise events, because PPW-169 and PPW-184 were raised by both passes.
- PPW-166 deserves the owner's eye: the requirements lens reads the bolt-052 design record as keeping cancelled originals, while the shipped code purges them on the owner's ruling. Reconcile the document.
- After the fixes, re-freeze and run certification again. The counter has reset.
