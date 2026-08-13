---
type: resolution
target: 042-thumbnail-cache
version: 4
answers: review-v4.md
status: resolved
fixed_commit: 6c4f334
closed: 2026-07-14
---

# Resolution v4 — 042-thumbnail-cache

## Findings

| ID | Status | Commit | Note |
|---|---|---|---|
| PPW-85 | fixed | `4d4d998` | After the path is persisted, the row's liveness is read again; if it was soft-deleted underneath, the just-written thumbnail is deleted. A conditional update was not used; see Decisions. Race test reddens on revert. |
| PPW-86 | fixed | `aad083d` | Saving writes to a unique temporary file and then moves it into place, so two writers of the same key no longer collide. A gated concurrency test reddens on revert. |
| PPW-84 | fixed | `aa6639c` | A process-wide decode limiter gates thumbnail generation, with a configurable slot count defaulting to the processor count. Limiter unit tests plus a gate-ordering test, red on revert. |
| PPW-87 | fixed | `f1c4ade` | The batch catch emits the reserved bomb event with the dimensions when the rejection is a bomb, matching the middleware. The test asserts the name and both dimensions, and also covers PPW-102 for this route. |
| PPW-88 | fixed | `80379f6` | HEIC is no longer accepted, since no decoder exists. The validator no longer classifies the container as an image, and the interface dropped the extension, the accept list and the copy at `63b815a`. |
| PPW-89 | fixed | `fea0d45` | A missing original on the cache-miss path is caught and raised as a 404, since the original is unrecoverable, which also lets the interface drop the dead entry. Regression test, red on revert. |
| PPW-90 | fixed | `2b22e25` | The generate catch logs a warning naming the storage path and carries the caught error inward, mirroring the sibling method. The test asserts both. |
| PPW-91 | fixed | `1bdb21b` | A restored entry is dropped on a 403 and on a still-failing 401 after re-init, as it already was on a 404. Only server errors and network failures keep it. Two tests, red on revert. |
| PPW-74 | deferred | `2945bda` | A SQLite smoke test now applies the real migration chain and asserts the column lands. The Postgres arm and the model snapshot stay deferred to the three-environment phase. See Decisions. |
| PPW-92 | fixed | `7a7170e` | The dimensions test now asserts the stored file is deleted exactly once. Proven by removing the delete and watching it redden. |
| PPW-93 | fixed | `1108d47` | The decode moved into an internal helper carrying the one-frame limit, which production uses. A reflection test asserts a three-frame image decodes to one frame; dropping the limit reddens it. |
| PPW-94 | fixed | `dfb8f56` | The cache-hit path reads directly and catches a missing file to regenerate, so there is no failure window and no second storage call. Test proves it regenerates rather than failing. |
| PPW-95 | fixed | `dfb8f56` | A distinct event is emitted when a recorded thumbnail is absent, folded into the same catch as PPW-94. Signal test, red on revert. |
| PPW-96 | fixed | `9b0bc81` | The cache-fill save is wrapped: on failure it emits a distinct event, deletes the just-written thumbnail on a best-effort basis, then re-raises. Test through a throwing context, red on revert. |
| PPW-97 | deferred | `8466658` | The constraint is documented at the write site, which is the finding's own minimum bar. Moving the cache fill off the read path waits until read-replica routing exists. See Decisions. |
| PPW-98 | fixed | `158b733` | The batch-reject log strips control characters and caps the name at 128 characters. Test with a newline and a 200-character name, red on revert. |
| PPW-99 | disputed | — | As written this asks to undo PPW-64, which was fixed and verified, and a passing test asserts the behaviour it wants reverted. Surfaced for the owner rather than implemented. See Decisions. |
| PPW-100 | fixed | `1bdb21b` | A persistent-401 upload test pins exactly two attempts then an error, which is what the one-shot guard gives. A regression would show as an endless loop rather than a clean red. |
| PPW-101 | fixed | `1bdb21b` | A re-init-after-settle test drives a completing init and then a null token, and asserts the init runs twice. Proven by neutralising the reset and watching it redden. |
| PPW-79 | deferred | — | Re-raises the first pass's cloud storage constraint: the entity tag reads the stream's length. Not triggerable until the cloud provider lands. The deferral stands. |
| PPW-102 | fixed | `c0c07c7` | The bomb log test uses distinct width and height and asserts both render. Proven by dropping them from the log and watching it redden. |
| PPW-103 | fixed | `e1c56c4` | The allocator's memory exception is mapped to 422 in the middleware; the test asserts the 422 and reddens when the mapping is removed. |
| PPW-104 | fixed | `ec8a894` | A corrupt-but-recognised PNG test reaches the broken-content branch. Proven by narrowing the catch to the unknown-format type and watching it redden. |
| PPW-105 | fixed | `af5cf74` | Preview object URLs are released on remove, drop, cart clear and page destroy. Test: removing an upload releases its URL. |
| PPW-106 | fixed | `af5cf74` | The upload error message was extracted into one field. |
| PPW-107 | fixed | `f444a81` | A real-seam test with the real authentication service: a guest 401 clears the same stored key the token reader reads, which covers the divergence without wiring the whole component. |
| PPW-108 | fixed | `6c4f334` | The walkthrough was refreshed to the shipped private directive, the tracking behaviour and the real migration, plus adjacent drift in the same document. See Decisions. |
| PPW-109 | fixed | `6c4f334` | Story 003's criterion now says 110 MP over the 100 MP cap, and its thumbnail size now says 800 px. |
| PPW-110 | fixed | `6c4f334` | Story 001's criterion now says `varchar(512)` and describes the column as the same shape as `FilePath`. |
| PPW-111 | fixed | `28aff33` | The owner chose to change the code: the thumbnail's longest side went from 300 px to 800 px, which the stories already said. Test: a 2000×1500 source scales to over 300 and at most 800. Red on revert. |

## Scope

| Cluster | Findings | Files | Approach-check |
|---|---|---|---|
| A — Safe cache-fill write (`4d4d998`, `aad083d`, `fea0d45`, `dfb8f56`, `9b0bc81`) | PPW-85, PPW-86, PPW-89, PPW-94, PPW-95, PPW-96 | `Services/UploadService.cs`, `Services/LocalStorageService.cs`, `Services/ImageProcessor.cs` | not needed (guards and a temporary-file write on existing paths) |
| B — Decode concurrency limiter (`aa6639c`) | PPW-84 | `Services/ImageProcessor.cs`, `ImageDecodeLimiter.cs`, `Program.cs` | not needed (one gate around one call) |
| C — Bomb signals and their tests (`f1c4ade`, `c0c07c7`, `e1c56c4`, `ec8a894`, `7a7170e`, `1108d47`) | PPW-87, PPW-92, PPW-93, PPW-102, PPW-103, PPW-104 | `Controllers/UploadsController.cs`, `Middleware/ExceptionHandlerMiddleware.cs`, `Tests/…` | not needed (one emit site plus tests) |
| D — Upload contract and logging (`80379f6`, `63b815a`, `2b22e25`, `158b733`) | PPW-88, PPW-90, PPW-98 | `Services/MimeValidator.cs`, `Services/ImageProcessor.cs`, `Controllers/UploadsController.cs`, `UI/…` | not needed (removing an advertised format and two log changes) |
| E — Guest session and previews (`1bdb21b`, `af5cf74`, `f444a81`) | PPW-91, PPW-100, PPW-101, PPW-105, PPW-106, PPW-107 | `UI/…/format-selector-page.ts` | not needed (drop conditions, URL release and tests) |
| F — Documents and criteria (`6c4f334`, `28aff33`) | PPW-108, PPW-109, PPW-110, PPW-111 | `memory-bank/…`, `Services/ImageProcessor.cs` | not needed (documents plus one constant) |
| G — Migration coverage (`2945bda`) | PPW-74 | `Tests/…` | not needed (tests only) |
| H — Left undone this round | PPW-97, PPW-99, PPW-79 | `Services/UploadService.cs` | not needed (a constraint note and two rulings) |

## Decisions

### The liveness re-read was used instead of a conditional update

The durable fix is a conditional update that sets the path only while the row is live, deleting the
just-written file when it matches nothing. The in-memory provider the integration tests run on cannot
execute one, so landing it now would ship code this suite cannot test. The re-read closes the ordering
the finding stated; the narrower symmetric window stays open behind PPW-82's sweep.

### The read-replica hazard was documented, not designed away

The preview writes on a cache miss. There is no read replica today — development uses SQLite and
production a single Postgres — so the hazard cannot fire. The finding's own minimum option was taken:
a constraint note at the write site. Moving the cache fill off the read path waits until read-replica
routing actually exists, rather than being built ahead of need.

### The guest self-heal finding conflicts with a verified decision

PPW-99 asks to restore the login redirect for unauthenticated 401s. PPW-64 deliberately removed it, because
this is a guest-first application where a guest has no account to log into, and a passing test asserts
an anonymous 401 does not navigate to a login page. A fixer must not revert a verified decision, so
this is surfaced for the owner and the re-reviewer instead. The residual breadth it names is harmless
today: clearing an absent token does nothing, and a token cleared by a stray 401 self-heals on the next
request. Scoping the clear to upload and preview requests would be a small follow-up that leaves PPW-64's
behaviour intact.

### The walkthrough fix went past the three contradictions named

The finding named the cache directive, the tracking behaviour and the migration. Fixing them exposed
three more errors in the same document: the thumbnail path scheme, the decode limit's name and units,
and the thumbnail size. A half-corrected walkthrough misleads exactly as much as an uncorrected one, so
all six were refreshed in the same commit, recorded here rather than done quietly.

### One finding was refuted rather than fixed

The suspicion that the change to accepted file types shipped untraceably is not real: it traces to the
commit that fixed PPW-75, and the scope document does cover it. The residual stale document text is
carried by PPW-108. It is recorded in review-v4's refuted table and gets no defect id.

### Fixes making new defects are the theme of this round

PPW-85, PPW-86 and PPW-89 exist because the first round made the thumbnail key deterministic. Anything changed
here needs its own concurrency self-review, and a finding is not fixed without the regression test the
review named — for PPW-74, PPW-92, PPW-93 and PPW-102 to PPW-104, the test is the fix.
