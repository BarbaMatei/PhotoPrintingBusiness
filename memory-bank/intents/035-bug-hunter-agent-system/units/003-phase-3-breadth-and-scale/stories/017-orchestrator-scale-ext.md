---
id: 017-orchestrator-scale-ext
unit: 003-phase-3-breadth-and-scale
intent: 035-bug-hunter-agent-system
status: ready
priority: must
created: 2026-06-10T10:40:14Z
assigned_bolt: 091-phase-3-oracle-grounding
implemented: false
---

# Story: 017-orchestrator-scale-ext (guide Prompt 24d — EXTENSION)

## User Story

**As** the six-slot pipeline at Phase 3 scale
**I want** the orchestrator's slots extended: live map, specialist dispatch, reachability + oracle into Verify, clustering into Triage, and cost control
**So that** runs see the whole codebase affordably — without restructuring anything

## Acceptance Criteria

- [ ] **Given** Prompt 24d, **When** applied, **Then** `orchestrator` is **re-opened and extended at its slots** via skill-creator, and the brief's three test prompts pass (diff-only run that says so; security hunter dispatched only on high-risk flows; contract-contradiction reported with contract cited)
- [ ] **Given** the slots, **When** extended, **Then**: **Map** refreshes `app-mapping` + `code-index` at open; **Hunt** dispatches the built specialists (flow-tracer, file-sweeper, security-auditor, dependency-audit, config-auditor, concurrency-auditor if built) prioritizing high-risk flows, with `intent-lookup` available (general-hunter demoted to fallback); **Verify** receives `reachability` + `intent-lookup` results; **Triage** runs `root-cause-clustering` after dedup, before scoring; **Report** keeps the floor
- [ ] **Given** **cost control**, **When** running, **Then** a per-run budget applies — **unit: hunter dispatches + sandbox sessions; on exhaustion stop dispatching, run Triage/Report/Close normally, record "stopped on budget" in coverage + run summary (v3.3)**; **incremental scanning** is the default (changed-since-last-commit via `git-revision-tracking`, occasional full sweeps); work is ordered **cheap-first** (deterministic tools before LLM hunters; sandbox only for survivors); sandbox time + concurrent hunters capped
- [ ] **Given** dispatch (v3.3), **When** naming specialists, **Then** the exact created skill names are used (`flow-tracer-agent`, `file-sweeper-agent`, `security-auditor-agent`, `dependency-audit-agent`, `config-auditor-agent`, `concurrency-auditor-agent`)
- [ ] **Given** the oracle bootstrap window (v3.4, review I8), **When** opening a run, **Then** the envelope's `oracle_coverage` is read and a warning is emitted while `backfill_complete` is false ("oracle backfill incomplete (N/M intents)"); affected findings tag `intent-unconfirmed: oracle-incomplete`, never `: no-contract`
- [ ] **Given** NFR-2, **Then** Prompts 7 and 11b's original tests still pass

## Technical Notes

- ⚠️ This is an **extension brief**: paste **Prompt 24d** from
  `docs/agent-systems/bug-hunter-build-guide-v3.6.md` into the **skill-creator** skill (`Skill`
  tool → `skill-creator:skill-creator`) to re-open `orchestrator`. Re-run prior
  orchestrator tests after. STOP and report if skill-creator is unavailable.
- Concurrent hunters write via `ledger-io` staging files — the single-writer merge
  from Prompt 1 is what makes the parallel dispatch safe.
- Per the v3.1 brief: at run open, capture the knowledge ledger's `as_of_commit` and
  warn when the oracle is stale beyond the threshold (Integration Contract §5) before
  relying on oracle results.

## Dependencies

### Requires
- Everything in bolts 088–090 + 014/015/016 (this bolt)

### Enables
- The Phase 3 milestone: full-breadth, budget-controlled, oracle-grounded runs

## Out of Scope

- The Learn slot (29b, Phase 4).
