# US-501 — Admin — Dashboard Overview (Frontend)

## Story
**As an** operator  
**I want to** see key business metrics at a glance when I open the admin panel

## Type
FRONTEND — Angular

## Epic
EPIC-5 | Panou Admin

## Dependencies
- US-505 (Admin Stats API)
- US-804 (AdminGuard, routing)

## Acceptance Criteria

1. **`/admin`** — protected route; redirects to `/admin/login` if not Admin role
2. **KPI cards row**: Comenzi azi, Venituri azi (RON), Comenzi luna aceasta, Venituri luna aceasta
3. **Orders by status**: donut chart (Recharts or Chart.js via ng2-charts)
4. **Revenue last 30 days**: bar chart (daily); RON on Y-axis
5. **Top 3 selling formats**: horizontal bar chart
6. **Auto-refresh** every 5 min; manual refresh button

## Technical Notes

### Component Location
`src/app/features/admin/dashboard/dashboard.component.ts`

### Implementation Details
- Protected by `AdminGuard` — checks `role=Admin` in JWT claims
- Load data from:
  - `GET /api/admin/stats/summary` → KPI cards
  - `GET /api/admin/stats/orders-by-status` → donut chart
  - `GET /api/admin/stats/revenue?days=30` → bar chart
  - `GET /api/admin/stats/products` → top products chart (take first 3)
- Charts: use `ng2-charts` (Chart.js wrapper for Angular)
- Auto-refresh: `setInterval` or `timer()` RxJS operator, every 5 minutes
- Manual refresh button: re-fetches all data
- KPI cards: large numbers with label below

### UI/UX
- Admin layout: sidebar navigation (Dashboard, Comenzi, Produse) + main content area
- Dashboard grid: KPI cards row at top, charts in 2-column grid below
- Responsive: single column on mobile
- Currency format: `XX.XXX,XX RON`
- All text in Romanian

## Files to Create/Modify
- `src/app/features/admin/dashboard/dashboard.component.ts`
- `src/app/features/admin/dashboard/dashboard.component.html`
- `src/app/features/admin/dashboard/dashboard.component.scss`
- `src/app/features/admin/admin-layout/admin-layout.component.ts`
- `src/app/features/admin/admin-routing.module.ts`
- `src/app/core/services/admin-stats.service.ts`

## Testing
- Unit test: KPI cards display correct values
- Unit test: charts render with data
- Unit test: auto-refresh triggers
- Unit test: AdminGuard redirects non-admin users
