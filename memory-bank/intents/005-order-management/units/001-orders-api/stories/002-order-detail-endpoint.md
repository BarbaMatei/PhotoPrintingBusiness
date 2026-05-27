---
id: 002-order-detail-endpoint
unit: 001-orders-api
intent: 005-order-management
status: draft
priority: must
created: 2026-05-22T07:10:00Z
assigned_bolt: 018-orders-api
implemented: false
---

# Story: 002-order-detail-endpoint

## User Story

**As a** logged-in customer  
**I want** to retrieve the full details of a specific order  
**So that** I can review what I ordered, what I paid, and the delivery status

## Acceptance Criteria

- [ ] **Given** a valid JWT and own order id, **When** `GET /api/orders/{id}` is called, **Then** returns 200 with `OrderDetailDto`
- [ ] **Given** a valid JWT but another user's order id, **When** called, **Then** returns 403
- [ ] **Given** an unknown order id, **When** called, **Then** returns 404
- [ ] **Given** no JWT, **When** called, **Then** returns 401
- [ ] **Given** Easybox order, **Then** response includes `lockerId`, `lockerName`; `shippingAddress` is null
- [ ] **Given** Courier order, **Then** response includes `shippingAddress`; `lockerId`/`lockerName` are null

## Technical Notes

- `OrderDetailDto` extends `OrderSummaryDto` with:
  - `items: OrderItemDto[]` — each with `uploadId`, `previewUrl`, `productName`, `finishName`, `quantity`, `unitPriceRon`, `lineTotal`
  - `shippingAddress: ShippingAddressDto | null`
  - `lockerId: string | null`, `lockerName: string | null`
  - `paymentProcessor: 'Stripe' | 'EuPlatesc'`
  - `paidAt: string | null`
- Join: `Order → OrderItems → Uploads` (for `previewUrl`)
- Endpoint: `[Authorize] GET /api/orders/{id:guid}`

## Dependencies

### Requires
- `001-orders-list-endpoint` (same bolt — entity/DTO already established)
- Existing `Order`, `OrderItem`, `Upload` entities (bolt 015 ✅)

### Enables
- `002-order-detail-page` (FE unit)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Order belongs to different user | 403 Forbidden |
| Order id is not a valid GUID | 400 Bad Request |
| Order has 0 items (edge) | Return order with empty `items: []` |

## Out of Scope

- Tracking shipment (no real-time courier API in MVP)
- Modifying order data
