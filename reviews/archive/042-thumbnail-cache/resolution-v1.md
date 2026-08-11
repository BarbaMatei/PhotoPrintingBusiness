---
type: resolution
target: 042-thumbnail-cache
version: 1
answers: review-v1.md
status: resolved
fixed_commit: fad7693
closed: 2026-07-14
---

# Resolution v1 — 042-thumbnail-cache

## Findings

| D# | Status | Commit | Note |
|---|---|---|---|
| D1 | fixed | `9af3b87` | The preview directive is now `private` with a 30-day lifetime, so response caching no longer stores it. An integration test pins private, not public, and the 30 days. |
| D2 | fixed | `533996c` | The per-axis check became a total-pixel cap using a wide multiply at both decode sites, plus a one-frame limit. A test against the real processor refuses a 54 MP image before decoding. |
| D3 | fixed | `978620c` | The interceptor specification covers both guest branches: a token present clears it without logging out, and no token neither logs out nor navigates. |
| D4 | fixed | `c245a1e` | Cleanup deletes the thumbnail alongside the original in the same guarded block. Tests: an upload with a thumbnail loses both files, one without loses only the original. |
| D5 | fixed | `c245a1e` | The thumbnail key is deterministic and owner-scoped under a separate prefix, so a racing or cancelled write overwrites the same key. No concurrency token; see Decisions. |
| D6 | fixed | `533996c` | The generate path catches the general image-format exception, which covers both unknown and broken content, and raises the 422 exception instead of a raw 500. |
| D7 | fixed | `533996c` | A 512 MB allocator cap was added at startup, and story 003 was amended at `eb8a6f8`. The width and height options the story named do not exist in the shipped library. |
| D8 | fixed | `c245a1e` | The path is `thumbs/{ownerId}/{uploadId}.jpg`, owner-scoped rather than the story's flat form so it cannot collide with the original. Story 002 records the built path. |
| D9 | fixed | `eb8a6f8` | Story 002's soft-delete case was amended to 404, matching the implemented and tested behaviour. See Decisions. |
| D10 | fixed | `eb8a6f8` | Both bundled changes are documented in the bolt with retroactive criteria, and the guest-auth half is now backed by real tests. Splitting them into separate bolts is left to the owner. |
| D11 | fixed | `f55daae` | One in-flight init is shared and reset when it settles, so concurrent callers do not mint two sessions. Specification: two concurrent calls trigger one init. |
| D12 | fixed | `f55daae` | The upload retries exactly once after a 401, since the interceptor clears the stale token and the page re-inits. Specification: a 401 then success uploads twice; a 500 does not retry. |
| D13 | fixed | `978620c` | The 401 handler now branches on whether the user is signed in: signed in logs out and redirects, otherwise the guest token is cleared with no navigation. |
| D14 | fixed | `f55daae` | Restore tells a 401 from a 404: a 401 re-inits and retries once, a 404 drops the entry. Both are pinned by specifications. |
| D15 | fixed | `21e66c8` | The batch endpoint takes a logger and each swallowed rejection emits a warning naming the file, the reason type and the correlation id. A controller test checks the log and the 200. |
| D16 | fixed | `26165a3` | The client-abort log was raised from Debug to Information as a distinct event, which is what the configured floor emits. A middleware test checks it. |
| D17 | fixed | `533996c` | A distinct bomb exception carrying the dimensions maps to 422, and the middleware emits the reserved event with the dimensions and the correlation id. |
| D18 | fixed | `c245a1e` | No-tracking is restored on the preview read; the miss branch attaches the row and marks only the thumbnail column as changed. |
| D19 | fixed | `c245a1e` | The miss branch rewinds and returns the generated stream instead of disposing it and re-reading the file. |
| D20 | fixed | `533996c` | One shared limit helper and one message constant, used at both decode sites. |
| D21 | fixed | `9af3b87` | The cache lifetime is a named constant derived from a 30-day span; the inline number is gone. |
| D22 | fixed | `eb8a6f8` | A short note records that the duplication across the two database branches is deliberate, which is what the finding allowed. |
| D23 | fixed | `bca68fa` | The migration is provider-aware again, matching the preceding one. Editing in place is safe because no Postgres has applied it. The database test stays deferred; see Decisions. |
| D24 | fixed | `f850f69` | The container brand is checked against a known set, so video containers are refused up front. A legitimate HEIC still fails at decode; see Decisions. |
| D25 | fixed | `533996c` | New tests drive the real processor: oversized raises the bomb exception, a small valid image produces a bounded JPEG, unreadable gives 422, and the limit helper is checked at its boundary. |
| D26 | fixed | `c245a1e` | The service is driven through a database context separate from the one that seeds and asserts, on the same store, so a fresh-context read proves the save actually ran. |
| D27 | fixed | `fad7693` | Tests added for the cache directive, the deterministic key, the shared init and the conditional GET. The migration test is deferred with D23. |
| D28 | deferred | — | Not triggerable today: every storage implementation returns a rewindable stream. Recorded as a design constraint for the cloud-storage bolt. See Decisions. |

## Scope

| Cluster | Findings | Files | Approach-check |
|---|---|---|---|
| A — Decode limits, bomb exception and the real-processor tests (`533996c`) | D2, D7, D17, D20, D25, D6 | `Services/ImageProcessor.cs`, `Services/UploadService.cs`, `Program.cs`, `Tests/…/ImageProcessorTests.cs` | not needed (one shared helper at two existing call sites) |
| B — Deterministic thumbnail key, cleanup and tracking (`c245a1e`) | D4, D5, D8, D18, D19, D26 | `Services/UploadService.cs`, `BackgroundJobs/UploadCleanupJob.cs` | not needed (a key scheme plus a delete at an existing site) |
| C — Cache directive and its constant (`9af3b87`) | D1, D21 | `Controllers/UploadsController.cs` | not needed (one header value) |
| D — Guest-session sharing and retry (`f55daae`) | D11, D12, D14 | `UI/…/format-selector-page.ts` | not needed (one shared observable) |
| E — Interceptor branches and their tests (`978620c`) | D3, D13 | `UI/…/error.interceptor.ts`, `UI/…/error.interceptor.spec.ts` | not needed (one condition and its tests) |
| F — Logging (`21e66c8`, `26165a3`) | D15, D16 | `Controllers/UploadsController.cs`, `Middleware/ExceptionHandlerMiddleware.cs` | not needed (two log statements) |
| G — Documents and criteria (`eb8a6f8`) | D9, D10, D22 | `memory-bank/…` | not needed (documents only) |
| H — Container brand check (`f850f69`) | D24 | `Services/MimeValidator.cs` | not needed (one byte-range comparison) |
| I — Migration provider parity (`bca68fa`) | D23 | `Migrations/20260527102718_AddUploadThumbnailPath.cs` | not needed (mirrors the preceding migration) |
| J — Remaining tests (`fad7693`) | D27 | `Tests/…` | not needed (tests only) |
| K — Left undone this round | D28 | — | not needed (no code changed) |

## Decisions

### The deterministic key was chosen over a concurrency token (D5)

A concurrency token without a reload-and-retry handler turns today's quiet leak into an uncaught
failure and a 500 on every conflict. The deterministic key makes a racing or cancelled write overwrite
the same path, which is harmless, so no token was added. The key is owner-scoped under a separate
prefix rather than the story's flat form, because the flat form would collide with the original's key.

### The soft-delete case was amended to 404 rather than serving the thumbnail (D9)

The code filters soft-deleted rows and a test pins the 404. A soft-deleted upload is on its way out,
and cleanup now deletes the thumbnail too, so serving it would revive a deleted resource. The story was
amended to match. Push back if the thumbnail should be served instead.

### The bundled changes were documented rather than split (D10)

Both bundled change sets now carry a retroactive criterion, and the guest-auth half has real tests.
Rewriting history into separate bolts is a process decision for the owner. The review's own minimum bar
was "document with criteria and tests", which is met.

### The migration was fixed in place; its database test is deferred (D23)

The provider-aware column type is corrected, which is safe because no Postgres has applied this
migration. A test that actually runs the migration chain belongs to the three-environment phase, in
line with the preceding migration's deferral and the review's own "flag, do not necessarily build now".

### The container brand check was fixed; HEIC is still advertised (D24)

Video containers are now refused up front. A legitimate HEIC still fails, because the shipped image
library has no decoder for it, but it now fails cleanly at decode as a 422. Whether to stop advertising
the format is a product decision left to the owner rather than taken here.

### The cloud storage contract is a design constraint, not a fix (D28)

The rewind, the length used for the entity tag, and the per-hit existence check all hold for the only
storage implementation that exists. They break only when a cloud provider lands, so the row is recorded
for that bolt. The D19 fix already removes one storage round-trip per miss ahead of that move.

### Two counts in this round's records are wrong

This round reached a terminal state on 28 findings: 27 fixed and 1 deferred. The original hand-off
prose, the verification pass and the index row all say 26 fixed, while the verification pass's own list
of verified findings names 27. The per-finding rows above are the accurate record.
