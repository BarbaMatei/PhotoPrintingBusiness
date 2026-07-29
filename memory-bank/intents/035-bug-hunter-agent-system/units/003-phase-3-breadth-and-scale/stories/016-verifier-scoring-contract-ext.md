---
id: 016-verifier-scoring-contract-ext
unit: 003-phase-3-breadth-and-scale
intent: 035-bug-hunter-agent-system
status: ready
priority: must
created: 2026-06-10T10:40:14Z
assigned_bolt: 091-phase-3-oracle-grounding
implemented: false
---

# Story: 016-verifier-scoring-contract-ext (guide Prompt 24c — EXTENSION)

## User Story

**As** the Verify stage
**I want** contract contradictions to raise confidence — and model-prior-only "logic bugs" tagged as judgment calls
**So that** documented-spec violations rank like the strong evidence they are, second only to a dynamic repro

## Acceptance Criteria

- [ ] **Given** Prompt 24c, **When** applied, **Then** `bug-verifier` is **re-opened and step (5) extended** via skill-creator, and the brief's three test prompts pass (contract-contradicting finding scores higher than an equivalent contract-less one; intent-unconfirmed finding still reported, tagged; dynamically-confirmed still outranks contract-corroborated)
- [ ] **Given** a finding contradicting a documented contract (via `intent-lookup`), **When** assigning confidence, **Then** confidence rises (real contract violation = strong evidence, second only to dynamic repro)
- [ ] **Given** a "logic bug" backed **only** by the model's prior with no governing contract, **When** assessed, **Then** it is tagged `intent-unconfirmed` and held at Low/Medium — reported, but marked a judgment call
- [ ] **Given** scoring, **When** flowing through, **Then** the effect rides the existing `confidence` factor — **no formula change** in `severity-scoring`; **Given** NFR-2, **Then** Prompt 10's and Prompt 8/14b's original tests still pass

## Technical Notes

- ⚠️ This is an **extension brief**: paste **Prompt 24c** from
  `docs/agent-systems/bug-hunter-build-guide.md` into the **skill-creator** skill (`Skill`
  tool → `skill-creator:skill-creator`) to re-open `bug-verifier` (and confirm the
  scoring path needs no change). Re-run prior tests after. STOP and report if
  skill-creator is unavailable.
- Weighting guards per the v3.1 brief (Integration Contract §2): a `contested`
  contract raises NO confidence (advisory until a human resolves it), and a contract
  with `verification: not-checked` corroborates more weakly than an `entailed` one.

## Dependencies

### Requires
- 014-intent-lookup; bug-verifier (bolt 087); 004-severity-scoring-reachability-ext (bolt 088)

### Enables
- Trustworthy contract-grounded rankings in reports

## Out of Scope

- Hunter-side contradiction surfacing (24b).
