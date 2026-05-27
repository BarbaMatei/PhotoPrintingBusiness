---
intent: 022-coupon-promo-codes
phase: inception
status: units-decomposed
created: 2026-05-25T10:45:00Z
updated: 2026-05-25T10:45:00Z
---

# Units: Coupon / Promo Codes

## Decomposition

| Unit | Type | Stories | Default Bolt Type |
|------|------|---------|-------------------|
| 001-coupon-domain-and-api | backend | US-022-1, US-022-2, US-022-3, US-022-4 | ddd-construction-bolt |
| 002-coupon-frontend | frontend | US-022-5 | simple-construction-bolt |

## Rationale

Schema + customer endpoints + admin endpoints + atomic redemption form a cohesive backend bolt. The Angular cart-page integration + Romanian copy mapping is a separate frontend bolt that lands independently.

## Execution Order

1. Days 1–5: Unit 001 backend.
2. Days 5–8: Unit 002 frontend.
