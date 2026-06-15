---
id: 006-orchestrator-learn-ext
unit: 004-phase-4-learn-and-measure
intent: 035-bug-hunter-agent-system
status: ready
priority: should
created: 2026-06-10T10:40:14Z
assigned_bolt: 092-phase-4-learn-and-measure
implemented: false
---

# Story: 006-orchestrator-learn-ext (guide Prompt 29b — EXTENSION)

## User Story

**As** the six-slot pipeline
**I want** the Learn slot (empty since Phase 1) pointed at the Curator
**So that** every run ends by learning — and activated suppressions visibly reduce repeats next run

## Acceptance Criteria

- [ ] **Given** Prompt 29b, **When** applied, **Then** `orchestrator` is **re-opened** via skill-creator with exactly one change — the Learn slot calls `curator-agent` at the end of each run after reporting — and the brief's two test prompts pass (run ends by curating; activated patterns reduce repeats next run)
- [ ] **Given** NFR-2, **When** done, **Then** Prompts 7 / 11b / 24d's original tests still pass (no other slot touched)

## Technical Notes

- ⚠️ This is an **extension brief**: paste **Prompt 29b** from
  `docs/agent-systems/bug-hunter-build-guide.md` into the **skill-creator** skill (`Skill`
  tool → `skill-creator:skill-creator`) to re-open `orchestrator`. Re-run prior
  orchestrator tests after. STOP and report if skill-creator is unavailable.
- Smallest brief in the system — deliberately: the Learn slot existed since Phase 1
  precisely so this is a one-line fill, not a redesign.

## Dependencies

### Requires
- 005-curator-agent; orchestrator (bolts 086/087/091 states)

### Enables
- The Phase 4 milestone: self-improving runs

## Out of Scope

- Anything beyond pointing the slot.
