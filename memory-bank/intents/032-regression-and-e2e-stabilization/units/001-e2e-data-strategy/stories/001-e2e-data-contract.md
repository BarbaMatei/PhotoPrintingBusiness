---
id: 001-e2e-data-contract
unit: 001-e2e-data-strategy
intent: 032-regression-and-e2e-stabilization
status: draft
priority: must
created: 2026-06-05T11:30:00Z
assigned_bolt: 070-e2e-data-strategy
implemented: false
---

# Story: 001-e2e-data-contract

## User Story

**As a** developer authoring e2e journey specs
**I want** a single documented contract describing every seeded entity the suite relies on
**So that** specs reference stable, known data instead of hand-creating it ad hoc

## Acceptance Criteria

- [ ] **Given** the existing `ProductCatalogSeed` and `DevDataSeed`, **When** the contract is written, **Then** it lists every entity specs depend on: product slugs/IDs, size/finish/pricing-tier values, the seeded admin credentials, and seeded Easybox locker IDs
- [ ] **Given** the contract, **When** a spec needs catalog/admin/locker data, **Then** it reads identifiers from the contract (a single fixtures module), not from inline literals scattered across specs
- [ ] **Given** the contract document, **When** the seed changes, **Then** the contract is the one place that must be updated (it is the source of truth)
- [ ] **Given** payment journeys, **When** the contract documents test-mode config, **Then** it records the Stripe test cards and EuPlatesc test-IPN approach to be used (detailed in story 003)

## Technical Notes

- Reuse the existing `--seed` (`ProductCatalogSeed`) and `--seed-dev` (`DevDataSeed`) modes — do not invent a new seed path.
- The contract lives alongside the e2e module (e.g. `e2e/fixtures/data-contract.md` + a typed `seed-data.ts`).

## Dependencies

### Requires
- bolt 062 (Builders) and bolt 066 (Playwright module) must exist

### Enables
- 002-builder-backed-fixtures
- All unit-002 journey specs

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Seed adds/removes a product | Contract + typed module updated in lockstep; specs fail loudly if a slug is missing |

## Out of Scope

- The fixtures implementation (story 002) and payment fixtures (story 003).
