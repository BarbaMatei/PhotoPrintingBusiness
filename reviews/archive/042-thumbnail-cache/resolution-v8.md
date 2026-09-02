---
type: resolution
target: 042-thumbnail-cache
version: 8
answers: review-v8.md
status: resolved
fixed_commit: bd0d5fd
closed: 2026-07-14
---

# Resolution v8 — 042-thumbnail-cache

## Findings

| ID | Status | Commit | Note |
|---|---|---|---|
| PPW-85 | deferred | — | The accepted cloud-storage orphan-sweep class. The durable conditional update cannot run on the in-memory test provider. The new PPW-140 log makes the race visible meanwhile. See Decisions. |
| PPW-136 | fixed | `ac0485b` | A unique tiebreaker was added to both paged queries. A sweep for the same shape found no other paged query; the other collection includes do not paginate. Regression test on tied timestamps, red on revert. |
| PPW-137 | fixed | `62a33cd` | Storing a session now merges: an empty incoming contact field keeps the stored value, and a fresh token always wins. This covers every re-init caller, since only two writers touch that key. Three tests, red on revert. |
| PPW-138 | fixed | `521fa15` | The upload-time bomb test now asserts the specific bomb exception and its dimensions rather than the base type, pinning what the alert emitters key on. The preview-time test already pinned it. |
| PPW-139 | fixed | `521fa15` | A distinct warning is emitted on the lost-original branch, so a storage-integrity incident is distinguishable from a request for an unknown id. Log-assertion test, red on revert. |
| PPW-140 | fixed | `521fa15` | A distinct warning is emitted around the soft-delete-race deletion, matching the sibling anomaly branches. The stale path left on the dead row is PPW-85's deferred work, not this signal. |
| PPW-128 | fixed | `bd0d5fd` | The preview decode is pinned to four bytes per pixel, so a legitimate image under the pixel cap cannot decode at eight and trip the 512 MB backstop. Bit-depth guard plus an end-to-end test. See Decisions. |
| PPW-141 | deferred | — | A real privacy point but a design call: requiring revalidation trades shared-device privacy against the 30-day cache the PPW-52 fix deliberately added. Flagged for the owner, not dropped. See Decisions. |
| PPW-117 | deferred | — | Raised twice: the existence check still has no production caller, and the inert stubs on the cache-hit tests would mask a reintroduced check-then-read. Both belong with the cloud-provider work. |
| PPW-126 | deferred | — | Moving a file over an open reader fails on Windows only; on Linux the rename over an open handle succeeds. Deferred to the next pass. |
| PPW-118 | deferred | — | The extra round-trip on a cache miss disappears only with PPW-85's conditional update. Paired with PPW-85 and deferred with it. |
| PPW-120 | deferred | — | No test pins the decode slot's release on a throwing decode. Latent, since today's code releases it. Deferred to the next pass. |
| PPW-144 | deferred | — | No end-to-end path reaches the bomb rejection, because dependency injection resolves a fake image processor that always reports 800×600. A coverage gap; deferred to the next pass. |
| PPW-74 | deferred | — | The Postgres arm of the migration and the model snapshot are exercised by no test, since the integration provider ignores migrations and only the PostgreSQL arm runs. The standing three-environment deferral. |
| PPW-119 | deferred | — | Limiter saturation and queue depth are unobservable. An observability follow-up; deferred to the next pass. |
| PPW-93 | deferred | — | The one-frame cap is tested on the internal helper, not through the public thumbnail call. Latent, since the cap holds today. Deferred to the next pass. |
| PPW-101 | deferred | — | Guest-session recovery after a failed init is untested, because all twelve specifications supply a successful init. A coverage gap; deferred to the next pass. |
| PPW-82 | deferred | — | The hard-kill shape: a process killed between the file write and the commit orphans the thumbnail. The same orphan-sweep deferral as PPW-85, since the sweep would cover it. |
| PPW-145 | deferred | — | A guest 401 away from the upload page is a silent dead end, because the self-heal lives on that page only. An interface follow-up, outside the recommended set. |
| PPW-146 | deferred | — | The thumbnail component mints an untracked object URL on each change-detection cycle for a photo held in the session. A leak for the life of the tab; deferred to the next pass. |
| PPW-143 | deferred | — | A restore preview that resolves after the page is destroyed leaks one object URL. Deferred to the next pass. |
| PPW-147 | deferred | — | The decode memory budget ignores concurrent upload buffering drawing on the same memory. A configuration matter; fold the buffering allowance into the budget next pass. |
| PPW-142 | fixed | `76d0b6a` | The bolt's change C now states the split-query default is a production query-execution change rather than no change, and carries a retroactive criterion naming the PPW-136 test and the Postgres verification. |
| PPW-79 | deferred | — | The stream contract is untested and stays latent until a provider that returns a stream which cannot rewind exists. Only rewindable implementations exist today. The standing deferral. |
| PPW-132 | deferred | — | The reserved bomb event is copied in three places and the controller copy omits the field naming which guard caught it. Extract a helper and add the missing value. Cleanup; deferred. |
| PPW-110 | fixed | `76d0b6a` | The implementation plan's stale column width was corrected, and the micro-review then caught a second stale copy in a requirements document the finding did not cite, fixed at `00b0d39`. |
| PPW-148 | deferred | — | The conditional GET compares the incoming validator as plain text, so a weak validator, a list or a wildcard silently becomes a full response. Bandwidth only; deferred to the next pass. |

## Scope

| Cluster | Findings | Files | Approach-check |
|---|---|---|---|
| A — Deterministic paging (`ac0485b`) | PPW-136 | `Services/AdminOrderService.cs`, `Services/OrderService.cs` | not needed (one ordering term at two sites, swept for others) |
| B — Session merge (`62a33cd`) | PPW-137 | `UI/…/guest-auth.service.ts` | not needed (one write turned into a merge) |
| C — Bomb type and anomaly signals (`521fa15`) | PPW-138, PPW-139, PPW-140 | `Services/UploadService.cs`, `Tests/…/UploadServiceTests.cs` | not needed (two warnings on existing branches and one test assertion) |
| D — Decode pixel type (`bd0d5fd`) | PPW-128 | `Services/ImageProcessor.cs` | run before implementation — it caught two defects in the review's suggested change, see Decisions |
| E — Documents (`76d0b6a`, `00b0d39`) | PPW-142, PPW-110 | `memory-bank/…/bolt.md`, `memory-bank/…/implementation-plan.md`, `memory-bank/…/requirements.md` | not needed (documents only) |
| F — Left undone this round | PPW-85, PPW-82, PPW-118, PPW-79, PPW-74, PPW-93, PPW-101, PPW-117, PPW-119, PPW-120, PPW-126, PPW-141, PPW-143, PPW-144, PPW-145, PPW-146, PPW-147, PPW-132, PPW-148 | — | not needed (no code changed) |

## Decisions

### The orphan race and its two companions stay deferred (PPW-85, PPW-82, PPW-118)

The durable fix has two halves. One is a conditional update that sets the path only while the row is
live, deleting the just-written file on no match. The other is a cleanup that deletes the derivable key
for every candidate. That
also removes the hard-kill leak recorded under PPW-82 and the extra round-trip under PPW-118. The in-memory
integration provider cannot run a conditional update. That is exactly why the earlier round used a
re-read. Landing it now would ship code this suite cannot test. The new PPW-140 log makes the race observable in the
meantime; it does not resolve the underlying problem.

### The decode was pinned to a fixed pixel type

An approach-check against the shipped library corrected the finding's write-up: the current failure
path is wrapped and re-raised as a plain 422, so no false bomb alert is emitted for a PNG. The real
defect, permanent failure to preview with every retry failing the same way, is exactly as described and
is what the fix removes. The check also caught two defects in the review's suggested one-line change:
it would not compile, and the return type had to stay as it was. Both were folded in.

### Pinning the pixel type raises the common case's memory, and it still fits

Forcing four bytes per pixel raises an ordinary eight-bit decode from three bytes to four, about 400 MB
at the 100 MP cap. That is still under the 512 MB allocator backstop and under the limiter's per-slot
allowance, so no sizing changes. The existing note that raising the pixel cap means raising the
backstop in step stays correct; the multiplier is now fixed at four. Grayscale sources now encode as
three-channel output, marginally larger and visually identical.

### The paging fix is proven in shape, not in symptom

The regression test proves deterministic ordering and that each order keeps its own items, and it
reddens without the tiebreaker. The in-memory provider does not split queries, so the missing-items
symptom itself can only be reproduced on Postgres. That verification rides with PPW-74, and the retroactive
criterion added under PPW-142 records it.

### The shared-device cache is an owner decision, not a patch

The stored preview is recoverable from the browser's own store on a shared computer profile. The
suggested fix changes a deliberate caching design that the PPW-52 fix introduced, it is a low finding, and
it is outside the recommended set, so it wants an owner decision rather than a reflexive change. It is
flagged, not dropped.

### The micro-review found a defect in the fixer's own work

Two micro-reviews read the whole fix diff with no prior context, split between the backend and the
frontend and documents. The backend cluster came back clean. The frontend and document cluster caught one class
sweep that stopped short: the PPW-110 fix had left a second stale column width in a requirements document
the finding never cited, which was then fixed and confirmed absent repository-wide.

### One nearby defect was spotted and deliberately left alone

While fixing PPW-136 the fixer found a pre-existing cousin: an administrator statistics query takes the top
ten of an in-memory grouping with no tiebreaker, so a tie at the tenth place is decided arbitrarily. It
is a display of top-ranked figures rather than paging, so it is cosmetic, and it was left untouched to
keep this round to the finding set. It is recorded here for a later pass to weigh.
