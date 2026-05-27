---
stage: test
bolt: 018-orders-api
created: 2026-05-22T08:00:00Z
---

## Test Report: 018-orders-api

### Summary

| Metric | Value |
|--------|-------|
| New tests added | 12 |
| Test type | Integration (WebApplicationFactory + InMemory DB) |
| Total suite (before) | 340 |
| Total suite (after) | 352 |
| Failed | 0 |

### Test Cases

| Test | Story | Status |
|------|-------|--------|
| `GetOrders_Unauthenticated_Returns401` | 001 | ✅ |
| `GetOrders_NoOrders_Returns200WithEmptyList` | 001 | ✅ |
| `GetOrders_WithOrders_ReturnsOrderList` | 001 | ✅ |
| `GetOrders_OnlyReturnsOwnOrders` | 001 | ✅ |
| `GetOrders_InvalidPage_Returns400` | 001 | ✅ |
| `GetOrders_PageSizeTooLarge_Returns400` | 001 | ✅ |
| `GetOrderDetail_Unauthenticated_Returns401` | 002 | ✅ |
| `GetOrderDetail_OwnOrder_Returns200WithDetail` | 002 | ✅ |
| `GetOrderDetail_OtherUsersOrder_Returns403` | 002 | ✅ |
| `GetOrderDetail_UnknownId_Returns404` | 002 | ✅ |
| `GetOrderDetail_EasyboxOrder_HasLockerName` | 002 | ✅ |
| `GetOrderDetail_CourierOrder_HasShippingAddress` | 002 | ✅ |

### Acceptance Criteria Coverage

**Story 001 (orders-list-endpoint):**
- ✅ Authenticated user gets their orders newest first
- ✅ Pagination params respected; `X-Total-Count` header present
- ✅ Empty list returns 200 with `[]`
- ✅ Guest (no JWT) returns 401
- ✅ `page=0` → 400; `pageSize=51` → 400
- ✅ Ownership isolation (other user's orders not returned)

**Story 002 (order-detail-endpoint):**
- ✅ Own order → 200 with full `OrderDetailDto`
- ✅ Another user's order → 403
- ✅ Unknown id → 404
- ✅ No JWT → 401
- ✅ Easybox order → `lockerName` populated, `shippingAddress` null
- ✅ Courier order → `shippingAddress` populated, `lockerId` null

### Notes

- `ProductSnapshot` (denormalized on `OrderItem` by bolt 015) used for `productName`/`size`/`finish` — no catalog join needed
- `EasyboxLocker` nav property resolved via `Include(o => o.EasyboxLocker)` in detail query
- Preview URL derived as `/api/uploads/{uploadId}/preview` (not stored — mirrors `CartService` pattern)
