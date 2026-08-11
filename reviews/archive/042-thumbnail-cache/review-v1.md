---
type: review
target: 042-thumbnail-cache
version: 1
supersedes: null
commit: cf78fb4
branch: feat/bolt-042-thumbnail-cache
pass-type: discovery
date: 2026-07-13
lenses: [correctness-cache, correctness-image, security, pr-requirements, quality-altitude, db-migration-parity, input-validation, observability, race-concurrency, frontend-ux, tests-coverage, completeness-critic]
lenses-not-run: []
verdict: request-changes
blockers: [D1, D2, D3]
findings: { high: 3, medium: 8, low: 14, cleanup: 3, refuted: 2 }
tests: { dotnet: "490/490", frontend: "not recorded" }
---

# Review v1 — 042-thumbnail-cache

## Findings

| F# | D# | Sev | Title | File | Fix now? |
|---|---|---|---|---|---|
| SEC-1 | D1 | 🔴 | Preview `Cache-Control: public` on an ownership-checked response leaks it between users | `Controllers/UploadsController.cs:126` | yes |
| BUG-1 | D2 | 🔴 | The decode-bomb guard checks each axis, so it misses total pixels and frame count | `Services/ImageProcessor.cs:47` | yes |
| TEST-1 | D3 | 🔴 | The guest-401 self-heal branch of the interceptor has no test | `UI/…/error.interceptor.ts:27` | yes |
| BUG-2 | D4 | 🟠 | The cleanup job never deletes `ThumbnailPath`, so thumbnails pile up forever | `BackgroundJobs/UploadCleanupJob.cs:90` | yes |
| BUG-3 | D5 | 🟠 | The cache-fill write is neither repeatable nor atomic, so it orphans thumbnails | `Services/UploadService.cs:145` | yes |
| REQ-1 | D7 | 🟠 | Story 003's memory-allocator cap was dropped with no equivalent | `Program.cs` | yes |
| FE-1 | D11 | 🟠 | Two overlapping `ensureGuestSession` calls mint two guest sessions | `UI/…/format-selector-page.ts:184` | no |
| FE-2 | D12 | 🟠 | The self-heal is not seamless: a stale token fails the first upload with no retry | `UI/…/format-selector-page.ts:168` | no |
| OBS-1 | D15 | 🟠 | Batch-upload rejections are swallowed with no logging | `Controllers/UploadsController.cs:98` | no |
| TEST-2 | D25 | 🟠 | The real image processor, and so the bomb guard, is mocked in every test | `Services/ImageProcessor.cs` | yes |
| TEST-3 | D26 | 🟠 | Cache persistence is unproven because one database context is shared across both calls | `Tests/…/UploadServiceTests.cs` | no |
| BUG-4 | D6 | 🟡 | An unreadable image at preview time is unmapped and returns 500 instead of 422 | `Services/ImageProcessor.cs:46` | no |
| REQ-2 | D8 | 🟡 | The thumbnail is saved at a random path, not the spec's id-keyed path | `Services/UploadService.cs:145` | no |
| REQ-3 | D9 | 🟡 | Story 002's soft-delete case contradicts the implemented and tested 404 | `Services/UploadService.cs:128` | no |
| REQ-4 | D10 | 🟡 | The bundled guest-auth and dev-warning changes have no story, criterion or test | `Program.cs` | yes |
| FE-3 | D13 | 🟡 | A visitor with no guest token is logged out to a login page they cannot use | `UI/…/error.interceptor.ts:27` | no |
| FE-4 | D14 | 🟡 | `restoreFromSession` wipes the restored grid on an expired-token 401 | `UI/…/format-selector-page.ts:347` | no |
| OBS-2 | D16 | 🟡 | The client-cancellation log sits at Debug, under the Information floor, so it never emits | `Middleware/ExceptionHandlerMiddleware.cs:54` | no |
| OBS-3 | D17 | 🟡 | A pixel-bomb 422 reads in the logs exactly like an ordinary unreadable image | `Services/ImageProcessor.cs:48` | no |
| QUAL-1 | D18 | 🟡 | `AsNoTracking` was dropped, so every cache hit change-tracks for nothing | `Services/UploadService.cs:127` | no |
| QUAL-2 | D19 | 🟡 | The miss branch throws away the generated thumbnail and re-reads it from storage | `Services/UploadService.cs:143` | no |
| DB-1 | D23 | 🟡 | The migration's Postgres arm and the model snapshot are exercised by no test | `Migrations/20260527102718_AddUploadThumbnailPath.cs:13` | no |
| INPUT-1 | D24 | 🟡 | The HEIC magic-byte check accepts any ISO-BMFF container, including video | `Services/MimeValidator.cs:33` | no |
| TEST-4 | D27 | 🟡 | Cache-Control, 304, the migration and the cache-miss race have no tests | `Tests/…` | no |
| CLOUD-1 | D28 | 🟡 | The storage contract assumes a rewindable stream with a readable length | `Controllers/UploadsController.cs:128` | no |
| QUAL-3 | D20 | ⚪ | The dimension check and its message are duplicated across two layers | `Services/UploadService.cs:83` | no |
| QUAL-4 | D21 | ⚪ | The 30-day cache lifetime is the inline number `2592000` | `Controllers/UploadsController.cs:126` | no |
| QUAL-5 | D22 | ⚪ | Split-query configuration is written out separately in both database branches | `Program.cs:33` | no |

## Refuted

| Suspicion | Why it is not real |
|---|---|
| The identify call can return nothing, so the size guard is skipped and the full decode runs | A verifier exercised the shipped image library: the call returns a value or throws. The null branch is unreachable and the check is harmless. The thrown type being unmapped is real and is kept as D6. |
| Resolving the owner id can throw when both owner columns are empty | The ownership check throws first whenever both are empty, because an empty column cannot equal a non-empty caller id. The state is unreachable regardless of what the test database enforces. |

## Notes for the fixer

- Fix D1, D2 and D3 before merge. D4 and D5 are strongly recommended in the same change: a
  deterministic key plus a cleanup that deletes the thumbnail closes the unbounded leak in one stroke
  and also satisfies D8.
- D2, D7 and D20 are one job. Put the total-pixel cap in one helper used at both decode sites, add the
  one-frame limit, and add the allocator cap as a second line of defence.
- D5 warns against adding a concurrency token: without a reload-and-retry handler it turns today's
  quiet leak into an uncaught failure and a 500. The deterministic key makes the race harmless instead.
- D25, D26 and D27 are the coverage half of the same work. The suite is green at 490 and proves very
  little: the real image processor is mocked everywhere, the whole guest-auth change has no test, and
  no test applies the migration.
- D10 is a process decision before it is a fix. Either split the bundled guest-auth and startup changes
  into their own bolts, or document them with criteria and tests before this merges under this label.
- D23's Postgres arm needs a real migration run. Fix the column type in place now, and leave the
  database test to the three-environment phase.
- D28 cannot be triggered today, because every storage implementation is local. It is recorded as a
  design constraint for the cloud-storage bolt, not as work for this round.
- Independent agreement, as an unbiased signal: five lenses reached D2 on their own and every check
  that tried to build the exploit succeeded. D4 and D5 were each reached by five lenses, D1 by three.
- This is one discovery pass, so it cannot certify the feature clean. Even after these fixes, closing
  the feature wants a later blinded pass that comes back quiet.
