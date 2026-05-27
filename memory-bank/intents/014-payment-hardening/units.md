---
intent: 014-payment-hardening
phase: inception
status: units-decomposed
created: 2026-05-25T10:05:00Z
updated: 2026-05-25T10:05:00Z
---

# Units: Payment Hardening

## Decomposition

| Unit | Type | Stories | Default Bolt Type |
|------|------|---------|-------------------|
| 001-shipping-cost-server-side | backend | US-014-1, US-014-2 | simple-construction-bolt |
| 002-payment-idempotency | backend | US-014-3, US-014-4, US-014-5 | simple-construction-bolt |

## Rationale

The two fixes touch different layers (DTO/validator vs. payment controllers + migration) and have different blast radii. Splitting them lets the shipping-cost fix ship without depending on the idempotency migration; the migration can land separately and be smoke-tested first.

## Unit Dependency Graph

```text
[001-shipping-cost-server-side]
                                \
                                 ───> [Order.TotalRon authoritative]
                                /
[002-payment-idempotency]
```

## Execution Order

1. Day 1–2: 001-shipping-cost-server-side (cheaper, no migration).
2. Day 2–3: 002-payment-idempotency (migration + idempotency wiring).
