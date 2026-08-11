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

| D# | Status | Commit | Note |
|---|---|---|---|
| D61 | fixed | `548663f` | The limiter's default is now the smaller of the core count and available memory divided by a 512 MB allowance per decode, which bounds the summed in-flight decode memory to the host. Three unit tests. |
| D48 | fixed | `069f5ea` | Clearing the guest token now drops only the token field and keeps the checkout contact details under the same stored key, removing the whole entry only when nothing but the token remains. Two tests. |
| D63 | fixed | `39b0098` | Guest status is captured before the request on both the upload and preview paths, and the self-heal runs only for a real guest, so an expired signed-in session no longer mints a guest. Two tests, red on revert. |
| D34 | deferred | — | The same class as the residual accepted last round. Deferred to the cloud-storage orphan sweep. The durable conditional update cannot run on the in-memory test provider. See Decisions. |
| D62 | fixed | `6b7ce09` | The middleware emits the reserved bomb event for the allocator's memory exception, tagged with the guard that caught it, alongside the pixel-guard branch. The test asserts the event and the tag. |
| D64 | fixed | `6e577fd` | The HEIC removal is recorded in the bolt as a bundled change with a retroactive criterion, mirroring the other two. Document only. |
| D65 | fixed | `79c2eda` | The test walkthrough now states the shipped private 30-day directive rather than its opposite, plus the assertion that pins it, and its test counts were reconciled with the real growth. Document only. |
| D28 | deferred | — | The stream's length is read with no check that the stream can rewind, and no such stream exists today. Latent until the cloud provider lands. The deferral stands. |
| D66 | deferred | — | The existence check has no production caller; it is a seam for the cloud provider. Documenting it or dropping it belongs with that work. |
| D71 | deferred | — | A failed thumbnail delete still soft-deletes the row, which is the same orphan family as D34 and D31. Deferred with them. |
| D79 | deferred | — | The broad catch collapses storage faults, input-output errors and cancellation into one unreadable-image answer. A new low; deferred to the next pass. |
| D77 | deferred | — | The pixel-area cap is blind to bytes per pixel, so legitimate large 16-bit images are refused. An input-validation refinement with no data loss. Deferred to the next pass. |
| D75 | deferred | — | The move onto the shared key can fail on Windows. Production runs Linux, where the rename is atomic. Deferred to the next pass. |
| D76 | deferred | — | A cache-hit read holds the file open and the cleanup delete then fails on Windows. Production runs Linux, which unlinks regardless. Deferred to the next pass. |
| D68 | deferred | — | Limiter saturation and queue depth are unobservable. An observability follow-up; deferred to the next pass. |
| D72 | deferred | — | Staggered parallel preview 401s can churn sessions. The grid's outcome is unchanged and the cost is waste. Deferred to the next pass. |
| D67 | deferred | — | The extra round-trip the D34 re-read costs disappears only with the conditional update deferred alongside it. Paired with D34. |
| D73 | fixed | `39b0098` | The coverage gap is closed by the D63 regression tests, which assert that a signed-in 401 on the upload and preview paths mints no guest session and fires no retry. |
| D74 | deferred | — | The guest-init error path when files are dropped is untested. A separate coverage gap, outside this round's recommended set. Deferred to the next pass. |
| D80 | deferred | — | The implementation plan still lists the public directive and the per-axis cap. Same drift family as D65 but a different file, and outside the recommended set. Deferred. |
| D69 | deferred | — | No test pins the decode slot's release on a throwing decode. Latent, since today's code releases it. Deferred to the next pass. |
| D70 | deferred | — | The exact-type mapping to 422 is proven only by an injected instance. Latent, since the shipped library throws that concrete type. Deferred to the next pass. |
| D78 | deferred | — | The fail-open branch when the identify call returns null is dead today, because the shipped library throws instead of returning null. Deferred to the next pass. |
| D23 | deferred | — | Raised twice this pass: the Postgres arm of the migration is unexercised, and the model snapshot carries the SQLite type. The standing three-environment deferral. |
| D81 | deferred | — | The bomb-alert template is duplicated across the controller and the middleware. The D62 fix added a third site, which makes extracting it worth marginally more. Cleanup, deferred. |
| D82 | deferred | — | `dropRestoredEntry` duplicates `onRemoveUpload`. Cleanup, deferred to the next pass. |
| D83 | deferred | — | The client-abort branch reads the raw correlation-id item instead of the accessor. Trivial cleanup in the file D62 touched; left deferred to keep this round's scope. See Decisions. |
| D84 | deferred | — | Storage save and delete traces sit at Debug under an Information floor, so they never emit. Cleanup, deferred to the next pass. |

## Scope

| Cluster | Findings | Files | Approach-check |
|---|---|---|---|
| A — Memory-aware limiter default (`548663f`) | D61 | `ImageDecodeLimiter.cs`, `Program.cs` | not needed (one default computed from host memory) |
| B — Guest session reach (`069f5ea`, `39b0098`) | D48, D63, D73 | `UI/…/guest-auth.service.ts`, `UI/…/format-selector-page.ts` | not needed (one field-level write and one captured flag) |
| C — Bomb event on the backstop branch (`6b7ce09`) | D62 | `Middleware/ExceptionHandlerMiddleware.cs` | not needed (one emit site beside an existing one) |
| D — Documents (`6e577fd`, `79c2eda`) | D64, D65 | `memory-bank/…/bolt.md`, `memory-bank/…/test-walkthrough.md` | not needed (documents only) |
| E — Left undone this round | D34, D28, D66, D67, D68, D69, D70, D71, D72, D74, D75, D76, D77, D78, D79, D80, D81, D82, D83, D84, D23 | — | not needed (no code changed) |

## Decisions

### The orphan race stays deferred to the cloud-storage sweep (D34, D67)

The durable fix is a conditional update that sets the path only while the row is live, deleting the
just-written file on no match. The in-memory provider the integration tests run on cannot execute one. That is
exactly why the previous round used a liveness re-read. Landing it now would ship code this suite
cannot test. It belongs with the cloud-storage orphan sweep, where a real provider is in play.
D67, the extra round-trip that re-read costs, disappears only with the same change and is folded into
the same deferral.

### One finding was reclassified as fixed by another fix (D73)

D73 asked for a test proving a signed-in 401 during upload mints no guest session. The D63 regression
tests add exactly that, on both the upload and the restored-preview paths, so it is recorded as fixed
against that commit rather than left open.

### The contact-details fix has a residual outside its own scenario (D48)

The fix stops the interceptor wiping contact details. It does not change what the upload page does when it
re-inits a session. That path still writes the stored entry with empty contact fields. An upload-page
re-init would therefore overwrite what was preserved. That is outside this finding's scenario, since
contact details are entered at checkout rather than on the upload page, and it predates the fix. It is
recorded here for the re-reviewer rather than fixed quietly.

### The new emit site raises the value of a deferred cleanup (D62, D81)

The D62 fix adds a third place emitting the reserved bomb event: the batch route in the controller, and
both the pixel guard and the allocator backstop in the middleware. That makes extracting the event name
to one constant worth more than when D81 was raised. Recorded so the next pass weighs it accordingly.

### A one-line cleanup in a touched file was still deferred (D83)

The D62 fix edited the middleware, and D83 is a one-line change in the same file. It was left deferred
to keep this round's change set to the recommended scope, and it batches trivially into a later cleanup
sweep.

### The long tail is deferred deliberately, not missed

The review judged that the search was not complete and asked for another blinded pass. The remaining low and
cleanup findings — development-only races, latent test gaps, observability polish and duplication — are
the tail that pass will re-weigh. None is a data-loss or a runtime break in production.
