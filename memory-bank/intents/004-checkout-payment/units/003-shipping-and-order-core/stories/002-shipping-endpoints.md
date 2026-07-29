---
id: 002-shipping-endpoints
unit: 003-shipping-and-order-core
intent: 004-checkout-payment
status: complete
priority: must
created: 2026-05-21T12:00:00Z
assigned_bolt: 015-shipping-and-order-core
implemented: true
---

# Story: 002-shipping-endpoints

## User Story

**As a** customer choosing a delivery method
**I want** to search for nearby Easybox lockers and see shipping costs
**So that** I can select a pickup point or home delivery and know the exact cost before paying

## Acceptance Criteria

- [ ] **Given** `GET /api/shipping/lockers?city=Cluj`, **When** the query runs, **Then** a 200 response returns `[{ lockerId, name, address, city, county, latitude, longitude }]` filtered by city (case-insensitive)
- [ ] **Given** `GET /api/shipping/lockers` with no `city` parameter, **When** the query runs, **Then** all active lockers are returned (full list)
- [ ] **Given** `GET /api/shipping/cost?type=Easybox`, **When** called, **Then** `{ costRon: 20.00 }` is returned — value from configuration, not hardcoded
- [ ] **Given** `GET /api/shipping/cost?type=Courier`, **When** called, **Then** `{ costRon: 25.00 }` is returned
- [ ] **Given** `type` query param is invalid, **When** called, **Then** 400 is returned with a validation error
- [ ] **Given** shipping cost values, **When** they are changed in `appsettings.json`, **Then** the endpoint returns the new values without code redeployment

## Technical Notes

- `ShippingController` with two endpoints: `GET /api/shipping/lockers` and `GET /api/shipping/cost`
- Shipping costs configured in `appsettings.json` under `Shipping:EasyboxCostRon` and `Shipping:CourierCostRon`
- `IShippingService` injected; `StaticShippingService` reads from `IOptions<ShippingOptions>`
- `ShippingOptions` record: `{ decimal EasyboxCostRon; decimal CourierCostRon }`
- City filtering: `IQueryable<EasyboxLocker>.Where(l => l.IsActive && EF.Functions.ILike(l.City, $"%{city}%"))`
- No auth required on these endpoints — public read-only data

## Dependencies

### Requires
- Story 001-easybox-locker-catalog (EasyboxLockers table + IShippingService)

### Enables
- Bolt 017 (checkout-ui — Angular delivery step calls these endpoints)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| City with no lockers | Returns empty array `[]` — not 404 |
| `type=Easybox` casing `easybox` | Enum parsing is case-insensitive |
| Shipping cost config missing | `IOptions` validation at startup prevents app from starting |

## Out of Scope

- Locker availability / capacity checks
- Distance-based locker sorting
- Address geocoding
