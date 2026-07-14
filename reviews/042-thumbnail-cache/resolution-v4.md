---
type: resolution
target: 042-thumbnail-cache
answers_review: review-v4.md
version: 4
branch: feat/bolt-042-thumbnail-cache
status: open
fixed_commit: null
opened: 2026-07-14
findings:
  M1:  { status: open, commit: null, note: null }   # write-vs-cleanup orphan (fix-generated)
  M2:  { status: open, commit: null, note: null }   # concurrent File.Create -> 500 (fix-generated)
  M3:  { status: open, commit: null, note: null }   # decode concurrency OOM (re-raises v3 deploy note)
  M4:  { status: open, commit: null, note: null }   # batch path omits bomb event
  M5:  { status: open, commit: null, note: null }   # HEIC over-accept (no decoder)
  M6:  { status: open, commit: null, note: null }   # missing original -> 500
  M7:  { status: open, commit: null, note: null }   # unreadable preview logged without path
  M8:  { status: open, commit: null, note: null }   # 403 leaves un-cartable orphan (guest expiry)
  M9:  { status: open, commit: null, note: null }   # migration DDL untested (re-raises v1 DB-1)
  M10: { status: open, commit: null, note: null }   # upload-bomb delete not verified
  M11: { status: open, commit: null, note: null }   # MaxFrames=1 untested
  L1:  { status: open, commit: null, note: null }   # cache-hit TOCTOU 500 + round-trip (conv 4)
  L2:  { status: false-positive, commit: null, note: "Refuted in review-v4 §H — MIME change IS traced to f850f69 (INPUT-1). Residual folded into C4/docs." }
  L3:  { status: open, commit: null, note: null }   # cache-vanish no signal
  L4:  { status: open, commit: null, note: null }   # orphan-on-failed-commit no signal
  L5:  { status: open, commit: null, note: null }   # GET does DB write (read-replica hazard)
  L6:  { status: open, commit: null, note: null }   # raw filename logged unbounded
  L7:  { status: open, commit: null, note: null }   # self-heal broadened to all 401
  L8:  { status: open, commit: null, note: null }   # one-shot retry untested
  L9:  { status: open, commit: null, note: null }   # shareReplay reinit untested
  L10: { status: open, commit: null, note: null }   # snapshot phantom AlterColumn (DB-1 theme)
  L11: { status: deferred, commit: null, note: "Re-raises v1 CLOUD-1 — seekable-stream/ETag assumption; not triggerable until bolt-043 cloud provider. Deferral stands." }
  L12: { status: open, commit: null, note: null }   # bomb log test asserts name not dimensions
  L13: { status: open, commit: null, note: null }   # 512MB backstop -> raw 500, untested
  L14: { status: open, commit: null, note: null }   # truncated-image 422 path untested
  C1:  { status: open, commit: null, note: null }   # object-URL leak
  C2:  { status: open, commit: null, note: null }   # dup error string x3
  C3:  { status: open, commit: null, note: null }   # self-heal seam tested with halves mocked
  C4:  { status: open, commit: null, note: null }   # walkthrough shows OLD insecure cache directive
  C5:  { status: open, commit: null, note: null }   # story AC 54MP vs shipped 100MP
  C6:  { status: open, commit: null, note: null }   # story AC varchar(500) vs shipped 512
  C7:  { status: open, commit: null, note: null }   # thumbnail 300px vs spec 800px
---

# Resolution — Bolt 042: Thumbnail Cache (answers review-v4)

Fixer-owned; one row per finding ID from [review-v4.md](review-v4.md). No blockers, so this is a
follow-up list, not a gate. IDs are pass-local to v4 (they do **not** map to v1's IDs).

## Recommended order (from review §I)

1. **M3** — decode concurrency gate (`SemaphoreSlim`); the only process-kill vector.
2. **M1 + M2 + M6** — make the deterministic-key write safe (temp-file+atomic-move, `DeletedAt`-guarded
   update, catch `FileNotFoundException`). One change closes most of cluster A.
3. **M4** — emit the bomb event on the batch path.
4. **M5** — HEIC: add a decoder or stop advertising it.
5. **M7, M9–M11** and §G lows/cleanup — fast-follows. **C4** (walkthrough's stale insecure
   `Cache-Control: public…immutable`) fix regardless — copying it reintroduces SEC-1.

## Decisions / deferrals (attached, not suppressed)

- **L11 → deferred** (re-raises v1 **CLOUD-1**): seekable-stream / ETag `stream.Length` assumption
  only breaks once the bolt-043 cloud `IStorageService` lands. Deferral stands; design constraint for 043.
- **M9 / L10 re-raise v1 DB-1** (migration DDL / snapshot never exercised): the Migrate()-based DDL
  test is deferred to the 3-env phase per the roadmap. The fixer may still add a cheap SQLite-file
  `Migrate()` smoke test now; the Postgres/Testcontainers arm stays deferred.
- **L2 → false-positive** (refuted in review-v4 §H).

## Notes for the fixer

- **Fix-generativity is the theme here** — M1/M2/M6 exist *because* of the v1 BUG-3 deterministic-key
  fix. Self-review the concurrency of whatever you change (README *Bounding fix-generativity*).
- Keep comments minimal and don't narrate the fixes in-code (rationale goes here + the commit).
- A finding isn't `fixed` without the regression test the review named (esp. M9–M11, L12–L14 are
  themselves coverage gaps — the "fix" is the test).
