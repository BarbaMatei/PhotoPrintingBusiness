---
type: resolution
target: 043-cloud-storage-provider
version: 3
answers: review-v3.md
status: resolved
fixed_commit: 972a8b4
closed: 2026-07-20
---

# Resolution v3 — 043-cloud-storage-provider

## Findings

| ID | Status | Commit | Note |
|---|---|---|---|
| PPW-168 | fixed | `2f49a8d` | The promotion scanner became a periodic background service mirroring its purge sibling, with its own validated interval setting. A test drives the entry point so the reflection gap PPW-170 records is not repeated. |
| PPW-173 | fixed | `4674dcd` | Unroutable cloud rows are skipped so the tier resolve cannot throw outside the per-upload guard and stall the batch, with a per-sweep count warning. Skipped rows are not soft-deleted, so they are retried when cloud returns. |
| PPW-170 | fixed | `fea2490` | A test drives the scanner's entry point and reddens with a timeout when the boot sweep is removed. The coverage the PPW-152 fix lost is back. |
| PPW-172 | fixed | `c30d734` | The production-complete purge is wrapped like its cancel sibling, so a purge that throws no longer returns 500 after the transition has already been committed, emailed and broadcast. |
| PPW-171 | fixed | `c30d734` | A test makes the purger throw on the cancel path and asserts the cancellation still succeeds. Removing the guard reddens it. |
| PPW-162 | fixed | `c4ec6ca` | A fetch failure is separated from a genuine empty result, with an error signal and a retry action. Partial: the empty copy still cannot tell not-yet-archived from cloud-off and purged. See Decisions. |
| PPW-153 | fixed | `a5cb0be` | Part b of PPW-153, carried on the ledger as row PPW-154. The lightbox reports the image error, the page re-fetches fresh signed URLs at `c4ec6ca`, and the grid tiles got the same handler at `972a8b4`. |
| PPW-180 | fixed | `f048dc1` | The object URL is created once per file and revoked when the file changes or the component is destroyed. A spec asserts one creation and the revoke. |
| PPW-174 | fixed | `c30d734` | The ZIP fails before writing any response byte when a cloud original cannot be routed, so the admin gets a clean error rather than a truncated archive. The customer preview path got the same guard at `0fc577a`. |
| PPW-175 | deferred | — | Reclaiming a failed promotion's cloud litter needs an orphan-sweep design, not a patch. See Decisions. |
| PPW-176 | deferred | — | The orphan race shares its root cause with PPW-158 and belongs with the concurrency-token work. See Decisions. |
| PPW-177 | fixed | `66a5f64` | Validator tests for both sweep intervals. Dropping a rule makes the validator succeed and the test fail, so it cannot pass for the wrong reason. |
| PPW-179 | fixed | `66a5f64` | Backfill boundary tests on both sides of the filter: cancelled, payment-failed and awaiting-payment are never promoted; shipped and delivered are. |
| PPW-178 | fixed | `66a5f64` | A test drives the re-resolve that succeeds on the second local read and asserts the file result. Coverage of a branch that is correct today. |
| PPW-169 | deferred | — | Postgres parity for the nullable original-path column belongs with the three-environment work. See Decisions. |
| PPW-181 | fixed | `c4ec6ca` | Definitive errors still redirect, an expired session is left to the interceptor, and a transient failure now shows an inline error with a retry instead of bouncing the user. |
| PPW-182 | fixed | `a5cb0be` | The lightbox gained a dialog role, a modal flag and a label, focus into the dialog on open, a tab trap on the close button, and focus restore to the trigger on close. |
| PPW-183 | deferred | — | Latent: no detail-to-detail navigation exists, so no failing trace can be built today. See Decisions. |

## Scope

| Cluster | Findings | Files | Approach-check |
|---|---|---|---|
| A — Promotion recovery made periodic (`2f49a8d`) | PPW-168 | `BackgroundJobs/PromotionRecoveryScanner.cs` | not needed (the purge sibling's approved shape reused) |
| B — Guarded tier resolve at all three sites (`4674dcd`, `c30d734`, `0fc577a`) | PPW-173, PPW-174 | `BackgroundJobs/UploadCleanupJob.cs`, `Services/AdminOrderService.cs`, `Services/UploadService.cs` | not needed (a guard added to existing calls) |
| C — Purge guards and their tests (`c30d734`) | PPW-172, PPW-171 | `Services/AdminOrderService.cs`, `Tests/…/AdminOrderServiceTests.cs` | not needed (the cancel path's guard applied to its sibling) |
| D — Frontend error handling, retry and URL refresh (`c4ec6ca`, `a5cb0be`, `972a8b4`) | PPW-162, PPW-153, PPW-181, PPW-182 | `UI/…/order-detail-page.ts`, `UI/…/photo-lightbox.component.ts` | not needed (no new mechanism) |
| E — Blob URL created once and revoked (`f048dc1`) | PPW-180 | `UI/…/photo-thumbnail.component.ts` | not needed (a memoised field) |
| F — Coverage only (`fea2490`, `66a5f64`) | PPW-170, PPW-177, PPW-178, PPW-179 | `Tests/…` | not needed (tests only) |
| G — Deferred this round | PPW-169, PPW-175, PPW-176, PPW-183 | — | not needed (no code changed) |

## Decisions

### Reclaiming a failed promotion's cloud litter needs its own design

When a promotion writes its three cloud objects and then fails the row update, the row stays on local
storage with empty preview keys, so cleanup routed by that tier never touches the cloud objects. A
proper fix reclaims by the deterministic key scheme regardless of the recorded tier, which is a design
and not a patch, and the same class is already deferred on bolt-042. Nothing is lost today: the local
original and the row both remain, and only cloud bytes leak, after a persistent mid-promotion failure.
This round's own periodic sweep narrows it further, because a transient failure now self-heals.

### The duplicate-promotion orphan race belongs with the concurrency-token work

The race needs two concurrent promotions of one order, and their precondition is the duplicate-webhook
race already deferred as PPW-158. The clean fix is the same in both cases: re-read the live tier and path
before the row update, or put a concurrency token on the upload. That is a change to the concurrency
model rather than a patch, so it goes with PPW-158 rather than being bolted onto a storage round.

### Postgres parity for the nullable column belongs with the three-environment work

The migration is asserted on PostgreSQL only, and a skeptic confirmed it is correct on Postgres today, so
this is a coverage gap and not a live defect. It is the same parity gap already carried on two other
bolts, and it lands with the container-test work rather than here.

### The stale-detail trap is recorded, not fixed

Every entry to the order detail route comes from the list route, which recreates the component, and
this round introduced no detail-to-detail link. The trap becomes real only when such a link is added,
so it is recorded for whoever adds one rather than fixed speculatively.

### The empty state is only half fixed

The fix separates a failed fetch, which is retryable, from a genuine empty result. Telling
not-yet-archived from cloud-off and from purged still needs the API to say which it is, which is a
contract change and belongs with whoever owns that endpoint. The defect the finding named — a
transient error shown as permanent, with no retry — is closed.

### The refresh re-fetches the whole photo list

Recovering one expired signed URL by re-fetching every photo is heavier than a per-photo refresh
endpoint would be, but a new endpoint is not worth it at this size. Recorded so a later change with
more photos per order reconsiders it.

### Two siblings of these fixes were found and fixed before hand-back

The fresh-eyes review of the whole fix diff caught two surviving siblings, both fixed in-round: the
customer preview path had the same unguarded tier resolve as the admin ZIP (`0fc577a`), and the grid
thumbnails shared the lightbox's expiring URL with no error handler (`972a8b4`). The same review
accepted one judgement that a later pass overturned: it called the periodic sweep's missing dedup
wasteful rather than corrupting, which the next verification showed was wrong, and that became PPW-184.
It also flagged an unrevoked blob URL in the cart feature as outside this review's surface.
