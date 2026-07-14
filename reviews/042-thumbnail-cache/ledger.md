---
type: defect-ledger
target: 042-thumbnail-cache
updated: 2026-07-14
---

<!-- v8 (2026-07-14, @e2093bd, third blinded discovery pass, Opus 4.8): +13 new (D85–D97),
     15 re-raises of open/deferred items, 3 refuted (D78 re-raise + 2 new candidates).
     New-finding curve finally decaying: v4 32 → v6 24 → v8 13. F3/D86 defeats the v7-verified
     F2 fix; fix-generativity loop still live. Not saturated. -->


# Defect ledger — Bolt 042: Thumbnail Cache

The **cross-pass** record. Each pass numbers its findings locally and blindly (v1: `SEC-1`…;
v4: `M1`/`L1`/`C1`; v5+: `F1`…) and deliberately does **not** map them to prior passes — that
protects blinding. This file is where the mapping lives instead: every real defect gets one
**stable `D#`** that lives forever, assigned by the synthesizer *after* each blinded pass. It answers
"is this a re-find, a re-raise of an accepted deferral, or new?" — recorded, not remembered.

Backfilled 2026-07-14 from review/resolution v1–v4. Full v4 detail: [findings-v4.md](findings-v4.md).
The join key **within** a pass is the pass-local ID; the join key **across** passes is `D#` here only.

## Summary

- **Cumulative distinct defects: 97** (D1–D97). 3 candidates refuted (not defects — v4-L2, v6-FP,
  v8's benign-→-bomb). D78 was raised then refuted at v8 (kept in the ledger, `refuted-this-pass`).
- **v8 (third discovery pass, @`e2093bd`, Opus 4.8):** +13 new (`D85–D97`), **15 re-raises** of already-open
  / deferred items, 3 refuted. **New-finding curve finally decaying: v4 32 → v6 24 → v8 13.** But **not
  saturated** — 5 new mediums, and the fix-generativity loop is still live: **`D86`/F3 defeats the v6 F2 fix
  that v7 just verified** (contact-info preserved by `clearGuestToken`, wiped by the next `storeSession`
  re-init), `D96`/F22 residual of the F1/D61 decode limiter, `D92`/`D95` residual of the C1/D54 blob-URL fix.
  Headline new medium `D85`/F2: the bundled Change C global `SplitQuery` default mis-pages un-tiebroken
  collection-`Include` queries — a correctness angle no prior pass audited.
- **Discovery-pass overlap (the saturation signal):** three blinded *discovery* passes — v1, v4, v6.
  v1↔v4 shared-identity overlap was **2** (`D23`, `D28`). v6 (post-v4-fix) re-found **all** items still
  open at its commit — the four standing deferrals/disputes `D23`, `D28`, `D31`/`D34`-residual, `D48` —
  and added **24 genuinely new** ones (5 medium, 15 low, 4 cleanup). Every v4 defect that was *fixed*
  was gone by v6's commit and correctly not re-found (`refinds_identity=0`).
- **Reading it:** v1 found 28, v4 found 32, v6 found 29. As with bolt-035 the passes ran against
  **different commits** (each is post-fix of the last), so overlap can't feed a capture–recapture
  population estimate — fixes removed the prior population. The honest signal is qualitative and it is
  the same each pass: the new-finding count is **not decaying**, and v6's new mediums are again
  **fix-generated residuals** (`D61` decode-limiter default re-opens `D33`'s OOM; `D75` move-target race
  residual of `D35`/M2; `D62` bomb-event gap on the `D52`/L13 422 mapping). The feature is **not
  saturated** — closure still wants *another* quiet discovery pass (one that finds nothing new).
- **Current state (after review-v8 discovery @`e2093bd`):** all v6 fixes (F1/F2/F3/F5 + doc F6/F7)
  remain **verified** (v7) and none re-found open — except **`D86`/F3**, which shows the v6 **F2** fix is
  **defeated** by an adjacent `storeSession` overwrite (a fix-generated residual, not a re-open of D48
  itself). v8 adds **D85–D97** (13 new, 0 High / 5 M / 7 L / 1 ⚪). Recommended before merge: `D85`/F2
  (split-query paging), `D86`/F3 (contact-info wipe), `D87`/F4 (bomb-alert test) + cheap obs `D88`/F5,
  `D89`/F6 + doc `D91`/F23; bounded fix `D77`/F7 (bit-depth cap). Cross-pass deferrals still stand:
  **`D28`**→bolt-043 (F24), **`D31`/`D34`**→bolt-043 orphan sweep (F1/F18), **`D23`** Npgsql-DDL/snapshot→3-env
  (F14). Prior v6 recommendations already fixed+verified: `D61`/`D62`/`D63`/`D64`/`D65`. **`D46`/L5**
  (read-replica) still upheld. Per-finding: [findings-v8.md](findings-v8.md). **Still not saturated, but the
  new-finding curve is decaying (32 → 24 → 13) and 15 of 28 findings are re-raises.**

## Cross-pass defects (the ones that matter)

| D# | Defect | Seen in (pass · local-id) | Status |
|----|--------|---------------------------|--------|
| **D23** | Migration provider-parity **and** its DDL exercised by no test | v1·`DB-1` (provider-aware **fixed**@bca68fa; DDL-test deferred) · v4·`M9` (DDL untested) · v4·`L10` (snapshot phantom AlterColumn) · v6·`F24`/`F25` · v8·`F14` | provider-aware **verified**; DDL/snapshot-test **deferred → 3-env phase** (v6/v8 skeptics confirm SQLite arm IS tested; only Npgsql uncovered) |
| **D28** | Cloud `IStorageService` seekable-stream / `stream.Length` assumptions | v1·`CLOUD-1` (deferred) · v4·`L11` (ETag/seek) · v6·`F8` · v8·`F24` | **deferred → bolt-043** (re-affirmed v4, v6, v8) |
| **D33** | Image decode has no aggregate/concurrency memory bound (OOM under concurrent large images) | v3·deploy-note · v4·`M3` (**fixed**) · v6·`F1`=**D61** (limiter default still OOMs) · v8·`F22`=**D96** (budget ignores upload buffering) | fix **verified (v5)**; **D61 open** (default = `ProcessorCount`, ignores RAM); **D96 open** (budget excludes concurrent upload buffers) |
| **D31 / D34** | Cache-fill write vs cleanup TOCTOU → permanently orphaned thumbnail | v2·`NEW-3` (deferred) · v4·`M1` (**fixed**) · v5·`V5-1` (M1 residual) · v6·`F4` · v8·`F1` (re-found) + v8·`F18` (hard-kill variant) | **deferred → bolt-043** orphan sweep; the M1 `stillLive` guard is non-atomic vs cleanup's end-of-batch commit; v8·F6/D89 adds the unlogged-partial-state face |
| **D48 / D86** | Guest self-heal + contact-info handling | v4·`L7` (disputed) · v6·`F2`=**D48** (sharpened: checkout contact-info wipe) — **fixed v6 (F2)**, verified v7 · v8·`F3`=**D86** (the F2 fix is *defeated*: `clearGuestToken` preserves contact info but `storeSession` re-init overwrites it) | **D48 fixed+verified**; **D86 open** — fix-generated residual, recommend before merge |

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

### v6 discovery (commit `6c0ed93`) — full detail in [findings-v6.md](findings-v6.md)

24 new (D61–D84). Re-raises (no new `D#`): v6·`F4`→**D34/D31**, `F2`→**D48**, `F8`→**D28**,
`F24`/`F25`→**D23**. Refuted: v6-FP (orphan-reclaim warning already signals — see review-v6 §G).

| D# | Defect | v6 id | Sev | Status |
|----|--------|-------|-----|--------|
| D61 | Decode-limiter default = `ProcessorCount`, ignores RAM → OOM DoS *(residual of D33)* | F1 | 🟠 | open — recommend before merge |
| D62 | Allocator-backstop bomb (`InvalidMemoryOperationException`) not emitted as bomb event | F5 | 🟠 | open — recommend before merge |
| D63 | Expired-JWT logged-in user re-attributed to a throwaway anonymous guest | F3 | 🟠 | open — recommend before merge |
| D64 | HEIC removal undocumented in `bolt.md` bundled-scope (3rd unlisted contract change) | F6 | 🟠 | open (doc) |
| D65 | `test-walkthrough.md` certifies `Cache-Control: public/immutable` code never emits | F7 | 🟠 | open (doc) |
| D66 | `ExistsAsync` added to `IStorageService` but has no production caller | F9 | 🟡 | open |
| D67 | Extra `AnyAsync` round-trip on every cache-miss preview *(from D34/M1 fix)* | F17 | 🟡 | open |
| D68 | Decode-limiter saturation/queuing unobservable | F15 | 🟡 | open |
| D69 | Decode slot-release-on-throw untested | F21 | 🟡 | open (plausible) |
| D70 | `InvalidMemoryOperationException→422` exact-type mapping proven only by injected instance | F22 | 🟡 | open (plausible) |
| D71 | Cleanup job thumbnail-delete-failure untested → silent re-leak | F10 | 🟡 | open |
| D72 | Parallel preview 401s defeat init dedup; late 401 wipes freshly minted token | F16 | 🟡 | open |
| D73 | Logged-in 401-during-upload interaction untested *(D63's path)* | F18 | 🟡 | open |
| D74 | `onFilesAccepted` guest-init error path untested → files hang 'uploading' | F19 | 🟡 | open |
| D75 | `File.Move` to shared key races concurrent writers on Windows → 500 *(residual of D35/M2)* | F13 | 🟡 | open (Windows-dev) |
| D76 | Cleanup `DeleteAsync` fails vs open read handle on Windows → orphan | F14 | 🟡 | open (Windows-dev) |
| D77 | Pixel-area cap ignores bytes-per-pixel → legit 16-bit PNG false-422 | F12 | 🟡 | open |
| D78 | Pixel guard skipped when `Identify` null (fail-open) | F23 | 🟡 | open (dead today, plausible) |
| D79 | `GetInfoAsync` collapses storage/IO faults + cancellation into 'unreadable' | F11 | 🟡 | open |
| D80 | `implementation-plan.md` AC list left stale after documented substitutions | F20 | 🟡 | open (doc) |
| D81 | Bomb-alert log template duplicated (controller + middleware) | F26 | ⚪ | open |
| D82 | `dropRestoredEntry` duplicates `onRemoveUpload` verbatim *(from D40/M8 fix)* | F27 | ⚪ | open |
| D83 | `client_aborted` reads raw `Items["CorrelationId"]` not `GetCorrelationId()` | F28 | ⚪ | open |
| D84 | Storage save/delete traces at `Debug` under Information floor → never emit | F29 | ⚪ | open |

### v8 discovery (commit `e2093bd`, Opus 4.8) — full detail in [findings-v8.md](findings-v8.md)

13 new (D85–D97). Re-raises (no new `D#`): v8·`F1`→**D34/D31**, `F7`→**D77** (bumped 🟡→🟠), `F9`→**D66**,
`F10`→**D75**, `F11`→**D67**, `F12`→**D69**, `F14`→**D23**, `F15`→**D68**, `F16`→**D42**, `F17`→**D50**,
`F18`→**D31**, `F24`→**D28**, `F25`→**D81**, `F26`→**D66**, `F27`→**D59**. Refuted: `H1`→**D78** (dead today),
plus 2 fresh candidates (benign-→-bomb; orphan-reclaim-swallow = same FP as v6). Full per-finding: [findings-v8.md](findings-v8.md).

| D# | Defect | v8 id | Sev | Status |
|----|--------|-------|-----|--------|
| D85 | Global `SplitQuery` default mis-pages un-tiebroken collection-`Include` (admin orders lose items) | F2 | 🟠 | open — recommend before merge |
| D86 | `storeSession` re-init overwrites the contact info the F2/D48 fix preserved *(defeats v7-verified F2)* | F3 | 🟠 | open — recommend before merge |
| D87 | Bomb test asserts base `UnprocessableEntityException`, so `decompression_bomb.rejected` alert can regress green | F4 | 🟠 | open — recommend before merge |
| D88 | Lost ORIGINAL blob on a live row logged as a generic 404 (no distinct signal) | F5 | 🟠 | open — recommend (cheap log) |
| D89 | Soft-delete-race thumbnail deletion leaves silent partial DB/file state | F6 | 🟠 | open — recommend (cheap log) |
| D90 | Preview `private, max-age=2592000` recoverable device-locally on a shared browser *(residual of D1)* | F8 | 🟡 | open |
| D91 | Bundled Change C (global split-query) has no AC/test, mislabeled "no behavior change" | F23 | 🟡 | open (doc) |
| D92 | In-flight restore preview resolving after `ngOnDestroy` leaks an object URL *(residual of C1/D54)* | F21 | 🟡 | open |
| D93 | No end-to-end bomb/oversize→422 test (integration `FakeImageProcessor` pins 800×600) | F13 | 🟡 | open |
| D94 | Guest 401 off the upload page is a silent dead-end (self-heal is format-selector-only) | F19 | 🟡 | open |
| D95 | `localUrl()` mints untracked blob URLs per change-detection for in-session photos *(residual of C1/D54)* | F20 | 🟡 | open |
| D96 | Decode memory budget ignores concurrent upload buffering sharing the same RAM *(residual of D61/F1)* | F22 | 🟡 | open |
| D97 | Conditional-GET `If-None-Match` only handles an exact strong tag (weak/list/`*` → full 200) | F28 | ⚪ | open |
