# US-502 — Admin — Real-time Order Queue (Frontend)

## Story
**As an** operator  
**I want to** see all incoming paid orders in real time so I know what to print next

## Type
FRONTEND — Angular

## Epic
EPIC-5 | Panou Admin

## Dependencies
- US-504 (Admin API + SignalR hub)
- US-501 (Admin layout)

## Acceptance Criteria

1. **`/admin/comenzi`** — table: order number, customer name (or `Oaspete`), photos count, format/finish, created at, status badge, actions
2. **Default sort**: oldest Paid orders first (print FIFO queue)
3. **Real-time**: new orders appear via SignalR without page refresh; subtle slide-in animation + sound (optional)
4. **Filter tabs**: `Toate` / `Plătite` / `În imprimare` / `Expediate`; search by order number or email
5. **Bulk action**: select multiple Paid orders → `Marchează ca În imprimare`
6. **Row click** → opens order detail side panel

## Technical Notes

### Component Location
`src/app/features/admin/order-queue/order-queue.component.ts`

### Implementation Details
- SignalR: connect to `AdminOrderHub` on component init
  - Install `@microsoft/signalr` npm package
  - Listen for `NewOrderReceived` → prepend to table with animation
  - Listen for `OrderStatusChanged` → update row status badge
  - Optional: play notification sound on new order
- Table: Angular Material `mat-table` or custom table with sortable columns
- Filter tabs: filter by status; maintain in URL query params
- Search: debounced text input, filter by orderNumber or email
- Bulk select: checkboxes on rows; bulk action bar appears when items selected
- Side panel: slide-in panel on row click showing full order detail (US-503)

### UI/UX
- FIFO queue emphasis: Paid orders sorted by oldest first
- Status badges with colors (reuse from US-401)
- Slide-in animation for new orders (CSS transition)
- Bulk action bar: fixed at bottom when items selected
- All text in Romanian

## Files to Create/Modify
- `src/app/features/admin/order-queue/order-queue.component.ts`
- `src/app/features/admin/order-queue/order-queue.component.html`
- `src/app/features/admin/order-queue/order-queue.component.scss`
- `src/app/core/services/admin-signalr.service.ts`
- `src/app/core/services/admin-orders.service.ts`

## Testing
- Unit test: table rendering with mock data
- Unit test: filter tabs change displayed data
- Unit test: search filtering
- Unit test: bulk selection and action
- Unit test: SignalR event handling (mock hub)
- E2E: order queue with real-time updates
