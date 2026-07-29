---
id: 001-multi-replica-readiness-doc
unit: 003-architecture-and-standards-docs
intent: 026-observability-boot-manifest
status: draft
priority: could
created: 2026-06-05T09:30:00Z
assigned_bolt: 057-architecture-and-standards-docs
implemented: false
---

# Story: 001-multi-replica-readiness-doc

## User Story

**As an** operator planning future scale-out
**I want** one doc consolidating the in-process-state reasoning
**So that** I don't have to read five ADRs to understand what blocks multi-replica

## Acceptance Criteria

- [ ] **Given** ADRs 010/013/015/016/023, **When** the doc is written, **Then** it has one section per concern (promotion queue, token caches, AWB dedupe, status CAS, ANAF dispatch), each stating "today: X / future bolt 046: Y"
- [ ] **Given** each section, **When** read, **Then** it cites the originating ADR
- [ ] **Given** `system-architecture.md`, **When** updated, **Then** it links to the new doc

## Technical Notes

- Documentation only. Aligns with [[project_bolt_046_deprioritized]] — do NOT implement the Redis backplane.

## Dependencies

### Requires
- None

### Enables
- None

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Reader assumes Redis is committed | Doc explicitly states "future bolt 046, deprioritized" |

## Out of Scope

- Any code change.
