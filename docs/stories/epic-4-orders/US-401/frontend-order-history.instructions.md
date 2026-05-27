# US-401 — Order History List (Frontend)

## Story
**As a** logged-in customer  
**I want to** see all my past and current orders with status at a glance

## Type
FRONTEND — Angular

## Epic
EPIC-4 | Istoricul Comenzilor & Tracking

## Dependencies
- US-403 (Orders API backend)
- US-804 (Angular App Shell — AuthGuard)

## Acceptance Criteria

1. **`/comenzile-mele`** — cards: order number, date, first photo thumbnail, item count, total RON, status badge
2. **Status badge colors**: Pending=gray, Paid=blue, Printing=orange, Shipped=purple, Delivered=green, Cancelled=red
3. **Pagination** 10 per page; sort by date desc
4. **Empty state** with `Comandă acum` CTA

## Technical Notes

### Component Location
`src/app/features/orders/order-list/order-list.component.ts`

### Implementation Details
- Protected by `AuthGuard` — requires login
- Call `GET /api/orders?page=1&pageSize=10` on init
- Display order cards with key info
- Status badge: reusable `StatusBadgeComponent` with color mapping
- Pagination: Angular Material paginator or custom component
- Click on order card → navigate to `/comanda/{orderId}`
- Empty state: illustration + `Comandă acum` button linking to upload page

### UI/UX
- Card layout: thumbnail on left, details on right
- Date format: Romanian locale (`dd.MM.yyyy`)
- Currency: `XX,XX RON`
- Responsive: full-width cards on mobile
- All text in Romanian

## Files to Create/Modify
- `src/app/features/orders/order-list/order-list.component.ts`
- `src/app/features/orders/order-list/order-list.component.html`
- `src/app/features/orders/order-list/order-list.component.scss`
- `src/app/shared/components/status-badge/status-badge.component.ts`
- `src/app/core/services/order.service.ts`
- `src/app/core/models/order.model.ts`

## Testing
- Unit test: orders displayed correctly
- Unit test: status badge colors
- Unit test: pagination
- Unit test: empty state
- E2E: order history page with orders
