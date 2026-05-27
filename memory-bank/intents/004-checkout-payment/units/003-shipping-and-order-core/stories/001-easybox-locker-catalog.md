---
id: 001-easybox-locker-catalog
unit: 003-shipping-and-order-core
intent: 004-checkout-payment
status: draft
priority: must
created: 2026-05-21T12:00:00Z
assigned_bolt: 015-shipping-and-order-core
implemented: false
---

# Story: 001-easybox-locker-catalog

## User Story

**As a** developer
**I want** an `EasyboxLocker` entity with seeded Romanian locker location data
**So that** the delivery step can show customers a map of nearby lockers without requiring a live Sameday API integration in Phase 1

## Acceptance Criteria

- [ ] **Given** the EF Core migration runs, **When** the database is updated, **Then** an `EasyboxLockers` table exists with: `Id (UUID)`, `Name`, `Address`, `City`, `County`, `Latitude (double)`, `Longitude (double)`, `IsActive (bool)`
- [ ] **Given** the database is seeded, **When** `EasyboxLockers` is queried, **Then** approximately 200 locker records exist across major Romanian cities
- [ ] **Given** `City` column is queried, **When** a case-insensitive partial match is used, **Then** a non-clustered index on `City` ensures the query is efficient
- [ ] **Given** a future Sameday API integration (Phase 2), **When** `IShippingService.GetLockersAsync` is called, **Then** the seeded data can be replaced by a live API implementation without changing callers

## Technical Notes

- `EasyboxLocker` entity in `Models/` folder; `IEntityTypeConfiguration<EasyboxLocker>` in `Data/Configurations/`
- Seed data: include in a dedicated migration `AddEasyboxLockerSeed` — NOT inline in the main entity migration, to keep migration history clean
- Seed data source: representative ~200 real Sameday Easybox locations from public Sameday locker list
- `IShippingService` interface: `GetLockersAsync(string? city) → Task<IEnumerable<EasyboxLockerDto>>`, `GetShippingCostAsync(ShippingType type) → Task<decimal>`
- Phase 1 implementation: `StaticShippingService` — queries `EasyboxLockers` table from DB

## Dependencies

### Requires
- Bolt 001 (error handling middleware — for 500 on DB errors)

### Enables
- Story 002-shipping-endpoints (needs IShippingService + EasyboxLockers table)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Migration applied to empty DB | Seed runs successfully; 200 rows inserted |
| Seed migration re-applied | EF Core `HasData` is idempotent — no duplicate rows |
| City query with diacritics (e.g., `Cluj-Napoca`) | City stored with diacritics; client must send correct string |
| `IsActive = false` locker | Excluded from `GetLockersAsync` results |

## Out of Scope

- Live Sameday API call (Phase 2 — `IShippingService` swap)
- Locker availability / capacity checks
- Admin locker management UI
