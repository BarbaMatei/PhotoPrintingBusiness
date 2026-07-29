---
id: 002-home-page-breakup
unit: 002-ui-scaling-and-e2e-ui
intent: 030-ui-scaling-and-e2e
status: draft
priority: should
created: 2026-06-05T09:30:00Z
assigned_bolt: 067-ui-scaling-and-e2e-ui
implemented: false
---

# Story: 002-home-page-breakup

## User Story

**As a** frontend developer
**I want** the 951-LOC `home-page.ts` split into a thin container + child components
**So that** the largest file in the project becomes maintainable

## Acceptance Criteria

- [ ] **Given** `home-page.ts` (951 LOC), **When** split, **Then** it becomes a ~100-LOC container + `hero-section`, `value-props`, `pricing-teaser`, `trust-strip`, `cta-banner` components under `home/components/`
- [ ] **Given** the breakup, **When** the page renders, **Then** it is visually unchanged (before/after screenshots)
- [ ] **Given** the change, **When** Vitest + the home e2e run, **Then** they pass

## Technical Notes

- Smart container owns data fetch; children are presentational (`@Input`-driven).

## Dependencies

### Requires
- 001-base-api-service

### Enables
- None

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Computed signals span sections | Keep in container; pass values down |

## Out of Scope

- Other pages (separate stories).
