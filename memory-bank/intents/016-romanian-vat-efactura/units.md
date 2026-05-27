---
intent: 016-romanian-vat-efactura
phase: inception
status: units-decomposed
created: 2026-05-25T10:15:00Z
updated: 2026-05-25T10:15:00Z
---

# Units: Romanian VAT + e-Factura

## Decomposition

| Unit | Type | Stories | Default Bolt Type |
|------|------|---------|-------------------|
| 001-vat-calculation | backend | US-016-1, US-016-2 | ddd-construction-bolt |
| 002-efactura-generation-and-anaf | backend | US-016-3, US-016-4, US-016-5, US-016-6 | ddd-construction-bolt |

## Rationale

VAT math + numbering sequence is an atomic domain concern (Unit 001). The e-Factura XML pipeline, ANAF transport, PDF rendering, and admin workflow form a separate operational subsystem (Unit 002). Splitting lets VAT ship and surface in the customer flow without blocking on ANAF certificate / sandbox availability.

## Unit Dependency Graph

```text
[001-vat-calculation] ──> [002-efactura-generation-and-anaf]
```

## Execution Order

1. Days 1–5: Unit 001 — VAT computation, schema, sequence.
2. Days 5–14: Unit 002 — XML, ANAF, PDF, admin endpoints.
