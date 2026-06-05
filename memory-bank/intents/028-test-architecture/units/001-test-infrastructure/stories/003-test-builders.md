---
id: 003-test-builders
unit: 001-test-infrastructure
intent: 028-test-architecture
status: draft
priority: should
created: 2026-06-05T09:30:00Z
assigned_bolt: 062-test-infrastructure
implemented: false
---

# Story: 003-test-builders

## User Story

**As a** developer writing tests
**I want** fluent Builders for the most-used entities
**So that** I stop inlining ad-hoc user/order/cart seeding in every test file

## Acceptance Criteria

- [ ] **Given** `tests/Builders/`, **When** created, **Then** it provides fluent builders for the 6 most-used entities (User, Order, CartItem, Invoice, Upload + one TBD)
- [ ] **Given** a builder, **When** used, **Then** it reads like `new UserBuilder().Confirmed().WithEmail("x@y.com").Build()`
- [ ] **Given** services with new `TimeProvider` params, **When** seeded via Builders, **Then** the builder hides the constructor signature and sets the clock
- [ ] **Given** refactored tests, **When** run, **Then** the suite is green

## Technical Notes

- Use the existing `AuthFactory.SeedConfirmedUserAsync` pattern as a template.

## Dependencies

### Requires
- 002-shared-test-application-factory

### Enables
- 004-reclassify-misnamed-unit-tests

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Entity needs an unusual state | Builder exposes a fluent setter; defaults stay sensible |

## Out of Scope

- Reclassification (next story).
