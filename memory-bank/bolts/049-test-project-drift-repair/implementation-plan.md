---
stage: plan
bolt: 049-test-project-drift-repair
created: 2026-05-25T11:55:00Z
---

## Implementation Plan: 001-test-project-drift-repair

### Objective

Restore `src/PhotoPrint.Tests` to a green build and run. Fix three test files that drifted from earlier production changes (cart-finishes migration + `IStorageService.SaveAsync` overload) without changing any production code.

### Deliverables

1. `UploadServiceTests.cs` — Moq `Setup`/`Verify` calls extended with `It.IsAny<Guid?>()` for the new `fileId` parameter.
2. `CartServiceTests.cs` — assertions and request constructors rewritten against the new grouped `CartResponseDto` and 4-arg `CartRequest`. Seed helper updated so seeded `CartItem`s carry a valid `SizeId`.
3. `CartControllerIntegrationTests.cs` — same `CartResponseDto` / `CartRequest` rewrites plus updates wherever `CartFactory` is used to construct or assert against cart shape. Inspect `CartFactory.cs` for a centralised request-builder; update once if found.
4. Final `dotnet test src/PhotoPrint.Tests/PhotoPrint.Tests.csproj` runs end-to-end with no `Directory.Build.targets` and no `--filter`.

### Dependencies

- No new NuGets.
- No production-code changes (anti-goal).
- The new shapes already exist in production:
  - `CartRequest(Guid ProductId, Guid SizeId, string? FinishName, IReadOnlyList<CartItemRequest> Items)`
  - `CartResponseDto(IReadOnlyList<CartGroupDto> Groups, decimal Subtotal, int ItemCount)`
  - `CartGroupDto(ProductId, ProductName, SizeId, SizeName, FinishName, Items, TotalCopies, UnitPrice, Subtotal)`
  - `IStorageService.SaveAsync(Stream, Guid ownerId, string ext, CancellationToken ct = default, Guid? fileId = null)`

### Technical Approach

**1. `UploadServiceTests.cs` — 2 errors**

Mechanical:

```csharp
// Before
_storageMock
    .Setup(s => s.SaveAsync(
        It.IsAny<Stream>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
    .ReturnsAsync("owner/abc.jpg");

// After
_storageMock
    .Setup(s => s.SaveAsync(
        It.IsAny<Stream>(), It.IsAny<Guid>(), It.IsAny<string>(),
        It.IsAny<CancellationToken>(), It.IsAny<Guid?>()))
    .ReturnsAsync("owner/abc.jpg");
```

Update every `Setup` and `Verify` lambda. Three call sites at most (`Setup` in ctor + any per-test `Verify`).

**2. `CartServiceTests.cs` — 42 errors**

Two structural changes per test:

**(a) Constructor migration.** Every `new CartRequest(product.Id, [items])` becomes `new CartRequest(product.Id, size.Id, finishName: null, [items])`. The seeded `ProductSize.Id` is reachable via `product.Sizes[0].Id`. `FinishName` is `null` because the seeded product has finishes but the tests never engaged with finish selection.

**(b) Assertion migration.** Three substitutions:

| Old | New |
|---|---|
| `result.Items.Should().HaveCount(N)` | `result.Groups[0].Items.Should().HaveCount(N)` *(single-product tests)* OR `result.Groups.SelectMany(g => g.Items).Should().HaveCount(N)` *(any test that doesn't care about grouping)* |
| `result.Items[0].UnitPrice` | `result.Groups[0].UnitPrice` |
| `result.Items[0].LineTotal` | `result.Groups[0].Subtotal` *(per-group total — same value when one product)* |
| `result.Items[0].UploadId` | `result.Groups[0].Items[0].UploadId` |
| `result.Items[0].Quantity` | `result.Groups[0].Items[0].Quantity` |

**(c) Seed helper bug fix.** `SeedCartItemAsync` currently sets only `ProductId` on the new `CartItem`, leaving `SizeId` as `Guid.Empty`. That works in EF In-Memory today but breaks the moment `CartService.GetCartAsync` groups by `(ProductId, SizeId, FinishName)`. Update `SeedCartItemAsync` to accept and pass a `sizeId` parameter; default from `product.Sizes[0].Id` at call sites.

**Per-test reviewer trail.** Every rewritten assertion must verify the *same behaviour* as before. Where the original test was checking a single-line total of `Quantity × UnitPrice`, the new equivalent is `result.Groups[0].Subtotal` (a group with one item has subtotal = line total).

**3. `CartControllerIntegrationTests.cs` — 26 errors**

Same `(a)` and `(b)` substitutions. `CartFactory` likely has a `SeedProductAsync` returning a Product with Sizes; integration tests can read `product.Sizes[0].Id` the same way.

Quick pass over `CartFactory.cs`:
- If it defines a `BuildCartRequest(product, upload, qty)` helper, fix once. If not, do the call-site rewrite (~5 call sites by error count).
- Pass `FinishName: null` literally; no test in this file engaged with finish selection.

**4. Final verification**

Run `dotnet test src/PhotoPrint.Tests/PhotoPrint.Tests.csproj` with no filter. Acceptance: every test method runs (pass or fail clearly reported). Any test that still fails — and isn't owned by one of the three repaired files — is documented as a separate ops follow-up in the test walkthrough.

### Deviations from Stories

None planned. The four stories map 1:1 to the four deliverables above.

### Acceptance Criteria (consolidated from stories)

- [ ] `dotnet build src/PhotoPrint.Tests` returns 0 errors.
- [ ] `dotnet test src/PhotoPrint.Tests` runs every test discovered (no `--filter`, no `Directory.Build.targets`).
- [ ] Bolt-033's three `Cleanup_*` tests pass without filter.
- [ ] All previously-passing tests in the three repaired files still pass against the new contract.
- [ ] Any test that exercises removed functionality is `[Fact(Skip = "...")]`-ed with a one-line reason, not silently deleted.

### Risk Notes

- **Test intent loss.** The biggest risk in this work is silently weakening an assertion during rewrite. Mitigation: review each rewritten line against the original. The walkthrough will list every method touched and confirm "same behaviour, new shape" per method.
- **Hidden runtime drift.** Production may have shifted further than the compile errors reveal (e.g. `CartService.GetCartAsync` might now require something we don't seed). Mitigation: run the suite, document any new runtime failure, do NOT widen scope to fix it — escalate as a separate intent.
- **`SeedCartItemAsync` `SizeId` fix is a real change in test behaviour.** Previously the helper produced an invalid `CartItem` with `SizeId = Guid.Empty`; the rewrite makes it valid. Document in the walkthrough so a reviewer knows this is intentional.
