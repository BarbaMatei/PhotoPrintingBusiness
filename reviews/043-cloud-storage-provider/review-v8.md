---
type: review
target: 043-cloud-storage-provider
version: 8
supersedes: 7
commit: ac97e42
branch: feat/bolt-043-cloud-storage-provider
pass-type: verification
date: 2026-07-22
reviewer: "independent verification (revert-and-rerun + mutation + deferral gate), fresh agent — NOT the fixer"
verifies: resolution-v7.md
verdict: approve-with-followups
blockers: []
verified: [F1, F2, F3, F4, F6, F7, F8, F9, F10, F11]
reopened: []
upheld: [F5, F12]
new: []
tests: { dotnet: "719/719 (+10 skipped MinIO, run in CI)", frontend: "439/439 (1 non-043 load-flake, green in isolation and on the clean run)" }
---

# Review v8 — 043-cloud-storage-provider (verification pass)

Independent, anchored verification of [resolution-v7.md](resolution-v7.md) against `fixed_commit`
`ac97e42` (HEAD `44e4d51` is docs-only; src identical; tree clean between every revert). Per the
runbook, a verification pass emits at most `approve-with-followups` and cannot certify.

**Verdict: `approve-with-followups`.** All **10** fixed findings (9 F#s + the micro-review's A4
class-sweep) **verified non-vacuous**; the 2 deferrals (F5/D53 → bolt-035, F12/D60 → 3-env) and the
22-item backlog **upheld**; **0 reopened, 0 new findings.**

## Verified fixes (revert-and-rerun; source-only reverts, green on restore)

| F# | D# | Result |
|----|----|--------|
| F1 | D49 🔴 | Reverting `S3StorageService.cs` → retry test red with the exact data-loss signature (*"Expected bytesPerAttempt[1] to be 8L … found 0L"*) + non-seekable test red. **The High is non-vacuously closed.** |
| F2 | D50 | `OriginalPurger.cs` revert → shared-with-live-order test red (10 others green); `ArchiveRetentionJob.cs` @`4dfd755~1` → in-window-sharer test red with the D56 audit tests still green (clean attribution). **A4** (`UploadCleanupJob.cs` third site): pre-fix state fails to compile confined to its test file — anticipated red-with-attribution; 16/16 green on restore. |
| F3 | D51 | Worker revert → slot-starvation test red with the exact 5s-timeout signature (the healthy job blocked behind the 1h backoff). |
| F4 | D52 | `OrderService.cs` revert → soft-deleted-upload test red, 54 green. |
| F6 | D54 | `UploadService.cs` revert → message-pin test red. |
| F7 | D55 | Same revert → overlong-filename test red. |
| F8 | D56 | `ArchiveRetentionJob.cs` @`04149fa~1` → save-fails-no-audit test red (+ expected D50 collateral at the pre-both-fixes file state, attributed). |
| F9 | D57 | `AdminOrderService.cs` revert → ship-with-cloud-off test red, 26 green. |
| F10 | D58 | Covered by F3's revert (slot test red); the re-enqueue test guards deletion of the re-enqueue (stayed green on revert, as designed). |
| F11 | D59 | **By mutation** (test-only fix): commenting out the Stripe-path `EnqueueAsync` in `WebhooksController` reddened exactly the new wiring test, 18 green; 19/19 on restore. |

## Deferral / backlog gate — PASS

`git diff ba493e4..ac97e42` touches only the 8 fixed source files + tests. `WebhooksController.cs`
unchanged in the round (D53 stands); `S3StorageServiceIntegrationTests.cs` untouched (D60 stands);
backlog spot-checks (`PromotionRecoveryScanner.cs`, `photo-lightbox.component.ts`, D45's ZIP
pre-flight) all untouched.

## Build & tests

**.NET 719/719** (+10 skipped MinIO, run in CI) · **FE 439/439**. One pre-existing, non-043
load-flake noted for the backlog: `format-selector-page.spec.ts > should create` timed out twice
under full-parallel load, green in isolation and on the clean run (no frontend file changed this
round).

## Loop state

The v7 certification counter-reset is now fully serviced: every serious finding from the
certification pair is **fixed and independently verified**, the deferrals carry owner-ratified
rationale, and the backlog is recorded. Per the protocol, the one remaining gate to feature closure
is the **re-run certification** on a re-frozen commit — the owner chooses between the full pair
(~4M tokens, by-the-book) and the recorded-deviation cheaper variant (one fresh blinded full pass
~2M, leveraging the existing pass-A/B overlap data; certifies only if quiet on serious findings).
Awaiting explicit owner go-ahead per the cost guard — nothing runs until then.
