---
id: 002-execute-regression-baseline
unit: 003-regression-methodology
intent: 032-regression-and-e2e-stabilization
status: draft
priority: must
created: 2026-06-05T11:40:00Z
assigned_bolt: 072-regression-methodology
implemented: false
---

# Story: 002-execute-regression-baseline

## User Story

**As a** maintainer
**I want** one full regression pass executed and recorded against the current build
**So that** the application has a dated, provable stability baseline before Phase 4

## Acceptance Criteria

- [ ] **Given** the checklist (story 001), **When** the pass is executed, **Then** every check is run (automated checks via the suites; manual checks via the running app) and a per-check result is recorded: pass / known-issue / fail
- [ ] **Given** the result, **When** recorded, **Then** it is stamped with the date and the build SHA it was run against
- [ ] **Given** the unit-002 e2e suite, **When** the baseline is taken, **Then** the e2e result (green across 3 consecutive runs) is referenced as the automated portion of the baseline
- [ ] **Given** the recorded baseline, **When** reviewed, **Then** it gives a clear go/known-issues verdict on Phase-3 stabilization

## Technical Notes

- Run against the real-Postgres docker-compose boot (unit 001) so the baseline reflects production-shaped behaviour.
- The recorded baseline can live as a dated section in `docs/testing/regression-checklist.md` or a sibling `regression-baseline-YYYY-MM-DD.md`.

## Dependencies

### Requires
- 001-regression-checklist
- unit 002 (e2e suite green)

### Enables
- 003-triage-findings-to-backlog

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| A check fails during the pass | Recorded as fail; routed to triage (story 003), not patched inline |
| The InMemory-vs-Postgres parity gap surfaces | Recorded as a known-issue; routed to the appropriate backlog item |

## Out of Scope

- Fixing the failures (story 003 routes them to the backlog).
