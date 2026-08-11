---
type: resolution
target: 042-thumbnail-cache
version: 6
answers: review-v6.md
status: resolved
fixed_commit: 79c2eda
closed: 2026-07-14
---

# Resolution v6 — 042-thumbnail-cache

## Findings

| ID | Status | Commit | Note |
|---|---|---|---|
| PPW-112 | fixed | `548663f` | The limiter's default is now the smaller of the core count and available memory divided by a 512 MB allowance per decode, which bounds the summed in-flight decode memory to the host. Three unit tests. |
| PPW-99 | fixed | `069f5ea` | Clearing the guest token now drops only the token field and keeps the checkout contact details under the same stored key, removing the whole entry only when nothing but the token remains. Two tests. |
| PPW-114 | fixed | `39b0098` | Guest status is captured before the request on both the upload and preview paths, and the self-heal runs only for a real guest, so an expired signed-in session no longer mints a guest. Two tests, red on revert. |
| PPW-85 | deferred | — | The same class as the residual accepted last round. Deferred to the cloud-storage orphan sweep. The durable conditional update cannot run on the in-memory test provider. See Decisions. |
| PPW-113 | fixed | `6b7ce09` | The middleware emits the reserved bomb event for the allocator's memory exception, tagged with the guard that caught it, alongside the pixel-guard branch. The test asserts the event and the tag. |
| PPW-115 | fixed | `6e577fd` | The HEIC removal is recorded in the bolt as a bundled change with a retroactive criterion, mirroring the other two. Document only. |
| PPW-116 | fixed | `79c2eda` | The test walkthrough now states the shipped private 30-day directive rather than its opposite, plus the assertion that pins it, and its test counts were reconciled with the real growth. Document only. |
| PPW-79 | deferred | — | The stream's length is read with no check that the stream can rewind, and no such stream exists today. Latent until the cloud provider lands. The deferral stands. |
| PPW-117 | deferred | — | The existence check has no production caller; it is a seam for the cloud provider. Documenting it or dropping it belongs with that work. |
| PPW-122 | deferred | — | A failed thumbnail delete still soft-deletes the row, which is the same orphan family as PPW-85 and PPW-82. Deferred with them. |
| PPW-130 | deferred | — | The broad catch collapses storage faults, input-output errors and cancellation into one unreadable-image answer. A new low; deferred to the next pass. |
| PPW-128 | deferred | — | The pixel-area cap is blind to bytes per pixel, so legitimate large 16-bit images are refused. An input-validation refinement with no data loss. Deferred to the next pass. |
| PPW-126 | deferred | — | The move onto the shared key can fail on Windows. Production runs Linux, where the rename is atomic. Deferred to the next pass. |
| PPW-127 | deferred | — | A cache-hit read holds the file open and the cleanup delete then fails on Windows. Production runs Linux, which unlinks regardless. Deferred to the next pass. |
| PPW-119 | deferred | — | Limiter saturation and queue depth are unobservable. An observability follow-up; deferred to the next pass. |
| PPW-123 | deferred | — | Staggered parallel preview 401s can churn sessions. The grid's outcome is unchanged and the cost is waste. Deferred to the next pass. |
| PPW-118 | deferred | — | The extra round-trip the PPW-85 re-read costs disappears only with the conditional update deferred alongside it. Paired with PPW-85. |
| PPW-124 | fixed | `39b0098` | The coverage gap is closed by the PPW-114 regression tests, which assert that a signed-in 401 on the upload and preview paths mints no guest session and fires no retry. |
| PPW-125 | deferred | — | The guest-init error path when files are dropped is untested. A separate coverage gap, outside this round's recommended set. Deferred to the next pass. |
| PPW-131 | deferred | — | The implementation plan still lists the public directive and the per-axis cap. Same drift family as PPW-116 but a different file, and outside the recommended set. Deferred. |
| PPW-120 | deferred | — | No test pins the decode slot's release on a throwing decode. Latent, since today's code releases it. Deferred to the next pass. |
| PPW-121 | deferred | — | The exact-type mapping to 422 is proven only by an injected instance. Latent, since the shipped library throws that concrete type. Deferred to the next pass. |
| PPW-129 | deferred | — | The fail-open branch when the identify call returns null is dead today, because the shipped library throws instead of returning null. Deferred to the next pass. |
| PPW-74 | deferred | — | Raised twice this pass: the Postgres arm of the migration is unexercised, and the model snapshot carries the SQLite type. The standing three-environment deferral. |
| PPW-132 | deferred | — | The bomb-alert template is duplicated across the controller and the middleware. The PPW-113 fix added a third site, which makes extracting it worth marginally more. Cleanup, deferred. |
| PPW-133 | deferred | — | `dropRestoredEntry` duplicates `onRemoveUpload`. Cleanup, deferred to the next pass. |
| PPW-134 | deferred | — | The client-abort branch reads the raw correlation-id item instead of the accessor. Trivial cleanup in the file PPW-113 touched; left deferred to keep this round's scope. See Decisions. |
| PPW-135 | deferred | — | Storage save and delete traces sit at Debug under an Information floor, so they never emit. Cleanup, deferred to the next pass. |

## Scope

| Cluster | Findings | Files | Approach-check |
|---|---|---|---|
| A — Memory-aware limiter default (`548663f`) | PPW-112 | `ImageDecodeLimiter.cs`, `Program.cs` | not needed (one default computed from host memory) |
| B — Guest session reach (`069f5ea`, `39b0098`) | PPW-99, PPW-114, PPW-124 | `UI/…/guest-auth.service.ts`, `UI/…/format-selector-page.ts` | not needed (one field-level write and one captured flag) |
| C — Bomb event on the backstop branch (`6b7ce09`) | PPW-113 | `Middleware/ExceptionHandlerMiddleware.cs` | not needed (one emit site beside an existing one) |
| D — Documents (`6e577fd`, `79c2eda`) | PPW-115, PPW-116 | `memory-bank/…/bolt.md`, `memory-bank/…/test-walkthrough.md` | not needed (documents only) |
| E — Left undone this round | PPW-85, PPW-79, PPW-117, PPW-118, PPW-119, PPW-120, PPW-121, PPW-122, PPW-123, PPW-125, PPW-126, PPW-127, PPW-128, PPW-129, PPW-130, PPW-131, PPW-132, PPW-133, PPW-134, PPW-135, PPW-74 | — | not needed (no code changed) |

## Decisions

### The orphan race stays deferred to the cloud-storage sweep (PPW-85, PPW-118)

The durable fix is a conditional update that sets the path only while the row is live, deleting the
just-written file on no match. The in-memory provider the integration tests run on cannot execute one. That is
exactly why the previous round used a liveness re-read. Landing it now would ship code this suite
cannot test. It belongs with the cloud-storage orphan sweep, where a real provider is in play.
PPW-118, the extra round-trip that re-read costs, disappears only with the same change and is folded into
the same deferral.

### One finding was reclassified as fixed by another fix

PPW-124 asked for a test proving a signed-in 401 during upload mints no guest session. The PPW-114 regression
tests add exactly that, on both the upload and the restored-preview paths, so it is recorded as fixed
against that commit rather than left open.

### The contact-details fix has a residual outside its own scenario

The fix stops the interceptor wiping contact details. It does not change what the upload page does when it
re-inits a session. That path still writes the stored entry with empty contact fields. An upload-page
re-init would therefore overwrite what was preserved. That is outside this finding's scenario, since
contact details are entered at checkout rather than on the upload page, and it predates the fix. It is
recorded here for the re-reviewer rather than fixed quietly.

### The new emit site raises the value of a deferred cleanup (PPW-113, PPW-132)

The PPW-113 fix adds a third place emitting the reserved bomb event: the batch route in the controller, and
both the pixel guard and the allocator backstop in the middleware. That makes extracting the event name
to one constant worth more than when PPW-132 was raised. Recorded so the next pass weighs it accordingly.

### A one-line cleanup in a touched file was still deferred

The PPW-113 fix edited the middleware, and PPW-134 is a one-line change in the same file. It was left deferred
to keep this round's change set to the recommended scope, and it batches trivially into a later cleanup
sweep.

### The long tail is deferred deliberately, not missed

The review judged that the search was not complete and asked for another blinded pass. The remaining low and
cleanup findings — development-only races, latent test gaps, observability polish and duplication — are
the tail that pass will re-weigh. None is a data-loss or a runtime break in production.
