---
id: 004-delivery-step-locker-selector
unit: 002-ui-scaling-and-e2e-ui
intent: 030-ui-scaling-and-e2e
status: draft
priority: should
created: 2026-06-05T09:30:00Z
assigned_bolt: 067-ui-scaling-and-e2e-ui
implemented: false
---

# Story: 004-delivery-step-locker-selector

## User Story

**As a** frontend developer
**I want** the 382-LOC `delivery-step.ts` to extract a `locker-selector` component
**So that** the checkout delivery step is leaner and the locker UI is reusable

## Acceptance Criteria

- [ ] **Given** `delivery-step.ts` (382 LOC), **When** refactored, **Then** a `locker-selector.component.ts` is extracted (it already imports `locker-map.ts`)
- [ ] **Given** the extraction, **When** the checkout e2e runs, **Then** the guest-checkout path still passes
- [ ] **Given** the change, **When** Vitest runs, **Then** it passes

## Technical Notes

- Keep the Sameday locker map integration intact.

## Dependencies

### Requires
- 001-base-api-service

### Enables
- None

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| No locker selected | Existing validation preserved |

## Out of Scope

- Other pages (separate stories).
