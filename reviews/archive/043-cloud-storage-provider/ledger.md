---
type: review-ledger
target: 043-cloud-storage-provider
updated: 2026-08-11
closed: 2026-07-22 — certified (v9 single-pass) @ac97e42
---

# Ledger — 043-cloud-storage-provider

## Findings

| D# | Sev | First seen | Title | File | Status | Affirmed |
|---|---|---|---|---|---|---|
| D1 | 🔴 | v1 (F1) | Admin ZIP fulfilment download reads promoted originals from the local tier only | `Services/AdminOrderService.cs:168` | verified | `1e7b9d3` |
| D2 | 🟠 | v1 (F2) | Cleanup job deletes Cloud uploads against the local tier and never deletes the large preview | `BackgroundJobs/UploadCleanupJob.cs:67` | verified | `1e7b9d3` |
| D3 | 🟠 | v1 (F3) | Missing cloud original throws `AmazonS3Exception`, not `FileNotFoundException` → preview 500 | `Services/S3StorageService.cs:91` | verified | `1e7b9d3` |
| D4 | 🟠 | v1 (F4) | Purge on Shipped fires once and skips an in-flight promotion → original never purged until reboot | `Services/AdminOrderService.cs:136` | verified | `1e7b9d3` |
| D5 | 🟠 | v1 (F5) | Presigned-URL lifetime and the hardcoded `Cache-Control` max-age diverge → expired images | `Controllers/UploadsController.cs:185` | verified | `1e7b9d3` |
| D5b | 🟠 | v1 (F5 part b) | Lightbox large URL is minted at list fetch and expires after its 1h lifetime, with no refresh | `UI/…/order-detail-page.ts` | verified | `972a8b4` |
| D6 | 🟠 | v1 (F6) | Promotion worker disposes the concurrency semaphore while tasks are still in flight | `BackgroundJobs/OrderPhotoPromotionWorker.cs:108` | verified | `1e7b9d3` |
| D7 | 🟠 | v1 (F7) | Migration dropping the `FilePath` NOT NULL constraint is unverified by any test | `Migrations/…MakeUploadFilePathNullable.cs` | verified | `1e7b9d3` |
| D8 | 🟡 | v1 (F8) | Preview read races promotion deleting the local thumbnail → 500 instead of 404 | `Controllers/UploadsController.cs:190` | verified | `1e7b9d3` |
| D9 | 🟡 | v1 (F9) | Duplicate payment webhooks race `Order.Status`; there is no concurrency token | `Controllers/WebhooksController.cs:218` | deferred | `2d02b13` |
| D10 | 🟡 | v1 (F10) | 403 rather than 404 for another user's order tells an attacker which order ids exist | `Services/OrderService.cs:468` | wont-fix | `ac97e42` |
| D11 | 🟡 | v1 (F11) | `/photos` returns presigned URLs with no `Cache-Control: private` | `Controllers/OrdersController.cs:82` | verified | `1e7b9d3` |
| D12 | 🟡 | v1 (F12) | Guest-placed orders cannot reach the new `/photos` endpoint | `Controllers/OrdersController.cs:10` | wont-fix | `1e7b9d3` |
| D13 | 🟠 | v1 (F13) | Empty-state copy collapses four causes into one permanent "no longer available", with no retry | `UI/…/order-detail-page.ts:103` | verified | `972a8b4` |
| D14 | 🟡 | v1 (F14) | Cloud preview regeneration branch never runs — every test presets the thumbnail path | `Tests/…/CloudPreviewIntegrationTests.cs:225` | verified | `2d02b13` |
| D15 | 🟡 | v1 (F15) | `GET /orders/{id}/photos` has no integration test, so its auth pipeline is unproven | `Controllers/OrdersController.cs:73` | verified | `1e7b9d3` |
| D16 | 🟡 | v1 (F16) | Promoter row-update-failure and preview-generation-failure branches untested | `Services/OrderPhotoPromoter.cs:198` | verified | `1e7b9d3` |
| D17 | 🟡 | v1 (F17) | Paid-then-cancelled originals: purge behaviour undecided and untested | `BackgroundJobs/OriginalPurgeRecoveryScanner.cs:54` | verified | `2d02b13` |
| D18 | 🟡 | v1 (F18) | `BackfillCommand` and `S3BucketVerifier` have no tests at all | `Cli/BackfillCommand.cs:42` | verified | `1e7b9d3` |
| D19 | 🟠 | v3 (F1) | Promotion recovery left boot-only while its purge sibling was made periodic | `BackgroundJobs/PromotionRecoveryScanner.cs` | verified | `972a8b4` |
| D20 | 🟡 | v3 (F15) | The `FilePath` NOT NULL drop is verified on SQLite only, never on Postgres | `Tests/…/UploadThumbnailPathMigrationTests.cs:48` | deferred | `ac97e42` |
| D21 | 🟠 | v3 (F3) | Purge sweep untested — tests call the sweep by reflection and `ExecuteAsync` is never driven | `BackgroundJobs/OriginalPurgeRecoveryScanner.cs:60` | verified | `972a8b4` |
| D22 | 🟠 | v3 (F5) | Cancel-purge try/catch untested — a throwing purger is never exercised | `Services/AdminOrderService.cs:235` | verified | `972a8b4` |
| D23 | 🟠 | v3 (F4) | Production-complete purge lacks the cancel path's try/catch → 500 after the transition committed | `Services/AdminOrderService.cs:135` | verified | `972a8b4` |
| D24 | 🟠 | v3 (F2) | Tier resolve throws with cloud disabled and wedges every cleanup batch | `BackgroundJobs/UploadCleanupJob.cs:92` | verified | `972a8b4` |
| D25 | 🟡 | v3 (F9) | ZIP tier resolve throws mid-stream with cloud disabled → truncated admin ZIP | `Services/AdminOrderService.cs:171` | verified | `972a8b4` |
| D26 | 🟡 | v3 (F10) | Cleanup routes by the row's tier, so a failed promotion's cloud litter is never reclaimed | `BackgroundJobs/UploadCleanupJob.cs:92` | deferred | `972a8b4` |
| D27 | 🟡 | v3 (F11) | Duplicate concurrent promotion re-creates a just-purged cloud original as an orphan | `Services/OrderPhotoPromoter.cs:168` | deferred | `2d02b13` |
| D28 | 🟡 | v3 (F12) | New sweep-interval validator untested → an interval of 0 boots, then crashes the host | `Configuration/ArchiveSettings.cs:86` | verified | `972a8b4` |
| D29 | 🟡 | v3 (F14) | Preview re-resolve-to-local success branch untested | `Controllers/UploadsController.cs:200` | verified | `972a8b4` |
| D30 | 🟡 | v3 (F13) | Backfill filter test never crosses the exclusion boundary | `Tests/…/BackfillCommandTests.cs:40` | verified | `972a8b4` |
| D31 | 🟠 | v3 (F8) | Upload thumbnail mints an unrevoked blob URL on every change-detection cycle | `UI/…/photo-thumbnail.component.ts:86` | verified | `972a8b4` |
| D32 | 🟡 | v3 (F16) | Order detail redirects on any fetch error, so a transient failure bounces the user with no retry | `UI/…/order-detail-page.ts:357` | verified | `972a8b4` |
| D33 | 🟡 | v3 (F17) | Lightbox modal has no focus trap, dialog role, modal flag or focus restore | `UI/…/photo-lightbox.component.ts` | verified | `972a8b4` |
| D34 | 🟡 | v3 (F18) | Order detail loads only in `ngOnInit` despite a route-bound id → latent staleness | `UI/…/order-detail-page.ts:357` | deferred | `972a8b4` |
| D35 | 🟡 | v4 (NF1) | Periodic promotion sweep has no in-flight dedup, so it can start a second promotion of one order | `BackgroundJobs/PromotionRecoveryScanner.cs` | deferred | `ac97e42` |
| D36 | 🟠 | v5 (F1) | A stale lightbox photo id re-opens a closed lightbox when a thumbnail URL expires | `UI/…/order-detail-page.ts` | verified | `2d02b13` |
| D37 | 🟠 | v5 (F3) | The periodic re-scan is untested and untestable — the interval has no test seam | `BackgroundJobs/PromotionRecoveryScanner.cs` | deferred | `ac97e42` |
| D38 | 🟠 | v5 (F2) | Unroutable Cloud rows are skipped after the fetch, starving local-orphan cleanup | `BackgroundJobs/UploadCleanupJob.cs` | verified | `2d02b13` |
| D39 | 🟡 | v5 (F13) | Renamed guard tests seed an empty database, so they pass for the wrong reason | `Tests/…/PromotionRecoveryScannerTests.cs` | backlog | `2d02b13` |
| D40 | 🟡 | v5 (F11) | The anti-refresh-loop guard has no test | `UI/…/order-detail-page.ts` | backlog | `2d02b13` |
| D41 | 🟡 | v5 (F12) | The lightbox focus trap has no spec | `UI/…/photo-lightbox.component.ts` | backlog | `ac97e42` |
| D42 | 🟡 | v5 (F9) | The lightbox tells the user to reload while the app is already fetching a fresh URL | `UI/…/photo-lightbox.component.ts` | backlog | `ac97e42` |
| D43 | 🟡 | v5 (F8) | A 401 for a non-authenticated user leaves a blank order body, with no error and no redirect | `UI/…/order-detail-page.ts` | backlog | `2d02b13` |
| D44 | ⚪ | v5 (F14) | Order retries and init subscriptions have no in-flight dedup or teardown | `UI/…/order-detail-page.ts` | backlog | `2d02b13` |
| D45 | 🟡 | v5 (F6) | The ZIP pre-flight throws an unmapped exception → a generic 500 logged as unhandled | `Services/AdminOrderService.cs` | backlog | `ac97e42` |
| D46 | 🟡 | v5 (F5) | The periodic sweep re-enqueues permanently failed promotions forever | `BackgroundJobs/PromotionRecoveryScanner.cs` | deferred | `ac97e42` |
| D47 | 🟡 | v5 (F7) | The cloud-enabled flag is fixed at boot, so switching provider at runtime needs a restart | `BackgroundJobs/PromotionRecoveryScanner.cs` | deferred | `ac97e42` |
| D48 | 🟡 | v5 (F10) | The lightbox failure flag is reset only on a changed URL, so an identical refreshed URL stays stuck | `UI/…/photo-lightbox.component.ts` | backlog | `ac97e42` |
| D49 | 🔴 | v7 (F1) | The S3 upload rewinds the stream outside the retry loop → a retried upload silently loses the photo | `Services/S3StorageService.cs:63` | verified | `ac97e42` |
| D50 | 🟠 | v7 (F2) | Purge and retention destroy a photo a second still-active order needs | `Services/OriginalPurger.cs:103` | verified | `ac97e42` |
| D51 | 🟠 | v7 (F3) | The promotion worker holds its concurrency slot through the whole retry backoff | `BackgroundJobs/OrderPhotoPromotionWorker.cs:107` | verified | `ac97e42` |
| D52 | 🟠 | v7 (F4) | The photos query has no soft-delete filter → presigned URLs for uploads whose blobs are gone | `Services/OrderService.cs:460` | verified | `ac97e42` |
| D53 | 🟠 | v7 (F5) | The webhook paid transition is an unguarded check-then-act → duplicate confirmation emails | `Controllers/WebhooksController.cs:215` | deferred | `ac97e42` |
| D54 | 🟠 | v7 (F6) | Upload service re-advertises HEIC while the validator and the UI still reject it | `Services/UploadService.cs:52` | verified | `ac97e42` |
| D55 | 🟠 | v7 (F7) | Client filename is not truncated to the column width → Postgres rejects it and returns 500 | `Services/UploadService.cs:113` | verified | `ac97e42` |
| D56 | 🟠 | v7 (F8) | The retention audit event is written before the save, so a failed save leaves false audit records | `BackgroundJobs/ArchiveRetentionJob.cs:123` | verified | `ac97e42` |
| D57 | 🟠 | v7 (F9) | Cloud-off purge refusal is logged at Error on every ship in the default configuration | `Services/OriginalPurger.cs:43` | verified | `ac97e42` |
| D58 | 🟠 | v7 (F10) | The promotion worker's retry, backoff and re-enqueue path is entirely untested | `BackgroundJobs/OrderPhotoPromotionWorker.cs:130` | verified | `ac97e42` |
| D59 | 🟠 | v7 (F11) | No test asserts that the payment webhook enqueues promotion | `Controllers/WebhooksController.cs:183` | verified | `ac97e42` |
| D60 | 🟠 | v7 (F12) | A real cloud provider is never exercised — only a skip-gated MinIO suite and fakes | `Tests/…/S3StorageServiceIntegrationTests.cs:18` | deferred | `ac97e42` |
| D61 | 🟡 | v7 | Retention's fixed candidate window is starved by rows whose delete keeps failing | `BackgroundJobs/ArchiveRetentionJob.cs:98` | backlog | `ac97e42` |
| D62 | 🟡 | v7 | A read failure part-way through the admin ZIP truncates the archive after the headers are sent | `Services/AdminOrderService.cs:197` | backlog | `ac97e42` |
| D63 | 🟡 | v7 | Preview cache-fill regeneration races the retention delete → an orphaned blob and a null reference | `Services/UploadService.cs:203` | backlog | `ac97e42` |
| D64 | 🟡 | v7 | A failed best-effort local delete in the promoter leaks local bytes nothing reclaims | `Services/OrderPhotoPromoter.cs:212` | backlog | `ac97e42` |
| D65 | 🟡 | v7 | The local storage root re-anchor uses a prefix match with no separator boundary | `Services/LocalStorageService.cs:99` | backlog | `ac97e42` |
| D66 | 🟡 | v7 | The ZIP entry extension is taken from the untrusted client filename, not the validated type | `Services/AdminOrderService.cs:190` | backlog | `ac97e42` |
| D67 | 🟡 | v7 | Batch upload caps total bytes but not the number of files | `Controllers/UploadsController.cs:102` | backlog | `ac97e42` |
| D68 | 🟡 | v7 | A broken grid thumbnail has no fallback or retry after the one presigned-URL refresh | `UI/…/order-detail-page.ts:472` | backlog | `ac97e42` |
| D69 | 🟡 | v7 | Originals of orders that never reach production-complete or Cancelled escape the retention window | `BackgroundJobs/ArchiveRetentionJob.cs:92` | backlog | `ac97e42` |
| D70 | 🟡 | v7 | The documented 502 for a persistent storage failure is not implemented; it surfaces as 500 | `Services/S3StorageService.cs:145` | backlog | `ac97e42` |
| D71 | 🟡 | v7 | Idempotent-skip reasons are logged at Debug and never emit under the Information floor | `Services/OrderPhotoPromoter.cs:120` | backlog | `ac97e42` |
| D72 | 🟡 | v7 | Transient and permanent cloud-write failures collapse into one warning, so poison is retried | `Services/OrderPhotoPromoter.cs:182` | backlog | `ac97e42` |
| D73 | 🟡 | v7 | The preview cache-hit path lost its no-tracking read | `Services/UploadService.cs:139` | backlog | `ac97e42` |
| D74 | 🟡 | v7 | The promotable-status set is written out three times under a false single-source comment | `Cli/BackfillCommand.cs:43` | backlog | `ac97e42` |
| D75 | 🟡 | v7 | The S3 retry classification, re-upload and presign protocol are untested | `Services/S3StorageService.cs:60` | backlog | `ac97e42` |
| D76 | 🟡 | v7 | Storage wiring, configuration and the CLI sat outside the lens list; the region setting is a trap | `Extensions/StorageExtensions.cs:56` | backlog | `ac97e42` |
| D77 | 🟡 | v7 | Recovery and retention sweeps run unindexed full scans every six hours | `Data/Configurations/UploadConfiguration.cs:30` | backlog | `ac97e42` |
| D78 | ⚪ | v7 | The promoter reads the whole original into an array and leaves memory streams undisposed | `Services/OrderPhotoPromoter.cs:138` | backlog | `ac97e42` |
| D79 | ⚪ | v7 | The best-effort orphan-thumbnail delete swallows its exception with no log | `Services/UploadService.cs:222` | backlog | `ac97e42` |
| D80 | ⚪ | v7 | Local preview cache header disagrees with the documented one | `Controllers/UploadsController.cs:26` | backlog | `ac97e42` |
| D81 | ⚪ | v7 | A freshly generated local thumbnail is re-read from disk on a cache miss | `Services/UploadService.cs:240` | backlog | `ac97e42` |
| D82 | ⚪ | v7 | Order detail shows both the interceptor toast and an inline error for one failure | `UI/…/order-detail-page.ts:403` | backlog | `ac97e42` |
| D83 | 🟠 | v9 | "Photos no longer available" is shown for a just-paid order and for pending orders | `UI/…/order-detail-page.ts` | fixed | `b9af326` |
| D84 | 🟠 | v9 | No test asserts that the EuPlatesc payment notification enqueues promotion | `Tests/…/PaymentControllerIntegrationTests.cs` | wont-fix | — |
| D85 | 🟠 | v9 | The backfill command was outside the review file list, and backfill against the live worker is untested | `Cli/BackfillCommand.cs` | deferred | `d041295` |
| D86 | 🟡 | v9 | Retention deletes the blobs before it persists the null keys → a broken-URL window | `BackgroundJobs/ArchiveRetentionJob.cs:146` | backlog | `ac97e42` |
| D87 | 🟡 | v9 | The retention sweep query has no soft-delete filter, so it reprocesses deleted rows | `BackgroundJobs/ArchiveRetentionJob.cs:96` | backlog | `ac97e42` |
| D88 | 🟡 | v9 | Promoter tests assert the cloud keys written but never the bytes | `Tests/…/OrderPhotoPromoterTests.cs` | backlog | `ac97e42` |
| D89 | ⚪ | v9 | Code comments cite finding, decision and design-record ids, which the repo rule bans | codebase-wide | fixed | `09173c4` |
| D90 | 🟡 | v9 | Closing the lightbox during a refresh has no spec; only closing before the error is tested | `UI/…/order-detail-page.spec.ts` | backlog | `ac97e42` |

## Details

### D1 — Admin ZIP fulfilment download reads promoted originals from the local tier only

- **What:** With the cloud tier on, the fulfilment ZIP read every original from local disk. A promoted
  order's download failed part-way through and the admin could not get the photos to print.
- **History:**
  - v1: found (F1) — the pass's only High, and the one fix it required before merge
  - round 1: fixed @`ec94fca` — the ZIP reads through the storage router
  - v2: verified @`1e7b9d3`

### D2 — Cleanup job deletes Cloud uploads against the local tier and never deletes the large preview

- **What:** The cleanup job resolved the local tier for cloud uploads, so its deletes did nothing and
  the large preview key was never referenced. The row was soft-deleted and three cloud objects were
  left with no row to reclaim them.
- **History:**
  - v1: found (F2)
  - round 1: fixed @`6b63bd7` — deletes route by the row's tier and the large preview is deleted too
  - v2: verified @`1e7b9d3`

### D3 — Missing cloud original throws `AmazonS3Exception`, not `FileNotFoundException` → preview 500

- **What:** The cloud adapter raised its own exception type for a missing object while the caller
  caught only the local type, so a missing cloud original returned 500 instead of 404. The cloud test
  fake threw the local type, which hid the gap.
- **History:**
  - v1: found (F3)
  - round 1: fixed @`0f85f56` — the adapter translates a missing object into the shared exception type
  - v2: verified @`1e7b9d3`

### D4 — Purge on Shipped fires once and skips an in-flight promotion → original never purged until reboot

- **What:** The purge ran once on the Shipped transition and skipped uploads still on local storage.
  A promotion finishing later was never re-purged, so the original stayed in cloud past its retention
  window until the process restarted.
- **History:**
  - v1: found (F4)
  - round 1: fixed @`cc69025` — the purge recovery scanner became a periodic background service
  - v2: verified @`1e7b9d3` — by inspection of the periodic structure plus the sweep test

### D5 — Presigned-URL lifetime and the hardcoded `Cache-Control` max-age diverge → expired images

- **What:** The preview redirect hardcoded a one-hour cache lifetime while the presigned URL lifetime
  was an operator setting. Setting it below an hour let the browser replay a cached redirect to an
  already-expired URL.
- **History:**
  - v1: found (F5) — part b split off as D5b
  - round 1: fixed @`d15b9af` — the cache lifetime is derived from the presign setting
  - v2: verified @`1e7b9d3`

### D5b — Lightbox large URL is minted at list fetch and expires after its 1h lifetime, with no refresh

- **What:** The large photo URL was signed when the photo list loaded and reused when the lightbox
  opened. A user who opened it after the lifetime elapsed got a broken image, and the image tag had no
  error handler, refresh or fallback.
- **History:**
  - v1: found (F5 part b) — deferred to the frontend lens the lean pass skipped
  - v3: re-found and confirmed with a trace (F7)
  - round 3: fixed @`a5cb0be`, `c4ec6ca`, `972a8b4` — the lightbox reports the error, the page re-fetches fresh URLs, and the grid tiles got the same handler
  - v4: verified @`972a8b4`

### D6 — Promotion worker disposes the concurrency semaphore while tasks are still in flight

- **What:** On shutdown the worker disposed its concurrency gate while detached promotion tasks were
  still running. Their release call then threw on a disposed object and the promotion was abandoned
  mid-write.
- **History:**
  - v1: found (F6)
  - round 1: fixed @`3d97258` — in-flight tasks are tracked and drained before disposal
  - v2: verified @`1e7b9d3`

### D7 — Migration dropping the `FilePath` NOT NULL constraint is unverified by any test

- **What:** No test proved the migration that makes the original-path column nullable. Purger tests ran
  on the in-memory provider, which allows nulls regardless, and the SQLite migration test asserted only
  the thumbnail column.
- **History:**
  - v1: found (F7) — hinted by the shared dual-database context
  - round 1: fixed @`3326607` — the SQLite migration test asserts the column is nullable
  - v2: verified @`1e7b9d3` — by construction over the real migration chain

### D8 — Preview read races promotion deleting the local thumbnail → 500 instead of 404

- **What:** The service reported the thumbnail as local, then a concurrent promotion deleted it before
  the controller opened the file. The uncaught file-missing exception surfaced as 500 rather than the
  intended 404 or redirect.
- **History:**
  - v1: found (F8)
  - round 1: fixed @`881547f` — the controller catches the missing file and re-resolves once
  - v2: verified @`1e7b9d3`

### D9 — Duplicate payment webhooks race `Order.Status`; there is no concurrency token

- **What:** Two overlapping deliveries of the same payment event both read the order as awaiting
  payment, both moved it to paid, both sent the confirmation email and both queued promotion.
  Promotion is idempotent, so there is no data loss, but the customer gets two emails.
- **Evidence:** `Controllers/WebhooksController.cs:218`. No event de-duplication and no row version on
  `Order` exist anywhere in the API.
- **Suggested fix:** Add a guarded paid transition or an event-dedup table, in the payment-idempotency
  work that owns concurrency for orders.
- **History:**
  - v1: found (F9)
  - round 1: deferred → bolt-035, the payment-idempotency remit
  - v2: decision upheld — no token exists at the reviewed tip
  - v7: re-raised (pass A), decision upheld @`2d02b13`; D53 adds the duplicate-email consequence to the same remit
  - 2026-07-22: target closed with the row still deferred

### D10 — 403 rather than 404 for another user's order tells an attacker which order ids exist

- **What:** The photos and detail endpoints return 404 for an unknown order but 403 for an order owned
  by somebody else, so a signed-in attacker can tell real order ids from invented ones. Order ids are
  random version-4 identifiers, so the practical gain is negligible.
- **History:**
  - v1: found (F10)
  - round 1: wont-fix — 403 for a non-owner is the codebase-wide convention
  - v2: decision upheld
  - v9: re-raised, decision upheld @`ac97e42`; the finder noted that the bolt-053 plan says 404, so the document and the code disagree

### D11 — `/photos` returns presigned URLs with no `Cache-Control: private`

- **What:** The photos endpoint returned signed URLs without the private cache header its sibling
  preview endpoint sets, so a caching proxy in front of the API could store and replay them.
- **History:**
  - v1: found (F11) — judged plausible, not proven live
  - round 1: fixed @`751894b` — the response sets a private, no-store cache header
  - v2: verified @`1e7b9d3`

### D12 — Guest-placed orders cannot reach the new `/photos` endpoint

- **What:** The photos endpoint requires a signed-in user and matches on the user id, so an order
  placed by a guest can never be read through it, unlike the uploads and payments endpoints.
- **History:**
  - v1: found (F12)
  - round 1: wont-fix @`cda3685` — owner ruling that guest order history is out of scope; a test pins the 401
  - v2: decision upheld

### D13 — Empty-state copy collapses four causes into one permanent "no longer available", with no retry

- **What:** The order page showed the same permanent-sounding message for a transient fetch failure, an
  expired session, a photo set not yet archived, and a purged one. On a server error it showed that
  message alongside a contradictory toast, and offered no retry.
- **History:**
  - v1: found (F13) — 🟡, scoped to the not-yet-archived case
  - round 1: deferred to the frontend lens the lean pass skipped
  - v2: decision upheld
  - v3: re-found and widened by that lens (F6), re-rated 🟠
  - round 3: fixed @`c4ec6ca` — a fetch failure is separated from a genuine empty result and offers a retry
  - v4: verified @`972a8b4`; the four-way empty signal stays an open follow-up, later raised as D83

### D14 — Cloud preview regeneration branch never runs — every test presets the thumbnail path

- **What:** Every cloud test stored the thumbnail and set its path, so the preview call returned at the
  cache hit. The regenerate, save and persist path never ran against the cloud tier.
- **History:**
  - v1: found (F14)
  - round 1: fixed @`0ceabf8` — a test seeds a cloud upload with no thumbnail and asserts the thumbnail is regenerated, saved and persisted
  - v2: verified @`1e7b9d3` — by construction
  - v7: re-raised (pass B) as a fake-stream coverage concern, decision upheld @`2d02b13`

### D15 — `GET /orders/{id}/photos` has no integration test, so its auth pipeline is unproven

- **What:** Ownership was unit-tested but the endpoint's HTTP wiring was not, so dropping the
  authorization attribute or the null-user guard would have reddened nothing.
- **History:**
  - v1: found (F15)
  - round 1: fixed @`cda3685` — integration tests cover no-auth, cross-user, unknown order and guest token
  - v2: verified @`1e7b9d3` — by construction

### D16 — Promoter row-update-failure and preview-generation-failure branches untested

- **What:** The save-failure and preview-generation-failure branches never ran, because the in-memory
  provider does not throw and the preview generator was always mocked. A miscount would have shipped
  green.
- **History:**
  - v1: found (F16)
  - round 1: fixed @`a770a13` — a throwing context and a throwing preview generator drive both branches
  - v2: verified @`1e7b9d3` — by construction

### D17 — Paid-then-cancelled originals: purge behaviour undecided and untested

- **What:** Purge fired only at Shipped or Delivered, so a paid order later cancelled kept its cloud
  original indefinitely. Nothing recorded whether that was intended.
- **History:**
  - v1: found (F17)
  - round 1: fixed @`2fcdf3d` — owner ruling is purge on cancel, with the sweep as a backstop
  - v2: verified @`1e7b9d3`
  - v7: re-raised (pass A) — the requirements lens read the bolt-052 design record as saying cancelled originals are kept, so document and code disagree; the owner ruling stands @`2d02b13`

### D18 — `BackfillCommand` and `S3BucketVerifier` have no tests at all

- **What:** The backfill command's order filter was a hand copy of a tested scanner and its exit codes
  drive operator automation, yet nothing tested either. The bucket verifier's boot abort was also
  unproven.
- **History:**
  - v1: found (F18)
  - round 1: fixed @`682f1e2` — exit-code unit tests plus MinIO-backed bucket-verifier tests
  - v2: verified @`1e7b9d3` — by construction

### D19 — Promotion recovery left boot-only while its purge sibling was made periodic

- **What:** The D4 fix made the purge scanner periodic but left the promotion scanner running once at
  boot. A paid order whose promotion exhausted its retries stayed on local storage until the next
  restart, so its original never reached the durable tier.
- **History:**
  - v2: noticed as a carry-forward, not acted on
  - v3: found and confirmed with a trace (F1)
  - round 3: fixed @`2f49a8d` — the promotion scanner became a periodic background service with its own validated interval
  - v4: verified @`972a8b4` — boot sweep by revert-and-rerun, the periodic loop by inspection

### D20 — The `FilePath` NOT NULL drop is verified on SQLite only, never on Postgres

- **What:** The migration that makes the original-path column nullable is asserted on SQLite only.
  Purger tests run in memory, where nulls are allowed regardless, so a Postgres regression would be
  counted as a failed purge and stay invisible.
- **Evidence:** `Tests/…/UploadThumbnailPathMigrationTests.cs:48`. A skeptic confirmed the migration is
  correct on Postgres today, so this is a coverage gap rather than a live defect.
- **Suggested fix:** Run the migration and a null write against a real Postgres container in the
  three-environment stage.
- **History:**
  - v3: found (F15) — hinted by the shared dual-database context
  - round 3: deferred → the three-environment and container-test track
  - v4: deferral upheld @`972a8b4`
  - v7: re-raised by both passes, decision upheld; the retention-column concern folded in here
  - v9: the untested sweep query on the purge scanner folded in here as well @`ac97e42`
  - 2026-07-22: target closed with the row still deferred

### D21 — Purge sweep untested — tests call the sweep by reflection and `ExecuteAsync` is never driven

- **What:** The D4 conversion left the tests calling the internal sweep method by reflection, and both
  entry-point tests returned at a guard. Deleting the boot sweep or the timer loop left the suite green.
- **History:**
  - v3: found (F3) — a coverage regression introduced by the D4 fix
  - round 3: fixed @`fea2490` — a test drives the entry point and reddens when the boot sweep is removed
  - v4: verified @`972a8b4`

### D22 — Cancel-purge try/catch untested — a throwing purger is never exercised

- **What:** The guard added for D17 kept a purge failure from failing an already-committed cancellation,
  but no test made the purger throw, so removing the guard reddened nothing.
- **History:**
  - v3: found (F5)
  - round 3: fixed @`c30d734` — a throwing-purger cancel test
  - v4: verified @`972a8b4`

### D23 — Production-complete purge lacks the cancel path's try/catch → 500 after the transition committed

- **What:** The D17 fix wrapped the cancel purge but not its production-complete sibling. A purge that
  threw there returned 500 to the admin after the status change had already been committed, emailed and
  broadcast.
- **History:**
  - v3: found (F4) — the D17 fix treated the instance, not the class
  - round 3: fixed @`c30d734` — the same guard on the production-complete purge
  - v4: verified @`972a8b4`

### D24 — Tier resolve throws with cloud disabled and wedges every cleanup batch

- **What:** The D2 routing fix resolved the tier outside the per-upload guard. With cloud switched off
  and cloud rows in the batch, the resolve threw before anything was soft-deleted, so the same batch was
  retried every hour and all cleanup stopped, local orphans included.
- **History:**
  - v3: found (F2) — a new fault introduced by the D2 fix
  - round 3: fixed @`4674dcd`, `0fc577a` — unroutable cloud rows are skipped with a count warning; the customer preview path got the same guard
  - v4: verified @`972a8b4`

### D25 — ZIP tier resolve throws mid-stream with cloud disabled → truncated admin ZIP

- **What:** With cloud switched off, the fulfilment ZIP resolved the cloud tier after the response
  headers were already written, so the admin got a truncated archive instead of a clean error.
- **History:**
  - v3: found (F9) — the same class as D24
  - round 3: fixed @`c30d734`, `0fc577a` — the ZIP fails before writing any response bytes
  - v4: verified @`972a8b4`

### D26 — Cleanup routes by the row's tier, so a failed promotion's cloud litter is never reclaimed

- **What:** When a promotion writes its three cloud objects and then fails the row update, the row stays
  local with empty preview keys. Cleanup routes by that recorded tier and deletes only the local
  original, so the three cloud objects leak with no row that can ever reclaim them.
- **Evidence:** `BackgroundJobs/UploadCleanupJob.cs:92`, with the promoter writing at
  `Services/OrderPhotoPromoter.cs:168-178` and failing the update at `:196`.
- **Suggested fix:** Reclaim by the deterministic key scheme regardless of the recorded tier, as an
  orphan-sweep design rather than a patch.
- **History:**
  - v3: found (F10) — a residual of the D2 routing fix
  - round 3: deferred — the reclaim needs its own design pass
  - v4: deferral upheld @`972a8b4`; the new periodic promotion sweep narrows it, because a transient failure now self-heals
  - 2026-07-22: target closed with the row still deferred

### D27 — Duplicate concurrent promotion re-creates a just-purged cloud original as an orphan

- **What:** Two concurrent promotions of one order can interleave with a purge. The second job rewrites
  the cloud original after the purge has deleted it and cleared the path, and its save never restores
  that path, so nothing can ever reclaim the object. Personal data outlives its retention window.
- **Evidence:** `Services/OrderPhotoPromoter.cs:168`. The purger, cleanup and the recovery scanner all
  key on a non-empty original path.
- **Suggested fix:** Re-read the live tier and path before the flip, or put a concurrency token on the
  upload, in the same work that owns D9.
- **History:**
  - v3: found (F11) — the new and more frequent purge triggers widened the window
  - round 3: deferred → bolt-035, with D9
  - v4: deferral upheld @`972a8b4`, but the rationale was judged incomplete: it rests on duplicate webhooks, and the new sweep is a second trigger. Raised as D35
  - v7: re-raised (pass A), decision upheld @`2d02b13`
  - 2026-07-22: target closed with the row still deferred

### D28 — New sweep-interval validator untested → an interval of 0 boots, then crashes the host

- **What:** The validator rule added with the D4 fix had no test. Dropping it let an interval of zero
  boot and then crash the host on the first timer construction, instead of failing fast.
- **History:**
  - v3: found (F12)
  - round 3: fixed @`66a5f64` — validator tests for both sweep intervals
  - v4: verified @`972a8b4`

### D29 — Preview re-resolve-to-local success branch untested

- **What:** The D8 fix's re-resolve had tests for the redirect and the double-miss, but not for the
  case where the second local read succeeds. A regression there would have reddened nothing.
- **History:**
  - v3: found (F14) — a coverage gap, no live defect
  - round 3: fixed @`66a5f64` — a test drives the successful re-resolve
  - v4: verified @`972a8b4`

### D30 — Backfill filter test never crosses the exclusion boundary

- **What:** The D18 test claimed to guard against filter drift but seeded only included statuses, so
  widening the filter to re-promote cancelled or refunded photos would have shipped green.
- **History:**
  - v3: found (F13) — a residual of the D18 fix
  - round 3: fixed @`66a5f64` — boundary cases on both sides of the filter
  - v4: verified @`972a8b4`

### D31 — Upload thumbnail mints an unrevoked blob URL on every change-detection cycle

- **What:** The thumbnail created a browser object URL inside a template-evaluated method with no
  memoisation, so every upload progress event minted a fresh unrevoked URL. Memory leaked and the image
  flickered. It predates this feature and was caught by the full-surface frontend pass.
- **History:**
  - v3: found (F8)
  - round 3: fixed @`f048dc1` — the URL is created once per file and revoked on destroy
  - v4: verified @`972a8b4`

### D32 — Order detail redirects on any fetch error, so a transient failure bounces the user with no retry

- **What:** The order fetch redirected on every error. A transient failure threw a still-signed-in user
  off the page with no way to retry. The stronger claim, that it stranded a signed-out user, was refuted:
  the route guard sends them to the login page anyway.
- **History:**
  - v3: found (F16) — the Medium strand refuted, the Low residual kept
  - round 3: fixed @`c4ec6ca` — definitive errors redirect, expired sessions go to the interceptor, transient errors show an inline retry
  - v4: verified @`972a8b4`

### D33 — Lightbox modal has no focus trap, dialog role, modal flag or focus restore

- **What:** Keyboard and screen-reader users could tab through the page behind the backdrop, the overlay
  was not announced as a dialog, and focus was not returned to the thumbnail on close.
- **History:**
  - v3: found (F17) — first accessibility coverage of this surface
  - round 3: fixed @`a5cb0be` — dialog role, modal flag, focus move, tab trap and focus restore
  - v4: verified @`972a8b4`

### D34 — Order detail loads only in `ngOnInit` despite a route-bound id → latent staleness

- **What:** The order id is bound from the route but the order and photos load only on component init.
  If a detail-to-detail link is ever added, the reused component would show the previous order's data.
- **Evidence:** `UI/…/order-detail-page.ts:357`. Every entry today comes from the list route, which
  recreates the component, so no failing trace exists against the current code.
- **Suggested fix:** React to the route-bound id and re-fetch, rather than loading once on init.
- **History:**
  - v3: found (F18) — latent, not triggerable today
  - round 3: deferred as a recorded trap
  - v4: deferral upheld @`972a8b4`
  - 2026-07-22: target closed with the row still deferred

### D35 — Periodic promotion sweep has no in-flight dedup, so it can start a second promotion of one order

- **What:** The sweep queues every paid order still holding a local upload, with no check against jobs
  already queued or running, and the worker keeps no set of active orders. A sweep during an in-flight
  promotion starts a second one, which reaches the D27 orphan outcome without needing duplicate webhooks.
  It also logs a missing local original for an order that promoted fine, wasting a retry.
- **Evidence:** `BackgroundJobs/PromotionRecoveryScanner.cs` sweep query, plus the worker's plain task
  list and the promoter never re-reading live state before its update.
- **Suggested fix:** Fold into the concurrency-token work that owns D9 and D27; de-duplicate the queue by
  order.
- **History:**
  - v4: found (NF1) while checking the D27 deferral rationale — introduced by the D19 fix
  - v5: independently re-found by the blinded race lens (F4), with the false failure signal as a new consequence
  - round 5: deferred → bolt-035 as part of cluster A, one design item
  - v6: deferral upheld @`2d02b13` — the file was untouched in the round
  - v7: re-raised by both passes, decision upheld
  - v9: re-raised through the worker, folded back here @`ac97e42`
  - 2026-07-22: target closed with the row still deferred

### D36 — A stale lightbox photo id re-opens a closed lightbox when a thumbnail URL expires

- **What:** Closing the lightbox cleared the image source but not the photo id. A later expired-thumbnail
  error triggered the URL refresh, which read the stale id and set the source again, so the modal
  re-opened with no user action. Four lenses agreed.
- **History:**
  - v5: found (F1) — a regression from the D5b refresh fix
  - round 5: fixed @`2d02b13` — close clears both fields and the refresh re-reads the id when it resolves
  - v6: verified @`2d02b13` — the spec reddens when the id clear is reverted, with no collateral

### D37 — The periodic re-scan is untested and untestable — the interval has no test seam

- **What:** The only test awaits the boot sweep, so deleting the periodic loop leaves the suite green and
  restores the exact D19 defect. The interval is configured in whole hours, so no fast periodic test can
  be written.
- **Evidence:** `BackgroundJobs/PromotionRecoveryScanner.cs` timer loop, with the only entry-point test in
  `PromotionRecoveryScannerTests.cs` awaiting the boot sweep.
- **Suggested fix:** Add an internal interval seam so a short-interval test can assert a second enqueue.
- **History:**
  - v5: found (F3) — the D19 fix shipped its headline behaviour untested
  - round 5: deferred → bolt-035 as part of cluster A
  - v6: deferral upheld @`2d02b13`
  - v8: the file was untouched in the round; deferral spot-checked @`ac97e42`
  - 2026-07-22: target closed with the row still deferred

### D38 — Unroutable Cloud rows are skipped after the fetch, starving local-orphan cleanup

- **What:** With cloud off and at least a full batch of aged cloud rows, the candidate query kept
  selecting the same oldest rows, which were skipped and never soft-deleted, so local orphans sorted
  behind them were never reached and disk cleanup stopped.
- **History:**
  - v5: found (F2) — the edge the D24 fix dismissed as out of scope
  - round 5: fixed @`036ba05` — unroutable rows are excluded in the query, with a cloud-off-only count kept for the operator signal
  - v6: verified @`2d02b13` — the seeded starvation test reddens on revert, with no collateral

### D39 — Renamed guard tests seed an empty database, so they pass for the wrong reason

- **What:** Two guard tests use an empty database, so removing the guard queues nothing anyway and the
  no-other-calls assertion still passes. Guard removal ships green.
- **Evidence:** `Tests/…/PromotionRecoveryScannerTests.cs`, the archive-disabled and cloud-off tests.
- **Suggested fix:** Seed one stuck paid order still on local storage in both tests, so removing a guard
  reddens them.
- **History:**
  - v5: found (F13) — test quality left by the D19 fix
  - round 5: sent to backlog under the severity-based stop rule
  - v6: backlog upheld @`2d02b13`
  - 2026-08-10: row carried to `reviews/backlog.md`

### D40 — The anti-refresh-loop guard has no test

- **What:** The guard caps URL refreshes at one per load or open, so a permanently bad URL cannot storm
  the photos endpoint. No spec dispatches a second image error to assert that no third fetch happens, so
  a regression resetting the guard would loop unbounded and ship green.
- **Evidence:** `UI/…/order-detail-page.ts`, with the specs in `order-detail-page.spec.ts` dispatching
  only one error.
- **Suggested fix:** Add a spec that dispatches a second error and asserts the photos endpoint was called
  exactly twice.
- **History:**
  - v5: found (F11) — coverage left by the D5b refresh fix
  - round 5: sent to backlog
  - v6: backlog upheld @`2d02b13` — the D36 fix did not touch the guard
  - v7: re-raised (pass B), still open @`2d02b13`
  - 2026-08-10: row carried to `reviews/backlog.md`

### D41 — The lightbox focus trap has no spec

- **What:** The tab trap added for D33 has no test. Dropping the prevent-default or the refocus lets tab
  escape the modal to the page behind the backdrop, and nothing reddens.
- **Evidence:** `UI/…/photo-lightbox.component.ts`; the accessibility spec covers only focus move on open
  and close.
- **Suggested fix:** Add a spec that dispatches a tab keydown and asserts the event was prevented and
  focus stayed on the close button.
- **History:**
  - v5: found (F12) — coverage left by the D33 fix
  - round 5: sent to backlog
  - v6: backlog upheld @`2d02b13`
  - v8: file untouched in the round; backlog spot-checked @`ac97e42`
  - 2026-08-10: row carried to `reviews/backlog.md`

### D42 — The lightbox tells the user to reload while the app is already fetching a fresh URL

- **What:** An expired large URL renders a failure message telling the user to reload the page, while the
  parent silently re-fetches a fresh URL. On success the image appears, so the user was told to reload for
  an error the app recovered from.
- **Evidence:** `UI/…/photo-lightbox.component.ts`, the image error handler.
- **Suggested fix:** Show a neutral reloading state first, and keep the reload message for after the one
  refresh attempt fails.
- **History:**
  - v5: found (F9) — user experience left by the D5b refresh fix
  - round 5: sent to backlog
  - v6: backlog upheld @`2d02b13`
  - v7: re-raised (pass B), still open
  - v9: re-raised, decision upheld @`ac97e42`
  - 2026-08-10: row carried to `reviews/backlog.md`

### D43 — A 401 for a non-authenticated user leaves a blank order body, with no error and no redirect

- **What:** A guest or expired-guest visitor opening an order URL gets a 401. The interceptor's guest
  branch only clears the token and does not navigate, and the page's 401 path sets neither an error nor a
  redirect, so every block is hidden and the body is blank with no retry.
- **Evidence:** `UI/…/order-detail-page.ts` load path, with the guest branch in `error.interceptor.ts`.
- **Suggested fix:** For a 401 on a non-authenticated visitor, show a retryable error or redirect rather
  than relying on the interceptor to navigate.
- **History:**
  - v5: found (F8) — hinted by the shared guest-authentication context
  - round 5: sent to backlog
  - v6: backlog upheld @`2d02b13` — the D36 fix did not touch the 401 path
  - v7: re-raised (pass B), still open @`2d02b13`
  - 2026-08-10: row carried to `reviews/backlog.md`

### D44 — Order retries and init subscriptions have no in-flight dedup or teardown

- **What:** Rapid clicks on the retry button fire overlapping fetches, so a slow stale response can
  overwrite a newer one. The init subscriptions are not torn down, so a late response can write signals
  after the component is destroyed.
- **Evidence:** `UI/…/order-detail-page.ts`, the retry action and the init subscriptions.
- **Suggested fix:** Disable retry while a load is in flight, and switch to a cancelling, destroy-aware
  subscription.
- **History:**
  - v5: found (F14) — recorded as ⚪, skeptics skipped
  - round 5: sent to backlog
  - v6: backlog upheld @`2d02b13`
  - 2026-08-10: row carried to `reviews/backlog.md`

### D45 — The ZIP pre-flight throws an unmapped exception → a generic 500 logged as unhandled

- **What:** With cloud off, the admin ZIP pre-flight added for D25 throws an exception the middleware does
  not map, so the operator sees a generic 500 logged as an unhandled exception and cannot tell a
  configuration error from a crash.
- **Evidence:** `Services/AdminOrderService.cs` ZIP guard, with no matching entry in the exception
  middleware map.
- **Suggested fix:** Throw a mapped domain error with a 409 or 422 status and log the cloud-off reason as
  a warning.
- **History:**
  - v5: found (F6) — a residual of the D25 fix
  - round 5: sent to backlog as a standalone item
  - v6: backlog upheld @`2d02b13`
  - v8: file untouched in the round; backlog spot-checked @`ac97e42`
  - 2026-08-10: row carried to `reviews/backlog.md`

### D46 — The periodic sweep re-enqueues permanently failed promotions forever

- **What:** A paid order whose local original was lost exhausts its retries and stays on local storage.
  There is no give-up marker, so every sweep re-selects it, burns the retry budget again and logs a
  terminal failure again, for every stuck order. The boot-only scanner never repeated this.
- **Evidence:** `BackgroundJobs/PromotionRecoveryScanner.cs` sweep query.
- **Suggested fix:** Add a row-level give-up marker excluded from the sweep query, or throttle re-sweeps
  of known-terminal orders.
- **History:**
  - v5: found (F5) — introduced by the D19 conversion to a periodic sweep
  - round 5: deferred → bolt-035 as part of cluster A
  - v6: deferral upheld @`2d02b13`
  - v9: the steady-state cost and poison-amplification flag folded back here @`ac97e42`
  - 2026-07-22: target closed with the row still deferred

### D47 — The cloud-enabled flag is fixed at boot, so switching provider at runtime needs a restart

- **What:** The storage router decides once at construction whether cloud is available, so switching the
  provider setting from local to S3 while the process runs never starts a sweep. That contradicts the
  cleanup comment promising the work is retried when cloud comes back.
- **Evidence:** `BackgroundJobs/PromotionRecoveryScanner.cs` entry point and the router's construction.
- **Suggested fix:** Document the restart requirement, or re-read the setting each sweep.
- **History:**
  - v5: found (F7) — surfaced by the D19 conversion
  - round 5: deferred → bolt-035 as part of cluster A
  - v6: deferral upheld @`2d02b13`
  - v8: file untouched in the round; deferral spot-checked @`ac97e42`
  - 2026-07-22: target closed with the row still deferred

### D48 — The lightbox failure flag is reset only on a changed URL, so an identical refreshed URL stays stuck

- **What:** The failure flag clears only when the image source string changes. If the refreshed signed URL
  is identical to the failed one, the flag stays set and the one-refresh guard blocks another attempt, so
  the error persists until a full page reload.
- **Evidence:** `UI/…/photo-lightbox.component.ts`, the failure-reset effect.
- **Suggested fix:** Reset the flag on every open or refresh assignment, regardless of string equality.
- **History:**
  - v5: found (F10) — an edge of the D5b and D33 fixes, judged narrow
  - round 5: sent to backlog
  - v6: backlog upheld @`2d02b13`
  - v8: file untouched in the round; backlog spot-checked @`ac97e42`
  - 2026-08-10: row carried to `reviews/backlog.md`

### D49 — The S3 upload rewinds the stream outside the retry loop → a retried upload silently loses the photo

- **What:** The upload rewound the stream once, before the retry policy ran. A retried attempt therefore
  read from the end of the stream and uploaded an empty or truncated object. The upload reported success,
  the row flipped to cloud and the local original was deleted, so a paid customer's photo was destroyed
  with no error anywhere.
- **History:**
  - v7: found (pass B, F1) — confirmed by direct inspection; the pass's only High, and the reason it did not certify
  - round 7: fixed @`c37ca44` — the rewind moved inside the retry attempt, and a non-seekable stream now fails loudly
  - v8: verified @`ac97e42` — the regression test reddens on revert with the exact data-loss signature

### D50 — Purge and retention destroy a photo a second still-active order needs

- **What:** One upload is shared by id across orders. Purge keyed on one order shipping, and retention
  keyed on the oldest order's payment date, both destroyed a photo that a second, still-active order still
  needed. Both certification passes found it independently.
- **History:**
  - v7: found by both passes (F2)
  - round 7: fixed @`4dfd755` — live-order guards at purge and retention, after an adversarial design check; the accepted residual is a sharer still awaiting payment
  - round 7: the same class fixed at a third site @`ac97e42` — the cleanup job's referenced-retention branch
  - v8: verified @`ac97e42` at all three sites
  - v9: the awaiting-payment residual independently re-found, owner decision unchanged

### D51 — The promotion worker holds its concurrency slot through the whole retry backoff

- **What:** The worker kept both the concurrency slot and its dependency scope for the entire retry
  wait, up to an hour. During a cloud outage every slot parked in backoff and fresh promotions starved.
- **History:**
  - v7: found by both passes (F3)
  - round 7: fixed @`df1026d` — the backoff is detached from the slot, parked retries are bounded and the sweep is the backstop
  - v8: verified @`ac97e42` — the slot-starvation test reddens on revert
  - v9: the cost and poison-amplification flag noted against this row and D46

### D52 — The photos query has no soft-delete filter → presigned URLs for uploads whose blobs are gone

- **What:** The order photos query did not exclude soft-deleted uploads, so it signed URLs for photos the
  cleanup job had already deleted. The customer saw broken thumbnails that no refresh could fix.
- **History:**
  - v7: found (pass A, F4)
  - round 7: fixed @`5cfc9f9` — the query filters soft-deleted rows
  - v8: verified @`ac97e42`

### D53 — The webhook paid transition is an unguarded check-then-act → duplicate confirmation emails

- **What:** Concurrent duplicate payment deliveries both pass the status check, so promotion is queued
  twice and the customer receives two confirmation emails. Same root cause as D9, with the email
  consequence added.
- **Evidence:** `Controllers/WebhooksController.cs:215`.
- **Suggested fix:** Guard the paid transition in the payment-idempotency work; a conditional update here
  would break the repository's no-optimistic-concurrency rule and the in-memory test provider.
- **History:**
  - v7: found (pass B, F5)
  - round 7: deferred → bolt-035, with D9
  - v8: deferral upheld @`ac97e42` — the controller was untouched in the round
  - 2026-07-22: target closed with the row still deferred

### D54 — Upload service re-advertises HEIC while the validator and the UI still reject it

- **What:** The rejection message listed HEIC as accepted although the type validator and the interface
  both refuse it, so the user is told to upload a format that always fails. It reintroduced a defect
  closed in bolt-042.
- **History:**
  - v7: found (pass B, F6)
  - round 7: fixed @`b171ce8` — the message no longer offers HEIC and the dead extension branch is gone
  - v8: verified @`ac97e42`

### D55 — Client filename is not truncated to the column width → Postgres rejects it and returns 500

- **What:** An overlong client filename was stored unmodified. The in-memory and SQLite providers accept
  it, so every test passes, but Postgres rejects it and the request fails with 500 in production only.
- **History:**
  - v7: found (pass B, F7) — hinted by the shared dual-database context
  - round 7: fixed @`b171ce8` — the filename is sanitised and truncated at the service boundary
  - v8: verified @`ac97e42`

### D56 — The retention audit event is written before the save, so a failed save leaves false audit records

- **What:** The archive-expired audit event was emitted before the batched save. A save failure left
  audit records for rows that were never persisted, and the same rows fired again on the next tick.
- **History:**
  - v7: found (pass A, F8)
  - round 7: fixed @`04149fa` — the audit is emitted only after the save succeeds
  - v8: verified @`ac97e42`

### D57 — Cloud-off purge refusal is logged at Error on every ship in the default configuration

- **What:** The ship path lacked the cloud-enabled gate the cancel path has, so the default local-only
  configuration logged an error on every shipment. Constant false errors mask real ones.
- **History:**
  - v7: found (pass A, F9)
  - round 7: fixed @`fe0e6d2` — the ship path is gated like the cancel path
  - v8: verified @`ac97e42`

### D58 — The promotion worker's retry, backoff and re-enqueue path is entirely untested

- **What:** No test drove the worker's retry path, so its backoff and re-queue behaviour was unproven.
- **History:**
  - v7: found (pass B, F10)
  - round 7: fixed @`df1026d` — slot-starvation and retry-success tests
  - v8: verified @`ac97e42` — by construction through the D51 revert

### D59 — No test asserts that the payment webhook enqueues promotion

- **What:** The wiring from the payment webhook to the promotion queue had no test, so deleting the call
  would have shipped green.
- **History:**
  - v7: found (pass B, F11)
  - round 7: fixed @`a80b819` — an integration test asserts the enqueue for a paid order
  - v8: verified @`ac97e42` — by mutation; commenting out the call reddens exactly that test

### D60 — A real cloud provider is never exercised — only a skip-gated MinIO suite and fakes

- **What:** Cloud behaviour is proven only against a MinIO container in continuous integration and
  in-memory fakes. No test ever runs against the real provider, including the retry path D49 lived in.
- **Evidence:** `Tests/…/S3StorageServiceIntegrationTests.cs:18`.
- **Suggested fix:** Exercise the real provider in the three-environment stage; this is environment work,
  not a code change.
- **History:**
  - v7: found (pass B, F12)
  - round 7: deferred → the three-environment track, with D20; the D49 regression tests shrank what only a real provider can prove
  - v8: deferral upheld @`ac97e42` — the integration tests were untouched in the round
  - 2026-07-22: target closed with the row still deferred

### D61 — Retention's fixed candidate window is starved by rows whose delete keeps failing

- **What:** The retention sweep orders by age and takes a fixed window. Rows whose delete keeps failing
  stay at the front of that window every tick, so newer expired rows are never reached.
- **Evidence:** `BackgroundJobs/ArchiveRetentionJob.cs:98`. The same shape as D38, in a second job.
- **Suggested fix:** Exclude repeatedly failing rows from the candidate window so it advances.
- **History:**
  - v7: found (pass A)
  - round 7: sent to backlog under the severity-based stop rule
  - v8: backlog upheld @`ac97e42`
  - 2026-08-10: row carried to `reviews/backlog.md`

### D62 — A read failure part-way through the admin ZIP truncates the archive after the headers are sent

- **What:** The admin ZIP opens the response, then reads each original in turn. A read that fails part-way
  through, including one racing a concurrent purge or promotion, leaves the admin with a truncated archive
  and no clean error, because the headers are already committed.
- **Evidence:** `Services/AdminOrderService.cs:197`.
- **Suggested fix:** Check every entry can be read before writing the response, or write to a buffer that
  can still fail cleanly.
- **History:**
  - v7: found by both passes
  - round 7: sent to backlog
  - v8: backlog upheld @`ac97e42`
  - v9: widened — a concurrent promotion moving the original to cloud and deleting the local copy is a
    second trigger alongside the concurrent purge
  - 2026-08-10: row carried to `reviews/backlog.md`, where it appears twice — one row to prune

### D63 — Preview cache-fill regeneration races the retention delete → an orphaned blob and a null reference

- **What:** Regenerating a preview at the same moment retention deletes it leaves the freshly written blob
  with no row pointing at it, because retention then clears the key.
- **Evidence:** `Services/UploadService.cs:203` against `BackgroundJobs/ArchiveRetentionJob.cs:124`.
- **Suggested fix:** Do not regenerate a preview for an upload the retention window has already expired,
  or re-check the row before persisting the key.
- **History:**
  - v7: found by both passes
  - round 7: sent to backlog
  - v8: backlog upheld @`ac97e42`
  - 2026-08-10: row carried to `reviews/backlog.md`

### D64 — A failed best-effort local delete in the promoter leaks local bytes nothing reclaims

- **What:** After a successful promotion the promoter deletes the local original on a best-effort basis.
  If that delete fails, the row is already recorded as cloud, so no later job looks at the local file and
  the bytes stay on disk forever.
- **Evidence:** `Services/OrderPhotoPromoter.cs:212`.
- **Suggested fix:** Record the failed delete so a sweep can retry it.
- **History:**
  - v7: found (pass B)
  - round 7: sent to backlog
  - v8: backlog upheld @`ac97e42`
  - 2026-08-10: row carried to `reviews/backlog.md`

### D65 — The local storage root re-anchor uses a prefix match with no separator boundary

- **What:** The local adapter re-anchors stored paths under the storage root using a plain prefix match.
  Without a separator boundary a sibling directory whose name starts with the root's name also matches.
- **Evidence:** `Services/LocalStorageService.cs:99`.
- **Suggested fix:** Compare on a path-separator boundary, or resolve and compare full paths.
- **History:**
  - v7: found (pass B) — judged plausible
  - round 7: sent to backlog
  - v8: backlog upheld @`ac97e42`
  - 2026-08-10: row carried to `reviews/backlog.md`

### D66 — The ZIP entry extension is taken from the untrusted client filename, not the validated type

- **What:** Entry names inside the admin ZIP take their extension from the filename the client sent,
  rather than the validated content type the server already knows.
- **Evidence:** `Services/AdminOrderService.cs:190`.
- **Suggested fix:** Derive the entry extension from the validated content type.
- **History:**
  - v7: found (pass B)
  - round 7: sent to backlog
  - v8: backlog upheld @`ac97e42`
  - 2026-08-10: row carried to `reviews/backlog.md`

### D67 — Batch upload caps total bytes but not the number of files

- **What:** A batch upload is limited to 500 MB in total with no limit on how many files it may contain,
  so a very large number of tiny files passes the check.
- **Evidence:** `Controllers/UploadsController.cs:102`.
- **Suggested fix:** Add a file-count cap alongside the byte cap.
- **History:**
  - v7: found (pass B)
  - round 7: sent to backlog
  - v8: backlog upheld @`ac97e42`
  - 2026-08-10: row carried to `reviews/backlog.md`

### D68 — A broken grid thumbnail has no fallback or retry after the one presigned-URL refresh

- **What:** The grid refreshes signed URLs once when a thumbnail fails. If the refreshed URL also fails,
  the tile stays broken with no placeholder and no further retry.
- **Evidence:** `UI/…/order-detail-page.ts:472`.
- **Suggested fix:** Show a placeholder tile and allow a manual retry after the single refresh fails.
- **History:**
  - v7: found (pass B)
  - round 7: sent to backlog
  - v8: backlog upheld @`ac97e42`
  - 2026-08-10: row carried to `reviews/backlog.md`

### D69 — Originals of orders that never reach production-complete or Cancelled escape the retention window

- **What:** Retention only considers orders that reached a production-complete status or were cancelled,
  so an order that stalls in any other state keeps its original indefinitely.
- **Evidence:** `BackgroundJobs/ArchiveRetentionJob.cs:92`.
- **Suggested fix:** Include stalled orders in the retention window, or record the intended exception.
- **History:**
  - v7: found (pass B)
  - round 7: sent to backlog
  - v8: backlog upheld @`ac97e42`
  - v9: re-raised, decision upheld @`ac97e42`
  - 2026-08-10: row carried to `reviews/backlog.md`

### D70 — The documented 502 for a persistent storage failure is not implemented; it surfaces as 500

- **What:** The bolt-043 requirement says a persistent storage failure returns 502. The code has no such
  mapping, so the caller gets a generic 500.
- **Evidence:** `Services/S3StorageService.cs:145`.
- **Suggested fix:** Map a persistent storage failure to the documented status, or correct the requirement.
- **History:**
  - v7: found (pass A)
  - round 7: sent to backlog
  - v8: backlog upheld @`ac97e42`
  - 2026-08-10: row carried to `reviews/backlog.md`

### D71 — Idempotent-skip reasons are logged at Debug and never emit under the Information floor

- **What:** The promoter logs why it skipped an already-promoted upload at Debug level, below the
  configured floor, so the reason never reaches the log.
- **Evidence:** `Services/OrderPhotoPromoter.cs:120`.
- **Suggested fix:** Raise these reasons to Information, or drop the calls.
- **History:**
  - v7: found (pass A)
  - round 7: sent to backlog
  - v8: backlog upheld @`ac97e42`
  - 2026-08-10: row carried to `reviews/backlog.md`

### D72 — Transient and permanent cloud-write failures collapse into one warning, so poison is retried

- **What:** Both a temporary cloud outage and a permanently bad object produce the same warning and the
  same retry, so a permanently failing upload is retried like a passing blip.
- **Evidence:** `Services/OrderPhotoPromoter.cs:182`.
- **Suggested fix:** Classify the failure and stop retrying the permanent case.
- **History:**
  - v7: found (pass A)
  - round 7: sent to backlog
  - v8: backlog upheld @`ac97e42`
  - 2026-08-10: row carried to `reviews/backlog.md`

### D73 — The preview cache-hit path lost its no-tracking read

- **What:** The hot preview cache-hit path now loads the upload with change tracking on, which it does
  not need. It was a no-tracking read before bolt-042.
- **Evidence:** `Services/UploadService.cs:139`.
- **Suggested fix:** Restore the no-tracking read on the cache-hit path.
- **History:**
  - v7: found (pass A) — a regression from bolt-042
  - round 7: sent to backlog
  - v8: backlog upheld @`ac97e42`
  - 2026-08-10: row carried to `reviews/backlog.md`

### D74 — The promotable-status set is written out three times under a false single-source comment

- **What:** The set of order statuses eligible for promotion is duplicated in three places, and a comment
  claims one of them is the single source of truth. The three can drift apart silently.
- **Evidence:** `Cli/BackfillCommand.cs:43`.
- **Suggested fix:** Define the set once and reference it from all three call sites.
- **History:**
  - v7: found (pass A) — judged plausible
  - round 7: sent to backlog
  - v8: backlog upheld @`ac97e42`
  - v9: re-raised, decision upheld @`ac97e42`
  - 2026-08-10: row carried to `reviews/backlog.md`

### D75 — The S3 retry classification, re-upload and presign protocol are untested

- **What:** Nothing tested which failures the adapter treats as retryable, what a retried upload sends, or
  which protocol the signed URL uses. This is the coverage gap D49 lived inside.
- **Evidence:** `Services/S3StorageService.cs:60` for the retry policy and `:41` for the signing.
- **Suggested fix:** Test the retry classification and the signing protocol directly.
- **History:**
  - v7: found (pass B)
  - round 7: sent to backlog; the re-upload half gained a regression test with the D49 fix @`c37ca44`
  - v8: backlog upheld @`ac97e42`
  - v9: re-raised, decision upheld @`ac97e42`
  - 2026-08-10: row carried to `reviews/backlog.md`

### D76 — Storage wiring, configuration and the CLI sat outside the lens list; the region setting is a trap

- **What:** The storage registration, its settings and the command-line entry point were not in the file
  list the lenses reviewed, so they got lighter scrutiny. The pass also flagged the provider region setting
  as easy to get wrong.
- **Evidence:** `Extensions/StorageExtensions.cs:56`.
- **Suggested fix:** Include these files in the review file list, and validate the region setting at boot.
- **History:**
  - v7: found (pass B)
  - round 7: sent to backlog
  - v8: backlog upheld @`ac97e42`
  - 2026-08-10: row carried to `reviews/backlog.md`

### D77 — Recovery and retention sweeps run unindexed full scans every six hours

- **What:** The sweep queries have no supporting index, so each one scans the whole uploads table every six
  hours. The in-memory test provider hides the cost.
- **Evidence:** `Data/Configurations/UploadConfiguration.cs:30`.
- **Suggested fix:** Add an index covering the sweep predicates.
- **History:**
  - v7: found (pass B) — judged plausible
  - round 7: sent to backlog
  - v8: backlog upheld @`ac97e42`
  - 2026-08-10: row carried to `reviews/backlog.md`

### D78 — The promoter reads the whole original into an array and leaves memory streams undisposed

- **What:** The promoter materialises the entire original photo in memory and creates more than one memory
  streams it never disposes, for every promoted upload.
- **Evidence:** `Services/OrderPhotoPromoter.cs:138`.
- **Suggested fix:** Stream the original through instead of buffering it, and dispose what is created.
- **History:**
  - v7: found (pass A) — two lenses
  - round 7: sent to backlog
  - v8: backlog upheld @`ac97e42`
  - 2026-08-10: row carried to `reviews/backlog.md`

### D79 — The best-effort orphan-thumbnail delete swallows its exception with no log

- **What:** A failed delete of an orphaned thumbnail is caught and discarded silently, so the leak leaves
  no trace.
- **Evidence:** `Services/UploadService.cs:222`.
- **Suggested fix:** Log the failure at warning level.
- **History:**
  - v7: found (pass A)
  - round 7: sent to backlog
  - v8: backlog upheld @`ac97e42`
  - 2026-08-10: row carried to `reviews/backlog.md`

### D80 — Local preview cache header disagrees with the documented one

- **What:** The design record says local previews are cached publicly and immutably; the code sends a
  private cache header instead.
- **Evidence:** `Controllers/UploadsController.cs:26`.
- **Suggested fix:** Align the header with the design record, or correct the record.
- **History:**
  - v7: found (pass A)
  - round 7: sent to backlog
  - v8: backlog upheld @`ac97e42`
  - v9: re-raised, decision upheld @`ac97e42`
  - 2026-08-10: row carried to `reviews/backlog.md`

### D81 — A freshly generated local thumbnail is re-read from disk on a cache miss

- **What:** On a cache miss the service generates the thumbnail, writes it to disk and then reads the same
  bytes back to serve them.
- **Evidence:** `Services/UploadService.cs:240`.
- **Suggested fix:** Serve the bytes already in hand.
- **History:**
  - v7: found (pass A)
  - round 7: sent to backlog
  - v8: backlog upheld @`ac97e42`
  - 2026-08-10: row carried to `reviews/backlog.md`

### D82 — Order detail shows both the interceptor toast and an inline error for one failure

- **What:** A single failed request produces two messages, the global toast and the page's own inline
  error or redirect.
- **Evidence:** `UI/…/order-detail-page.ts:403`.
- **Suggested fix:** Pick one channel per failure class.
- **History:**
  - v7: found (pass B)
  - round 7: sent to backlog
  - v8: backlog upheld @`ac97e42`
  - 2026-08-10: row carried to `reviews/backlog.md`

### D83 — "Photos no longer available" is shown for a just-paid order and for pending orders

- **What:** A customer who has just paid sees the message saying their photos are gone, during the seconds
  before archiving runs, and so does a customer whose order has not been paid yet. It is the concrete form
  of the D13 follow-up.
- **History:**
  - v9: found — the certification pass's one genuinely new user-facing Medium
  - 2026-07-27: owner ruled fix now
  - round 9: fixed @`d041295` and `b9af326` — the message is gated on the order's lifecycle, with specs across the whole status matrix

### D84 — No test asserts that the EuPlatesc payment notification enqueues promotion

- **What:** The Stripe path has a wiring test; the EuPlatesc path does not, so deleting its enqueue call
  would ship green.
- **History:**
  - v9: found — coverage sibling of D59
  - 2026-07-27: owner ruled wont-fix — the EuPlatesc gateway is slated for removal, so its coverage is not worth building

### D85 — The backfill command was outside the review file list, and backfill against the live worker is untested

- **What:** The backfill command was not in the file list the lenses reviewed, so it got lighter scrutiny.
  Running it while the live worker promotes the same orders is also untested.
- **Evidence:** `Cli/BackfillCommand.cs`, absent from the pass's changed-file list.
- **Suggested fix:** Review the command and test the concurrent case when the operator tooling is first
  used against a real environment.
- **History:**
  - v9: found — recorded as a confidence caveat for that file
  - 2026-07-27: owner ruled defer to the three-environment stage, with D20 and D60; affirmed @`d041295`
  - 2026-07-22: target closed with the row still deferred

### D86 — Retention deletes the blobs before it persists the null keys → a broken-URL window

- **What:** Retention deletes the preview and thumbnail objects and only then clears their keys in the
  database. A read landing in between gets a URL for an object that is already gone.
- **Evidence:** `BackgroundJobs/ArchiveRetentionJob.cs:146`.
- **Suggested fix:** Clear the keys first, then delete the objects.
- **History:**
  - v9: found — sent to backlog
  - 2026-08-10: row carried to `reviews/backlog.md`

### D87 — The retention sweep query has no soft-delete filter, so it reprocesses deleted rows

- **What:** The retention candidate query does not exclude soft-deleted uploads, so it processes them
  again and emits audit records for rows that are already gone.
- **Evidence:** `BackgroundJobs/ArchiveRetentionJob.cs:96`. The same class as D52 and D56, missed when
  those were fixed.
- **Suggested fix:** Add the soft-delete filter to the candidate query.
- **History:**
  - v9: found — sent to backlog
  - 2026-08-10: row carried to `reviews/backlog.md`

### D88 — Promoter tests assert the cloud keys written but never the bytes

- **What:** The promoter tests check which keys were written to cloud storage but never what was written
  to them, so a wrong or empty payload would pass.
- **Evidence:** `Tests/…/OrderPhotoPromoterTests.cs`.
- **Suggested fix:** Assert the written content, not only the key.
- **History:**
  - v9: found — sent to backlog
  - 2026-08-10: row carried to `reviews/backlog.md`

### D89 — Code comments cite finding, decision and design-record ids, which the repo rule bans

- **What:** Fix comments across the codebase named finding, decision and design-record identifiers, which
  the repository comment rule forbids. 67 occurrences in 27 files, mostly predating this feature.
- **History:**
  - v9: found — sent to backlog as a dedicated sweep, not a per-file scramble
  - 2026-07-30: fixed @`09173c4` — 371 occurrences removed across 118 tracked files, both suites green; the
    records auditor now counts them

### D90 — Closing the lightbox during a refresh has no spec; only closing before the error is tested

- **What:** The D36 fix also covers closing the lightbox while the refresh is still in flight, by re-reading
  the photo id when the fetch resolves. Only the close-before-error case has a spec.
- **Evidence:** `UI/…/order-detail-page.spec.ts`.
- **Suggested fix:** Add a spec that closes the lightbox during the refresh and asserts it stays closed.
- **History:**
  - v9: found — sent to backlog
  - 2026-08-10: row carried to `reviews/backlog.md`
