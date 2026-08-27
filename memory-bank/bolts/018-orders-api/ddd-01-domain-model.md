---
stage: domain-model
bolt: 018-orders-api
created: 2026-05-22T07:25:00Z
---

## Static Model: 001-orders-api

### Entities

- **Order** (existing — bolt 015): `Id (Guid)`, `UserId (Guid)`, `OrderNumber (string)`, `Status (OrderStatus)`, `SubtotalRon (decimal)`, `ShippingCostRon (decimal)`, `TotalRon (decimal)`, `DeliveryType (DeliveryType)`, `PaymentProcessor (PaymentProcessor)`, `LockerId (string?)`, `LockerName (string?)`, `RecipientName (string?)`, `Street (string?)`, `StreetNumber (string?)`, `Block (string?)`, `City (string?)`, `County (string?)`, `PostalCode (string?)`, `Phone (string?)`, `CreatedAt (DateTime)`, `PaidAt (DateTime?)`
  - Business rules: `UserId` identifies the owning customer; `Status` follows the state machine (Pending → Paid → Printing → Shipped → Delivered | Cancelled); `TotalRon = SubtotalRon + ShippingCostRon`

- **OrderItem** (existing — bolt 015): `Id (Guid)`, `OrderId (Guid)`, `UploadId (Guid)`, `ProductId (Guid)`, `Quantity (int)`, `UnitPriceRon (decimal)`, `LineTotal (decimal)`
  - Business rules: `LineTotal = UnitPriceRon × Quantity`; immutable after order creation

- **Upload** (existing — earlier bolts): `Id (Guid)`, `UserId (Guid)`, `OriginalFileName (string)`, `PreviewUrl (string?)`, `StoragePath (string)`
  - Role here: provides `PreviewUrl` and `OriginalFileName` for the order detail line items; read-only access

- **Product** (existing — bolt 009): `Id (Guid)`, `Name (string)`
  - Role here: provides `Name` for display on order detail; read-only access

- **ProductFinish** (existing — bolt 009): `Id (Guid)`, `Name (string)`
  - Role here: provides finish name for display on order detail; read-only access

---

### Value Objects

- **OrderStatus** (enum/string): `Pending | Paid | Printing | Shipped | Delivered | Cancelled`
  - Constraints: transitions enforced by payment and admin flows; this bolt reads only

- **DeliveryType** (enum/string): `Easybox | Courier`
  - Constraints: determines which address fields are populated on the Order

- **PaymentProcessor** (enum/string): `Stripe | LegacyProcessor`

- **ShippingAddress** (derived — not a separate entity, flattened columns on Order):
  - Meaningful only when `DeliveryType = Courier`
  - Fields: `RecipientName`, `Street`, `StreetNumber`, `Block`, `City`, `County`, `PostalCode`, `Phone`

---

### Aggregates

- **Order Aggregate Root**: `Order` + `OrderItem[]`
  - Invariants: all `OrderItem` records belong to exactly one `Order`; `TotalRon` is consistent with item sum + shipping
  - Boundaries: this bolt does NOT modify the aggregate — reads only
  - Repository: `IOrderRepository` (or direct EF Core DbSet queries via service)

---

### Domain Events

> This bolt produces **no new domain events** — it is a read-only query layer. Events were emitted by the payment flow (bolt 016) when orders transitioned to `Paid`.

---

### Domain Services

- **IOrderService** (new):
  - `GetOrdersAsync(Guid userId, int page, int pageSize, CancellationToken ct) → (IReadOnlyList<OrderSummaryDto>, int total)`
    - Queries orders owned by `userId`, ordered by `CreatedAt DESC`, with skip/take pagination
  - `GetOrderDetailAsync(Guid orderId, Guid userId, CancellationToken ct) → OrderDetailDto`
    - Fetches single order; throws `NotFoundException` (→ 404) if not found; throws `ForbiddenException` (→ 403) if `Order.UserId ≠ userId`

---

### Repository Interfaces

> The project uses EF Core DbContext directly via `IApplicationDbContext` pattern (established in bolt 001). No new repository interface is required — `IOrderService` implementations will inject the DbContext directly.

- **Read access needed on**: `DbSet<Order>`, `DbSet<OrderItem>`, `DbSet<Upload>`, `DbSet<Product>` (join for display names)

---

### Response DTOs (Domain Contracts)

**OrderSummaryDto** (list item):
```
{
  id: Guid
  orderNumber: string
  status: string           // "Pending" | "Paid" | "Printing" | "Shipped" | "Delivered" | "Cancelled"
  totalRon: decimal
  createdAt: DateTime
  deliveryType: string     // "Easybox" | "Courier"
  itemCount: int           // sum of OrderItem.Quantity
}
```

**OrderDetailDto** (full detail — extends summary):
```
{
  id, orderNumber, status, totalRon, subtotalRon, shippingCostRon, createdAt, deliveryType,
  paymentProcessor: string
  paidAt: DateTime?
  // Easybox fields (null for Courier)
  lockerId: string?
  lockerName: string?
  // Courier fields (null for Easybox)
  shippingAddress: {
    recipientName, street, streetNumber, block, city, county, postalCode, phone
  } | null
  items: OrderItemDto[]
}
```

**OrderItemDto**:
```
{
  uploadId: Guid
  previewUrl: string?
  productName: string
  finishName: string
  quantity: int
  unitPriceRon: decimal
  lineTotal: decimal
}
```

---

### Ubiquitous Language

| Term | Definition |
|------|-----------|
| **Order** | A completed purchase made by a customer; immutable after creation except for `Status` |
| **Order Summary** | Lightweight view of an order for list display (no line items) |
| **Order Detail** | Full view of an order including all line items and delivery info |
| **Line Item (OrderItem)** | One photo product within an order (one upload × format/finish × quantity) |
| **Ownership** | An Order belongs to the user whose `UserId` matches; cross-user access is forbidden |
| **Pagination** | Dividing a large result set into pages; controlled by `page` (1-based) and `pageSize` (max 50) |
| **Item Count** | The total number of photo prints across all line items (`SUM(OrderItem.Quantity)`) |
| **X-Total-Count** | HTTP response header carrying the unpaged total count for client-side pagination |
| **Easybox** | Sameday parcel locker pickup delivery; identified by `lockerId` and `lockerName` |
| **Courier** | Home/office delivery; identified by a full shipping address |

---

### Coverage Check

| Story | Covered By |
|-------|-----------|
| 001-orders-list-endpoint | `IOrderService.GetOrdersAsync`, `OrderSummaryDto`, pagination rules |
| 002-order-detail-endpoint | `IOrderService.GetOrderDetailAsync`, `OrderDetailDto`, `OrderItemDto`, ownership rules |
