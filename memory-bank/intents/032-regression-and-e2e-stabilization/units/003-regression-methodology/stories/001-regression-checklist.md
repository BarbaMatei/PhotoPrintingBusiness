---
id: 001-regression-checklist
unit: 003-regression-methodology
intent: 032-regression-and-e2e-stabilization
status: draft
priority: should
created: 2026-06-05T11:40:00Z
assigned_bolt: 072-regression-methodology
implemented: false
---

# Story: 001-regression-checklist

## User Story

**As a** maintainer running a stabilization pass
**I want** a regression checklist mapping every shipped feature to a verifiable check
**So that** a full pass is repeatable and nothing shipped is silently un-verified

## Acceptance Criteria

- [ ] **Given** the shipped inventory (story-index: intents 001–024 + any shipped 025–031 bolts), **When** the checklist is written, **Then** each shipped feature has at least one check
- [ ] **Given** each check, **When** tagged, **Then** it is marked one of: automated-by-e2e (cross-linking the unit-002 spec), automated-by-integration (cross-linking the test class), or manual
- [ ] **Given** the document, **When** stored, **Then** it lives at `docs/testing/regression-checklist.md` (Q3) and is grouped by intent for easy scanning
- [ ] **Given** the unit-002 e2e coverage, **When** the checklist is authored, **Then** checks covered by those specs are explicitly tagged automated-by-e2e so the manual surface is visible and shrinking

## Technical Notes

- Source the shipped inventory from `memory-bank/story-index.md`; cross-reference `status-integrity.cjs` to avoid drift.
- The checklist is durable scaffolding; the executed pass (story 002) records a point-in-time result against it.

## Dependencies

### Requires
- unit 002 journey specs (so automated-by-e2e tags are accurate)

### Enables
- 002-execute-regression-baseline

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| A shipped feature has no test at all | Tagged `manual` with a note recommending future automation |

## Out of Scope

- Executing the pass (story 002); fixing defects (story 003 triages them).
