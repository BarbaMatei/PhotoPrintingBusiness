---
id: 001-orders-list-endpoint
unit: 001-orders-api
intent: 005-order-management
status: complete
priority: must
created: 2026-05-22T07:10:00Z
assigned_bolt: 018-orders-api
implemented: true
---

# Story: 001-orders-list-endpoint

## User Story

**As a** logged-in customer  
**I want** to retrieve a paginated list of my past orders  
**So that** I can see my purchase history in the app

## Acceptance Criteria

- [ ] **Given** a valid JWT, **When** `GET /api/orders` is called, **Then** returns 200 with `OrderSummaryDto[]` for that user, newest first
- [ ] **Given** query params `?page=2&pageSize=5`, **When** called, **Then** returns the correct page slice and `X-Total-Count` header
- [ ] **Given** no orders exist for the user, **When** called, **Then** returns 200 with empty array
- [ ] **Given** no JWT (guest), **When** called, **Then** returns 401
- [ ] **Given** default call (no params), **When** called, **Then** uses `page=1`, `pageSize=10`
- [ ] **Given** `pageSize > 50`, **When** called, **Then** returns 400 bad request

## Technical Notes

- EF Core query: `_db.Orders.Where(o => o.UserId == currentUserId).OrderByDescending(o => o.CreatedAt)`
- Use `.Skip((page-1)*pageSize).Take(pageSize)` with `.CountAsync()` for total
- `OrderSummaryDto`: `{ id, orderNumber, status, totalRon, createdAt, deliveryType, itemCount }`
- `itemCount` = sum of `OrderItem.Quantity` for that order
- Endpoint: `[Authorize] GET /api/orders`

## Dependencies

### Requires
- Existing `Order` + `OrderItem` EF Core entities (bolt 015 ✅)
- JWT auth middleware (bolt 005 ✅)

### Enables
- `002-order-detail-endpoint` (sibling story)
- `001-order-history-page` (FE unit)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| pageSize > 50 | Return 400 with validation error |
| page = 0 or negative | Return 400 with validation error |
| User has 0 orders | Return 200 `[]` with `X-Total-Count: 0` |

## Out of Scope

- Filtering by status or date range
- Admin viewing another user's orders
