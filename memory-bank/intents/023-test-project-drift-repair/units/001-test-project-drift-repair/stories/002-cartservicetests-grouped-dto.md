---
id: 002-cartservicetests-grouped-dto
unit: 001-test-project-drift-repair
intent: 023-test-project-drift-repair
status: complete
priority: must
created: 2026-05-25T11:45:00Z
assigned_bolt: 049-test-project-drift-repair
implemented: true
implemented_at: 2026-05-25T12:20:00Z
---

# Story: 002-cartservicetests-grouped-dto

## User Story

**As** the test project
**I want** `CartServiceTests` to assert against the current grouped `CartResponseDto` shape
**So that** the file compiles and every test's intent is preserved against the new contract

## Acceptance Criteria

- [ ] `dotnet build src/PhotoPrint.Tests` no longer reports any error in `Unit/Services/CartServiceTests.cs`.
- [ ] Every `result.Items` reference rewritten as `result.Groups[0].Items` (or `result.Groups.SelectMany(g => g.Items)` where the original test was group-agnostic).
- [ ] Every `result.ProductId` and similar flat-shape property rewritten against the new structure.
- [ ] Every `CartRequest(...)` constructor call passes `FinishName` (null when the seeded product has no finishes; the actual finish name when seeded with one).
- [ ] All test methods pass.
- [ ] Per-test semantic check: each rewritten assertion verifies the same behaviour as before (a reviewer should be able to point at any rewritten line and trace it back to the original intent).

## Technical Notes

- New shape: `CartResponseDto(IReadOnlyList<CartGroupDto> Groups, decimal Subtotal, int ItemCount)`.
- `CartGroupDto` carries `ProductId`, `ProductName`, `SizeId`, `SizeName`, `FinishName`, `Items`, `TotalCopies`, `UnitPrice`, `Subtotal`.
- `CartItemDto` (inside `Items`) carries the per-line numbers like `Quantity`, `LineTotal`, etc.
- Where a test asserted `result.Items.Should().HaveCount(N)` on a flat list, the equivalent against the grouped shape is `result.Groups.SelectMany(g => g.Items).Should().HaveCount(N)` OR `result.Groups[0].Items.Should().HaveCount(N)` when the test was about a single product.

## Dependencies

### Requires
- None

### Enables
- 003-cart-controller-tests-grouped-dto (shares the same rewrite playbook), 004-suite-green-verification

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Test asserted `result.UnitPrice` directly | Rewrite to `result.Groups[0].UnitPrice` |
| Test asserted item counts across multiple products | Switch to `Groups.SelectMany(...)` or assert per group |
| Test was actually exercising removed behaviour | Mark `[Fact(Skip = "Removed by intent 0XX — see comment")]` rather than deleting silently; flag in walkthrough |

## Out of Scope

- Adding new test cases for the grouped behaviour.
