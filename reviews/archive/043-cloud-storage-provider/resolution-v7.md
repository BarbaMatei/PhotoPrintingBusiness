---
type: resolution
target: 043-cloud-storage-provider
version: 7
answers: review-v7.md
status: resolved
fixed_commit: ac97e42
closed: 2026-07-22
---

# Resolution v7 — 043-cloud-storage-provider

## Findings

| D# | Status | Commit | Note |
|---|---|---|---|
| D49 | fixed | `c37ca44` | The rewind moved inside the retry attempt, so every attempt sends the whole photo, and a stream that cannot be rewound fails loudly instead of uploading nothing. The test was red with the exact signature: the retry got 0 of 8 bytes. |
| D50 | fixed | `4dfd755` | Live-order guards at both destruction sites, after a design check. Purge skips while another order sharing the upload is paid or printing; retention deletes only when no sharing order paid inside the window. See Decisions. |
| D51 | fixed | `df1026d` | The retry wait is detached from the concurrency slot, so the slot covers active work only. Parked retries are bounded at 100 and anything past the cap falls to the recovery sweep with a warning. Corrections from the design check applied. |
| D52 | fixed | `5cfc9f9` | The order photos query filters soft-deleted uploads, so blobs the cleanup job already removed are no longer signed. Test: a soft-deleted upload yields no photos and no signing calls. |
| D53 | deferred | — | Same root cause as D9. The repository rule bars optimistic concurrency, so a conditional update here would break both the rule and the in-memory test provider. See Decisions. |
| D54 | fixed | `b171ce8` | The rejection message no longer offers HEIC and the dead extension branch is gone. A test pins the message. |
| D55 | fixed | `b171ce8` | The filename is stripped of its directory part and truncated to the column's 260 characters at the service boundary. Enforcing it in the database stays with the three-environment work. |
| D56 | fixed | `04149fa` | The archive-expired audit event is emitted only after the batched save, with the ids collected in the loop. Tests: a failed save emits nothing, a successful one emits one event per upload. |
| D57 | fixed | `fe0e6d2` | The ship path is gated on the archive and cloud settings, mirroring the cancel path, so the supported local-only configuration no longer logs an error on every shipment. |
| D58 | fixed | `df1026d` | The retry path is now tested: a healthy job is not blocked behind a failed job's one-hour wait, and a failed job re-queues and succeeds on its second attempt with no wait. |
| D59 | fixed | `a80b819` | An integration test asserts the succeeded webhook enqueues promotion for the paid order, using a recording promoter. Deleting the call now reddens it. |
| D60 | deferred | — | Exercising a real cloud provider beyond the MinIO suite is environment work, not a code change. The sharpest gap it named is now unit-covered by the D49 tests. See Decisions. |
| D61 | backlog | — | Low. Exclude repeatedly failing rows from the retention candidate window so it advances. |
| D62 | backlog | — | Low. Check every entry can be read before the ZIP response starts, or write somewhere that can still fail cleanly. |
| D63 | backlog | — | Low. Do not regenerate a preview for an upload whose retention window has expired, or re-check the row before persisting the key. |
| D64 | backlog | — | Low. Record a failed best-effort local delete so a sweep can retry it. |
| D65 | backlog | — | Low. Compare the storage root on a path-separator boundary rather than a plain prefix. |
| D66 | backlog | — | Low. Derive the ZIP entry extension from the validated content type, not the client's filename. |
| D67 | backlog | — | Low. Add a file-count cap to the batch upload alongside the byte cap. |
| D68 | backlog | — | Low. Show a placeholder tile and allow a manual retry after the single URL refresh fails. |
| D69 | backlog | — | Low. Include orders stalled outside the production-complete and cancelled states in the retention window, or record the exception. |
| D70 | backlog | — | Low. Map a persistent storage failure to the documented status, or correct the requirement. |
| D71 | backlog | — | Low. Raise the idempotent-skip reasons to Information, or drop the calls. |
| D72 | backlog | — | Low. Classify cloud-write failures and stop retrying the permanent ones. |
| D73 | backlog | — | Low. Restore the no-tracking read on the preview cache-hit path. |
| D74 | backlog | — | Low. Define the promotable-status set once and reference it from all three call sites. |
| D75 | backlog | — | Low. Test the retry classification and the signing protocol directly; the re-upload half gained a test with the D49 fix. |
| D76 | backlog | — | Low. Put the storage wiring, settings and command-line entry point in the review file list, and validate the region setting at boot. |
| D77 | backlog | — | Low. Add an index covering the sweep predicates. |
| D78 | backlog | — | Cleanup. Stream the original through instead of buffering it, and dispose what is created. |
| D79 | backlog | — | Cleanup. Log the failed orphan-thumbnail delete at warning level. |
| D80 | backlog | — | Cleanup. Align the local preview cache header with the design record, or correct the record. |
| D81 | backlog | — | Cleanup. Serve the freshly generated thumbnail bytes already in hand. |
| D82 | backlog | — | Cleanup. Pick one feedback channel per failure class instead of both the toast and the inline error. |

## Scope

| Cluster | Findings | Files | Approach-check |
|---|---|---|---|
| A — Retry-safe cloud upload (`c37ca44`) | D49 | `Services/S3StorageService.cs` | not needed (the rewind moved inside the existing retry) |
| B — Live-order guards at every destruction site (`4dfd755`, `ac97e42`) | D50 | `Services/OriginalPurger.cs`, `BackgroundJobs/ArchiveRetentionJob.cs`, `BackgroundJobs/UploadCleanupJob.cs` | run before implementation — three corrections applied, see Decisions |
| C — Retry detached from the concurrency slot (`df1026d`) | D51, D58 | `BackgroundJobs/OrderPhotoPromotionWorker.cs` | run before implementation — three corrections applied, see Decisions |
| D — Query, message, filename, audit and log fixes (`5cfc9f9`, `b171ce8`, `04149fa`, `fe0e6d2`) | D52, D54, D55, D56, D57 | `Services/OrderService.cs`, `Services/UploadService.cs`, `BackgroundJobs/ArchiveRetentionJob.cs`, `Services/OriginalPurger.cs` | not needed (no new mechanism) |
| E — Webhook wiring test (`a80b819`) | D59 | `Tests/…/PaymentControllerIntegrationTests.cs` | not needed (tests only) |
| F — Deferred and backlog | D53, D60, D61–D82 | — | not needed (no code changed) |

## Decisions

### The round ran lean, with only the two subagents the process requires

Owner direction, under cost pressure. No discovery fan-outs ran, because every finding already carried
its file, line and fix. Two subagents ran and both were required: the design check covering the two
risky changes, D50 and D51, and the fresh-eyes review of the final diff.

### The shared-photo guard leaves one accepted residual (D50)

The guard blocks destruction while another order referencing the same upload is paid or printing. An
order still awaiting payment that pays after the purge ships without that photo. Blocking on
awaiting-payment instead would let every abandoned checkout pin storage indefinitely. Owner direction:
the sharing flows are a corner case today, so the residual is accepted and revisited when the
concurrency-token work lands, which also allows a cleaner reservation model. The design check
contributed three corrections that were applied: bounded thread-safe retry tracking, the paid and
printing status set, and a matching paid-date clause in the retention condition.

### The webhook race is deferred, not patched (D53)

It is the same defect D9 was deferred for. The repository rule is that there is no optimistic
concurrency anywhere and that unique indexes plus violation detection are the mechanism, so a
conditional update here would break the rule and would not work on the in-memory test provider. The
concurrency-token work now explicitly carries the duplicate-confirmation-email consequence.

### Real-provider coverage is environment work (D60)

MinIO runs on every push, so what is missing is exposure to the real provider, which belongs to the
deployment track alongside D20. The sharpest gap the finding named, the untested retry path, is now
covered by the D49 regression tests.

### The fresh-eyes review found the same data-loss class at a third site

The review of the final diff caught one real problem and it was fixed in-round at `ac97e42`: the
cleanup job's referenced-retention branch destroyed shared uploads on age alone, carrying none of the
new guards, so it could delete a photo a paid or printing order still needed. The shared retention
condition now excludes those, with three regression tests. The same review confirmed there are no
other stream-consuming retry sites, that the other signing path already filters soft-deleted rows,
that the retry counter is balanced on every path, and that no current caller passes a stream that
cannot be rewound, so the new loud failure is defensive only.
