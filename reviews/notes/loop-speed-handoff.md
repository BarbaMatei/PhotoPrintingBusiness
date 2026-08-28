---
type: design-note
status: paused — owner order 2026-08-28 after Phase 4; resume from this file
created: 2026-08-22
updated: 2026-08-28
owner: Matei Barba
---

# Loop speed redesign — handoff

Branch `chore/loop-speed-redesign` (worktree `.claude/worktrees/loop-speed`, base
`feat/bolt-038-vat-calculation` @ `4dd6763`), PR #12 into that base branch. The
implementation plan is committed beside this file as
[loop-speed-plan.md](loop-speed-plan.md) — task numbers below refer to it. Execution
ran subagent-driven: every task got an independent review and a scoped re-review;
all listed work is review-clean. The lib test suite stands at **565 assertions, all
passing**, split per script (`node reviews/lib/tests/run-tests.mjs [--only <name>]`;
the full run takes 2–3 minutes).

2026-08-28: the owner merged `feat/bolt-038-vat-calculation` (038 loop closed
2026-08-27 without certification) and its "accepted fix-round audit" machinery (R1–R6:
protocol blocks, round-scope review, test-meaning audit, override log, convergence rule,
lens-coverage debt, design-pass gate, verify-fixes `revert-broke-build`) into this branch
at `2831ffb`. None of the redesign's vocabulary was in that merge's doc text, so Tasks
14–17 stay as written but become **integration edits over the merged text** — never
overwrite the audit sections. `reviews/lib/vocab.mjs` (MANIFEST_LENSES, AREAS, V4_CUTOFF)
is now the machine authority the scripts share.

## Done (tasks 1–13 of 18, plus the suite split)

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
| T9b `wl.mjs` accepts the audit machinery's events (`protocol-written`, `check-dispatched/returned` with `ids`, `round-review-*`, `test-audit-*`) | `7f11a33` `4b61337` |
| T9 `run-scoped-tests.mjs` — stamping test wrapper with a machine-global lock (ownership-guarded; Windows: dead-pid steal is the recovery, signal handlers are POSIX-only) | `61ec043` `ae98e5d` `41ba6e0` |
| T10 `mint-id.mjs` — PPW minting + ledger/resolution scaffolds read from the templates at runtime, CRLF-safe | `38e0a31` `a97b0e3` |
| T11 `speed-report.mjs` — the acceptance metrics; fixtures frozen on the real 038 records | `2a9590b` `4408979` |
| T12 `summary-data.mjs` — computed half of the owner summary; certification pairs merged per pass | `3f88c8e` `9661aee` |
| T13 consolidation — 565 assertions across 13 test files; every CLI prints usage on bad args | — |

**Acceptance baseline as `speed-report.mjs` measures it** (definitions implemented, not
the plan's prose): reference snapshot of 2026-08-21 (log cut at the v12 pass-launch,
175 events) — span 702.0 min, fix-round work 262.1, records+gates 191.3, idle 114.6,
doc-gate first-pass 0.636, correction lines 25 (cumulative to the day); per-round all-in
min per fixed finding r6 7.8 · r8 41.2 · r9 29.0 · r10 21.4, median 25.2. The frozen
full-day fixture (5 later evening events) reads span 763.4 / first-pass 0.667 /
sittings-per-fix 0.414. Records+gates uses a 30-minute anchor-carry cap; a doc-gate
"sitting" is a run of adjacent doc-gate events — both chosen because the plan's literal
rules landed outside the measured ranges. Targets stand: ≤15 min/fix, ≥90% first-pass,
≤0.15 sittings/fix, ~0 corrections.

## Remaining (plan tasks 14–18)

- **T14** `metrics-schema.md` + `doc-contracts.md` — describe the new reality.
- **T15** `README.md` router table + `runbook-verification.md`.
- **T16** loop-driver + fix-review skills: reviewed-unit sequence, queue/sweep,
  judge fix-inline mandate, persistent fixer.
- **T17** future-ideas script backlog in `self-driving-loop-design.md`.
- **T18** sandbox replay against the 2026-08-21 records + final whole-branch review.

T14+T15 and T16+T17 can run as two parallel doc dispatches (disjoint files); T18's
sandbox replay is already done for the renderer half (below) and needs only the
speed-report run and the final whole-branch review. Estimated remaining effort:
~1.5–2 h wall.

**T18 replay, renderer half (done 2026-08-28, sandbox copy under the session
scratchpad, live records untouched):** with three `void` events (the mis-stamped
round-start 7 @08:07:59, round-end 7 @08:57:07, round-start 8 @10:15:47) and three
hand-inserted `round-start` stamps (round 7 @10:15:47, round 9 @14:57:17 and
@16:15:29), `render-records.mjs --dry-run` computes round 7 = 1694 s (28.2 min; the old
line said 9362 s), round 8 = 1399 s (23.3 min; old 6268 s), round 9 = 7653 s (127.6 min
over three spans, idle 0, 21 test invocations; old 11735 s). The router reads the
sandbox as `loop CLOSED` — correct.

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
- **T14 also states:** `wl.mjs` is the only sanctioned way to stamp worklog events, and
  its vocabulary now includes the audit events; `speed-report.mjs`'s metric definitions
  as implemented (above); `summary-data.mjs` merges certification-pair lines per pass.
- **T16 also states:** the fixer stamps `check-dispatched` with `round`, `cluster` AND
  `ids` (the merged skill's prose at line ~175 names only `ids`); test runs go through
  `run-scoped-tests.mjs`; the policy may answer `lens-coverage discovery (<lens>)` and
  `fix round` (sweep) — the driver executes both like router answers.
- Merged-text integration: the audit machinery's sections (protocol blocks, round
  review, test audit, convergence rule, override log) are the owner's and stay verbatim;
  the redesign's text is added beside them.

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
only `*.cs`/`*.ts`. Phase 4: run-scoped-tests steal-path two-collision has no fixture
(correct by inspection); mint-id gained multi-line rationale comments in function bodies
(beyond the header-block allowance); speed-report duplicates render-records'
event-loading and gate-miner's disapproval rule; summary-data last-match-wins for two
same-type lines sharing a pass number outside a certification pair; gate-miner IO-error
path untested, malformed lines silently skipped, header overclaims day-granularity for
print order.

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
- Phase 4 ran as five parallel implementers in isolated worktrees
  (`.claude/worktrees/agent-*`, branches `task/9-run-scoped-tests`, `task/9b-wl-audit-events`,
  `task-10-mint-id`, `task/11-speed-report`, `task-12-summary-data`); every commit was
  cherry-picked onto this branch, so those worktrees and branches are disposable —
  `git worktree remove` + `git branch -D` from the main checkout.
- The merged pre-commit hook logs every `COMMENTS_OK`/`DOCGATE_OK` use to
  `reviews/state/overrides.jsonl`, and the unattended policy stops on any override newer
  than the run's start — the stale-hook `DOCGATE_OK` workaround used here therefore ends
  the moment the branch's hook governs (post-merge), and should not be needed then.
