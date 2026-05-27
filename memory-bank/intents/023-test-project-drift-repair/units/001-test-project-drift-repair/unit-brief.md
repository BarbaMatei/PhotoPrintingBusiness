---
unit: 001-test-project-drift-repair
intent: 023-test-project-drift-repair
phase: inception
status: draft
created: 2026-05-25T11:45:00Z
updated: 2026-05-25T11:45:00Z
---

# Unit Brief: Test Project Drift Repair

## Purpose

Restore `src/PhotoPrint.Tests` to a green build and a green test run by repairing three test files that drifted from production-code changes landed in earlier intents.

## Scope

### In Scope
- `src/PhotoPrint.Tests/Unit/Services/UploadServiceTests.cs` — fix Moq setup against the new `IStorageService.SaveAsync` 5-arg overload
- `src/PhotoPrint.Tests/Unit/Services/CartServiceTests.cs` — adapt every assertion to the grouped `CartResponseDto.Groups[].Items` shape; pass `FinishName` in every `CartRequest` constructor
- `src/PhotoPrint.Tests/Integration/CartControllerIntegrationTests.cs` — same adaptations for the integration test
- Final verification: full `dotnet test` runs to completion with no exclusions

### Out of Scope
- Adding new tests or new helpers
- Production-code changes
- Any test files that already build (the architecture analysis flagged ~20 test files; only these three need work)

---

## Assigned Requirements

| FR | Requirement | Priority |
|----|-------------|----------|
| FR-1 | Fix UploadServiceTests Moq setup | Must |
| FR-2 | Fix CartServiceTests against new DTO shape | Must |
| FR-3 | Fix CartControllerIntegrationTests | Must |
| FR-4 | Remove any bolt-033 isolation residue | Must |

---

## Domain Concepts

### Affected Production Types
| Type | Change That Caused Drift |
|------|-------------------------|
| `IStorageService.SaveAsync` | Optional `Guid? fileId = null` parameter added (intent 004 work) |
| `CartResponseDto` | Restructured from flat `Items` to `Groups[].Items` shape with per-group totals |
| `CartRequest` | Required `FinishName` parameter added (intent 012 cart-finishes work, migration `20260524131359_AddFinishNameToCartItem`) |

---

## Story Summary

| Metric | Count |
|--------|-------|
| Total Stories | 4 |
| Must Have | 4 |
| Should Have | 0 |
| Could Have | 0 |

### Stories

| Story ID | Title | Priority | Status |
|----------|-------|----------|--------|
| 001-uploadservicetests-mock-fileid | Update Moq setup for new SaveAsync overload | Must | Planned |
| 002-cartservicetests-grouped-dto | Adapt CartServiceTests assertions to `Groups[].Items` shape | Must | Planned |
| 003-cart-controller-tests-grouped-dto | Adapt CartControllerIntegrationTests + pass FinishName | Must | Planned |
| 004-suite-green-verification | Final `dotnet test` runs end-to-end without exclusions | Must | Planned |

---

## Dependencies

### Depends On
- None (drift is pre-existing on `main`)

### Depended By
- Bolt-033 tests run end-to-end without the `Directory.Build.targets` isolation trick.
- Every future bolt that adds tests against `IStorageService`, `CartService`, or cart endpoints.
