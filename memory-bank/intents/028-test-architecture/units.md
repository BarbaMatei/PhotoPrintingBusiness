---
intent: 028-test-architecture
phase: inception
status: units-decomposed
updated: 2026-06-05T09:30:00Z
---

# Test Architecture - Unit Decomposition

## Units Overview

Decomposes into **1 unit**. The four stories are tightly coupled (TimeProvider params feed the Builders; the factory base and reclassification share the same folder reshape), so a single unit avoids artificial cross-unit ordering. `simple-construction-bolt`.

### Unit 1: 001-test-infrastructure

**Description**: Adopt TimeProvider across the older services (P28), promote a shared `PhotoPrintTestApplicationFactory` base + `Builders/` (P27), and reclassify the 25 misnamed unit tests.

**Stories**:
- 001-timeprovider-adoption (P28)
- 002-shared-test-application-factory (P27)
- 003-test-builders (P27)
- 004-reclassify-misnamed-unit-tests (P27)

**Deliverables**: `TimeProvider` injection + banned-API rule; `tests/_Base/PhotoPrintTestApplicationFactory.cs`; `tests/Builders/`; `tests/Integration/ServiceLevel/` reclassification; updated CI filters.

**Dependencies**: Depends on None (but lockstep with intent 027) · Depended by None
**Estimated Complexity**: L

## Requirement-to-Unit Mapping

- **FR-1 (P28)** → `001-test-infrastructure`
- **FR-2 (P27)** → `001-test-infrastructure`

## Unit Dependency Graph

```text
(lockstep with 027) ──> [001-test-infrastructure]
```

## Execution Order

1. Single unit; internal order P28 → P27 (TimeProvider first, then factory/builders/reclassification). Interleave PRs with intent 027.
