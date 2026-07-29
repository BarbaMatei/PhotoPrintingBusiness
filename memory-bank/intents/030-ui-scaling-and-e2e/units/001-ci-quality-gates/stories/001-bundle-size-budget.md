---
id: 001-bundle-size-budget
unit: 001-ci-quality-gates
intent: 030-ui-scaling-and-e2e
status: draft
priority: should
created: 2026-06-05T09:30:00Z
assigned_bolt: 066-ci-quality-gates
implemented: false
---

# Story: 001-bundle-size-budget

## User Story

**As a** developer
**I want** CI to fail when the bundle exceeds a budget
**So that** main.bundle.js doesn't silently bloat as features ship

## Acceptance Criteria

- [ ] **Given** `angular.json`, **When** budgets are set, **Then** `initial` has `maximumWarning: 500kB` / `maximumError: 750kB` and `anyComponentStyle` has `maximumError: 4kB`
- [ ] **Given** a build over the error threshold, **When** CI runs, **Then** it fails
- [ ] **Given** the current bundle size, **When** the budget is set, **Then** it is just above current with a documented reduction target

## Technical Notes

- Measure current size first; set realistically to avoid an immediately-red CI.

## Dependencies

### Requires
- None

### Enables
- 002-playwright-e2e-smoke-tests (same CI workflow surface)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Lazy chunk bloats | Add a per-chunk budget if needed |

## Out of Scope

- The e2e tests (next story).
