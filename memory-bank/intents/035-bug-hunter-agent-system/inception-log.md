---
intent: 035-bug-hunter-agent-system
created: 2026-06-10T10:40:14Z
completed: 2026-06-10T10:40:14Z
status: complete
---

# Inception Log: 035-bug-hunter-agent-system

## Overview

**Intent**: Tooling-only intent building the multi-agent bug-hunting system from
`docs/agent-systems/bug-hunter-build-guide.md` — 42 briefs across 5 additive phases + an optional
integration tier, all as Claude Code skills, all **read-only on application source**.
**Type**: Infrastructure/Tooling (no production code)
**Created**: 2026-06-10
**Source feed (spec of record)**: `docs/agent-systems/bug-hunter-build-guide.md`
**Construction mandate**: every component built with the **skill-creator** skill
(`Skill` tool → `skill-creator:skill-creator`) per the guide's build loop — paste
brief → build → run its three test prompts → fix → next, in master-build-order.

## Artifacts Created

| Artifact | Status | File |
|----------|--------|------|
| Requirements | ✅ | requirements.md |
| System Context | ✅ | system-context.md |
| Units | ✅ | units.md / units/{unit}/unit-brief.md (6 units) |
| Stories | ✅ | units/{unit}/stories/*.md (42 stories) |
| Bolt Plan | ✅ | memory-bank/bolts/085–094/bolt.md (10 bolts) |

## Summary

| Metric | Count |
|--------|-------|
| Functional Requirements | 8 (FR-1/FR-2 cross-cutting) |
| Non-Functional Requirements | 6 |
| Pinned Decisions | 7 (D1–D7) |
| Units | 6 (= the guide's phases + optional tier) |
| Stories | 42 (= the guide's briefs, 1:1) |
| Bolts Planned | 10 (085–094) |

## Units Breakdown

| Unit | Stories | Bolts | Priority |
|------|---------|-------|----------|
| 001-phase-1-skeleton | 7 | 085, 086 | Must |
| 002-phase-2-trust | 5 | 087 | Must |
| 003-phase-3-breadth-and-scale | 17 | 088, 089 ∥ 090, 091 ⛔ | Must (1 Should) |
| 004-phase-4-learn-and-measure | 6 | 092 | Should |
| 005-phase-5-remediation | 4 | 093 | Should |
| 006-optional-integration | 3 | 094 ⏸ | Could |

## Decision Log

> **Note (2026-06-15):** rows below reference the four cross-system review files
> (`docs/agent-systems/reviews/cross-system-review-v<N>-<date>.md`, v1–v4 — read any of them with
> `git show b4329a8^:docs/agent-systems/reviews/cross-system-review-v<N>-<date>.md`) and
> intermediate spec versions (v3.1–v3.6,
> v1.1–v1.5). Once the specs reached final form, those review files and versioned copies were removed;
> the specs are now versionless (`bug-hunter-build-guide.md`, `integration-contract.md`,
> `knowledge-builder-build-guide.md`). All of it remains recoverable in **git history** — the rows are
> kept verbatim as the historical record.

| Date | Decision | Rationale | Approved |
|------|----------|-----------|----------|
| 2026-06-10 | **Correction**: the first inception run this day misread the subject — it specced porting the specsmd inception agent itself to Claude skills (intent `035-specsmd-inception-claude-skills`, bolts 085–087). Owner corrected: the subject is the **bug-hunter build guide**. Wrong artifacts deleted, replaced by this intent (same numbers reused) | Owner correction in-session | Yes (owner) |
| 2026-06-10 | One story per guide brief (42), incl. extension briefs; stories point at Prompt N, never duplicate it | The guide IS the construction spec (briefs are skill-creator prompts); memory-bank adds tracking + acceptance criteria (D7) | Yes (per owner instruction) |
| 2026-06-10 | Construction MUST use skill-creator for every component | Explicit owner requirement + the guide's own Part I build loop — encoded as FR-1 and repeated in every unit brief, story, and bolt | Yes (owner instruction) |
| 2026-06-10 | Units = the guide's phases; bolts respect the master build order; 089 ∥ 090 is the only parallel pair; 091 runs alone (re-opens skills from 5 prior bolts) | Additive design: extension briefs re-open existing skills → parallel waves would conflict; the guide is explicitly dependency-ordered | Inception decision |
| 2026-06-10 | Component names verbatim from the guide (D1); skills at `.claude/skills/` (D2); outputs rooted at `bug-hunting/` (D3); sandbox recipe from repo compose assets (D4) | See requirements Pinned Decisions | Inception decision — flagged for owner review |
| 2026-06-10 | `concurrency-auditor-agent` in scope at Should (D5) | Guide-optional, but the stack is async/await + BackgroundServices + EF transactions | Inception decision — flagged for owner review |
| 2026-06-10 | Bolt 091 (oracle) marked `blocks: true` (D6) | `intent-lookup` needs the knowledge builder's `ledger-query` interface, which doesn't exist in this repo; owner must confirm availability or descope — no silent stubbing | Inception decision — flagged for owner review |
| 2026-06-10 | Priorities: P1–P3 Must, P4–P5 Should, Optional Could; 094 parked on adoption | The guide's "build only as far as your bottleneck demands" | Inception decision |
| 2026-06-11 | Adopted `docs/agent-systems/integration-contract.md` as the normative cross-system interface; spec of record bumped to `docs/agent-systems/bug-hunter-build-guide.md` (8 mirror edits applied); bolt 091's external gate is now a schedule (after knowledge-builder Phases 1–2, contract §7); loop mailboxes pinned (`fix_status` on fix-requests, `correlation_id` in bug-bolt frontmatter) | Knowledge-builder guide v3 + integration contract published; both systems now build against one interface | Yes (owner) |
| 2026-06-11 | Cross-system review G1–G16 applied (read it with `git show b4329a8^:docs/agent-systems/reviews/cross-system-review-v1-2026-06-11.md`): spec of record bumped to guide **v3.2** + contract **v1.1** (KB guide → v3.1). Story-level deltas: injection convention (data-never-instructions) + secret redaction everywhere; reporting floor fixed to the confidence axis with body callouts; signature matches are candidate duplicates (never auto-collapsed); `fix_status: fix-reported` written on signal pickup + run-open mailbox scan; eval runs isolated to `bug-hunting/eval-runs/`; record-model-per-run replaces eval pinning; triage queue capped/age-escalating + intake takes the writer lock; ledger gains `schema_version` + Windows-safe publish; harvested-test pre-approval checklist | Review accepted by owner; minor-version bumps chosen over major (additive, no structural change) | Yes (owner) |
| 2026-06-12 | Cross-system review v2 H1–H35 applied (read it with `git show b4329a8^:docs/agent-systems/reviews/cross-system-review-v2-2026-06-12.md`): spec of record bumped to guide **v3.3** + contract **v1.2** (KB guide → v3.2). **NEW Prompt 31b** (run-open fix-request mailbox scan — H1) added as story 005-orchestrator-remediation-ext in unit 005 / bolt 093 (now 5 stories; intent total 43). Owner decisions recorded in contract v1.2: **publish = git commit by the publishing orchestrator** (A); **`main` is the designated single-history integration home**. Other story-level deltas: run lock owned by orchestrator Open/Close + write audit; `closed-unverified` terminal state; `correlation_id` allocation rule; `Reopened` in the enum + full-record embedding + content hash in ledger-io; regression-candidate compare in bug-lifecycle; injection/secret/PII guards at candidate emission; budget semantics; model-change eval trigger | Review accepted by owner 2026-06-12; mechanisms-over-policy pattern (the review's central finding) addressed by naming a builder for every rule | Yes (owner) |
| 2026-06-12 | Cross-system review v3 I1–I13 applied (read it with `git show b4329a8^:docs/agent-systems/reviews/cross-system-review-v3-2026-06-12.md`): spec of record bumped to guide **v3.4** + contract **v1.3** (KB guide → v3.3). Runtime co-residence closed: **cross-system mutex** (each Open checks the sibling's `.run-lock`), write audits **scoped to the run's own store**, publish-commits **path-scoped + serialized** (I1/I7); Prompt 31b's scan predicate now includes **`fix-failed`** so re-fixes are re-checked (I2 — a state-machine bug in the H-round text); **owner decision (I3, option B): `closed-unverified` fixes produce NO oracle entry of any kind** — blind spot accepted and recorded in contract §4; hunting-host posture (clean checkout, egress allowlist, pinned scanner toolchain — I4/I5); code-index atomic pointer-swap refresh (I6); envelope `oracle_coverage` + run-open incomplete-backfill warning (I8); injection-resistance is now a graded eval metric and the report surfaces `injection_suspected` (I9); fix-failed only when the fixing commit is at HEAD (I11); pre-merge runs are read-only advisory (I12). No new stories or briefs — structure unchanged | Review accepted by owner 2026-06-12 with decision B (stricter than the reviewer's queue-only proposal); review cadence judged at diminishing returns — next signal source is Phase 1 construction | Yes (owner) |
| 2026-06-15 | Cross-system review v4 J1–J4 applied (read it with `git show b4329a8^:docs/agent-systems/reviews/cross-system-review-v4-2026-06-15.md`): spec of record bumped to guide **v3.5** + contract **v1.4** (KB guide → v3.4). The code-index seam (root cause of J1–J3): the close audit keeps its store-scoped diff **and** gains a **forbidden-ground check** (no write under app source / `memory-bank/` / `docs/` except the one approved test file) — restoring the "never edit app code" backstop the I-round's store-scoping had blinded (J1); the shared code index is now a **gitignored, untracked, regenerable build artifact** — never committed, never audited, regenerated on demand — which dissolves the dual-writer commit/audit gap (J2 convention carve-out + J3); J4 confirmed the schedule's ordering is intentional (Phase 5's eval doesn't exercise the loop, so it may precede Phase 4) and both documents now say so, KB cross-system summary completed. No new stories or briefs — structure unchanged; story touch-ups on orchestrator audit + code-index | Review accepted by owner 2026-06-15; all 4 verified real (3 self-referential to the I-round). Cadence call reaffirmed: stop iterating doc reviews — index made an untracked artifact to end the per-round seam findings; next move is KB inception + Phase 1 build | Yes (owner) |
| 2026-06-15 | **Operating model factored into pluggable profiles** (owner design, not a review): spec of record bumped to guide **v3.6** + contract **v1.5** (KB guide → v3.5). Contract §5.5 (NEW) defines the invariant core + two independent policies — **TriggerPolicy** (`local-hook` / `ci-pipeline` / `manual`) and **CommitPolicy** (`direct-to-main` / `pr-auto-merge`) — composed into named profiles; **active profile for this repo = `solo-local`** (`local-hook` post-merge on `main` + `direct-to-main`); `team-ci` captured but NOT built (YAGNI). §1's serialization reframed as profile-supplied (the `.run-lock` mutex is the `local-hook` mechanism; CI uses a concurrency group). Orchestrator skills are **profile-agnostic** — the hook/CI/branch-PR mechanics are deployment-side adapters, so the systems port across contexts unchanged. Steady-state: post-merge hook fires → librarian then inspector run in sequence (bookmark catch-up; mid-pass triggers ignored) → each commits its own store → approvals drained async. No new stories/briefs/bolts; story touch-up on orchestrator (007). ARCHITECTURE.md gains an operating-model diagram | Owner design decision — motivated by reusing these systems as a product across projects (solo-push-to-main here; protected-`main`/CI on future team projects); seam defined now, only the active profile built | Yes (owner) |

## Scope Changes

| Date | Change | Reason | Impact |
|------|--------|--------|--------|

## Ready for Construction

**Checklist**:
- [x] All requirements documented
- [x] System context defined
- [x] Units decomposed
- [x] Stories created for all units
- [x] Bolts planned
- [x] Human review complete — owner review 2026-09-03, after the re-scope (see the
      2026-09-03 sections below) (was: this inception ran autonomously per the
      owner's 2026-06-10 instruction (the instruction + guide = Checkpoint 1); owner
      review of the generated specs stands in for Checkpoints 2–3 before construction)

## Next Steps

1. Owner reviews the pinned decisions (D1–D7 in requirements.md) — especially D3
   (`bug-hunting/` output root), D5 (concurrency-auditor in scope), and D6 (the
   knowledge-ledger gate on bolt 091).
2. Owner provides/approves the sandbox recipe before bolt 087 (D4).
3. Plan waves with the **bolt-parallel-planner** over bolts 085–094. Expected shape:
   085 → 086 → 087 → 088 → (089 ∥ 090) → 091 ⛔ → 092 → 093; 094 parked.
   Mostly sequential by design — the guide's additive build order is binding; 089 ∥ 090
   is the one parallel pair.
4. Construction entry per bolt: bolt.md → unit brief → stories → paste each story's
   Prompt N into **skill-creator**. **STOP if skill-creator is unavailable.**

## Dependencies

085 → 086 → 087 → 088 → {089, 090} → 091 → 092 → 093; 094 hangs off 092 (adoption-
gated). External: sandbox recipe (087+), knowledge ledger `ledger-query` (091),
tracker/CI adoption (094). No dependencies on application bolts; zero production code.

## 2026-09-03 — Re-scope after the review loop (see requirements.md)

The rows above are the June 2026 record and stay as written. This section records what changed
in September 2026; `requirements.md` and `units.md` carry the current scope.

- **43 briefs, unchanged** as the definition of the system. What changed is who satisfies them.
- **12 are satisfied by the review loop** under `reviews/` — the engine was built June–September
  2026 while reviewing, in pre-merge mode. Per-brief status: the guide's
  "Implementation status (2026-09)" table (`docs/agent-systems/bug-hunter-build-guide.md`).
  **31 remain**: 16 missing, 15 partial.
- **Bolts 085 and 086 are retired** — satisfied by that engine, and removed rather than marked
  `complete`, because `standards/bolt-process.md` allows `complete` only after a bolt's first
  discovery pass. Their stories stay under `units/001-phase-1-skeleton/`, each carrying the file
  that satisfies it; unit 001 is complete by equivalence.
- **8 bolts remain, 087–094**, in the order ruled by the owner and written up in
  `docs/agent-systems/integration-contract.md` §7:

  **087** (trust upgrades: `tool-ingest`, the risk score + reachability weight, execution proof,
  moved/fixed detection) → **088** (the Map slot: `app-mapping`, `code-index`, `reachability`,
  the budget unit) → **089 ∥ 090** (specialists, both waiting on the Map slot) → **092** (learn &
  measure) → **093** (remediation hand-off) → **091** (oracle tier, last, ⛔ gated on the
  knowledge builder's `ledger-query`); **094** (optional integration) hangs off 092 and is
  listed after 091 in §7, adoption-gated.

- **The oracle tier is the last bolt of unit 003**, not a unit of its own: bolt 091 keeps
  `unit: 003-phase-3-breadth-and-scale`, runs after 089/090, and is the only piece of this intent
  that cannot start until the knowledge builder exists. Nothing waits on it.
- Standing-sweep mode — the second of the engine's two modes, a scheduled pass over all of
  `main` — has no bolt of its own yet (§7, step 5).
- Construction method: a piece that extends the review loop is built as a script or skill in that
  tree at the seam its story names, with a test under `reviews/lib/tests`; skill-creator remains
  the builder for a new standalone skill (`intent-lookup`, `issue-sync`, `ci-gate`).
- **2026-09-03 (later): 085/086 restored as verification bolts at the owner's direction.** The
  "Bolts 085 and 086 are retired" bullet above stays as the record of what was decided earlier
  that day. The owner then ruled that deleting them was wrong: it left a hole in the record, and
  the equivalence claim was never checked. Both bolts come back with the same ids and the same
  stories, `status: planned`, as **verification bolts** — they confirm the claim story by story,
  record the verdict in a `test-walkthrough.md`, and complete through the normal process. They
  build nothing; a gap they find becomes a new story for a construction bolt. Bolts 085/086
  verify the seven Phase 1 stories — six of the twelve satisfied briefs; the other six
  (orchestrator wiring 11b, security-auditor, concurrency-auditor, bug-lifecycle,
  fix-verification, mailbox scan 31b) are verified in the plan stage of the construction bolt
  that carries them. Ten bolts are therefore scheduled, not eight: 085 → 086 first, then 087–094
  in the order above.
