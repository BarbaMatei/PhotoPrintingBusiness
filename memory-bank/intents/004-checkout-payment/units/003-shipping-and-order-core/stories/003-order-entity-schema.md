---
id: 003-order-entity-schema
unit: 003-shipping-and-order-core
intent: 004-checkout-payment
status: draft
priority: must
created: 2026-05-21T12:00:00Z
assigned_bolt: 015-shipping-and-order-core
implemented: false
---

# Story: 003-order-entity-schema

## User Story

**As a** developer
**I want** `Order` and `OrderItem` entities with an `FT-YYYYNNNN` order number generator
**So that** the payment bolts have a stable, well-structured domain model to create orders against

## Acceptance Criteria

- [ ] **Given** an `Order` is created, **When** inspected, **Then** it contains: `Id (UUID)`, `OrderNumber (string)`, `UserId? (nullable FK)`, `GuestSessionId? (nullable)`, `Status (OrderStatus enum)`, `ShippingType (enum: Easybox/Courier)`, `EasyboxLockerId? (nullable FK)`, `DeliveryAddress? (nullable JSON)`, `ShippingCostRon (decimal)`, `PricingSnapshot (JSON)`, `TotalRon (decimal)`, `CreatedAt (DateTimeOffset)`, `PaidAt? (DateTimeOffset)`, `Notes (string?)`
- [ ] **Given** an `OrderItem` is created, **When** inspected, **Then** it contains: `Id (UUID)`, `OrderId (FK)`, `UploadId (FK)`, `ProductId (FK)`, `Quantity (int)`, `UnitPriceRon (decimal)` (snapshotted at order time)
- [ ] **Given** two orders are created in the same year simultaneously, **When** `OrderNumber` is generated, **Then** both receive unique `FT-YYYY000N` numbers (no collision — use DB sequence or `SELECT FOR UPDATE` counter)
- [ ] **Given** the year rolls over (e.g. 2026 → 2027), **When** the first order of the new year is placed, **Then** `OrderNumber` resets to `FT-20270001`
- [ ] **Given** `PricingSnapshot` is stored, **When** a product price changes later, **Then** the order still shows the original price from the snapshot

## Technical Notes

- `OrderNumber` generation: use a `OrderNumberCounters` table with `(Year int, Counter int)` — `SELECT FOR UPDATE` (EF Core raw SQL) to atomically increment and return; wrap in transaction
- `PricingSnapshot`: stored as `jsonb` (PostgreSQL) — EF Core `HasColumnType("jsonb")` with `OwnsMany` or `HasConversion` to `string`
- `DeliveryAddress`: JSON object `{ recipientName, phone, street, city, county, postalCode }` — stored as `jsonb`
- `OrderStatus` enum: `AwaitingPayment = 0, Paid = 1, Printing = 2, Shipped = 3, Delivered = 4, PaymentFailed = 5, Cancelled = 6`
- EF Core migration: `Orders` + `OrderItems` tables + `OrderNumberCounters` table + indexes on `(UserId, CreatedAt)`, `(OrderNumber)` (unique)

## Dependencies

### Requires
- Story 001-easybox-locker-catalog (EasyboxLocker FK)
- Bolt 013 (cart-api — CartItem FK for OrderItem creation)
- Bolt 005 (auth-core — User FK)

### Enables
- Story 004-order-status-machine (OrderStatus enum)
- Bolt 016 (payment-backends — creates Order entity)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| DB sequence counter row missing for current year | `IOrderNumberService.NextAsync()` creates the row on first use |
| `EasyboxLockerId` and `DeliveryAddress` both set | DB constraint: exactly one must be non-null |
| `OrderItems` count is 0 at creation | Rejected by service-layer validation before insert |

## Out of Scope

- Order modification after creation
- Refund / cancellation flow (future intent)
