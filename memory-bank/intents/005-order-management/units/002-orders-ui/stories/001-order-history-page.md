---
id: 001-order-history-page
unit: 002-orders-ui
intent: 005-order-management
status: complete
priority: must
created: 2026-05-22T07:10:00Z
assigned_bolt: 019-orders-ui
implemented: true
---

# Story: 001-order-history-page

## User Story

**As a** logged-in customer  
**I want** to see a list of all my past orders  
**So that** I can track what I've bought and navigate to any order's details

## Acceptance Criteria

- [ ] **Given** a logged-in user, **When** they navigate to `/comenzi`, **Then** they see a paginated list of their orders newest first
- [ ] **Given** each order row, **Then** it shows: order number, status badge, total (RON), date, delivery type, item count
- [ ] **Given** a status badge, **Then** colour + icon conveys status (not colour alone)
- [ ] **Given** clicking a row, **Then** navigates to `/comenzi/{id}`
- [ ] **Given** more orders than page size (10), **Then** pagination controls are visible and functional
- [ ] **Given** a guest (unauthenticated), **When** navigating to `/comenzi`, **Then** redirected to `/autentificare`
- [ ] **Given** zero orders, **Then** a friendly empty state message is shown

## Technical Notes

- Standalone Angular component at `features/orders/pages/order-history-page.ts`
- Route: `{ path: 'comenzi', component: OrderHistoryPage, canActivate: [authGuard] }`
- `OrderService.getOrders(page, pageSize): Observable<{ orders: OrderSummaryDto[], total: number }>`
- Pagination: `page` signal, page size = 10; use `HttpClient` with `observe: 'response'` to read `X-Total-Count`
- `ChangeDetectionStrategy.OnPush` + `signal()`
- Status badge: use `OrderStatusPipe` for label + map status → CSS class for colour

## Dependencies

### Requires
- `003-order-status-pipe` (sibling story — needed for labels/colours)
- `001-orders-list-endpoint` (API must exist — bolt 018)

### Enables
- `002-order-detail-page` (navigation target)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| API error (500) | Show error toast, preserve last loaded page |
| Guest navigates directly | Redirect to `/autentificare` via authGuard |
| Zero orders | Empty state: "Nu ai nicio comandă încă." with CTA to upload |

## Out of Scope

- Order filtering / search
- Bulk actions
