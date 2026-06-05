---
unit: 001-test-infrastructure
intent: 028-test-architecture
phase: inception
status: draft
unit_type: backend
default_bolt_type: simple-construction-bolt
created: 2026-06-05T09:30:00Z
updated: 2026-06-05T09:30:00Z
---

# Unit Brief: Test Infrastructure

## Purpose

Make the test suite honest and deterministic: finish `TimeProvider` adoption (P28), promote a shared factory base + Builders (P27), and reclassify the 25 misnamed unit tests. Enables the intent-027 refactor to be reviewable PR-by-PR.

## Scope

### In Scope
- Inject `TimeProvider` across the 35 older files (63 calls); banned-API rule.
- Promote `ObservabilityFactoryBase` → `PhotoPrintTestApplicationFactory`; refactor 11 factories to inherit it.
- `tests/Builders/` for the 6 most-used entities.
- Move 25 DbContext-constructing tests to `tests/Integration/ServiceLevel/`.

### Out of Scope
- The intent-027 production refactor (this unit is its test companion).
- Introducing repositories (rejected by intent 027 P24).

---

## Assigned Requirements

| FR | Requirement | Priority |
|----|-------------|----------|
| FR-1 (P28) | Adopt TimeProvider consistently | Should |
| FR-2 (P27) | Shared factory base + Builders + reclassification | Should |

---

## Domain Concepts

### Key Entities
| Entity | Description | Attributes |
|--------|-------------|------------|
| PhotoPrintTestApplicationFactory | Shared WAF base | 25 standard config keys, InMemory swap, no-op email |
| Builders | Fluent test data | UserBuilder, OrderBuilder, CartItemBuilder, InvoiceBuilder, UploadBuilder, … |

### Key Operations
| Operation | Description | Inputs | Outputs |
|-----------|-------------|--------|---------|
| Inject clock | Replace UtcNow with TimeProvider | service ctors | FakeTimeProvider-driven tests |
| Reclassify | Move DbContext tests out of Unit/ | misnamed tests | Integration/ServiceLevel |

---

## Story Summary

| Metric | Count |
|--------|-------|
| Total Stories | 4 |
| Must Have | 0 |
| Should Have | 4 |
| Could Have | 0 |

### Stories

| Story ID | Title | Priority | Status |
|----------|-------|----------|--------|
| 001-timeprovider-adoption | TimeProvider across older services | Should | Planned |
| 002-shared-test-application-factory | Promote shared WAF base | Should | Planned |
| 003-test-builders | Fluent Builders | Should | Planned |
| 004-reclassify-misnamed-unit-tests | Move DbContext tests to Integration | Should | Planned |

---

## Dependencies

### Depends On
| Unit | Reason |
|------|--------|
| (lockstep) 027 units | Folder shape must match the production refactor |

### Depended By
| Unit | Reason |
|------|--------|
| None | — |

### External Dependencies
| System | Purpose | Risk |
|--------|---------|------|
| FakeTimeProvider | Deterministic clock | Low |

---

## Technical Context

### Suggested Technology
xUnit `IClassFixture<T>`, `WebApplicationFactory<Program>`, `Microsoft.Extensions.TimeProvider.Testing`, BannedApiAnalyzers (UtcNow rule).

---

## Constraints

- Ship P28 before P27; interleave PRs with intent 027.
- `IClassFixture` share-per-class ordering gotchas — verify no per-test isolation breaks.

---

## Success Criteria

### Functional
- [ ] Zero raw `DateTimeOffset.UtcNow` in Application/Infrastructure; FakeTimeProvider used in ≥1 scenario per refactored service.
- [ ] 11 factories inherit the shared base; standard config in one file.
- [ ] Builders cover 6 entities; misnamed tests under Integration/ServiceLevel.

### Non-Functional
- [ ] No production behaviour change.

### Quality
- [ ] Full suite green; CI filters updated.

---

## Bolt Suggestions

| Bolt | Type | Stories | Objective |
|------|------|---------|-----------|
| 062-test-infrastructure | simple | 001–004 | TimeProvider + factory base + builders + reclassification |

---

## Notes

Lockstep with intent 027 bolts 059–061 — interleave, don't sequence.
