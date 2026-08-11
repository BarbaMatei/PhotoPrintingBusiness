---
type: review
target: 042-thumbnail-cache
version: 8
supersedes: 6
commit: e2093bd
branch: feat/bolt-042-thumbnail-cache
pass-type: discovery
date: 2026-07-14
lenses: [correctness, security, requirements, quality, tests-coverage, db-parity, input-validation, observability, race, frontend-ux, completeness-critic]
lenses-not-run: []
verdict: approve-with-followups
blockers: []
findings: { high: 0, medium: 7, low: 17, cleanup: 4, refuted: 3 }
tests: { dotnet: "535/535", frontend: "413/413" }
---

# Review v8 — 042-thumbnail-cache

## Findings

| F# | D# | Sev | Title | File | Fix now? |
|---|---|---|---|---|---|
| F1 | D34 | 🟠 | The cache-fill write races the cleanup job and strands a thumbnail on the dead row | `BackgroundJobs/UploadCleanupJob.cs:101` | no |
| F2 | D85 | 🟠 | The global split-query default mis-pages a collection include that has no tiebreaker | `Services/AdminOrderService.cs:67` | yes |
| F3 | D86 | 🟠 | `storeSession` overwrites the contact details the earlier fix preserved | `UI/…/format-selector-page.ts:205` | yes |
| F4 | D87 | 🟠 | The bomb test asserts the base exception, so the alert can regress while tests stay green | `Tests/…/UploadServiceTests.cs:480` | yes |
| F5 | D88 | 🟠 | A lost original blob is logged as a plain 404, with no distinct signal | `Services/UploadService.cs:183` | yes |
| F6 | D89 | 🟠 | The soft-delete-race deletion leaves database and file state silently out of step | `Services/UploadService.cs:219` | yes |
| F7 | D77 | 🟠 | The pixel-area cap ignores bytes per pixel, so a legitimate 16-bit PNG is refused | `Services/ImageProcessor.cs:23` | yes |
| F8 | D90 | 🟡 | The 30-day private preview cache is recoverable on a shared device | `Controllers/UploadsController.cs:26` | no |
| F9 | D66 | 🟡 | `ExistsAsync` was added to the storage interface but nothing in production calls it | `IStorageService.cs:21` | no |
| F10 | D75 | 🟡 | Moving a file onto a shared key races other writers on Windows and returns 500 | `Services/LocalStorageService.cs:45` | no |
| F11 | D67 | 🟡 | Every cache-miss preview pays an extra database round-trip to spot the soft-delete race | `Services/UploadService.cs:216` | no |
| F12 | D69 | 🟡 | No test proves the decode slot is released when the decode throws | `Services/ImageProcessor.cs:67` | no |
| F13 | D93 | 🟡 | No end-to-end test reaches the bomb-to-422 path because the integration fake pins 800×600 | `Tests/…/UploadFactory.cs:239` | no |
| F14 | D23 | 🟡 | The migration's Postgres arm and the model snapshot are exercised by no test | `Migrations/20260527102718_AddUploadThumbnailPath.cs:19` | no |
| F15 | D68 | 🟡 | Nothing reports how saturated or how queued the decode limiter is | `ImageDecodeLimiter.cs:30` | no |
| F16 | D42 | 🟡 | The one-frame decode cap is proven only through the internal helper, not the public call | `Services/ImageProcessor.cs:81` | no |
| F17 | D50 | 🟡 | Guest-session recovery after a failed init is untested | `UI/…/format-selector-page.ts:214` | no |
| F18 | D31 | 🟡 | Nothing reclaims a thumbnail written between the cleanup job's read and its commit | `Services/UploadService.cs:187` | no |
| F19 | D94 | 🟡 | A guest 401 away from the upload page is a silent dead end | `UI/…/error.interceptor.ts:33` | no |
| F20 | D95 | 🟡 | `localUrl()` mints an untracked object URL on every change-detection cycle | `UI/…/photo-thumbnail.component.ts:86` | no |
| F21 | D92 | 🟡 | A restore preview that resolves after the page is destroyed leaks an object URL | `UI/…/format-selector-page.ts:404` | no |
| F22 | D96 | 🟡 | The decode memory budget ignores the upload buffering that shares the same memory | `ImageDecodeLimiter.cs:30` | no |
| F23 | D91 | 🟡 | Bundled change C has no criterion or test and is labelled as changing no behaviour | `memory-bank/…/bolt.md:73` | yes |
| F24 | D28 | 🟡 | The storage contract assumes a rewindable stream with a readable length | `Controllers/UploadsController.cs:155` | no |
| F25 | D81 | ⚪ | The bomb-alert log template is copied across the controller and the middleware | `Controllers/UploadsController.cs:122` | no |
| F26 | D66 | ⚪ | `ExistsAsync` was added to the storage interface but nothing in production calls it | `Tests/…/UploadServiceTests.cs:296` | no |
| F27 | D59 | ⚪ | Story 001 names `varchar(500)` and `StoragePath`; the column shipped as `varchar(512)` and `FilePath` | `memory-bank/…/implementation-plan.md:17` | yes |
| F28 | D97 | ⚪ | The conditional GET matches only an exact strong tag, so weak, list and `*` fall back to 200 | `Controllers/UploadsController.cs:158` | no |

## Refuted

| Suspicion | Why it is not real |
|---|---|
| The pixel guard is skipped when the identify call returns null, so an oversized image decodes in full (a re-raise of D78) | The shipped image library returns a value or throws, so the value is never null at that line and the skipping branch is dead. The allocator backstop and the one-frame cap fire regardless. It becomes live only on a library upgrade that changes this. |
| A legitimate image close to the size limit trips the allocator backstop and is logged as a bomb | The allocator cap is per single allocation, not a running total. A legal image at the pixel cap is one buffer under the cap, and resizing allocates only small separate buffers, so the backstop cannot fire on it. |
| The reclaim of an orphaned thumbnail is swallowed, so operators never learn about it | The same suspicion the previous discovery pass refuted. The warning naming the orphaned key is emitted before the best-effort delete, so the orphan is signalled whatever the delete does. |

## Notes for the fixer

- Fix F2, F3, F4, plus the cheap signals F5 and F6, plus the document fix F23, plus a bounded F7. Defer
  F1 and F18 to the cloud-storage orphan sweep, F14 to the three-environment phase, and F24 to the
  cloud-storage bolt. Leave the rest of the tail to the next blinded pass.
- F3 is the one to read first. It defeats the fix made for D48 two rounds ago and verified one round
  ago: one writer preserves the contact details, the next writer overwrites them. Fix the class, not the
  one call site — find every writer of that stored entry.
- F2 is a new surface no earlier pass audited: the bundled split-query default changes how every paged
  query with a collection include executes. Sweep for every paged query, not just the one named.
- F7 is bounded on purpose. Pinning the decode's bytes per pixel keeps the pixel cap and the large-format
  use case; raising the cap instead would move the memory budget the limiter depends on.
- F1, F11 and F18 are one deferral. They all disappear with the conditional atomic write plus a cleanup
  that deletes the derivable key, and the in-memory test provider still cannot run it.
- F8 is an owner decision, not a patch: it trades privacy on a shared device against the 30-day cache
  that the D1 fix deliberately added.
- The new-finding count is decaying at last: 32, then 24, then 13 genuinely new here. The other 15 are
  re-raises of items already catalogued as open or deferred.
- It is still not quiet. Five new mediums, two of them made by this feature's own earlier fixes, and
  one of those defeats a fix verified one pass ago. Closing the feature still wants a blinded pass that comes back quiet.
- Method note: this pass ran on a different model from the earlier ones. Three launch attempts on the
  usual model died on its session limit before any lens finished and were discarded; the run recorded
  here is one clean run of 53 agents with no errors.
