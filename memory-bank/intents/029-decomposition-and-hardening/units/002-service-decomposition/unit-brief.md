---
unit: 002-service-decomposition
intent: 029-decomposition-and-hardening
phase: inception
status: draft
unit_type: backend
default_bolt_type: simple-construction-bolt
created: 2026-06-05T09:30:00Z
updated: 2026-06-05T09:30:00Z
---

# Unit Brief: Service Decomposition

## Purpose

Decompose the remaining god-classes into the layered shape: split the 424-LOC `AuthService` into three focused services (P13), and finish thinning `WebhooksController` + extract `OrderPhotoQueryService` (P14 residual).

## Scope

### In Scope
- `AuthService` → `AuthService` (login/refresh) + `AccountRegistrationService` + `PasswordResetService`.
- Move `GetOrderPhotosAsync` to `OrderPhotoQueryService`; remove data-access orchestration from `WebhooksController`.

### Out of Scope
- `CreateFromCartAsync` / post-Paid fan-out (intent 027 P25/P11 already extract these).

---

## Assigned Requirements

| FR | Requirement | Priority |
|----|-------------|----------|
| FR-2 (P13) | Decompose AuthService into 3 | Should |
| FR-3 (P14) | Thin WebhooksController + OrderPhotoQueryService | Should |

---

## Domain Concepts

### Key Operations
| Operation | Description | Inputs | Outputs |
|-----------|-------------|--------|---------|
| Split AuthService | Separate register/reset/auth | — | 3 services + 3 test files |
| Extract query service | Move presign-only logic | order id | OrderPhotoQueryService |

---

## Story Summary

| Metric | Count |
|--------|-------|
| Total Stories | 2 |
| Must Have | 0 |
| Should Have | 2 |
| Could Have | 0 |

### Stories

| Story ID | Title | Priority | Status |
|----------|-------|----------|--------|
| 001-decompose-auth-service | Split AuthService into 3 | Should | Planned |
| 002-thin-webhooks-and-order-photo-query | Thin webhooks + OrderPhotoQueryService | Should | Planned |

---

## Dependencies

### Depends On
| Unit | Reason |
|------|--------|
| 027 (all) | New files land in the layered shape |

### Depended By
| Unit | Reason |
|------|--------|
| None | — |

### External Dependencies
| System | Purpose | Risk |
|--------|---------|------|
| None | — | — |

---

## Technical Context

### Suggested Technology
Service split within `Application/Auth/Services/` and `Application/Orders/Services/`.

---

## Constraints

- TimeProvider already injected (intent 028 P28) → deterministic expiry tests.
- Coordinates with intent 027 P14-overlap to avoid double-extraction.
- Auth + order-creation are high-traffic — full integration suites must pass.

---

## Success Criteria

### Functional
- [ ] Three auth services with clean boundaries + own tests.
- [ ] `GetOrderPhotosAsync` in `OrderPhotoQueryService`; webhooks free of direct `_db.SaveChangesAsync`/lazy-load.

### Non-Functional
- [ ] No behaviour change.

### Quality
- [ ] AuthService/Order test files split proportionally; CI green.

---

## Bolt Suggestions

| Bolt | Type | Stories | Objective |
|------|------|---------|-----------|
| 064-service-decomposition | simple | 001, 002 | Auth split + webhook/order residuals |

---

## Notes

After intent 027. Scope P14 to residuals only.
