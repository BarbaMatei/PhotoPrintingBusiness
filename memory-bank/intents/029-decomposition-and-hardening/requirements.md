---
intent: 029-decomposition-and-hardening
phase: inception
status: inception-complete
created: 2026-06-05T09:00:00Z
updated: 2026-06-05T09:00:00Z
source: docs/analysis/architect-review-2026-06-03.md (Group 5 — P08, P13, P14, P15)
priority_score: 17
---

# Requirements: God-Method Decomposition & Access Hardening

## Intent Overview

Once the layered folder structure (intent 027) is in place, the oversized classes get decomposed **into** the new shape rather than the old flat one. This intent covers the four Group-5 proposals: split the 424-LOC `AuthService` into three focused services (P13); finish thinning `WebhooksController` and `OrderService`'s god-methods, moving `GetOrderPhotosAsync` to its own query service (P14); move the 437-LOC `OnModelCreating` into per-entity `IEntityTypeConfiguration<T>` files (P15); and add a global fallback rate limit plus a centralised admin-role policy constant to kill the string-literal `[Authorize(Roles="Admin")]` footgun (P08). P08 is small and independent (ship first); P15 touches only `Data/`; P13/P14 touch overlapping service surfaces and coordinate with intent 027's handler work (P25/P11) to avoid double-extracting `CreateFromCartAsync`.

## Business Goals

| Goal | Success Metric | Priority |
|------|----------------|----------|
| Eliminate the admin-role typo footgun | Zero string-literal `Roles="Admin"`; one `Policies.Admin` constant | Should |
| Bound abuse of the non-auth API surface | Global per-IP rate limit on `/api/*` | Should |
| Focused, testable auth services | `AuthService` split into 3; each < ~200 LOC with its own tests | Should |
| Readable persistence config + per-entity diffs | `DbContext` ≤ 100 LOC; one config file per entity | Could |

---

## Functional Requirements

### FR-1 (P08): Global rate limit + per-endpoint admin role policy constant
- **Description**: (a) Add a global fallback rate limiter (e.g. 200 req/min/IP sliding window) on `/api/*` that the existing auth-specific policies override; key it on the real client IP (depends on `ForwardedHeadersMiddleware`, intent 025 P05). (b) Introduce `public static class Policies { public const string Admin = "AdminRole"; }`, register it via `AddAuthorization`, and replace the 6 string-literal `[Authorize(Roles="Admin")]` usages.
- **Acceptance Criteria**:
  - Global limiter registered; auth endpoints still honour their stricter policies.
  - All admin controllers use `[Authorize(Policy = Policies.Admin)]`; no `Roles="Admin"` literal remains.
  - Integration test: anonymous request to `/api/admin/*` returns 401 (not 403); over-limit request to a public endpoint returns 429.
  - Limit tuned during pre-launch load test (admin uploading 30 photos in 10s must not be throttled).
- **Priority**: Should
- **Related Stories**: TBD

### FR-2 (P13): Decompose `AuthService` (424 LOC) into three services
- **Description**: Split into `IAccountRegistrationService` (Register, ConfirmEmail, ResendConfirmation), `IPasswordResetService` (Forgot, Reset), and a slimmer `IAuthService` (Login, Refresh, RevokeRefreshToken). New files land in `Application/Auth/Services/` (post-027 shape).
- **Acceptance Criteria**:
  - Three services with clean, non-overlapping responsibilities; each has its own test file (`AuthServiceTests.cs` 636 LOC split proportionally).
  - No behaviour change; full auth integration suite green.
  - `TimeProvider` already injected (intent 028 P28) so expiry logic is deterministically tested.
- **Priority**: Should
- **Related Stories**: TBD

### FR-3 (P14): Finish decomposing `WebhooksController` + `OrderService` god-methods
- **Description**: Make `WebhooksController` thin (signature verification + routing only) and reduce `OrderService`'s 145-LOC `CreateFromCartAsync`. Move `GetOrderPhotosAsync` (pure cloud-presign, wrong class) to a new `OrderPhotoQueryService`. **Coordinates with intent 027**: the `CreateOrderHandler` (P25) and `OrderPaidEventDispatcher` (P11) already carry most of this; P14's residual scope is the `OrderPhotoQueryService` extraction and final controller/service cleanup — avoid double-extraction.
- **Acceptance Criteria**:
  - `GetOrderPhotosAsync` lives in `OrderPhotoQueryService`; `IOrderService` delegates a one-liner.
  - `WebhooksController` contains no data-access orchestration (no direct `_db.SaveChangesAsync` / lazy-load); it routes to handlers/dispatcher.
  - No behaviour change; payment/webhook integration suite green (highest-traffic write path).
- **Priority**: Should
- **Related Stories**: TBD

### FR-4 (P15): Per-entity `IEntityTypeConfiguration<T>` files; shrink `DbContext` < 100 LOC
- **Description**: Move each of the 17 entities' inline `modelBuilder.Entity<X>(...)` blocks from `OnModelCreating` (437 LOC) into `Data/Configurations/<Entity>Configuration.cs`, and replace the body with `ApplyConfigurationsFromAssembly(...)` (keeping the cross-cutting SQLite-`DateTimeOffset` value-converter loop).
- **Acceptance Criteria**:
  - One config file per entity; `OnModelCreating` ≤ 100 LOC.
  - `Add-Migration NoOpRefactorVerify` produces empty up/down (no dropped `HasIndex`/`HasConversion` — visually inspected).
- **Priority**: Could
- **Related Stories**: TBD

---

## Non-Functional Requirements

### Security
| Requirement | Standard | Notes |
|-------------|----------|-------|
| Admin authorization | Centralised policy constant | No typo-able role string |
| API abuse protection | Per-IP global rate limit | 200 req/min default (tunable) |

### Reliability
| Requirement | Metric | Target |
|-------------|--------|--------|
| No behaviour change (decompositions) | Test suite | Baseline maintained; zero migration drift (P15) |

### Performance
| Requirement | Metric | Target |
|-------------|--------|--------|
| Global limiter overhead | Per-request | Negligible; legitimate bursts not throttled |

---

## Constraints

### Technical Constraints
- **Ships after intent 027** — files land in `Application/Auth/Services/`, `Application/Orders/Handlers/`, etc., not the old flat folders.
- P08 keys on the real client IP → depends on intent 025 P05 (`ForwardedHeadersMiddleware`).
- P14 must coordinate with 027's P25/P11 to avoid re-extracting `CreateFromCartAsync` / the post-Paid fan-out.
- Ship order within group: P08 (smallest, unrelated) → P15 (parallel, `Data/` only) → P14 → P13 (overlapping service surfaces).

### Business Constraints
- P08 (admin policy constant) is a soft pre-launch must-have (string-role footgun). Decompositions (P13/P14/P15) are post-launch / non-blocking.

---

## Assumptions

| Assumption | Risk if Invalid | Mitigation |
|------------|-----------------|------------|
| 200 req/min is a safe global default | Legitimate burst users throttled | Tune during pre-launch load test; per-route overrides |
| Intent 027's handlers already cover most of P14 | Double-extraction / conflict | Scope P14 to residuals (`OrderPhotoQueryService` + cleanup) |
| Per-entity config translates 1:1 | A constraint/index silently dropped | `Add-Migration` verify must show empty diff |

---

## Open Questions

| Question | Owner | Due Date | Resolution |
|----------|-------|----------|------------|
| Q1: Global rate-limit value and partition key (IP vs IP+route)? | Dev/Ops | 2026-07-03 | Recommend 200/min/IP sliding; finalise after load test |
| Q2: Does P25 (intent 027) fully subsume `CreateFromCartAsync`, leaving only `OrderPhotoQueryService` for P14? | Dev | 2026-07-03 | Confirm during 027 construction |
| Q3: Split `AuthService` into exactly 3, or also extract `ITokenService`/email-token concerns? | Dev | 2026-07-03 | Recommend 3; token services already separate |
