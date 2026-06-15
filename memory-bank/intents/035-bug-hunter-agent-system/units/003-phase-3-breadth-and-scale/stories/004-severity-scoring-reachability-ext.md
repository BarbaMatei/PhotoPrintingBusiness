---
id: 004-severity-scoring-reachability-ext
unit: 003-phase-3-breadth-and-scale
intent: 035-bug-hunter-agent-system
status: ready
priority: must
created: 2026-06-10T10:40:14Z
assigned_bolt: 088-phase-3-map-and-reachability
implemented: false
---

# Story: 004-severity-scoring-reachability-ext (guide Prompt 14b — EXTENSION)

## User Story

**As** the risk formula
**I want** reachability as the third factor: risk = severity × confidence × reachability
**So that** an unreachable Critical ranks below a reachable High

## Acceptance Criteria

- [ ] **Given** Prompt 14b, **When** applied, **Then** the existing `severity-scoring` skill is **re-opened and extended at its planned seam** via skill-creator, and the brief's test passes (reachable High vs unreachable Critical — order flips appropriately)
- [ ] **Given** the new factor, **When** scoring, **Then** weights are explicit and tunable (e.g. reachable 1.0 / unknown 0.4 / unreachable 0.1) and the **framework-aware unknown weight** from `reachability` is honored so dynamic stacks aren't flattened
- [ ] **Given** the rationale, **When** updated, **Then** it explains the unreachable-Critical-below-reachable-High principle
- [ ] **Given** the additive rule (NFR-2), **When** done, **Then** Prompt 8's original test prompts still pass

## Technical Notes

- ⚠️ This is an **extension brief**: paste **Prompt 14b** from
  `docs/agent-systems/bug-hunter-build-guide-v3.6.md` into the **skill-creator** skill (`Skill`
  tool → `skill-creator:skill-creator`) to re-open `severity-scoring`. Run its test
  AND re-run Prompt 8's tests. STOP and report if skill-creator is unavailable.
- Small change by design — the Phase 2 formula was built to extend.

## Dependencies

### Requires
- 003-reachability (this unit); severity-scoring (bolt 087)

### Enables
- Honest risk ordering across the whole report; 24c stacks on the same formula via
  the confidence input

## Out of Scope

- Contract-corroborated confidence (24c).
