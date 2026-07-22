---
type: resolution
target: 043-cloud-storage-provider
version: 7
answers: review-v7.md
status: resolved
fixed_commit: ac97e42
opened: 2026-07-22
closed: 2026-07-22
tests: { dotnet: "719/719 (+10 skipped MinIO, run in CI)", frontend: "439/439" }
findings:
  F1:  { status: fixed, commit: c37ca44, note: "D49 HIGH. Stream rewind moved INSIDE the Polly retry lambda (per-attempt); a non-seekable stream on retry now throws NotSupportedException (loud failure, never a truncated re-upload). Regression tests: retried attempt must receive the full payload (was 0 of 8 bytes — red with the exact bug signature before the fix); non-seekable retry surfaces after exactly one SDK attempt." }
  F2:  { status: fixed, commit: 4dfd755, note: "D50. Live-order-reference guards at both destruction sites, design-checked: purge skips while any OTHER order referencing the upload is in {Paid, Printing} (liveness via the recovery sweep — its query keys on FilePath != null and re-fires once the blocker resolves); retention deletes only when NO referencing order has PaidAt >= cutoff (null-PaidAt orders do not extend retention). ACCEPTED RESIDUAL (owner direction — sharing flows are corner-case): an AwaitingPayment sharer that pays after the purge ships without that photo; blocking on AwaitingPayment would let abandoned checkouts pin storage forever. Revisit with the bolt-035 concurrency work. Tests: shared-with-live-order skip (strict mock proves zero deletes), sharer-Delivered purge proceeds, in-window sharer survives retention, SQLite translation smoke for the new NOT-EXISTS." }
  F3:  { status: fixed, commit: df1026d, note: "D51. Retry backoff detached from the semaphore-guarded region — the slot now covers only active promotion work. Design-check corrections applied: retries bounded (100 parked; past the cap dropped to the recovery sweep with a warning), no shared mutable list (Interlocked counter only), post-shutdown enqueue is safe (channel never completed; the sweep backstops dropped retries). New surface: the bounded detach itself — failure modes covered by the D58 tests below." }
  F4:  { status: fixed, commit: 5cfc9f9, note: "D52. GetOrderPhotosAsync now filters DeletedAt == null; soft-deleted uploads (blobs already deleted by cleanup) are no longer presigned. Test: soft-deleted upload yields empty photos + zero presign calls." }
  F5:  { status: deferred, commit: null, note: "D53 → bolt-035. Same root as the deferred D9 (webhook Paid transition check-then-act, no concurrency token — repo rule: no optimistic concurrency; the fix is the Orders unique-guard/token work bolt-035 owns). v7 adds the duplicate-confirmation-EMAIL consequence to that remit. See decisions." }
  F6:  { status: fixed, commit: b171ce8, note: "D54. Rejection copy no longer advertises HEIC (dropped end-to-end in bolt 042/M5); dead image/heic extension arm removed. Test pins the copy." }
  F7:  { status: fixed, commit: b171ce8, note: "D55. SanitizeFileName strips the directory component and truncates to the column's 260 (prod-Postgres 22001 → 500 was invisible on InMemory/SQLite). Test asserts truncation at the service boundary; DB-level enforcement remains the D20/3-env Testcontainers item." }
  F8:  { status: fixed, commit: 04149fa, note: "D56. ArchiveExpired audit emitted only AFTER the batched SaveChanges (IDs collected in-loop). Tests: save failure emits no audit; success emits one per upload." }
  F9:  { status: fixed, commit: fe0e6d2, note: "D57. Ship-path purge gated on Enabled && CloudEnabled, mirroring the cancel path — no more per-ship Error in the supported Provider=local config. The archive-on-but-cloud-off mismatch stays surfaced by the recovery scanners' cloud-tier-off logs. Test: ship with cloud off never invokes the purger." }
  F10: { status: fixed, commit: df1026d, note: "D58. The retry path is now tested: a healthy job is not blocked behind a failed job's 1h backoff (the D51 regression test — times out red on pre-fix code), and a failed job re-enqueues and succeeds on attempt 2 with zero backoff." }
  F11: { status: fixed, commit: a80b819, note: "D59. Integration test asserts the succeeded webhook enqueues promotion for the paid order (RecordingPhotoPromoter in PaymentFactory). Deleting the EnqueueAsync call now reddens." }
  F12: { status: deferred, commit: null, note: "D60 → 3-env track (with D20). Exercising real R2/AWS (beyond the CI MinIO suite) is environment/infra work, not a code fix; the D49 retry path gained a unit-level regression test in F1, shrinking what only a real provider can prove. See decisions." }
---

# Resolution v7 — 043-cloud-storage-provider

Fixer responses to [review-v7.md](review-v7.md) (certification pair: 1 High, 11 Medium, 17 Low,
5 Cleanup). Per-finding detail in the review; canonical `D#` in [ledger.md](ledger.md). This round
was run **lean by owner direction** (cost pressure): no discovery fan-outs — the findings carried
file/line/fix; exactly two subagents ran, both required by process: the **rule-3 design check**
(one agent, covering the two risky fixes D51 + D50 — its three mandatory corrections were applied)
and the **fresh-eyes micro-review** of the final diff.

**Scope: all 8 fixable serious findings fixed** (D49 High + D50/D51/D52/D54/D55/D56/D57 + coverage
D58/D59), each with a regression test. **2 deferred with rationale** (D53 → bolt-035 with D9;
D60 → 3-env with D20). **Lows D61–D77 and Cleanups D78–D82 → backlog** per the severity-based stop
rule — recorded in the ledger, drained by the groomer / next bolt in the area / the re-run
certification pair.

## Status table

| F# | D# | Sev | Status | Commit | How |
|----|----|-----|--------|--------|-----|
| F1 | D49 | 🔴 | **fixed** | c37ca44 | Rewind inside the retry lambda; non-seekable retry fails loudly (revert-red: retry got 0 of 8 bytes) |
| F2 | D50 | 🟠 | **fixed** | 4dfd755 | Live-order-reference guards at purge + retention (design-checked; accepted residual recorded) |
| F3 | D51 | 🟠 | **fixed** | df1026d | Backoff detached from the slot; bounded (100) parked retries; sweep backstop |
| F4 | D52 | 🟠 | **fixed** | 5cfc9f9 | `DeletedAt == null` filter in GetOrderPhotosAsync |
| F5 | D53 | 🟠 | **deferred** | | → bolt-035 with D9 (concurrency token); adds the duplicate-email consequence to that remit |
| F6 | D54 | 🟠 | **fixed** | b171ce8 | HEIC copy + dead extension arm removed (bolt-042 M5 regression) |
| F7 | D55 | 🟠 | **fixed** | b171ce8 | Filename truncated to the 260 column cap at the service boundary |
| F8 | D56 | 🟠 | **fixed** | 04149fa | ArchiveExpired audit after the durable commit |
| F9 | D57 | 🟠 | **fixed** | fe0e6d2 | Ship-path purge gated on cloud-on (mirrors cancel) |
| F10 | D58 | 🟠 | **fixed** | df1026d | Retry/backoff/re-enqueue path tested (slot-starvation + retry-success) |
| F11 | D59 | 🟠 | **fixed** | a80b819 | Webhook→promotion wiring integration test |
| F12 | D60 | 🟠 | **deferred** | | → 3-env with D20 (real-provider exercise is env infra) |
| — | D61–D77 | 🟡 | **backlog** | | Severity-based stop rule; ledger rows carry the detail |
| — | D78–D82 | ⚪ | **backlog** | | Same |

## Fresh-eyes micro-review outcome (before hand-back)

One anchored agent over the full fix diff (three questions). **One real finding, fixed in-round:**
**A4** — `UploadCleanupJob`'s referenced-retention branch was the D50 shared-upload data-loss class
at a **third site**, carrying none of the new guards (age alone deleted uploads a Paid/Printing
order still needed to fulfil, or an in-window paid order was entitled to view). Fixed @`ac97e42`:
the shared `retentionExpired` predicate now excludes uploads with any referencing order in
{Paid, Printing} or paid inside the archive window; 3 regression tests (live-order survives,
recently-paid survives, complete+past-window still reclaimed). The same pass confirmed: no other
stream-consuming retry sites; the other presign path already filters `DeletedAt`; `OriginalPurged`
already logs post-commit; the retry counter pairs increment/decrement on all paths; {Paid, Printing}
is exactly the pre-fulfilment set; no current caller passes a non-seekable stream (the new throw is
defensive-only). One comment nuance on D57's "where the mismatch is surfaced" corrected in the same
commit.

## Decisions / rationale

- **D50 residual (accepted).** The guard's status set is {Paid, Printing} (design-checked). An
  AwaitingPayment order sharing the upload that pays *after* the purge would ship without that
  photo — but blocking on AwaitingPayment lets every abandoned checkout pin storage indefinitely.
  Owner direction: the sharing flows are corner-case today; revisit when bolt-035 adds the
  concurrency token (which also enables a cleaner reservation model).
- **D53 deferred, not patched.** The repo rule is "no optimistic concurrency anywhere; unique
  indexes + violation detection are the mechanism" — a conditional-UPDATE patch here would violate
  that and break InMemory tests (no ExecuteUpdate). It is the same defect D9 was deferred for;
  bolt-035's remit now explicitly includes the duplicate-confirmation-email consequence.
- **D60 deferred.** MinIO runs on every CI push; what's missing is real R2/AWS exposure, which is
  the 3-env deployment track's remit (with D20). The sharpest gap it named — the untested S3 retry
  path — is now unit-covered by F1's regression tests.
- **Fix-round tier: delta-worthy by definition (fixed a 🔴, changed the worker's concurrency
  behavior)** — but the next blinded instrument here is the **re-run certification pair** (the
  protocol's counter reset), which subsumes a delta. No separate delta pass.

## Hand-back

`status: resolved` at `fixed_commit: ac97e42` (includes the micro-review's A4 class-sweep fix).
Suites **.NET 719/719** (+10 CI MinIO) · **FE 439/439** (+17 backend tests vs v7's frozen tree).
Next: **verification** re-review against `ac97e42` → review-v8 (flips fixed → verified or reopens);
then re-freeze and re-run the **certification pair** per the protocol — with the owner deciding
between the full pair (~4M tok) and the proposed cheaper variant (one fresh full pass leveraging
the existing A/B overlap data).
