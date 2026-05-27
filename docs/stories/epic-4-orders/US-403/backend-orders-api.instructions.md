# US-403 — Orders API (Backend)

## Story
**As a** system  
**I want to** expose order data scoped to the requesting user or guest session

## Type
BACKEND — ASP.NET Core

## Epic
EPIC-4 | Istoricul Comenzilor & Tracking

## Dependencies
- US-305/US-306 (Orders must be created by payment flow)
- US-105/US-109 (Auth)

## Acceptance Criteria

1. **`GET /api/orders`** — paginated, scoped to `userId` OR `guestSessionId`; `?status=` filter
2. **`GET /api/orders/{id}`** — full detail; `403` if order belongs to different user/session
3. **OrderDto**: `orderId`, `orderNumber` (FT-YYYYNNNN), `status`, `items[]`, `shippingType`, `shippingAddress`, `deliveryLockerId`, `awbNumber`, `trackingUrl`, `paymentTotal`, `shippingCost`, `createdAt`
4. **Guest access** via `?email=` param: validates orderId+email match before returning data

## Technical Notes

### Endpoints
```
GET /api/orders?page=1&pageSize=10&status=Paid
Authorization: Bearer {jwt} OR X-Guest-Token: {uuid}
→ 200 {
  "items": [{ OrderDto }],
  "totalCount": 25,
  "page": 1,
  "pageSize": 10
}
```

```
GET /api/orders/{id}
Authorization: Bearer {jwt} OR X-Guest-Token: {uuid}
→ 200 { OrderDto with items[] }
→ 403 (order belongs to different user)
→ 404 (order not found)
```

```
GET /api/orders/{id}?email=guest@email.com
(No auth required — guest access by email)
→ 200 { OrderDto } (if email matches order's guest email)
→ 403 (email mismatch)
```

### Implementation Details
- Pagination: standard offset-based with `page` and `pageSize`
- Scoping: middleware identifies user by JWT or guest token; query filters by userId or guestSessionId
- Guest email access: special case — no auth header needed, but must match order's guest session email
- OrderDto includes nested `OrderItemDto[]` with: uploadId, previewUrl, productName, finish, quantity, unitPrice, lineTotal
- `orderNumber` format: `FT-{YYYY}{NNNN}` — auto-generated sequence per year
- Sort: `CreatedAt DESC` by default
- Status filter: optional query param

## Files to Create/Modify
- `src/PhotoPrint.API/Controllers/OrdersController.cs`
- `src/PhotoPrint.API/DTOs/Orders/OrderDto.cs`
- `src/PhotoPrint.API/DTOs/Orders/OrderItemDto.cs`
- `src/PhotoPrint.API/DTOs/Orders/OrderListResponse.cs`
- `src/PhotoPrint.API/Services/IOrderService.cs` (GetOrdersAsync, GetOrderByIdAsync)

## Testing
- Unit test: orders scoped to requesting user only
- Unit test: guest email access validation
- Unit test: pagination
- Unit test: status filter
- Unit test: 403 for unauthorized access
- Integration test: order retrieval flow
