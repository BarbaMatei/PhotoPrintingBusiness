---
intent: 015-sameday-shipping-integration
phase: inception
status: units-decomposed
created: 2026-05-25T10:10:00Z
updated: 2026-05-25T10:10:00Z
---

# Units: Sameday Shipping Integration

## Decomposition

| Unit | Type | Stories | Default Bolt Type |
|------|------|---------|-------------------|
| 001-sameday-api-client | backend | US-015-1, US-015-2, US-015-6 | ddd-construction-bolt |
| 002-awb-and-tracking-jobs | backend | US-015-3, US-015-4, US-015-5 | ddd-construction-bolt |

## Rationale

Splits the integration into the **client layer** (auth, HTTP plumbing, settings, schema) and the **operational layer** (AWB creation pipeline + tracking job). The client can be developed and unit-tested against recorded fixtures before the jobs land, reducing coupling.

## Unit Dependency Graph

```text
[001-sameday-api-client] ──> [002-awb-and-tracking-jobs]
```

## Execution Order

1. Days 1–4: 001-sameday-api-client (client + settings + migration).
2. Days 4–8: 002-awb-and-tracking-jobs (background jobs + status transitions).
