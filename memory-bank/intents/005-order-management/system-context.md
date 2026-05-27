---
intent: 005-order-management
created: 2026-05-22T07:10:00Z
---

# System Context: Order Management

## System Boundary

The Order Management feature reads and exposes order data that was written by the payment flow (bolts 015–017). No new order creation happens here — this intent is purely a **read + display** layer on top of the existing `Orders` table in PostgreSQL.

## Actors

| Actor | Type | Interaction |
|-------|------|-------------|
| Authenticated Customer | Human | Calls `GET /api/orders`, `GET /api/orders/{id}`; visits `/comenzi`, `/comenzi/:id` |
| Guest User | Human | No order history (API returns 200 with empty list); cannot access FE order pages |
| Angular Frontend | System | Calls backend Orders API over HTTPS; renders Order History and Detail pages |
| ASP.NET Core API | System | Queries PostgreSQL for order records owned by the requesting user |
| PostgreSQL | System | Stores `Orders`, `OrderItems`, `Uploads` (written by checkout/payment flow) |

## External Systems

| System | Direction | Data | Protocol |
|--------|-----------|------|----------|
| PostgreSQL | Inbound | Order records, line items, upload refs | EF Core / SQL |
| Angular UI | Outbound | OrderListDto[], OrderDetailDto | REST / JSON |

## Data Flows

### Inbound (to API)
- `GET /api/orders?page=1&pageSize=10` — JWT-authenticated request from Angular
- `GET /api/orders/{id}` — JWT-authenticated request; requires ownership

### Outbound (from API)
- `OrderSummaryDto[]` + pagination headers → Angular Order History page
- `OrderDetailDto` with line items → Angular Order Detail page

## Key Constraints

- **Ownership**: An order belongs to exactly one user; cross-user access → 403
- **No writes**: This intent adds zero write endpoints; all mutation is in payment flow
- **Existing entity**: `Order` entity (with `OrderItem`, `UploadId`, `Status`, `DeliveryType`) already exists — no migration needed for core fields
- **Status labels**: Romanian labels shared with the Confirmation page (bolt 017)
