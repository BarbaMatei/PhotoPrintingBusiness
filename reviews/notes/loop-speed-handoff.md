---
type: design-note
status: paused — owner order 2026-08-22 (subscription budget); resume from this file
created: 2026-08-22
owner: Matei Barba
---

# Loop speed redesign — handoff

Branch `chore/loop-speed-redesign` (worktree `.claude/worktrees/loop-speed`, base
`feat/bolt-038-vat-calculation` @ `4dd6763`), PR #12 into that base branch. The
implementation plan is committed beside this file as
[loop-speed-plan.md](loop-speed-plan.md) — task numbers below refer to it. Execution
ran subagent-driven: every task got an independent review and a scoped re-review;
all listed work is review-clean. The lib test suite stands at **299 assertions, all
passing**, now split per script (`node reviews/lib/tests/run-tests.mjs [--only <name>]`).

## Done (tasks 1–8 of 18, plus the suite split)

| Work | Commits |
|---|---|
| T1 `wl.mjs` validated worklog stamper (+ git-env scrub fix in verify-fixes after a real corruption incident) | `2aa0ce1` `eedc114` `2e8b23a` |
| T2 renderer correctness: paired spans, `void` events, render-at-resolved, fail-loud | `fcb493c` `afd9e66` |
| T3 renderer outputs: index rows, ledger flips, `--verification` mode | `4d4f0fe` `3227521` |
| T4 verify-fixes emits buffered `verify-result` events; restore path kept pristine | `fc69368` `d35e502` |
| T5 doc-gate: one invocation lints target + state; Decisions-block + sha/id checks; pre-commit glob fix | `89f500c` `c742bb7` |
| T6 auditor: reviewed-unit window (resolved-no-line legal, V3-cutoff guard) | `676d142` `0266c15` |
| T7 gate-miner (lint-miner reporter), instant-parsed date handling | `3a872e5` `ae60510` |
| T8 router + policy: ledger-driven routing, queue threshold 3, sweep before certification, reviewed-unit wording, shared `ledger.mjs`/`standsDown` | `2780fd0` `a15b5bb` `52e9162` `256acaa` |
| Test-suite split: per-script `tests/*.test.mjs` + `lib.mjs` + `integration.test.mjs`, `--only` scoping | `4301c62` |

## Remaining (plan tasks 9–18)

- **T9** `run-scoped-tests.mjs` — stamping, machine-global-locking test wrapper.
- **T10** `mint-id.mjs` — PPW minting + ledger/resolution scaffolds.
- **T11** `speed-report.mjs` — the acceptance metrics, fixture frozen on the real
  2026-08-21 worklog (baseline: 35–50 min/fix tail, 58% gate first-pass, 0.38
  sittings/fix, 25 corrections; targets: ≤15, ≥90%, ≤0.15, ~0).
- **T12** `summary-data.mjs` — the computed half of the owner summary.
- **T13** suite consolidation for phases 1–4.
- **T14** `metrics-schema.md` + `doc-contracts.md` — describe the new reality.
- **T15** `README.md` router table + `runbook-verification.md`.
- **T16** loop-driver + fix-review skills: reviewed-unit sequence, queue/sweep,
  judge fix-inline mandate, persistent fixer.
- **T17** future-ideas script backlog in `self-driving-loop-design.md`.
- **T18** sandbox replay against the 2026-08-21 records + final whole-branch review.

T9–T12 are now parallelizable (each brings its own `tests/<script>.test.mjs`; the
shared-file serialization the split removed was the only blocker). Estimated
remaining effort: ~2.5–3 h wall.

## Rulings and constraints the remaining tasks must honor

- **T14 must state:** paired-span runtime derivation (idle = Σ span durations −
  active − blocked; between-span time counts nowhere); `void` + `verify-result`
  events; render-at-resolved; the renderer's two mechanical prose writes (index
  rows, ledger flips); doc-gate Check B (ids/shas in state cells must exist/
  resolve); corrections reserved for post-hoc discoveries.
- **T15 must state:** the reviewed-unit definition; queue threshold 3 + sweep row;
  🟠 rows stand down at loop-close (open 🟠 at close roll to backlog — reconcile
  with README note ²) while 🔴/reopened/fix-caused-regression still arm; multi-part
  rounds re-stamp `round-start` per part; verify-fixes ends with a dirty worklog the
  driver commits.
- **T16 must state:** the driver accepts `fix round` as a policy NEXT (sweep
  answers); the close step reads open 🟠 from the ledger (router prints no QUEUED at
  close); a stall detector for a sweep that never drains; fixer stamps via wl.mjs
  and tests via the wrapper; the fixer's own renderer/auditor/gate sitting is
  removed (unit records render after verification); persistent fixer lifecycle
  (kept per target, retired on discovery pass / reopened fix / close).
- Sweep guard scope: certification-answering paths only — a delta-worthy round
  keeps its delta discovery (pinned by tests).
- Regression trigger: `findings[]` flat-mapped across all verification lines,
  filtered to still-open mediums (pinned by tests).

## Deferred minors (for T18's final whole-branch review to triage)

wl.mjs: repo-root re-derivation vs paths.mjs; 1-second timestamp resolution makes
same-second `void` matches ambiguous; no append lock. render-records: sameVal vs
deepEqual key-order divergence on nested `of`; `--verification`-mode stray-char
refusal untested; detail-block boundary only breaks on `###`; status word written
without vocabulary check; `verify-result` missing id/verdict handling; unanchored
hex regex on Commit cells; planIndex error off-by-one; index read-then-write window.
verify-fixes: `runCmd` env unscrubbed for test-spawned git; `commit` field = parsed
shas joined. router: no mechanical check that a pass's `findings[]` ids reached
ledger rows; route-next-pass row 3 re-implements part of `standsDown` by hand
(reorder hazard). gate-miner: IO-error path untested; malformed lines silently
skipped; header overclaims day-granularity for print order. Misc: two-line fixture
comment at the worklog-in-fix-commit test; `.githooks/pre-commit` comment gate scans
only `*.cs`/`*.ts`.

## Operational notes

- `core.hooksPath` points at the MAIN worktree's `.githooks` — the branch's hook
  fixes (fixture-path exclusion) govern only post-merge; until then commits touching
  fixture markdown need `DOCGATE_OK=1` for that known false positive (used four
  times, each verified false-positive-only; never `--no-verify`).
- The live `reviews/038-039-invoicing/` records were never touched. Its worklog
  still carries the round-7/8/9 mislabels; T18's sandbox replay repairs them with
  `void` events (rounds 7/8) and two hand-appended historical `round-start 9`
  stamps in the sandbox copy only.
- The SDD execution ledger (rulings, fix-round history, task reports) lives in the
  worktree's git-ignored `.superpowers/sdd/loop-speed-plan/`; everything needed to
  resume is in this file and the committed plan.
