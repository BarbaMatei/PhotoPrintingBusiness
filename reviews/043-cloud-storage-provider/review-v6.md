---
type: review
target: 043-cloud-storage-provider
version: 6
supersedes: 5
commit: 2d02b13
branch: feat/bolt-043-cloud-storage-provider
pass-type: verification
date: 2026-07-22
reviewer: "independent verification (revert-and-rerun + fix-diff review), fresh agent — NOT the fixer"
verifies: resolution-v5.md
verdict: approve-with-followups
blockers: []
verified: [F1, F2]
reopened: []
upheld: [F3, F4, F5, F6, F7, F8, F9, F10, F11, F12, F13, F14]
new: []
tests: { dotnet: "702/702 (+10 skipped MinIO, run in CI)", frontend: "439/439" }
---

# Review v6 — 043-cloud-storage-provider (verification pass)

Independent, anchored verification of [resolution-v5.md](resolution-v5.md) against `fixed_commit`
`2d02b13` (HEAD `73d70ba` is docs-only; src at HEAD is identical to `2d02b13`, tree clean at start
and end). Verification, not discovery: it checks that the two `fixed` findings hold and that the
round's triage decisions still stand — not a fresh audit, and it **cannot certify saturation**
(README *Two loops*). Per the runbook a verification pass emits **at most** `approve-with-followups`.

**Verdict: `approve-with-followups`.** Both `fixed` findings (**D36/F1**, **D38/F2**) **verified**
non-vacuously; the 4 cluster-A deferrals (D35/D37/D46/D47 → bolt-035) and the 8 backlog items
(D39–D45, D48) **upheld** — each still validly open, none touched or worsened by the fixes; **0
reopened**; **0 new findings**. No blockers.

## How this was verified

Verified **independently** by a fresh agent (the fixer's own revert-checks during the fix round did
not count — README fixer contract). The working tree was confirmed clean (`git status --short`
empty) before and after; every revert restored source-only, never the test files.

### Verified fixes (revert-and-rerun — the fix's regression test must go red without the fix)

| F# | D# | Sev | Revert-and-rerun result |
|----|----|-----|-------------------------|
| F1 | D36 | 🟠 | Reverting `order-detail-page.ts` only → exactly one spec red: *"does NOT re-open a closed lightbox when a grid thumbnail errors after close (D36 regression)"* — `expected 'https://cdn/fresh-l' to be null` (the closed lightbox re-opened, precisely the finding). Other 19 specs green (no collateral). Restore → 20/20 green. **Non-vacuous.** |
| F2 | D38 | 🟠 | Reverting `UploadCleanupJob.cs` only → exactly one test red: `Cleanup_manyUnroutableCloudRows_doNotStarveLocalOrphanCleanup` — `Expected deleted to be 1, but found 0` (the local orphan behind 500 unroutable Cloud rows was never reached). Companion `Cleanup_cloudRowWithCloudDisabled_...` stayed green (no collateral). Restore → 13/13 green. **Non-vacuous.** |

### Fix-diff review (class / new-surface / regression) — no findings

- **D38 return semantics unchanged.** The only caller is `ExecuteAsync`'s log line. Pre-fix returned
  `candidates.Count - unroutable`; post-fix `candidates.Count` — equivalent, since unroutable Cloud
  rows are now excluded from `candidates` and every candidate is soft-deleted. `fileErrors` untouched.
- **New surface at the bar (D38).** The `upload.cleanup.unroutable` diagnostic `CountAsync` is gated
  inside `if (!cloudEnabled)` — zero extra query on the hot path when cloud is enabled — and the
  shared `retentionExpired` `Expression<Func<Upload,bool>>` is EF-translatable, reused by both the
  candidate query and the count. Observability preserved (definition-of-done class 6).
- **D36 open-lightbox flow preserved.** `refreshPhotoUrls` still re-points an *open* lightbox
  (`lightboxPhotoId` non-null at resolve time); the pre-existing "refresh re-points the lightbox"
  spec passed in both the reverted and restored runs. Only the *closed* case changed.

### Deferral / backlog gate (`git diff 972a8b4..2d02b13` on each cited file)

- **Cluster A → bolt-035 (D35/D37/D46/D47), `PromotionRecoveryScanner.cs`:** unchanged this round —
  validly deferred, stands.
- **D45, `AdminOrderService.cs`:** unchanged — validly backlog.
- **Frontend backlog on `photo-lightbox.component.ts` (D41/D42/D48):** unchanged — validly backlog.
- **Frontend backlog on `order-detail-page.ts` (D40/D43/D44):** the file changed +12/−2, but the diff
  is exactly the D36 fix (template binding, `closeLightbox()`, the `openPhotoId` re-read). It does not
  touch `loadOrder`'s 401 path (D43), the `urlsRefreshed` guard or its coverage (D40), or retry/init
  dedup (D44). All three remain validly open.

## Build & tests

- **.NET 702/702** (+10 skipped MinIO, run in CI) · **FE 439/439** — the fix round added one backend
  and one frontend regression test vs the v5 frozen tree (701/438).

## Loop state

This was a **patch-grade** fix round (no High fixed, no mechanism added/converted, no design changed),
so per the router (README, *The stop rule*) it does **not** re-arm a delta pass. With D36/D38 verified,
0 reopened, and 0 new serious findings, the fix→verify loop for the v5 population is **complete and
the loop is quiet.**

A verification pass cannot emit `approved` and cannot certify saturation. Feature closure now needs
the risk-tiered **certification pair** (043 storage is full-loop tier): two parallel blinded full
passes against a frozen commit, folding in the still-owed full-manifest lenses (db-parity,
observability, input-validation, whole-feature requirements) that no pass has yet run on this feature.
Per the cost guard, certification (~2× a full pass) **waits for explicit owner go-ahead.** The
cluster-A design item (D35/D37/D46/D47) is owed to **bolt-035**; the backlog (D39–D45, D48) awaits the
groomer / next frontend bolt / the certification pair.
