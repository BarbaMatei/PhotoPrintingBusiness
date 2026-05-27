---
intent: 023-test-project-drift-repair
phase: inception
status: complete
created: 2026-05-25T11:45:00Z
updated: 2026-05-25T11:45:00Z
source: bolt-033 carry-over (option B from the Stage 2 review)
priority_score: 18
---

# Requirements: Test Project Drift Repair

## Intent Overview

`src/PhotoPrint.Tests` does not compile. Three test files have drifted from production-code changes that landed in earlier intents without their tests being updated:

- `Unit/Services/UploadServiceTests.cs` (2 errors) — `IStorageService.SaveAsync` gained an optional `Guid? fileId = null` parameter; the Moq `Setup` expression tree can't bind to the 4-arg overload anymore.
- `Unit/Services/CartServiceTests.cs` (42 errors) — `CartResponseDto` was restructured from a flat `Items` list to a grouped `Groups[].Items` shape; `CartRequest` gained a required `FinishName` parameter.
- `Integration/CartControllerIntegrationTests.cs` (26 errors) — same `CartResponseDto`/`CartRequest` drift.

This intent restores the test project to a green build, so the full suite (and bolt-033's tests in particular) can run without the temporary isolation trick used in bolt 033.

## Business Goals

| Goal | Success Metric | Priority |
|------|----------------|----------|
| Restore green test build | `dotnet build src/PhotoPrint.Tests` exits 0 with zero errors | Must |
| Restore green test run | `dotnet test src/PhotoPrint.Tests` runs every test (pass or fail clearly reported) | Must |
| Preserve test intent | Each repaired test still asserts the original behaviour against the new contract | Must |
| Unblock CI on `main` | A merged PR for this intent yields a passing CI workflow | Should |

---

## Functional Requirements

### FR-1: Fix `UploadServiceTests` Moq setup
- **Description**: Update the `IStorageService.SaveAsync` Moq `Setup(...)` lambdas so they bind to the current 5-arg interface (including `Guid? fileId`). All assertion behaviour preserved.
- **Acceptance Criteria**:
  - `dotnet build src/PhotoPrint.Tests` no longer reports CS0854 in `UploadServiceTests.cs`.
  - All existing `UploadServiceTests` methods continue to pass.
- **Priority**: Must
- **Related Stories**: US-023-1

### FR-2: Fix `CartServiceTests` against new DTO shape
- **Description**: Adapt every reference to `result.Items`, `result.ProductId`, and the `CartRequest` constructor in `CartServiceTests.cs` to the new grouped contract. Each assertion's *meaning* is preserved — the structural path changes, not the semantic check.
- **Acceptance Criteria**:
  - Zero compile errors from `CartServiceTests.cs`.
  - All test methods pass.
- **Priority**: Must
- **Related Stories**: US-023-2

### FR-3: Fix `CartControllerIntegrationTests`
- **Description**: Same as FR-2 but for the integration test file, plus update any `CartRequest` constructor call sites to provide `FinishName` (null when the seeded product has no finishes).
- **Acceptance Criteria**:
  - Zero compile errors from `CartControllerIntegrationTests.cs`.
  - All test methods pass.
- **Priority**: Must
- **Related Stories**: US-023-3

### FR-4: Remove any bolt-033 isolation residue
- **Description**: Confirm that no `Directory.Build.targets` or `<Compile Remove>` leftovers from bolt 033 exist in `src/PhotoPrint.Tests`. (The bolt-033 cleanup already deleted the file, but this intent's bolt explicitly verifies.)
- **Acceptance Criteria**:
  - `Directory.Build.targets` does not exist under `src/PhotoPrint.Tests/`.
  - Final `dotnet test` run hits every file with no exclusions.
- **Priority**: Must
- **Related Stories**: US-023-4

---

## Non-Functional Requirements

### Performance
| Requirement | Metric | Target |
|-------------|--------|--------|
| Full suite duration | Wall clock on a warm build | < 60 s |

### Reliability
| Requirement | Metric | Target |
|-------------|--------|--------|
| Flake budget | Pre-existing flaky tests | None introduced; quarantine any discovered |

### Maintainability
| Requirement | Standard | Notes |
|-------------|----------|-------|
| Test intent preservation | No semantic regression | Reviewer must verify each rewritten assertion checks the same behaviour as before |

---

## Constraints

### Technical Constraints
- No production-code changes. This is purely test-side repair.
- Do not introduce new test-helper abstractions unless they materially reduce the diff.

### Business Constraints
- Should land before the next intent that needs to add tests (which is nearly every remaining bolt).

---

## Assumptions

| Assumption | Risk if Invalid | Mitigation |
|------------|-----------------|------------|
| The new `Groups[].Items` shape is correct and stable | Repair work churns again on next iteration | Verify by reading `CartService.GetCartAsync` once before rewriting tests |
| Original test intent is recoverable from current assertions | Some tests may be obsolete | Flag obsolete tests in the walkthrough rather than silently dropping |

---

## Open Questions

| Question | Owner | Due Date | Resolution |
|----------|-------|----------|------------|
| Q1: Are any `CartServiceTests` cases truly obsolete (asserting against removed functionality)? | Backend | 2026-06-01 | Decide at Stage 1; default is to preserve as `Skip` with reason if uncertain |
