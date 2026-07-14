---
type: defect-ledger
target: 042-thumbnail-cache
updated: 2026-07-14
---

# Defect ledger — Bolt 042: Thumbnail Cache

The **cross-pass** record. Each pass numbers its findings locally and blindly (v1: `SEC-1`…;
v4: `M1`/`L1`/`C1`; v5+: `F1`…) and deliberately does **not** map them to prior passes — that
protects blinding. This file is where the mapping lives instead: every real defect gets one
**stable `D#`** that lives forever, assigned by the synthesizer *after* each blinded pass. It answers
"is this a re-find, a re-raise of an accepted deferral, or new?" — recorded, not remembered.

Backfilled 2026-07-14 from review/resolution v1–v4. Full v4 detail: [findings-v4.md](findings-v4.md).
The join key **within** a pass is the pass-local ID; the join key **across** passes is `D#` here only.

## Summary

- **Cumulative distinct defects: 60** (D1–D60). 1 v4 candidate refuted (not a defect — v4-L2).
- **Discovery-pass overlap (the saturation signal):** the only two blinded *discovery* passes are v1
  and v4. Their shared-identity overlap is **2** — `D23` (migration DDL/parity) and `D28` (cloud
  seekable-stream). Both were **deferred** in v1, so they were still present for v4 to re-find; every
  v1 defect that was *fixed* was gone by v4's commit and correctly not re-found.
- **Reading it:** v1 found 28, v4 found 32, overlap 2 → near-disjoint. But (as with bolt-035) the two
  passes ran against **different commits** (v4 is post-fix), so this can't feed a capture–recapture
  population estimate — fixes removed v1's population. The honest signal is qualitative: among defects
  *still open* at v4's commit, v4 re-found both; and it added ~30 genuinely new ones (several
  fix-generated), so the feature is **not saturated** — closure still wants another discovery pass.
- **Current state:** 28 open (27 new in v4 + `D33` re-raised), 3 deferred (`D28`→bolt-043,
  `D31`→bolt-043, `D23` DDL/snapshot-test→3-env phase), 29 verified/closed (+ `D23` provider-aware part).

## Cross-pass defects (the ones that matter)

| D# | Defect | Seen in (pass · local-id) | Status |
|----|--------|---------------------------|--------|
| **D23** | Migration provider-parity **and** its DDL exercised by no test | v1·`DB-1` (provider-aware **fixed**@bca68fa; DDL-test deferred) · v4·`M9` (DDL untested) · v4·`L10` (snapshot phantom AlterColumn) | provider-aware **verified**; DDL/snapshot-test **deferred → 3-env phase** |
| **D28** | Cloud `IStorageService` seekable-stream / `stream.Length` assumptions | v1·`CLOUD-1` (deferred) · v4·`L11` (ETag/seek) | **deferred → bolt-043** (re-affirmed v4) |
| **D33** | Image decode has no aggregate/concurrency memory bound (OOM under concurrent large images) | v3·deploy-note (decode concurrency) · v4·`M3` | **open** (recommend a `SemaphoreSlim` gate pre-merge) |
| D31 | Orphaned-thumbnail sweep for the un-recorded case | v2·`NEW-3` (deferred) | **deferred → bolt-043** |

> Note the fix-generativity chain: v1·`BUG-3` (D5, deterministic thumbnail key — *fixed*) is the
> **cause** of three new v4 defects: `D34` (`M1` write-vs-cleanup orphan), `D35` (`M2` concurrent
> `File.Create` 500), and `D38` (`M6` missing-original 500). Recorded so the link isn't lost.

## Full ledger

### v1 discovery (commit `fad7693` fixed / `095285c` verified in review-v2)

| D# | Defect | v1 id | Status |
|----|--------|-------|--------|
| D1 | Preview `Cache-Control: public` → cross-user cache leak | SEC-1 | verified (v2) |
| D2 | Decode-bomb guard per-axis, misses total pixels + frames | BUG-1 | verified (v2) |
| D3 | Guest-401 self-heal interceptor branch untested | TEST-1 | verified (v2) |
| D4 | Cleanup job never deletes `ThumbnailPath` (leak) | BUG-2 | verified (v2) |
| D5 | Cache-fill write non-idempotent/non-atomic (orphans) | BUG-3 | verified (v2) · *cause of D34/D35/D38* |
| D6 | `UnknownImageFormatException` unmapped → 500 | BUG-4 | verified (v2) |
| D7 | Story 003 `MemoryAllocator` cap silently dropped | REQ-1 | verified (v2) |
| D8 | Thumbnail path not deterministic (spec) | REQ-2 | verified (v2) |
| D9 | Story 002 soft-delete edge case contradicts code | REQ-3 | verified (v2) |
| D10 | Bundled guest-auth + dev-warning scope undocumented | REQ-4 | verified (v2) |
| D11 | No in-flight guest-session dedup → duplicate sessions | FE-1 | verified (v2) |
| D12 | Self-heal not seamless (no auto-retry) | FE-2 | verified (v2) |
| D13 | Anon no-token 401 → login dead-end | FE-3 | verified (v2) |
| D14 | `restoreFromSession` wipes uploads on 401 | FE-4 | verified (v2) |
| D15 | Batch rejections swallowed, no logging | OBS-1 | verified (v2) |
| D16 | Client-cancel log at `Debug` → never emitted | OBS-2 | verified (v2) |
| D17 | Pixel-bomb 422 indistinguishable in logs | OBS-3 | verified (v2) |
| D18 | `AsNoTracking` dropped → tracking on cache-hit hot path | QUAL-1 | verified (v2) |
| D19 | Generate re-reads just-written thumbnail from storage | QUAL-2 | verified (v2) |
| D20 | Dimension check + message duplicated across layers | QUAL-3 | verified (v2) |
| D21 | 30-day TTL magic number `2592000` | QUAL-4 | verified (v2) |
| D22 | Split-query config duplicated per provider | QUAL-5 | verified (v2) |
| **D23** | *(cross-pass — see above)* migration parity + DDL untested | DB-1 | partial: provider-aware verified; DDL-test deferred |
| D24 | HEIC/ISO-BMFF magic-byte over-accept | INPUT-1 | verified (v2) *(HEIC-still-undecodable persists → D37)* |
| D25 | Real `ImageProcessor`/bomb guard never exercised | TEST-2 | verified (v2) |
| D26 | Cache persistence unproven (shared `DbContext`) | TEST-3 | verified (v2) |
| D27 | Untested: cache-control, 304, migration, etc. | TEST-4 | verified (v2) |
| **D28** | *(cross-pass — see above)* cloud seekable-stream assumptions | CLOUD-1 | deferred → bolt-043 |

### v2 verification follow-ups (fixed @`e3a77d9`, verified in review-v3)

| D# | Defect | v2 id | Status |
|----|--------|-------|--------|
| D29 | Decode cap too low for large-format prints (→100 MP) | NEW-1 | verified (v3) |
| D30 | Restored uploads dropped on transient preview error | NEW-2 | verified (v3) |
| **D31** | *(cross-pass)* orphan-thumbnail sweep (un-recorded case) | NEW-3 | deferred → bolt-043 |
| D32 | Storage keys not OS-independent (forward-slash) | NEW-4 | verified (v3) |

*(v3 also corrected 2 stale comments left by D29/D30 — doc cleanup, not new defects.)*

### v4 discovery (commit `9e44714`) — full detail in [findings-v4.md](findings-v4.md)

| D# | Defect | v4 id | Sev | Status |
|----|--------|-------|-----|--------|
| **D33** | *(cross-pass)* no aggregate/concurrency decode memory bound | M3 | 🟠 | open |
| D34 | Lazy preview-write races cleanup → orphan *(from D5)* | M1 | 🟠 | open |
| D35 | Concurrent `File.Create` (FileShare.None) → 500 *(from D5)* | M2 | 🟠 | open |
| D36 | Batch bomb never emits the reserved alert event | M4 | 🟠 | open |
| D37 | HEIC accepted but no decoder exists (100% fail) | M5 | 🟠 | open |
| D38 | Preview cache-miss w/ missing original → 500 *(from D5)* | M6 | 🟠 | open |
| D39 | Unreadable stored image logged w/o path/cause | M7 | 🟠 | open |
| D40 | 403 leaves un-cartable orphan after guest expiry | M8 | 🟠 | open |
| D41 | Upload-bomb delete not verified by any test | M10 | 🟠 | open |
| D42 | `MaxFrames=1` (APNG defence) zero coverage | M11 | 🟠 | open |
| D43 | Cache-hit TOCTOU 500 + redundant round-trip | L1 | 🟡 | open |
| D44 | Cache-vanish silently regenerates, no signal | L3 | 🟡 | open |
| D45 | Orphan-on-failed-commit, no signal | L4 | 🟡 | open |
| D46 | GET `/preview` does a DB write (read-replica hazard) | L5 | 🟡 | open |
| D47 | Batch-reject log emits raw client filename unbounded | L6 | 🟡 | open |
| D48 | Self-heal broadened to swallow every unauth 401 | L7 | 🟡 | open |
| D49 | One-shot retry guard untested for a still-failing retry | L8 | 🟡 | open |
| D50 | `shareReplay`/finalize re-init-after-settle untested | L9 | 🟡 | open |
| — | *(re-raise of **D23** — snapshot phantom `AlterColumn`)* | L10 | 🟡 | see D23 |
| — | *(re-raise of **D28** — ETag `stream.Length` seekable)* | L11 | 🟡 | see D28 (deferred) |
| D51 | Bomb log test asserts event name not dimensions | L12 | 🟡 | open |
| D52 | 512 MB backstop → `InvalidMemoryOperationException` → 500, untested | L13 | 🟡 | open |
| D53 | Truncated-but-recognized image 422 path untested | L14 | 🟡 | open |
| D54 | Preview object-URLs never revoked (blob leak) | C1 | ⚪ | open |
| D55 | Upload-error string duplicated ×3 | C2 | ⚪ | open |
| D56 | Self-heal seam tested only with each half mocked | C3 | ⚪ | open |
| D57 | Walkthrough shows OLD insecure `Cache-Control: public` | C4 | ⚪ | open |
| D58 | Story AC "54 MP rejected" vs shipped 100 MP | C5 | ⚪ | open |
| D59 | Story AC `varchar(500)`/`StoragePath` vs shipped `varchar(512)`/`FilePath` | C6 | ⚪ | open |
| D60 | Thumbnail 300 px vs stories/brief 800 px | C7 | ⚪ | open |

*(v4-`L2` "MIME change untraced" — **refuted**, not a defect; recorded in review-v4 §H. v4-`M9` and
v4-`L10` map to **D23**, v4-`L11` to **D28**, v4-`M3` to **D33** — re-raises, no new `D#`.)*
