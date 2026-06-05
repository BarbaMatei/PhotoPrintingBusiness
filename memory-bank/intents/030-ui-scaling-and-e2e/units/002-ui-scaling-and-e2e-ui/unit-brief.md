---
unit: 002-ui-scaling-and-e2e-ui
intent: 030-ui-scaling-and-e2e
phase: inception
status: draft
unit_type: frontend
default_bolt_type: simple-construction-bolt
created: 2026-06-05T09:30:00Z
updated: 2026-06-05T09:30:00Z
---

# Unit Brief: UI Scaling Refactor

## Purpose

Break up the four largest Angular pages (home-page.ts is 951 LOC) into smart-container + dumb-child components and introduce a shared `BaseApiService` so the 14 services stop hand-rolling HttpClient calls.

## Scope

### In Scope
- `BaseApiService`; decompose home, saved-addresses, profile, delivery-step pages.

### Out of Scope
- CI gates (unit 001); backend work.

---

## Assigned Requirements

| FR | Requirement | Priority |
|----|-------------|----------|
| FR-2 (P26) | Break up large pages + BaseApiService | Should |

---

## Domain Concepts

### Key Operations
| Operation | Description | Inputs | Outputs |
|-----------|-------------|--------|---------|
| Page breakup | Smart container + dumb children | large page | smaller components |
| BaseApiService | Centralise HTTP plumbing | url/body/opts | typed Observable |

---

## Story Summary

| Metric | Count |
|--------|-------|
| Total Stories | 4 |
| Must Have | 0 |
| Should Have | 4 |
| Could Have | 0 |

### Stories

| Story ID | Title | Priority | Status |
|----------|-------|----------|--------|
| 001-base-api-service | Shared BaseApiService | Should | Planned |
| 002-home-page-breakup | Break up home-page.ts (951 LOC) | Should | Planned |
| 003-account-pages-breakup | Break up saved-addresses + profile | Should | Planned |
| 004-delivery-step-locker-selector | Extract locker-selector from delivery-step | Should | Planned |

---

## Dependencies

### Depends On
| Unit | Reason |
|------|--------|
| 001-ci-quality-gates | E2e + budget verify the refactor |

### Depended By
| Unit | Reason |
|------|--------|
| None | — |

### External Dependencies
| System | Purpose | Risk |
|--------|---------|------|
| PhotoPrint.API | Same APIs via BaseApiService | Low |

---

## Technical Context

### Suggested Technology
Angular 21 standalone components, smart/dumb split, RxJS `catchError` wrapper in `BaseApiService`.

---

## Constraints

- Migrate one service at a time onto `BaseApiService` (start with `order.service.ts`).
- Component breakups land one PR per page; take before/after screenshots of home.

---

## Success Criteria

### Functional
- [ ] home-page → thin container + 5 child components; saved-addresses/profile/delivery-step decomposed.
- [ ] All services route through `BaseApiService` (withCredentials, error translation, idempotency-key).

### Non-Functional
- [ ] No page > ~200 LOC; within bundle budget; no home visual regression.

### Quality
- [ ] Vitest green after each migration.

---

## Bolt Suggestions

| Bolt | Type | Stories | Objective |
|------|------|---------|-----------|
| 067-ui-scaling-and-e2e-ui | simple | 001–004 | BaseApiService + 4 page breakups |

---

## Notes

After unit 001. Parallelisable with backend intents on a second developer.
