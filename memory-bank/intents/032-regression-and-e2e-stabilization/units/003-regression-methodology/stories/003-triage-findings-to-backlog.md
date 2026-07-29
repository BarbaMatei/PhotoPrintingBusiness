---
id: 003-triage-findings-to-backlog
unit: 003-regression-methodology
intent: 032-regression-and-e2e-stabilization
status: draft
priority: should
created: 2026-06-05T11:40:00Z
assigned_bolt: 072-regression-methodology
implemented: false
---

# Story: 003-triage-findings-to-backlog

## User Story

**As a** maintainer
**I want** every regression/e2e finding triaged into the backlog
**So that** defects are tracked and fed back into the build pipeline, never lost

## Acceptance Criteria

- [ ] **Given** the recorded baseline (story 002), **When** triage runs, **Then** each failure/known-issue is linked to exactly one of: a new bolt, an existing planned bolt, or a `KNOWN_FAILURES.md` entry (the file from bolt 057)
- [ ] **Given** a finding routed to `KNOWN_FAILURES.md`, **When** recorded, **Then** it states the symptom, the why-accepted, and the trigger to revisit
- [ ] **Given** the triage, **When** complete, **Then** no finding is left unrouted (the baseline's fail/known-issue rows each have a backlog reference)
- [ ] **Given** the story-index conventions, **When** new bolts are proposed from findings, **Then** they follow NNN-kebab numbering and are recorded for the owner to approve (not auto-created here unless trivial)

## Technical Notes

- Prefer KNOWN_FAILURES.md for accepted-for-now issues; propose bolts only for real defects.
- If bolt 057 (which introduces KNOWN_FAILURES.md) has not shipped, note the dependency and stage the entries for when it lands.

## Dependencies

### Requires
- 002-execute-regression-baseline
- bolt 057 (KNOWN_FAILURES.md) — soft dependency

### Enables
- Future stabilization waves (re-run the checklist cheaply)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Finding is a flaky test, not a product bug | Routed to a test-stability fix, not a product bolt |
| KNOWN_FAILURES.md absent | Entries staged + dependency on bolt 057 noted |

## Out of Scope

- Actually fixing the routed defects (they become their own bolts).
