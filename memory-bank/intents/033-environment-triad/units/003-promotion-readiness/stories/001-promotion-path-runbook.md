---
id: 001-promotion-path-runbook
unit: 003-promotion-readiness
intent: 033-environment-triad
status: draft
priority: should
created: 2026-06-05T12:40:00Z
assigned_bolt: 075-promotion-readiness
implemented: false
---

# Story: 001-promotion-path-runbook

## User Story

**As a** maintainer
**I want** a repeatable dev→prod promotion runbook written as readiness documentation
**So that** a future deployment (Phase 6) is safe and predictable — without performing one now

## Acceptance Criteria

- [ ] **Given** the triad outputs, **When** the runbook is written, **Then** it sequences the dev-env → prod promotion as ordered, repeatable steps: config swap (config map), secret swap test→live (secrets matrix), image tag/promote (existing `deploy.yml` flow), migration apply, seed policy application, smoke verification
- [ ] **Given** the runbook, **When** reviewed, **Then** it **cross-references** the config map (unit 001), secrets matrix + seeding policy (unit 002), and `deploy.yml` rather than restating them
- [ ] **Given** the migration caveat, **When** the runbook lists preconditions, **Then** it records "verify migrations against real Postgres before the first prod apply" and links DEPLOYMENT.md §7
- [ ] **Given** the runbook, **When** stored, **Then** it lives at `docs/environments/promotion-path.md` (Q4) and reads as readiness — every step labelled as *what a future promotion would do*, not an instruction to run now

## Technical Notes

- This is documentation only; it must not invoke `deploy.yml`, provision a host, or change any pipeline.
- Keep steps tool-agnostic where possible so the runbook survives a later platform choice.

## Dependencies

### Requires
- unit 001 (config map + compose)
- unit 002 (secrets matrix + seeding policy)

### Enables
- 002-deployment-deferral-note

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Reader treats it as a deploy-now guide | The Phase-6 deferral note (story 002) + per-step labelling prevent this |

## Out of Scope

- Performing any promotion or deployment (Phase 6).
