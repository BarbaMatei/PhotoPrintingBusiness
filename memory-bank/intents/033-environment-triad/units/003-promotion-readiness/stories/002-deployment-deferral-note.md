---
id: 002-deployment-deferral-note
unit: 003-promotion-readiness
intent: 033-environment-triad
status: draft
priority: should
created: 2026-06-05T12:40:00Z
assigned_bolt: 075-promotion-readiness
implemented: false
---

# Story: 002-deployment-deferral-note

## User Story

**As a** maintainer (and any future AI session)
**I want** an explicit note that deployment is deferred to roadmap Phase 6
**So that** nobody — human or agent — mistakes this infrastructure-readiness work for a signal to deploy

## Acceptance Criteria

- [ ] **Given** the roadmap (ai-workflow-review §6), **When** the note is written, **Then** it states plainly that deployment is the **final** phase (Phase 6), that this intent prepared readiness only, and that standing up the dev-env host and any prod deploy are out of scope until then
- [ ] **Given** the note, **When** placed, **Then** it is cross-linked from `docs/DEPLOYMENT.md` and from the promotion runbook (story 001), so the readiness/execution boundary is visible from both
- [ ] **Given** the note, **When** read by an agent, **Then** it explicitly counters the default "deploy next" assumption that ai-workflow-review §6 warns about
- [ ] **Given** the note, **When** complete, **Then** it lists the preconditions still outstanding before any deploy (Phase-3 stabilization done, Phase-5 EU-readiness considered, migration caveat resolved)

## Technical Notes

- Short and unambiguous; its whole job is to hold the readiness/execution line.
- Mirror the framing language from ai-workflow-review §6 so it reads as the owner's intent.

## Dependencies

### Requires
- 001-promotion-path-runbook

### Enables
- None (terminal story of the intent)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Future reader skims the runbook only | The runbook links to this note up top; the boundary is unmissable |

## Out of Scope

- Any deployment action.
