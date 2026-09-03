---
intent: 029-decomposition-and-hardening
phase: inception
status: units-decomposed
updated: 2026-06-05T09:30:00Z
---

# God-Method Decomposition & Access Hardening - Unit Decomposition

## Units Overview

Decomposes into **3 backend units**: access hardening (independent, ship first), service decomposition, and persistence config. All `simple-construction-bolt`.

### Unit 1: 001-access-hardening
**Description**: Global per-IP rate limit + centralised admin policy constant (P08).
**Stories**: 001-global-rate-limit, 002-admin-policy-constant
**Deliverables**: `GlobalRateLimitPolicy`; `Policies.Admin` constant + `AddAuthorization` registration; 6 controllers switched to `[Authorize(Policy = Policies.Admin)]`.
**Dependencies**: Depends on 025/001 (P05 real client IP) · Depended by None
**Estimated Complexity**: S

### Unit 2: 002-service-decomposition
**Description**: Split AuthService into 3 (P13); thin WebhooksController + extract OrderPhotoQueryService (P14, residual scope).
**Stories**: 001-decompose-auth-service, 002-thin-webhooks-and-order-photo-query
**Deliverables**: `AuthService`/`AccountRegistrationService`/`PasswordResetService`; `OrderPhotoQueryService`; thinned `WebhooksController`.
**Dependencies**: Depends on 027 (layered shape) + coordinates with 027/003 handlers · Depended by None
**Estimated Complexity**: M

### Unit 3: 003-persistence-config
**Description**: Per-entity `IEntityTypeConfiguration<T>` files; shrink `DbContext` < 100 LOC (P15).
**Stories**: 001-per-entity-configurations
**Deliverables**: 17 `Data/Configurations/<Entity>Configuration.cs`; `ApplyConfigurationsFromAssembly` in `OnModelCreating`.
**Dependencies**: Depends on 027 (placement under Infrastructure/Data) · Depended by None
**Estimated Complexity**: S

## Requirement-to-Unit Mapping

- **FR-1 (P08)** → `001-access-hardening`
- **FR-2 (P13)** → `002-service-decomposition`
- **FR-3 (P14)** → `002-service-decomposition`
- **FR-4 (P15)** → `003-persistence-config`

## Unit Dependency Graph

```text
025/001 (P05) ──> [001-access-hardening]
027 ──> [002-service-decomposition]
027 ──> [003-persistence-config]
```

## Execution Order

1. 001-access-hardening (after 025 P05)
2. 003-persistence-config (parallel; Data/ only)
3. 002-service-decomposition (after 027; coordinates with 027/003)
