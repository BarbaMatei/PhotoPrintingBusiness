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

| ID | Status | Commit | Note |
|---|---|---|---|
| PPW-198 | fixed | `c37ca44` | The rewind moved inside the retry attempt, so every attempt sends the whole photo, and a stream that cannot be rewound fails loudly instead of uploading nothing. The test was red with the exact signature: the retry got 0 of 8 bytes. |
| PPW-199 | fixed | `4dfd755` | Live-order guards at both destruction sites, after a design check. Purge skips while another order sharing the upload is paid or printing; retention deletes only when no sharing order paid inside the window. See Decisions. |
| PPW-200 | fixed | `df1026d` | The retry wait is detached from the concurrency slot, so the slot covers active work only. Parked retries are bounded at 100 and anything past the cap falls to the recovery sweep with a warning. Corrections from the design check applied. |
| PPW-201 | fixed | `5cfc9f9` | The order photos query filters soft-deleted uploads, so blobs the cleanup job already removed are no longer signed. Test: a soft-deleted upload yields no photos and no signing calls. |
| PPW-202 | deferred | — | Same root cause as PPW-158. The repository rule bars optimistic concurrency, so a conditional update here would break both the rule and the in-memory test provider. See Decisions. |
| PPW-203 | fixed | `b171ce8` | The rejection message no longer offers HEIC and the dead extension branch is gone. A test pins the message. |
| PPW-204 | fixed | `b171ce8` | The filename is stripped of its directory part and truncated to the column's 260 characters at the service boundary. Enforcing it in the database stays with the three-environment work. |
| PPW-205 | fixed | `04149fa` | The archive-expired audit event is emitted only after the batched save, with the ids collected in the loop. Tests: a failed save emits nothing, a successful one emits one event per upload. |
| PPW-206 | fixed | `fe0e6d2` | The ship path is gated on the archive and cloud settings, mirroring the cancel path, so the supported local-only configuration no longer logs an error on every shipment. |
| PPW-207 | fixed | `df1026d` | The retry path is now tested: a healthy job is not blocked behind a failed job's one-hour wait, and a failed job re-queues and succeeds on its second attempt with no wait. |
| PPW-208 | fixed | `a80b819` | An integration test asserts the succeeded webhook enqueues promotion for the paid order, using a recording promoter. Deleting the call now reddens it. |
| PPW-209 | deferred | — | Exercising a real cloud provider beyond the MinIO suite is environment work, not a code change. The sharpest gap it named is now unit-covered by the PPW-198 tests. See Decisions. |
| PPW-210 | backlog | — | Low. Exclude repeatedly failing rows from the retention candidate window so it advances. |
| PPW-211 | backlog | — | Low. Check every entry can be read before the ZIP response starts, or write somewhere that can still fail cleanly. |
| PPW-212 | backlog | — | Low. Do not regenerate a preview for an upload whose retention window has expired, or re-check the row before persisting the key. |
| PPW-213 | backlog | — | Low. Record a failed best-effort local delete so a sweep can retry it. |
| PPW-214 | backlog | — | Low. Compare the storage root on a path-separator boundary rather than a plain prefix. |
| PPW-215 | backlog | — | Low. Derive the ZIP entry extension from the validated content type, not the client's filename. |
| PPW-216 | backlog | — | Low. Add a file-count cap to the batch upload alongside the byte cap. |
| PPW-217 | backlog | — | Low. Show a placeholder tile and allow a manual retry after the single URL refresh fails. |
| PPW-218 | backlog | — | Low. Include orders stalled outside the production-complete and cancelled states in the retention window, or record the exception. |
| PPW-219 | backlog | — | Low. Map a persistent storage failure to the documented status, or correct the requirement. |
| PPW-220 | backlog | — | Low. Raise the idempotent-skip reasons to Information, or drop the calls. |
| PPW-221 | backlog | — | Low. Classify cloud-write failures and stop retrying the permanent ones. |
| PPW-222 | backlog | — | Low. Restore the no-tracking read on the preview cache-hit path. |
| PPW-223 | backlog | — | Low. Define the promotable-status set once and reference it from all three call sites. |
| PPW-224 | backlog | — | Low. Test the retry classification and the signing protocol directly; the re-upload half gained a test with the PPW-198 fix. |
| PPW-225 | backlog | — | Low. Put the storage wiring, settings and command-line entry point in the review file list, and validate the region setting at boot. |
| PPW-226 | backlog | — | Low. Add an index covering the sweep predicates. |
| PPW-227 | backlog | — | Cleanup. Stream the original through instead of buffering it, and dispose what is created. |
| PPW-228 | backlog | — | Cleanup. Log the failed orphan-thumbnail delete at warning level. |
| PPW-229 | backlog | — | Cleanup. Align the local preview cache header with the design record, or correct the record. |
| PPW-230 | backlog | — | Cleanup. Serve the freshly generated thumbnail bytes already in hand. |
| PPW-231 | backlog | — | Cleanup. Pick one feedback channel per failure class instead of both the toast and the inline error. |

## Scope

| Cluster | Findings | Files | Approach-check |
|---|---|---|---|
| A — Retry-safe cloud upload (`c37ca44`) | PPW-198 | `Services/S3StorageService.cs` | not needed (the rewind moved inside the existing retry) |
| B — Live-order guards at every destruction site (`4dfd755`, `ac97e42`) | PPW-199 | `Services/OriginalPurger.cs`, `BackgroundJobs/ArchiveRetentionJob.cs`, `BackgroundJobs/UploadCleanupJob.cs` | run before implementation — three corrections applied, see Decisions |
| C — Retry detached from the concurrency slot (`df1026d`) | PPW-200, PPW-207 | `BackgroundJobs/OrderPhotoPromotionWorker.cs` | run before implementation — three corrections applied, see Decisions |
| D — Query, message, filename, audit and log fixes (`5cfc9f9`, `b171ce8`, `04149fa`, `fe0e6d2`) | PPW-201, PPW-203, PPW-204, PPW-205, PPW-206 | `Services/OrderService.cs`, `Services/UploadService.cs`, `BackgroundJobs/ArchiveRetentionJob.cs`, `Services/OriginalPurger.cs` | not needed (no new mechanism) |
| E — Webhook wiring test (`a80b819`) | PPW-208 | `Tests/…/PaymentControllerIntegrationTests.cs` | not needed (tests only) |
| F — Deferred and backlog | PPW-202, PPW-209, PPW-210–PPW-231 | — | not needed (no code changed) |

## Decisions

### The round ran lean, with only the two subagents the process requires

Owner direction, under cost pressure. No discovery fan-outs ran, because every finding already carried
its file, line and fix. Two subagents ran and both were required: the design check covering the two
risky changes, PPW-199 and PPW-200, and the fresh-eyes review of the final diff.

### The shared-photo guard leaves one accepted residual

The guard blocks destruction while another order referencing the same upload is paid or printing. An
order still awaiting payment that pays after the purge ships without that photo. Blocking on
awaiting-payment instead would let every abandoned checkout pin storage indefinitely. Owner direction:
the sharing flows are a corner case today, so the residual is accepted and revisited when the
concurrency-token work lands, which also allows a cleaner reservation model. The design check
contributed three corrections that were applied: bounded thread-safe retry tracking, the paid and
printing status set, and a matching paid-date clause in the retention condition.

### The webhook race is deferred, not patched

It is the same defect PPW-158 was deferred for. The repository rule is that there is no optimistic
concurrency anywhere and that unique indexes plus violation detection are the mechanism, so a
conditional update here would break the rule and would not work on the in-memory test provider. The
concurrency-token work now explicitly carries the duplicate-confirmation-email consequence.

### Real-provider coverage is environment work

MinIO runs on every push, so what is missing is exposure to the real provider, which belongs to the
deployment track alongside PPW-169. The sharpest gap the finding named, the untested retry path, is now
covered by the PPW-198 regression tests.

### The fresh-eyes review found the same data-loss class at a third site

The review of the final diff caught one real problem and it was fixed in-round at `ac97e42`: the
cleanup job's referenced-retention branch destroyed shared uploads on age alone, carrying none of the
new guards, so it could delete a photo a paid or printing order still needed. The shared retention
condition now excludes those, with three regression tests. The same review confirmed there are no
other stream-consuming retry sites, that the other signing path already filters soft-deleted rows,
that the retry counter is balanced on every path, and that no current caller passes a stream that
cannot be rewound, so the new loud failure is defensive only.
