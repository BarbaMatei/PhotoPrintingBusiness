---
id: 004-reclassify-misnamed-unit-tests
unit: 001-test-infrastructure
intent: 028-test-architecture
status: draft
priority: should
created: 2026-06-05T09:30:00Z
assigned_bolt: 062-test-infrastructure
implemented: false
---

# Story: 004-reclassify-misnamed-unit-tests

## User Story

**As a** developer trusting the test pyramid
**I want** the 25 DbContext-constructing "unit" tests moved to Integration
**So that** the pyramid is honest and `Unit/` holds only genuine unit tests

## Acceptance Criteria

- [ ] **Given** the 25 tests that `new PhotoPrintDbContext(...)`, **When** moved to `tests/Integration/ServiceLevel/` (Option A — folder rename), **Then** their `[Fact]` content is unchanged
- [ ] **Given** `Unit/`, **When** the move completes, **Then** it contains only pure `Domain/` logic tests + mocked-dep `Application/` tests
- [ ] **Given** the new folders mirror the intent-027 feature shape, **When** CI runs, **Then** `dotnet test --filter` patterns are updated and the suite is green

## Technical Notes

- Option B (repositories) is rejected by intent 027 P24 — keep the in-memory pattern via the shared helper.

## Dependencies

### Requires
- 003-test-builders

### Enables
- None

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| A reclassified test was actually wrong | Surfaces immediately; fix as a bug, not a move |

## Out of Scope

- Rewriting test logic (content-preserving move).
