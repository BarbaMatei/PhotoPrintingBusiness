---
id: 015-shipping-and-order-core
unit: 003-shipping-and-order-core
intent: 004-checkout-payment
type: ddd
status: complete
started: 2026-05-21T22:00:00Z
completed: 2026-05-21T23:30:00Z
current_stage: done
stages_completed: [design, implement, test]
stories:
  - 001-easybox-locker-catalog
  - 002-shipping-endpoints
  - 003-order-entity-schema
  - 004-order-status-machine
created: 2026-05-21T22:00:00Z

requires_bolts: ["013-cart-api", "005-auth-core"]
enables_bolts: ["016-payment-backends"]
---

## Bolt: 015-shipping-and-order-core

### Summary
Backend domain layer for shipping (EasyboxLocker catalog, shipping cost config, AWB stub)
and order management (Order + OrderItem entities, OrderStatus state machine, order number service).
No payment logic — that is bolt 016.

### What This Bolt Builds

- `EasyboxLocker` entity + seeded migration (~50 representative Romanian Easybox locations)
- `IShippingService` / `StaticShippingService` — lockers (DB query), cost (config), AWB (manual stub)
- `GET /api/shipping/lockers?city=` — public, case-insensitive city filter
- `GET /api/shipping/cost?type=Easybox|Courier` — public, reads from config
- `POST /api/shipping/awb` — Admin JWT required, returns `{ manual: true }`
- `Order` entity (JSONB shipping address, enums for status/processor/delivery)
- `OrderItem` entity (JSONB product snapshot, price snapshot)
- `OrderStatus`, `PaymentProcessor`, `DeliveryType` enums
- `InvalidOrderTransitionException` → mapped to 400 in ExceptionHandlerMiddleware
- `OrderStatusMachine` static class — valid transitions + `CanTransition` guard
- `IOrderNumberService` / `OrderNumberService` — PostgreSQL sequence per year, InMemory fallback
- Shipping config in `appsettings.json`: `Shipping:EasyboxCostRon`, `Shipping:CourierCostRon`
