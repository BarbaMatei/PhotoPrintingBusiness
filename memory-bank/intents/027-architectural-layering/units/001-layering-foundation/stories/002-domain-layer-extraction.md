---
id: 002-domain-layer-extraction
unit: 001-layering-foundation
intent: 027-architectural-layering
status: draft
priority: could
created: 2026-06-05T09:30:00Z
assigned_bolt: 059-layering-foundation
implemented: false
---

# Story: 002-domain-layer-extraction

## User Story

**As a** developer
**I want** the pure-functional helpers moved into a `Domain/` namespace
**So that** domain logic is isolated from infrastructure and easy to unit-test

## Acceptance Criteria

- [ ] **Given** the 6 pure types (`OrderStatusMachine`, `VatCalculator`, `StorageKeys`, `InvoiceNumber`, `PromotionOutcome`, `PurgeOutcome`), **When** moved to `Domain/<area>/`, **Then** namespaces update and `using static` references find/replace mechanically
- [ ] **Given** `Domain/`, **When** the analyzer runs, **Then** it forbids references to EF Core / `System.Net.Http`
- [ ] **Given** the move, **When** built/tested, **Then** CI is green and `Add-Migration` shows empty diff

## Technical Notes

- This is first-pass P16, folded into P21 as PR1 of the layering sequence.

## Dependencies

### Requires
- 001-no-split-adr

### Enables
- 003-infrastructure-layer

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| A "pure" type secretly touches infra | Analyzer flags it; refactor or leave in Application |

## Out of Scope

- Moving DbContext/services (later PRs).
