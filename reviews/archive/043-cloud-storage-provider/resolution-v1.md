---
type: resolution
target: 043-cloud-storage-provider
version: 1
answers: review-v1.md
status: resolved
fixed_commit: 319d7b3
closed: 2026-07-14
---

# Resolution v1 — 043-cloud-storage-provider

## Findings

| D# | Status | Commit | Note |
|---|---|---|---|
| D1 | fixed | `ec94fca` | The ZIP takes the storage router and reads each original through the tier recorded on its upload. A regression test seeds a cloud upload, makes the local tier throw, and asserts the archive still streams. |
| D2 | fixed | `6b63bd7` | Deletes route by the upload's tier and the large preview key is deleted too. Test: an aged cloud upload loses all three keys from the cloud tier while the local tier is untouched. |
| D3 | fixed | `0f85f56` | The cloud adapter translates a missing object into the shared file-missing exception, as the existence check already did. A mocked unit test proves a missing key translates and a forbidden one passes through. |
| D4 | fixed | `cc69025` | The purge recovery scanner became a periodic background service, boot sweep plus every six hours by default. See Decisions for the alternative that was rejected. |
| D5 | fixed | `d15b9af` | The preview cache lifetime is derived from the presign setting; a controller test asserts 1800 seconds for a 30-minute setting. Part b, the lightbox URL signed at page load, is deferred and carries the ledger id D5b. |
| D6 | fixed | `3d97258` | In-flight promotions are tracked and drained before the concurrency gate is disposed. Test: shutdown with a gated in-flight promotion blocks until it finishes. |
| D7 | fixed | `3326607` | The SQLite migration-chain test asserts the original-path column is nullable. The Postgres arm stays with the three-environment work, as the finding said. |
| D8 | fixed | `881547f` | The controller catches the missing local thumbnail and re-resolves once, redirecting to cloud or returning 404. Unit tests cover both outcomes. |
| D9 | deferred | — | No event de-duplication and no row version exist anywhere in the API. The fix needs a schema change that belongs to the payment-idempotency work. See Decisions. |
| D10 | wont-fix | — | 403 for a non-owner is the codebase-wide convention and order identifiers are unguessable. See Decisions. |
| D11 | fixed | `751894b` | The photos response sets a private, no-store cache header, matching the preview endpoint. An integration test asserts it on the owner's success response. |
| D12 | wont-fix | `cda3685` | Owner ruling: the photos endpoint stays signed-in-user only. No behaviour changed; a guest-token request returns 401 and a test pins that. See Decisions. |
| D13 | deferred | — | The empty state has four causes the page cannot tell apart without a signal from the API. That contract change belongs to the frontend lens this pass skipped. See Decisions. |
| D14 | fixed | `0ceabf8` | A test seeds a cloud upload with no thumbnail and only the original stored, then asserts the thumbnail is regenerated, saved to cloud and persisted. |
| D15 | fixed | `cda3685` | Integration tests for the photos endpoint: no authentication returns 401, another user 403, an unknown order 404, a guest token 401. |
| D16 | fixed | `a770a13` | Two promoter tests: a throwing database context proves a failed row update is counted as failed and leaves the row on local storage; a throwing preview generator does the same. |
| D17 | fixed | `2fcdf3d` | Owner ruling: purge on cancel. The cancel path purges after the refund and the sweep adds Cancelled as a backstop. Hardened at `957f61a` so it cannot log a false error or fail the committed cancel. |
| D18 | fixed | `682f1e2` | Backfill exit-code tests for cloud-off, no work, dry run, success and failure. The bucket verifier is covered by MinIO tests against the real protocol rather than a mock. |

## Scope

| Cluster | Findings | Files | Approach-check |
|---|---|---|---|
| A — Route storage reads and deletes by the upload's tier (`ec94fca`, `6b63bd7`, `0f85f56`) | D1, D2, D3 | `Services/AdminOrderService.cs`, `BackgroundJobs/UploadCleanupJob.cs`, `Services/S3StorageService.cs` | not needed (one interface swap at three call sites) |
| B — Periodic purge sweep (`cc69025`) | D4 | `BackgroundJobs/OriginalPurgeRecoveryScanner.cs`, `Configuration/ArchiveSettings.cs` | run before implementation — the alternative was rejected, see Decisions |
| C — Drain in-flight promotions on shutdown (`3d97258`) | D6 | `BackgroundJobs/OrderPhotoPromotionWorker.cs` | run before implementation — the tracked-list drain was approved |
| D — Preview lifetime and bounded re-resolve (`d15b9af`, `881547f`) | D5, D8 | `Controllers/UploadsController.cs` | not needed (a derived value and a single retry) |
| E — Photos endpoint header, scope and tests (`751894b`, `cda3685`) | D11, D12, D15 | `Controllers/OrdersController.cs`, `Tests/…/OrdersControllerIntegrationTests.cs` | not needed (one header and test coverage) |
| F — Purge on cancel (`2fcdf3d`, `957f61a`) | D17 | `Services/AdminOrderService.cs`, `BackgroundJobs/OriginalPurgeRecoveryScanner.cs` | not needed (an owner ruling implemented on the existing sweep) |
| G — Coverage only (`3326607`, `0ceabf8`, `a770a13`, `682f1e2`) | D7, D14, D16, D18 | `Tests/…` | not needed (tests only) |
| H — Left undone this round | D9, D10, D13 | — | not needed (no code changed) |

## Decisions

### The periodic sweep was chosen over calling the purger from the promoter (D4)

The obvious alternative was to fire the purge from the promoter when it finishes. The adversarial
check showed that path would read the order's status from a stale identity map and would carry
tracked entities across request scopes. A periodic sweep avoids both, is idempotent because a delete
of a missing key does nothing, and is bounded by a batch size. It costs one new setting, validated
above zero, and one log line per sweep.

### Guest order history stays out of scope (D12)

Owner ruling. The photos endpoint keeps its signed-in-user requirement, and a guest order's photos
stay unreachable through it. Nothing changed in the code; a test pins the 401 so the behaviour is
deliberate rather than accidental. If this is revisited, the change is the dual-authentication policy
plus a guest-session branch in the ownership check, mirroring the uploads controller.

### Cancelled orders' originals are purged (D17)

Owner ruling, taken to keep storage and personal-data exposure down rather than to keep refund
evidence. The cancel path purges straight away and the periodic sweep covers the case where the
promotion was still running when the order was cancelled. Because the purge runs after money has
already moved, it is guarded so a purge failure can never fail the cancellation, and it is gated on
the cloud tier so a local-only deployment logs nothing.

### 403 stays the answer for a non-owner (D10)

Returning 404 would hide whether an order exists, but 403 is what the account service, the admin
service and the sibling order-detail call already return. Changing only these two endpoints would
make them inconsistent with everything else for a negligible gain, since order identifiers are
random and unguessable. If the owner prefers to hide existence, that is a codebase-wide change, not
a fix for this feature.

### The duplicate-webhook race belongs to the payment-idempotency work (D9)

There is no event de-duplication table and no concurrency token on the order anywhere in the API, so
closing this needs a schema change. That is squarely the payment-idempotency remit, not storage.
What happens today is a duplicate confirmation email and a second promotion that is idempotent by
its deterministic keys, so nothing is lost.

### The four-way empty state needs an API signal first (D13)

The page cannot tell "not archived yet" from "cloud tier off", "purged by retention" and "genuinely
no photos" without something in the response saying so. Adding that signal is a small contract
change best designed under the frontend lens this lean pass skipped, so no code changed this round.

### The promotion recovery scanner was left boot-only, on purpose (observation)

Outside the finding set: the promotion scanner still runs once at boot, which is the same shape as
D4 in a different subsystem. Converting it is its own small feature, with its own retry semantics,
settings and tests, and the consequence is milder, because an upload that never promotes still
serves previews from local storage. Flagged for the next pass to weigh rather than fixed quietly.
