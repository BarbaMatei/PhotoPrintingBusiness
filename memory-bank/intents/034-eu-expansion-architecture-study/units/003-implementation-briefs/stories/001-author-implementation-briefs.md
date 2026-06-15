---
id: 001-author-implementation-briefs
unit: 003-implementation-briefs
intent: 034-eu-expansion-architecture-study
status: ready
priority: must
created: 2026-06-05T12:57:50Z
assigned_bolt: 084-implementation-briefs
implemented: false
---

# Story: 001-author-implementation-briefs

## User Story

**As the** inception agent for the next cycle
**I want** the ADR translated into concrete, ready-to-consume implementation brief(s)
**So that** I can create the real implementation intent(s) for EU readiness with no missing context

## Acceptance Criteria

- [ ] **Given** the ADR (D3), **When** the brief(s) are authored, **Then** they translate the decision into concrete **readiness requirements** (which seams to prepare, in what order, with acceptance criteria) — **seam preparation only, no translations**
- [ ] **Given** the deliverable, **When** complete, **Then** at least one brief exists at `docs/planning/i18n-readiness-brief-<date>.md`, authored in the same style as `docs/planning/eu-expansion-research-brief-2026-06-05.md`
- [ ] **Given** the decision splits the work, **When** authoring, **Then** the work is split into multiple briefs (e.g. infra-readiness / i18n-seam-readiness / multi-currency-readiness) as needed
- [ ] **Given** the brief(s), **When** handed to inception, **Then** they are complete enough to create the implementation intent(s) with no additional context
- [ ] **Given** roadmap sequencing, **When** authoring, **Then** the brief(s) state explicitly that deployment remains Phase 6 (this is Phase 5 readiness)

## Technical Notes

- Docs only — `simple-construction-bolt`, but the output is documentation, not code.
- Mirror the source brief's structure (context, tracks/requirements, deliverables, acceptance criteria, constraints).
- Pull the retrofit sizing from T7 and the costed scope from the chosen bundle.

## Dependencies

### Requires
- 002-owner-decision-adr (the ADR)

### Enables
- A future implementation intent (created by feeding D4 back into inception)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| ADR chose the partner-fallback bundle | Brief reflects the partner-model seams, not RO-only assumptions |
| Scope too large for one intent | Split into ordered briefs with explicit dependencies |

## Out of Scope

- The translations themselves; any production code; deployment.
