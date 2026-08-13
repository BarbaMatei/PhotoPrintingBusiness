---
type: review
target: 042-thumbnail-cache
version: 4
supersedes: 3
commit: 9e44714
branch: feat/bolt-042-thumbnail-cache
pass-type: discovery
date: 2026-07-14
lenses: [correctness, security, requirements, quality, db-parity, input-validation, observability, race, frontend-ux, tests-coverage, completeness-critic]
lenses-not-run: []
verdict: approve-with-followups
blockers: []
findings: { high: 0, medium: 11, low: 13, cleanup: 7, refuted: 1 }
tests: { dotnet: "515/515", frontend: "403/403" }
---

# Review v4 — 042-thumbnail-cache

## Findings

| ID | Sev | Title | File | Fix now? |
|---|---|---|---|---|
| M1 | PPW-85 | 🟠 | The cache-fill write races the cleanup job and strands a thumbnail on the dead row | `Services/UploadService.cs:159` | yes |
| M2 | PPW-86 | 🟠 | Two first previews at once collide on an exclusive file create and return 500 | `Services/LocalStorageService.cs:32` | yes |
| M3 | PPW-84 | 🟠 | Image decode has no total or concurrent memory bound, so many large images exhaust memory | `Services/UploadService.cs:158` | yes |
| M4 | PPW-87 | 🟠 | A bomb sent to the batch endpoint never emits the reserved alert event | `Controllers/UploadsController.cs:119` | yes |
| M5 | PPW-88 | 🟠 | HEIC is accepted but no decoder exists, so every HEIC upload fails | `Services/MimeValidator.cs:52` | yes |
| M6 | PPW-89 | 🟠 | A cache miss whose original is gone returns 500 instead of a clean 4xx | `Services/ImageProcessor.cs:63` | yes |
| M7 | PPW-90 | 🟠 | An unreadable stored image is logged without its storage path or its cause | `Services/ImageProcessor.cs:88` | yes |
| M8 | PPW-91 | 🟠 | A preview kept on 403 leaves an upload the guest can never put in a cart | `UI/…/format-selector-page.ts:400` | yes |
| M9 | PPW-74 | 🟠 | The migration's Postgres arm and the model snapshot are exercised by no test | `Migrations/20260527102718_AddUploadThumbnailPath.cs:34` | yes |
| M10 | PPW-92 | 🟠 | No test proves the bomb rejection deletes the file it already stored | `Tests/…/UploadServiceTests.cs:381` | yes |
| M11 | PPW-93 | 🟠 | The one-frame decode cap is proven only through the internal helper, not the public call | `Services/ImageProcessor.cs:68` | yes |
| L1 | PPW-94 | 🟡 | The cache-hit path checks then reads, so a vanished file gives 500 and costs a round-trip | `Services/UploadService.cs:150` | no |
| L3 | PPW-95 | 🟡 | A vanished cache file quietly regenerates with no signal | `Services/UploadService.cs:150` | no |
| L4 | PPW-96 | 🟡 | A thumbnail orphaned by a failed commit emits no signal | `Services/UploadService.cs:159` | no |
| L5 | PPW-97 | 🟡 | The preview GET writes to the database, which fails against a read replica | `Services/UploadService.cs:166` | no |
| L6 | PPW-98 | 🟡 | The batch-rejection log prints the raw client filename with no length limit | `Controllers/UploadsController.cs:120` | no |
| L7 | PPW-99 | 🟡 | Any unauthenticated 401 wipes the whole guest session, checkout contact details included | `UI/…/error.interceptor.ts:30` | no |
| L8 | PPW-100 | 🟡 | The one-shot retry guard is untested for a retry that still fails | `UI/…/format-selector-page.ts:216` | no |
| L9 | PPW-101 | 🟡 | Guest-session recovery after a failed init is untested | `UI/…/format-selector-page.ts:210` | no |
| L10 | PPW-74 | 🟡 | The migration's Postgres arm and the model snapshot are exercised by no test | `Migrations/PhotoPrintDbContextModelSnapshot.cs:707` | no |
| L11 | PPW-79 | 🟡 | The storage contract assumes a rewindable stream with a readable length | `Controllers/UploadsController.cs:144` | no |
| L12 | PPW-102 | 🟡 | The bomb log test asserts the event name but not the dimensions the event exists to carry | `Tests/…/ExceptionHandlerMiddlewareTests.cs:254` | no |
| L13 | PPW-103 | 🟡 | The 512 MB allocator backstop throws an unmapped exception, giving 500, and is untested | `Program.cs:95` | no |
| L14 | PPW-104 | 🟡 | The recognised-but-broken image path to 422 is untested | `Services/ImageProcessor.cs:81` | no |
| C1 | PPW-105 | ⚪ | Preview object URLs are never released, so every restore and retry leaks one | `UI/…/format-selector-page.ts:388` | no |
| C2 | PPW-106 | ⚪ | The upload error message is written out at three sites | `UI/…/format-selector-page.ts:220` | no |
| C3 | PPW-107 | ⚪ | The self-heal seam is tested only with each half mocked | `UI/…/format-selector-page.ts:227` | no |
| C4 | PPW-108 | ⚪ | The implementation walkthrough contradicts the shipped code | `memory-bank/…/implementation-walkthrough.md:32` | yes |
| C5 | PPW-109 | ⚪ | Story 003 says 54 MP is refused while the shipped cap is 100 MP | `memory-bank/…/003-imagesharp-max-pixels.md:27` | no |
| C6 | PPW-110 | ⚪ | Story 001 names `varchar(500)` and `StoragePath`; the column shipped as `varchar(512)` and `FilePath` | `memory-bank/…/001-thumbnail-path-schema.md:22` | no |
| C7 | PPW-111 | ⚪ | The thumbnail shipped at 300 px while the stories and the brief say 800 px | `memory-bank/…/002-persist-thumbnail-on-first-request.md:39` | no |

## Refuted

| Suspicion | Why it is not real |
|---|---|
| The change to which file types are accepted shipped with no story and is missing from the scope document | The change is traceable to the commit that fixed PPW-75, and the scope document does cover it. What remains is stale document text, which is already carried by PPW-108. |

## Notes for the fixer

- The theme of this pass is fixes that made new defects. PPW-85, PPW-86 and PPW-89 exist because PPW-56 made the
  thumbnail key deterministic. Self-review the concurrency of anything you change here.
- Order: PPW-84 first, since it is the only way to kill the process. Then PPW-85, PPW-86 and PPW-89 together — one
  change makes the deterministic-key write safe. Then PPW-87, then PPW-88.
- PPW-90, PPW-74, PPW-92, PPW-93 and the whole low and cleanup tail are fast-follows. Fix PPW-108 whatever else you
  skip: copying from that document reintroduces PPW-52.
- PPW-74 stays split. Fix what can be fixed now and leave the Postgres arm to the three-environment phase.
- PPW-79 is not triggerable until the cloud provider lands. The deferral stands as a design constraint.
- PPW-99 as written asks to undo PPW-64, which was fixed and verified: this application deliberately does not
  send an unauthenticated visitor to a login page they have no account for, and a passing test says so.
  Do not revert a verified decision. Surface it for the owner instead.
- PPW-97 has no read replica to fail against today, so document the constraint at the write site rather
  than rebuilding the read path.
- A finding is not fixed without the regression test the review named. For PPW-74, PPW-92, PPW-93 and PPW-102 to
  PPW-104, the test is the fix.
- The strongest signal this pass can give: not one of the three fixes the first pass required was
  re-found by an independent blinded lens. A verification pass cannot give that.
- Zero High findings does not mean the search is complete. This pass found 32; closing the feature
  still wants a later blinded pass that comes back quiet.
