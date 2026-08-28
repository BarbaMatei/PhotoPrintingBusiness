# US-505 — Admin — Stats API (Backend)

## Story
**As a** system  
**I want to** provide aggregated analytics efficiently

## Type
BACKEND — ASP.NET Core

## Epic
EPIC-5 | Panou Admin

## Dependencies
- US-305 (Orders with payment data must exist)
- US-802 (Admin authorization)

## Acceptance Criteria

1. **`GET /api/admin/stats/summary`** — `ordersToday`, `revenueToday`, `ordersThisMonth`, `revenueThisMonth` (all RON)
2. **`GET /api/admin/stats/revenue?days=30`** — `[{date, totalRon}]` array
3. **`GET /api/admin/stats/products`** — `[{productName, orderCount, revenueRon}]` sorted desc by orderCount
4. **`GET /api/admin/stats/orders-by-status`** — `[{status, count}]`
5. **All stats queries** filtered to `Status != Cancelled` for revenue figures
6. **Queries use indexed columns** (CreatedAt, Status); cached 5 min in `IMemoryCache`

## Technical Notes

### Endpoints
```
GET /api/admin/stats/summary
→ 200 { "ordersToday": 15, "revenueToday": 450.00, "ordersThisMonth": 320, "revenueThisMonth": 9600.00 }

GET /api/admin/stats/revenue?days=30
→ 200 [{ "date": "2026-05-01", "totalRon": 320.50 }, ...]

GET /api/admin/stats/products
→ 200 [{ "productName": "10×15 Lucios", "orderCount": 150, "revenueRon": 4500.00 }, ...]

GET /api/admin/stats/orders-by-status
→ 200 [{ "status": "Paid", "count": 12 }, { "status": "Printing", "count": 5 }, ...]
```

### Implementation Details
- All endpoints: `[Authorize(Roles='Admin')]`
- Summary: aggregate queries on Orders table using `CreatedAt` for today/this month; exclude Cancelled
- Revenue: `GROUP BY date(CreatedAt)` for the last N days; fill in zero-days for gaps
- Products: `JOIN OrderItems → Products`, `GROUP BY product`, `SUM(quantity * unitPrice)`
- Orders by status: simple `GROUP BY Status, COUNT(*)`
- Caching: `IMemoryCache` with 5-minute sliding expiration per endpoint
- Indexes: ensure composite index on `(Status, CreatedAt)` for efficient filtering

### Performance
- Use raw SQL or optimized LINQ to avoid N+1 queries
- Revenue query: single query with GROUP BY, not per-day lookups

## Files to Create/Modify
- `src/PhotoPrint.API/Controllers/AdminStatsController.cs`
- `src/PhotoPrint.API/DTOs/Admin/StatsSummaryDto.cs`
- `src/PhotoPrint.API/DTOs/Admin/RevenueDataPoint.cs`
- `src/PhotoPrint.API/DTOs/Admin/ProductStatsDto.cs`
- `src/PhotoPrint.API/DTOs/Admin/OrderStatusCount.cs`
- `src/PhotoPrint.API/Services/IStatsService.cs` + `StatsService.cs`

## Testing
- Unit test: summary calculations
- Unit test: revenue aggregation with date gaps
- Unit test: product stats sorting
- Unit test: cancelled orders excluded from revenue
- Unit test: cache hit/miss behavior
- Integration test: stats with seeded order data
