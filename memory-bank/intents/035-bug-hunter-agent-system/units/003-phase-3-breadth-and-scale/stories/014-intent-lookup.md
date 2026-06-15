---
id: 014-intent-lookup
unit: 003-phase-3-breadth-and-scale
intent: 035-bug-hunter-agent-system
status: ready
priority: must
created: 2026-06-10T10:40:14Z
assigned_bolt: 091-phase-3-oracle-grounding
implemented: false
---

# Story: 014-intent-lookup (guide Prompt 24, NEW in v3 — the oracle read)

## User Story

**As** the system's grounding in real intent
**I want** the knowledge ledger's contracts retrievable per location/flow/symbol
**So that** a genuine spec violation is distinguishable from the model's own opinion of "correct"

## Acceptance Criteria

- [ ] **Given** Prompt 24, **When** built, **Then** skill `intent-lookup` exists, created via skill-creator, and the brief's three test prompts pass (contracts governing a file; superseded contract returned flagged; current-state/advisory entries never returned as contracts)
- [ ] **Given** a target, **When** querying the knowledge builder's **`ledger-query`** interface, **Then** relevant contracts return as `{statement, contract_kind, confidence, status, source_ref}`
- [ ] **Given** the authority rules, **When** classifying, **Then** only `intent_contracts` are treated as authority (never current-state map or advisory entries); **superseded**, **retracted** (v3.2), and not-yet-`done` contracts return tagged so consumers don't over-rely
- [ ] **Given** boundaries, **When** operating, **Then** strictly read-only on the knowledge ledger
- [ ] **Given** a degraded oracle (v3.3), **When** the envelope carries **`tamper_warning`**, **Then** results are usable context but never confidence-raising authority (corroborate like `verification: not-checked`) until the operator reconciles

## Technical Notes

- ⚠️ Build by pasting **Prompt 24** from `docs/agent-systems/bug-hunter-build-guide-v3.6.md` into the
  **skill-creator** skill (`Skill` tool → `skill-creator:skill-creator`); run the
  brief's three test prompts and fix before proceeding. STOP and report if
  skill-creator is unavailable.
- ⛔ **Cross-system gate (requirements D6)**: the knowledge builder's `ledger-query`
  interface is specified in `docs/agent-systems/integration-contract-v1.5.md` (§2 envelope — this
  skill's five expected fields are its required subset; §3 flow identity) and built
  per `docs/agent-systems/knowledge-builder-build-guide-v3.5.md`. Bolt 091 runs after the knowledge
  builder's Phases 1–2 (contract §7). Do NOT stub the interface silently — absence is
  a bolt-gating fact, not an implementation detail.
- Per the v3.1 brief: record the oracle's `as_of_commit` (from the query envelope)
  into run metadata, and never treat a `contested` contract as live authority.

## Dependencies

### Requires
- 002-code-index (location resolution); the knowledge ledger's query interface (⛔ external)

### Enables
- 015-hunters-contract-ext, 016-verifier-scoring-contract-ext, 017-orchestrator-scale-ext

## Out of Scope

- Building/maintaining the knowledge ledger (sibling project).
