---
intent: 015-sameday-shipping-integration
phase: inception
status: complete
created: 2026-05-25T10:10:00Z
updated: 2026-05-25T10:10:00Z
source: docs/architecture-analysis-2026-05-25.md#3
priority_score: 19
---

# Requirements: Sameday Shipping Integration

## Intent Overview

Today's `StaticShippingService.GenerateAwbAsync` returns `{ Manual: true, Message: "AWB se generează manual în portalul Sameday" }`. Every shipped order requires an operator to hand-copy the recipient into the Sameday web portal. This intent replaces the stub with a real Sameday API integration: token authentication, AWB creation, label retrieval, and asynchronous tracking polling.

## Business Goals

| Goal | Success Metric | Priority |
|------|----------------|----------|
| Eliminate manual AWB generation | ≥ 95% of `Paid` orders auto-receive an `AwbNumber` within 60 s of payment confirmation | Must |
| Provide downloadable shipping label to admin | `Order.AwbLabelUrl` populated and surfaced in admin order detail | Must |
| Auto-progress order status on physical delivery | `Shipped` orders transition to `Delivered` within 30 min of Sameday marking parcel delivered | Should |
| Survive Sameday API outages without losing orders | Failed AWB calls queue for retry; order state never breaks | Must |

---

## Functional Requirements

### FR-1: SamedaySettings + IShippingService implementation
- **Description**: Add `SamedaySettings` configuration (BaseUrl, Username, Password, PickupPointId). Implement `SamedayShippingService : IShippingService` registered when `Sameday:Enabled=true`; otherwise `StaticShippingService` remains the default.
- **Acceptance Criteria**:
  - With `Sameday:Enabled=false`, behaviour is identical to today.
  - With `Sameday:Enabled=true`, `IShippingService` is `SamedayShippingService`.
  - Credentials never logged in plaintext.
- **Priority**: Must
- **Related Stories**: US-015-1

### FR-2: Token authentication with Sameday API
- **Description**: Authenticate via Sameday's token endpoint at startup and on 401; cache token + refresh window.
- **Acceptance Criteria**:
  - Token requests use `IHttpClientFactory` with typed client `SamedayClient`.
  - On 401, retry token fetch then original call exactly once.
  - On repeated 401, raise `SamedayAuthException`; do **not** retry indefinitely.
- **Priority**: Must
- **Related Stories**: US-015-2

### FR-3: AWB generation on order Paid → Processing
- **Description**: When an order transitions to `Paid`, the system asynchronously calls Sameday to create an AWB. Parcel weight estimated as `N × 50 g + 50 g` where `N` is total print count.
- **Acceptance Criteria**:
  - `Order.AwbNumber` populated within the SLA (≤ 60 s p95 from `Paid`).
  - `Order.AwbLabelUrl` populated with the label PDF link (Sameday-hosted).
  - AWB creation failure does NOT block order completion or customer email — failure is logged and retried.
  - Server uses `Polly` with exponential backoff (3 attempts, 1 / 4 / 16 s).
- **Priority**: Must
- **Related Stories**: US-015-3

### FR-4: AWB retry job for failed creations
- **Description**: A `BackgroundService` retries AWB creation for orders in `Paid` state without an `AwbNumber`, hourly.
- **Acceptance Criteria**:
  - Retries up to 24 h then transitions to manual fallback (logs Error + admin notification placeholder).
  - Each retry attempt logged with order id + correlation id.
- **Priority**: Must
- **Related Stories**: US-015-4

### FR-5: ShipmentTrackingJob — Shipped → Delivered
- **Description**: Background job polls Sameday every 15 min for orders in `Shipped` status. On `delivered` status from Sameday, transitions order to `Delivered` and fires the delivery confirmation email.
- **Acceptance Criteria**:
  - `Order.LastTrackingSyncAt` updated every poll.
  - Order auto-transitions at most once (idempotent transition).
  - Polling stops 30 days after `ShippedAt`; remaining orders flagged for manual closure.
- **Priority**: Should
- **Related Stories**: US-015-5

### FR-6: Schema additions
- **Description**: Add `AwbLabelUrl varchar(500) NULL` and `LastTrackingSyncAt timestamptz NULL` to `Orders`.
- **Acceptance Criteria**:
  - EF migration applied cleanly.
  - `AwbNumber` already exists; no change needed there.
- **Priority**: Must
- **Related Stories**: US-015-6

---

## Non-Functional Requirements

### Performance
| Requirement | Metric | Target |
|-------------|--------|--------|
| AWB creation latency | p95 | < 5 s (Sameday side) |
| Tracking poll throughput | 200 active shipments / tick | < 30 s wall clock |

### Reliability
| Requirement | Metric | Target |
|-------------|--------|--------|
| AWB success rate | Auto-created without manual fallback | ≥ 98% |
| API call rate limit compliance | Sameday limits at ~10 req/s | Polly `RateLimitPolicy` + 5 req/s ceiling |

### Security
| Requirement | Standard | Notes |
|-------------|----------|-------|
| Credential storage | dotnet user-secrets (dev) + env vars (prod) | Aligns with intent 018 |

---

## Constraints

### Technical Constraints
- Must reuse existing `OrderStatusMachine` transitions; no new states.
- Must register Sameday via `IHttpClientFactory`.

### Business Constraints
- Sandbox credentials require ops support; do not block bolt on production creds.

---

## Assumptions

| Assumption | Risk if Invalid | Mitigation |
|------------|-----------------|------------|
| Sameday sandbox mirrors production API surface | Integration breaks in prod | First production order is a controlled shadow shipment |
| Parcel weight heuristic acceptable | Pricing variance / overweight rejection | Compute from real product weight in catalog (intent 016+ add invoice line weight) |
| 50 g per print is upper-bound for our paper | Underestimates → Sameday rejects | Bump heuristic; consider table per `ProductSize` |

---

## Open Questions

| Question | Owner | Due Date | Resolution |
|----------|-------|----------|------------|
| Q1: Auto-cancel order if AWB unrecoverable after 24 h? | Product | 2026-06-01 | Pending — recommend manual admin decision in `AdminOrderHub` notification |
| Q2: Use SOAP or REST? Sameday offers both | Backend | 2026-05-30 | Pending — start with REST per documented endpoints |
