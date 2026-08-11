---
type: review
target: 042-thumbnail-cache
version: 6
supersedes: 5
commit: 6c0ed93
branch: feat/bolt-042-thumbnail-cache
pass-type: discovery
date: 2026-07-14
lenses: [correctness, security, requirements, quality, tests-coverage, db-parity, input-validation, observability, race, frontend-ux, completeness-critic]
lenses-not-run: []
verdict: approve-with-followups
blockers: []
findings: { high: 0, medium: 8, low: 17, cleanup: 4, refuted: 1 }
tests: { dotnet: "531/531", frontend: "409/409" }
---

# Review v6 — 042-thumbnail-cache

## Findings

| F# | D# | Sev | Title | File | Fix now? |
|---|---|---|---|---|---|
| F1 | D61 | 🟠 | The decode limiter defaults to the core count and ignores memory, so it still exhausts memory | `Program.cs:359` | yes |
| F2 | D48 | 🟠 | Any unauthenticated 401 wipes the whole guest session, checkout contact details included | `UI/…/error.interceptor.ts:33` | yes |
| F3 | D63 | 🟠 | A logged-in user whose token expired is re-attributed to a throwaway guest | `UI/…/format-selector-page.ts:232` | yes |
| F4 | D34 | 🟠 | The cache-fill write races the cleanup job and strands a thumbnail on the dead row | `Services/UploadService.cs:216` | no |
| F5 | D62 | 🟠 | A bomb caught by the allocator backstop never emits the reserved bomb event | `Middleware/ExceptionHandlerMiddleware.cs:106` | yes |
| F6 | D64 | 🟠 | The HEIC removal is missing from the bolt's bundled-scope document | `memory-bank/…/bolt.md:57` | yes |
| F7 | D65 | 🟠 | The test walkthrough certifies a `Cache-Control` value the code never emits | `memory-bank/…/test-walkthrough.md:28` | yes |
| F8 | D28 | 🟠 | The storage contract assumes a rewindable stream with a readable length | `Controllers/UploadsController.cs:155` | no |
| F9 | D66 | 🟡 | `ExistsAsync` was added to the storage interface but nothing in production calls it | `IStorageService.cs:21` | no |
| F10 | D71 | 🟡 | A failed thumbnail delete in the cleanup job is untested and silently leaks the file again | `BackgroundJobs/UploadCleanupJob.cs:114` | no |
| F11 | D79 | 🟡 | Storage faults and cancellation are reported as an unreadable image | `Services/ImageProcessor.cs:56` | no |
| F12 | D77 | 🟡 | The pixel-area cap ignores bytes per pixel, so a legitimate 16-bit PNG is refused | `Services/ImageProcessor.cs:23` | no |
| F13 | D75 | 🟡 | Moving a file onto a shared key races other writers on Windows and returns 500 | `Services/LocalStorageService.cs:45` | no |
| F14 | D76 | 🟡 | A cleanup delete fails against an open read handle on Windows and leaves an orphan | `Services/LocalStorageService.cs` | no |
| F15 | D68 | 🟡 | Nothing reports how saturated or how queued the decode limiter is | `ImageDecodeLimiter.cs:27` | no |
| F16 | D72 | 🟡 | Parallel preview 401s defeat the init sharing, and a late 401 wipes a fresh token | `UI/…/format-selector-page.ts:381` | no |
| F17 | D67 | 🟡 | Every cache-miss preview pays an extra database round-trip to spot the soft-delete race | `Services/UploadService.cs:216` | no |
| F18 | D73 | 🟡 | The logged-in 401-during-upload path has no test | `UI/…/error.interceptor.ts:24` | no |
| F19 | D74 | 🟡 | The guest-init error path in `onFilesAccepted` is untested, so files hang as uploading | `UI/…/format-selector-page.ts:176` | no |
| F20 | D80 | 🟡 | The implementation plan's acceptance criteria are stale after documented substitutions | `memory-bank/…/implementation-plan.md:59` | no |
| F21 | D69 | 🟡 | No test proves the decode slot is released when the decode throws | `Services/ImageProcessor.cs:67` | no |
| F22 | D70 | 🟡 | The allocator-exception-to-422 mapping is proven only by an injected instance | `Middleware/ExceptionHandlerMiddleware.cs:26` | no |
| F23 | D78 | 🟡 | The pixel guard is skipped when the identify call returns null | `Services/ImageProcessor.cs:77` | no |
| F24 | D23 | 🟡 | The migration's Postgres arm and the model snapshot are exercised by no test | `Migrations/20260527102718_AddUploadThumbnailPath.cs:19` | no |
| F25 | D23 | 🟡 | The migration's Postgres arm and the model snapshot are exercised by no test | `Migrations/PhotoPrintDbContextModelSnapshot.cs:707` | no |
| F26 | D81 | ⚪ | The bomb-alert log template is copied across the controller and the middleware | `Controllers/UploadsController.cs:130` | no |
| F27 | D82 | ⚪ | `dropRestoredEntry` repeats the body of `onRemoveUpload` word for word | `UI/…/format-selector-page.ts:420` | no |
| F28 | D83 | ⚪ | The client-abort log reads the raw correlation-id item instead of the accessor | `Middleware/ExceptionHandlerMiddleware.cs:64` | no |
| F29 | D84 | ⚪ | Storage save and delete traces sit at Debug under an Information floor, so they never emit | `Services/LocalStorageService.cs:53` | no |

## Refuted

| Suspicion | Why it is not real |
|---|---|
| The reclaim of an orphaned thumbnail is swallowed, so operators never learn about it | The warning that names the orphaned key is emitted before the best-effort delete, so the orphan is signalled whatever the delete does. The empty catch only drops a second, redundant log line. |

## Notes for the fixer

- Fix F1, F2, F3 and F5, plus the two cheap document fixes F6 and F7. Defer F4 and F8 to the
  cloud-storage bolt. Leave the long tail from F9 to F29 for the next blinded pass.
- The pattern repeats from the last discovery pass: the most interesting findings are residuals of the
  last round's fixes. F1 is the limiter that fixed the memory exhaustion, F13 is the move that fixed the
  concurrent-write failure, F5 is the mapping that fixed the allocator backstop.
- F2 and F3 are one cluster. Both are the guest self-heal reaching further than it should: F2 across
  storage keys, F3 across account types. Fix them together and test both paths.
- F4's durable fix is a conditional update that writes the path only while the row is live. The
  in-memory test provider cannot run one, which is why the last round used a re-read instead. F17
  disappears with the same change, so keep the two together.
- Every fix needs a regression test that reddens when the fix is reverted. F6 and F7 are document-only.
- The fixes from the previous round held: not one of the 26 verified findings was re-found as open by an
  independent blinded lens.
- The branch is 27 commits ahead of its remote, so this commit is not reproducible until it is pushed.
- 29 findings and none High, but the new-finding count is not decaying and the new mediums are again
  caused by the last round's fixes. The search is not complete: the feature wants another blinded pass
  that comes back quiet.
