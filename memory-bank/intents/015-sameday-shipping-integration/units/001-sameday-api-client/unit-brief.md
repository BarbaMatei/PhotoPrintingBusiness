---
unit: 001-sameday-api-client
intent: 015-sameday-shipping-integration
phase: inception
status: draft
created: 2026-05-25T10:10:00Z
updated: 2026-05-25T10:10:00Z
---

# Unit Brief: Sameday API Client

## Purpose

Stand up a typed HTTP client (`SamedayClient`) authenticated via the Sameday token endpoint, plus the `IShippingService` implementation that calls it. Add schema fields for the new label URL and tracking sync timestamp.

## Scope

### In Scope
- `Configuration/SamedaySettings.cs` + binding + `ValidateOnStart`
- Typed client via `IHttpClientFactory` + `Polly` retry/rate-limit policies
- `SamedayShippingService : IShippingService` — replaces `StaticShippingService` when enabled
- Schema migration adding `Orders.AwbLabelUrl` + `Orders.LastTrackingSyncAt`
- Tests against recorded HTTP fixtures (e.g. `RichardSzalay.MockHttp`)

### Out of Scope
- AWB creation background job (002)
- Tracking poll job (002)
- Admin UI changes (handled in intent 015 follow-up against admin bolt)

---

## Assigned Requirements

| FR | Requirement | Priority |
|----|-------------|----------|
| FR-1 | SamedaySettings + IShippingService implementation | Must |
| FR-2 | Token authentication with Sameday API | Must |
| FR-6 | Schema additions | Must |

---

## Story Summary

| Story ID | Title | Priority |
|----------|-------|----------|
| 001-sameday-settings-and-typed-client | Settings, typed client, Polly policies | Must |
| 002-token-auth-and-refresh | Token endpoint + 401 retry once | Must |
| 003-sameday-schema-additions | EF migration for `AwbLabelUrl` and `LastTrackingSyncAt` | Must |

---

## Dependencies

### Depends On
- bolt 015-shipping-and-order-core (existing `IShippingService` interface)

### Depended By
- 002-awb-and-tracking-jobs
