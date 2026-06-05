---
id: 001-no-split-adr
unit: 001-layering-foundation
intent: 027-architectural-layering
status: draft
priority: could
created: 2026-06-05T09:30:00Z
assigned_bolt: 059-layering-foundation
implemented: false
---

# Story: 001-no-split-adr

## User Story

**As a** future contributor reading the layer rules
**I want** an ADR explaining why we layer with folders, not four csproj projects
**So that** I don't "helpfully" split the solution and add ceremony for no benefit

## Acceptance Criteria

- [ ] **Given** the ADR, **When** written, **Then** it states the decision, the rejected 4-project alternative, and the load-bearing reasons (single deployable; EF migrations need Design reachable from the DbContext project; tests would reference 4 csproj; 1–2 dev team)
- [ ] **Given** the ADR, **When** read, **Then** it lists revisit triggers (team > 4 devs; a domain ships as a service; a domain's deps don't belong in the package)
- [ ] **Given** `system-architecture.md`, **When** updated, **Then** it links to the ADR

## Technical Notes

- Ship first so the layering PRs can reference it.

## Dependencies

### Requires
- None

### Enables
- 002-domain-layer-extraction (and the rest of P21)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Reader wants microservices later | ADR points to the namespace boundary as the future cut line |

## Out of Scope

- Any code move.
