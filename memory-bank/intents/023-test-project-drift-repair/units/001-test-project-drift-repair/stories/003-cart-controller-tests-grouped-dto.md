---
id: 003-cart-controller-tests-grouped-dto
unit: 001-test-project-drift-repair
intent: 023-test-project-drift-repair
status: implemented
priority: must
created: 2026-05-25T11:45:00Z
assigned_bolt: 049-test-project-drift-repair
implemented: true
implemented_at: 2026-05-25T12:20:00Z
---

# Story: 003-cart-controller-tests-grouped-dto

## User Story

**As** the test project
**I want** `CartControllerIntegrationTests` to assert against the current grouped `CartResponseDto` and pass `FinishName` on every `CartRequest`
**So that** the integration tests exercise the real HTTP surface against the current contract

## Acceptance Criteria

- [ ] `dotnet build src/PhotoPrint.Tests` no longer reports any error in `Integration/CartControllerIntegrationTests.cs`.
- [ ] Every `CartRequest(...)` constructor call supplies `FinishName` (null or the seeded finish, mirroring the seeded product).
- [ ] Every assertion against the response navigates the new `Groups[].Items` shape.
- [ ] All test methods pass.
- [ ] Per-test semantic check identical to story 002.

## Technical Notes

- Tests deserialize the controller's JSON response into `CartResponseDto`; no production-side serialization change required.
- Validator `CartRequestValidator` will reject requests missing required fields — the integration tests previously relied on the *old* constructor shape. Add `FinishName: null` literally everywhere the seeded product has no finishes.

## Dependencies

### Requires
- 002-cartservicetests-grouped-dto (rewrite playbook stabilises here first)

### Enables
- 004-suite-green-verification

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Integration factory `CartFactory` carries a pre-baked `CartRequest` helper | Update the helper once instead of every call site |
| Test seeded a product *with* finishes but never passed `FinishName` | Choose the seeded finish; document the choice in a one-line comment if non-obvious |

## Out of Scope

- Re-running integration tests against a real Postgres instance (xUnit's in-process `WebApplicationFactory` is the existing pattern).
