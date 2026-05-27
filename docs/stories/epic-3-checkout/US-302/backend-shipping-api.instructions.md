# US-302 — Shipping API (Backend)

## Story
**As a** system  
**I want to** provide locker list and shipping cost; generate AWB via Sameday API automatically where possible

## Type
BACKEND — ASP.NET Core

## Epic
EPIC-3 | Checkout & Plată

## Dependencies
- US-801 (Error handling)
- Database: `EasyboxLockers` table

## Acceptance Criteria

1. **`GET /api/shipping/lockers?city=`** — Phase 1: returns seeded static locker list (~200 lockers); Phase 2: proxies Sameday Lockers API
2. **`GET /api/shipping/cost?type=Easybox|Courier`** — returns `{costRon}`; Easybox=20 RON, Courier=25 RON (config-driven)
3. **`POST /api/shipping/awb {orderId}`** — Phase 2 (Sameday API): generates AWB automatically, stores `AwbNumber` + `TrackingUrl` on Order
4. **Phase 1 fallback**: endpoint returns `{manual: true}` — operator generates AWB in Sameday portal manually; admin UI shows `AWB manual required` flag
5. **Sameday API credentials** stored in environment config; `IShippingService` abstraction allows switching

## Technical Notes

### Endpoints
```
GET /api/shipping/lockers?city=București
→ 200 [
  { "id": "uuid", "samedayId": "SAM123", "name": "Easybox Mega Mall", "address": "Bd. Pierre de Coubertin 3-5", "city": "București", "lat": 44.4268, "lng": 26.1025, "isActive": true }
]
```

```
GET /api/shipping/cost?type=Easybox
→ 200 { "costRon": 20.00 }
```

```
POST /api/shipping/awb (Phase 2 — Admin only)
Authorization: Bearer {admin-jwt}
{ "orderId": "uuid" }
→ 200 { "awbNumber": "SAM123456", "trackingUrl": "https://..." }
→ 200 { "manual": true } (Phase 1 fallback)
```

### Implementation Details
- `EasyboxLockers` table: `Id`, `SamedayId`, `Name`, `Address`, `City`, `Lat`, `Lng`, `IsActive`
- Seed ~200 Romanian Easybox lockers in migration (hardcoded or from CSV)
- City filter: case-insensitive LIKE query on `City` column; indexed
- Shipping cost: read from `appsettings.json` → `Shipping:EasyboxCostRon`, `Shipping:CourierCostRon`
- `IShippingService` interface: `GetLockersAsync(city)`, `GetCostAsync(type)`, `GenerateAwbAsync(orderId)`
- Phase 1: `GenerateAwbAsync` returns `{ manual: true }`
- Phase 2: implement `SamedayShippingService` that calls Sameday API

## Files to Create/Modify
- `src/PhotoPrint.API/Controllers/ShippingController.cs`
- `src/PhotoPrint.API/DTOs/Shipping/LockerDto.cs`
- `src/PhotoPrint.API/DTOs/Shipping/ShippingCostDto.cs`
- `src/PhotoPrint.API/Models/EasyboxLocker.cs`
- `src/PhotoPrint.API/Services/IShippingService.cs` + `StaticShippingService.cs`
- EF Core migration + seed data for EasyboxLockers

## Testing
- Unit test: locker search by city
- Unit test: shipping cost returns config values
- Unit test: Phase 1 AWB returns manual flag
- Integration test: GET /api/shipping/lockers with seeded data
