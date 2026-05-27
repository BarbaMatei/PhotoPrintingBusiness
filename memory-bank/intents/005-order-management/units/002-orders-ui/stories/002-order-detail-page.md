---
id: 002-order-detail-page
unit: 002-orders-ui
intent: 005-order-management
status: draft
priority: must
created: 2026-05-22T07:10:00Z
assigned_bolt: 019-orders-ui
implemented: false
---

# Story: 002-order-detail-page

## User Story

**As a** logged-in customer  
**I want** to view the complete details of a specific order  
**So that** I can review exactly what I ordered, how much I paid, and where it's being delivered

## Acceptance Criteria

- [ ] **Given** navigating to `/comenzi/:id`, **Then** the page loads and shows full order detail
- [ ] **Given** each line item, **Then** shows: photo thumbnail, product name, finish, quantity, line total
- [ ] **Given** an Easybox order, **Then** shows locker name and address; no street address
- [ ] **Given** a Courier order, **Then** shows full shipping address; no locker info
- [ ] **Given** the status stepper, **Then** completed stages are highlighted (reusing ConfirmationPage's `isAtLeast` logic)
- [ ] **Given** cost summary section, **Then** shows subtotal, shipping cost, grand total
- [ ] **Given** an unknown or other-user's order id, **Then** redirects to `/comenzi` (not 404 page)
- [ ] **Given** a guest navigating directly, **Then** redirected to `/autentificare` via authGuard

## Technical Notes

- Standalone component at `features/orders/pages/order-detail-page.ts`
- Route: `{ path: 'comenzi/:id', component: OrderDetailPage, canActivate: [authGuard] }`
- Uses `input.required<string>()` for `orderId` signal input
- `OrderService.getOrderDetail(id): Observable<OrderDetailDto>` — on 403/404, `catchError` → navigate to `/comenzi`
- Status stepper: extract `STATUS_ORDER` constant and `isAtLeast()` helper shared with `ConfirmationPage`
- `ChangeDetectionStrategy.OnPush` + `signal()`

## Dependencies

### Requires
- `003-order-status-pipe` (sibling story — shared status stepper)
- `001-order-history-page` (sibling — `OrderService` defined there)
- `002-order-detail-endpoint` (API — bolt 018)

### Enables
- Nothing (leaf story)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| API returns 403 | catchError → navigate to `/comenzi` |
| API returns 404 | catchError → navigate to `/comenzi` |
| Item has no previewUrl | Show placeholder thumbnail |
| Order status is Cancelled | Stepper shows no stages highlighted |

## Out of Scope

- Re-order functionality
- Printing the order
