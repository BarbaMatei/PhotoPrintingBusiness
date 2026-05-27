---
stage: implement
bolt: 049-test-project-drift-repair
created: 2026-05-25T12:05:00Z
---

## Implementation Walkthrough: 001-test-project-drift-repair

### Summary

Mechanical repair of three drifted test files plus one supporting test factory. Zero production-code changes. The full test project now builds with zero errors.

### Structure Overview

Four edits across three test files and one factory:
- Moq lambda widening in `UploadServiceTests`.
- DTO-shape rewrite + `SizeId` threading in `CartServiceTests` and its `SeedCartItemAsync` helper.
- DTO-shape rewrite + `SizeId` threading + `Sizes` navigation hydration in `CartControllerIntegrationTests` and `CartFactory`.

### Completed Work

- [x] `src/PhotoPrint.Tests/Unit/Services/UploadServiceTests.cs` — the single `_storageMock.Setup(s => s.SaveAsync(...))` lambda now includes `It.IsAny<Guid?>()` as a fifth argument for the new optional `fileId` parameter on the production interface.
- [x] `src/PhotoPrint.Tests/Unit/Services/CartServiceTests.cs` — `SeedCartItemAsync` helper now takes a `Guid sizeId` parameter and assigns it to the `CartItem.SizeId` field (previously left as `Guid.Empty`); every test method threads `product.Sizes.First().Id` through; every `CartRequest(...)` constructor uses the 4-arg form with `FinishName: null`; every `result.Items` assertion was rewritten against the grouped shape (`result.Groups[0].Items[...]` for single-product tests, `result.Groups.SelectMany(g => g.Items)` for merge tests that didn't care about grouping).
- [x] `src/PhotoPrint.Tests/Integration/CartControllerIntegrationTests.cs` — same DTO-shape rewrites as the unit-test file; every `CartRequest(...)` migrated to the 4-arg form; reads `size.Id` from `product.Sizes.First()`.
- [x] `src/PhotoPrint.Tests/Integration/CartFactory.cs` — `SeedProductAsync` now hydrates `product.Sizes`, `size.PricingTiers`, and `product.Finishes` navigation collections so callers can read `product.Sizes.First()` directly; `SeedCartItemAsync` accepts a `Guid sizeId` parameter and persists it on the new `CartItem`.

### Key Decisions

- **Named-argument convention `FinishName:` (PascalCase).** Positional record parameters are PascalCase by C# convention; named-argument syntax must use the declared property name. An initial attempt with `finishName:` (camelCase) failed to compile across 9 call sites — fixed via `replace_all` once recognised.
- **`result.Groups.SelectMany(...)` for merge tests.** Merge-cart tests semantically asserted against the *total* set of items, not against any specific grouping. Using `SelectMany` preserves the original intent without forcing a particular group structure on the test.
- **`SeedCartItemAsync` SizeId fix is intentional behaviour change.** The pre-existing helper produced invalid `CartItem` rows (`SizeId = Guid.Empty`) that happened to work under EF In-Memory because grouping by `Guid.Empty` collapsed identical groups silently. The new tests would have masked a real bug if the helper had stayed as it was — anytime production `CartService.GetCartAsync` started grouping by `(ProductId, SizeId, FinishName)`, the assertions would have silently shifted meaning. Threading a real `SizeId` is the correct fix.
- **`CartFactory.SeedProductAsync` now sets the `Sizes` navigation property.** EF Core's identity tracking populated `product.Id` after `SaveChangesAsync` but `Sizes` remained null because it was never assigned. Assigning `product.Sizes = [size]` explicitly mirrors the `CartServiceTests` pattern and lets every caller read `product.Sizes.First()` without a separate query.

### Deviations from Plan

None. The plan's "exact substitutions" table held up across both unit and integration files. The PascalCase named-argument fix was a one-line `replace_all` and didn't require revisiting the plan.

### Dependencies Added

None. No new NuGet packages. No production-code changes.

### Developer Notes

- **`SetCart_RejectsNotFoundUpload`'s line 197 became line 200 after the upgrade.** Line numbers shifted because of added `var size = product.Sizes.First();` declarations. All assertions still verify the same exception types.
- **No test was marked `[Fact(Skip = ...)]`.** Every original test had an equivalent assertion against the new shape — no behaviour was removed by production drift, only its representation changed.
- The full test project now compiles. Stage 3 will run `dotnet test` end-to-end to verify behaviour.
