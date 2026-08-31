---
type: design-note
status: approved 2026-08-28 (owner) — runs after loop-speed phases 5 and 6
created: 2026-08-28
owner: Matei Barba
---

# Phase 7 — restructure the review machinery into named subsystems

Owner decision 2026-08-28: after the loop-speed redesign finishes (phases 5 and 6 in
[loop-speed-handoff.md](loop-speed-handoff.md)), take a step back from adding behaviour
and fix the architecture: how the machinery is modeled, how its files and folders are
laid out, and what in it is legacy. The split of "the review machinery" into pieces that
each name their own job is the organizing principle of this phase, not a later phase.

## Why

What lives under `reviews/` is six systems sharing one folder and one name. The growth
followed the real goal (an autonomous engineering loop, the photo site as its vehicle) —
the mistake is only that everything kept the first piece's name, so responsibilities blur
and the same logic is written several times.

| System | Today's pieces | Review? |
|---|---|---|
| **Review** | `discovery-review.wf.js` (lenses, dedup, skeptics), `reconcile-findings` and `owner-summary` skills, `summary-data.mjs`, `runbook-discovery.md` | yes |
| **Verify** | `verify-fixes.mjs`, `runbook-verification.md` | adjacent |
| **Fix** | `fix-review` skill (triage, clusters, protocol blocks, approach-checks, round review, test audit), `run-scoped-tests.mjs` | no — an engineering workflow |
| **Drive** | `route-next-pass.mjs`, `autonomy-policy.mjs`, `loop-driver` skill, convergence and re-arm rules, unattended runs | no — a workflow state machine |
| **Records** | worklog, metrics, ledger, resolutions, index, backlog, id-counter; `wl.mjs`, `render-records.mjs`, `records-auditor.mjs`, `doc-gate.mjs` + judge, `mint-id.mjs`, `doc-contracts.md`, `metrics-schema.md`, templates | no — a defect tracker plus telemetry store on markdown/JSONL |
| **Measure** | `speed-report.mjs` (the loop measurement and its `--disapprovals` lint miner), `measure/gates.mjs`, the overrides log, the `system` meta-review target, `track-record.md`, `rationale.md` | no — process measurement and improvement |
| **Enforce** | `.githooks/pre-commit` (comment gate, doc-gate backstop, suite run, override log) | no — repo policy |

Concrete symptoms this phase removes:

- "read metrics.jsonl, skip corrections" is implemented in the router, the policy, the
  auditor, summary-data and speed-report; worklog loading plus void filtering in the
  renderer and speed-report; the ledger table parsed by four separate regexes.
- Rules live in three places (README prose, skill prose, script code) and drift; the
  loop-speed phase 5 exists only to re-sync them.
- Legacy tolerance (V2/V3/V4 cut-offs, old frontmatter maps, D-numbered ids, archive
  grandfathering) is a large share of the auditor and renderer.
- Hand-back passes seven gates with no data yet on which earn their keep.
- 565 assertions, ~45 fixture targets, a 112-assertion integration grab-bag, a 2–3 minute
  suite.

## Shape

- **7a Architecture review** (read-only; one design note the owner approves): a
  dependency map; an inventory that assigns every script, rule, doc, skill, test and
  fixture to exactly one system with a **keep / merge / delete** verdict — a piece
  belonging to two systems is a smell to fix; the target layout and model. Working
  sketch, for 7a to refine:

  ```
  reviews/lib/
    records/   schema (event registry, statuses, severities, vocab, cut-offs — vocab.mjs
               is the seed) · one reader/writer per artifact · validators · renderer
    model/     targets → passes, rounds (units), findings, spans, open counts, queue,
               stands-down — computed once, consumed by every engine
    review/    discovery workflow, reconciliation, summary data
    verify/    revert-and-rerun
    fix/       test wrapper, id minting and scaffolds, fixer-side helpers
    drive/     router rows, policy delegations, threshold/sweep/re-arm, convergence
    measure/   speed report, gate mining, track record
    cli/       thin entry points that keep today's command names stable
    tests/     unit per module · flows per real path (unit hand-back, verification,
               certification, close) · ~6 canonical fixture targets
  ```

  plus a `docs-sync` check: the router table, event table and vocabulary lists in the
  README, doc-contracts and the skills are generated from `drive`/`records` constants,
  and the suite fails when prose and code disagree — the drift problem ends there.
- **7b Restructure**: implement the approved layout and model as a **refactor, not a
  rewrite** — every pinned assertion stays green throughout (they are the record of
  every hard-won fix); then re-cut the tests by layer and shrink the fixtures.
- **7c Cleanup**: legacy branches behind dated cut-offs, stale docs and mentions,
  redundant tests and fixtures, dead rules; conventions that are not machinery (commit
  style, comment rule) move to the standards that own them; standards updated in the
  same change.

## Principles

- A boundary earns its place only if it removes total surface — every boundary is one
  more thing an agent must read.
- Names are navigation for LLM agents: each subsystem's name states its job.
- Delete candidates are asked "is this needed at all" before "which box": the Sonnet
  judge on non-owner prose, the backlog as a markdown file, validating archived targets
  forever.
- Nothing is cut without data or an owner ruling: one real target run after phase 6 is
  the recommended evidence for 7a's delete column (which gates fire, what speed-report
  says, what nobody uses).

## Acceptance

- Every piece under `reviews/` and the review skills belongs to one named system.
- The five duplicate record parsers are one; each rule has one home in code and the
  prose cites it.
- `docs-sync` passes; the suite is green and smaller; the full run is under one minute.
- No legacy-tolerance code for record shapes the repo can no longer produce.

## Sequence

Loop-speed phase 5 (docs/skills) → phase 6 (replay + final review) → one real target run
(recommended) → 7a → owner approval of the inventory → 7b → 7c. Cost is of the order of
loop-speed phases 1–4.
