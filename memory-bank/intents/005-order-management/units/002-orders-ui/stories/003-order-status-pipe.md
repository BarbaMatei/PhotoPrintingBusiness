---
id: 003-order-status-pipe
unit: 002-orders-ui
intent: 005-order-management
status: draft
priority: must
created: 2026-05-22T07:10:00Z
assigned_bolt: 019-orders-ui
implemented: false
---

# Story: 003-order-status-pipe

## User Story

**As a** developer building the order UI  
**I want** a single shared pipe and status constants for order statuses  
**So that** Romanian labels and colour tokens are consistent across the History, Detail, and Confirmation pages

## Acceptance Criteria

- [ ] **Given** `OrderStatus` string, **When** piped with `orderStatus`, **Then** returns the Romanian label
- [ ] **Given** `'Paid'`, **Then** returns `'Plătită'`
- [ ] **Given** `'Printing'`, **Then** returns `'În tipărire'`
- [ ] **Given** `'Shipped'`, **Then** returns `'Expediată'`
- [ ] **Given** `'Delivered'`, **Then** returns `'Livrată'`
- [ ] **Given** `'Pending'`, **Then** returns `'În așteptare'`
- [ ] **Given** `'Cancelled'`, **Then** returns `'Anulată'`
- [ ] **Given** an unknown status, **Then** returns the raw status string unchanged
- [ ] **Given** `STATUS_ORDER` constant, **Then** exposes ordered array for stepper logic
- [ ] **Given** `statusClass(status)` helper, **Then** returns CSS class string for badge colouring

## Technical Notes

- Create `core/pipes/order-status.pipe.ts` — `@Pipe({ name: 'orderStatus', standalone: true, pure: true })`
- Create `core/models/order-status.constants.ts` — export `STATUS_LABELS`, `STATUS_ORDER`, `statusClass()`
- Extract `STATUS_ORDER` and `isAtLeast()` from `ConfirmationPage` into the constants file; update `ConfirmationPage` to import from there
- CSS classes: `status--pending`, `status--paid`, `status--printing`, `status--shipped`, `status--delivered`, `status--cancelled`

## Dependencies

### Requires
- None (leaf utility)

### Enables
- `001-order-history-page`
- `002-order-detail-page`
- `ConfirmationPage` refactor (import shared constant)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Unknown status string | Return raw string (graceful fallback) |
| Null/undefined input | Return empty string |

## Out of Scope

- Server-side translation
- i18n pipe integration
