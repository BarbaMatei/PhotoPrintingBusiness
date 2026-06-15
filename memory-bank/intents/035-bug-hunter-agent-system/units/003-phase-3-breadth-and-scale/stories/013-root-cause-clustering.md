---
id: 013-root-cause-clustering
unit: 003-phase-3-breadth-and-scale
intent: 035-bug-hunter-agent-system
status: ready
priority: must
created: 2026-06-10T10:40:14Z
assigned_bolt: 090-phase-3-specialists-b
implemented: false
---

# Story: 013-root-cause-clustering (guide Prompt 23)

## User Story

**As** the Triage stage
**I want** findings sharing one underlying cause grouped into a single multi-location bug
**So that** twelve symptoms of one unchecked helper read as one fix, not twelve bugs

## Acceptance Criteria

- [ ] **Given** Prompt 23, **When** built, **Then** skill `root-cause-clustering` exists, created via skill-creator, and the brief's three test prompts pass (twelve null-derefs → one clustered record; two unrelated same-file bugs stay separate; clustered record shows all locations)
- [ ] **Given** candidates after dedup, **When** clustering, **Then** shared-root-cause groups (same unchecked function called everywhere; same missing validation across endpoints; one data-flow origin) become ONE record with multiple `location` entries and a single root-cause `developer_detail`
- [ ] **Given** the conservatism rule, **When** uncertain, **Then** distinct defects stay separate — only genuinely shared causes cluster

## Technical Notes

- ⚠️ Build by pasting **Prompt 23** from `docs/agent-systems/bug-hunter-build-guide-v3.6.md` into the
  **skill-creator** skill (`Skill` tool → `skill-creator:skill-creator`); run the
  brief's three test prompts and fix before proceeding. STOP and report if
  skill-creator is unavailable.
- Relies on `bug-documentation`'s multi-location support (story 002 of unit 001).

## Dependencies

### Requires
- bug-documentation (bolt 085)

### Enables
- orchestrator Triage wiring (24d)

## Out of Scope

- Dedup itself (Prompt 3); scoring.
