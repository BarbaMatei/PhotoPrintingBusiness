---
stage: implement
bolt: 034-shipping-cost-server-side
created: 2026-05-25T12:40:00Z
---

## Implementation Walkthrough: 001-shipping-cost-server-side

### Summary

Eliminated the client's ability to influence the order total. `CreateOrderRequest` no longer carries `ShippingCostRon`; the server resolves it from `IShippingService` based on the chosen `DeliveryType`. A FluentValidation validator rejects mismatched delivery configurations with 422. A small resource filter detects the legacy field in the JSON body and logs a Warning — purely for observability, no exception.

### Structure Overview

Six production-side files (2 new, 4 modified) plus two test-side updates (constructor + DTO callers). No interface changes to `IShippingService`. No new NuGet packages.

### Completed Work

- [x] `src/PhotoPrint.API/Filters/DetectLegacyShippingCostFilter.cs` — **new**. `IAsyncResourceFilter` that buffers the JSON body, peeks for a `shippingCostRon` key (case-insensitive), logs `WARN payments.shipping-cost-tampering-attempt path={Path}` if found, and rewinds the body for the model binder. Pure observation.
- [x] `src/PhotoPrint.API/Validators/Payments/CreateOrderRequestValidator.cs` — **new**. FluentValidation rules: `PaymentProcessor` and `DeliveryType` must be defined enum values; Easybox requires `EasyboxLockerId`; Courier requires a `ShippingAddress` with non-empty `City`, `County`, `PostalCode`. Auto-discovered by the existing `AddValidatorsFromAssemblyContaining<Program>` registration.
- [x] `src/PhotoPrint.API/DTOs/Payments/CreateOrderRequest.cs` — dropped the `ShippingCostRon` field. The DTO is now a 4-arg record.
- [x] `src/PhotoPrint.API/Services/OrderService.cs` — constructor now takes `IShippingService`; `CreateFromCartAsync` resolves shipping via `_shipping.GetShippingCostAsync(request.DeliveryType.ToString(), ct)` and writes both `order.ShippingCostRon` and `order.TotalRon` from the resolved value.
- [x] `src/PhotoPrint.API/Controllers/PaymentsController.cs` — class-level `[ServiceFilter(typeof(DetectLegacyShippingCostFilter))]` so both payment-create endpoints inspect the body.
- [x] `src/PhotoPrint.API/Program.cs` — singleton DI registration for the new filter, alongside the existing middleware registrations.
- [x] `src/PhotoPrint.Tests/Unit/Services/OrderServiceTests.cs` — constructor wires a `Mock<IShippingService>` returning `ShippingCostDto(20.00m)` for any input. `MakeRequest` helper became a 3-arg constructor (no `shippingCost` parameter). One test that previously asserted against the helper-supplied cost now asserts against the server-mocked cost — semantically equivalent, both 20.00 RON.
- [x] `src/PhotoPrint.Tests/Integration/PaymentControllerIntegrationTests.cs` — the two static `CreateOrderRequest` instances (`ValidRequest`, `LegacyProcessorRequest`) updated to the 4-arg shape.

### Key Decisions

- **Resource filter chosen over middleware for the legacy-field detector.** Resource filters run before model binding (which is the only window in which `Request.EnableBuffering()` is useful) and can be scoped to a single controller via `[ServiceFilter]`. A middleware would have applied globally; an action filter would have run after binding (too late). The resource filter is precisely the right pipeline slot.
- **`DeliveryType.ToString()` as the lookup key into `IShippingService`.** The existing service signature is `GetShippingCostAsync(string type, ...)`. The enum names `Easybox` / `Courier` exactly match the switch case strings in `StaticShippingService`. Calling `.ToString()` keeps the API change to zero and matches reality on disk — see "Deviations from Plan" for why the story's assumed enum overload was discarded.
- **`order.ShippingCostRon` is still persisted from the resolved value.** The Order entity column is legitimate (audit / display); the only change is that its source is the server, not the request. No schema change.
- **Constructor injection ripples through OrderServiceTests only.** No other consumer of `OrderService` had to change — the controller and any other callers don't care about the constructor.

### Deviations from Plan

None affecting scope. Two clarifications versus the original story tech notes (already documented in `implementation-plan.md`):

1. `IShippingService.GetShippingCostAsync(string type, CancellationToken)` is the actual signature — no enum overload, no county-code parameter. Plan calls it with `request.DeliveryType.ToString()`. Per-county rates land in intent 015 (Sameday integration).
2. `ShippingAddressSnapshot.County` (not `CountyCode`) is the real field name. Validator validates `County` accordingly.

### Dependencies Added

None. No new NuGet packages. `IShippingService` was already DI-registered (`StaticShippingService` in `Program.cs`).

### Developer Notes

- **Pre-existing `OrderServiceTests` continue to pass without semantic re-derivation.** The Moq-supplied 20.00 RON cost happens to equal what the helper used to pass — totals stay identical (`subtotal + 20.00 = 26.00 RON` for the existing seed). This is a happy coincidence that made the test diff smaller; reviewers should not read it as accidental coupling.
- **`OrderServiceTests` and `PaymentControllerIntegrationTests` were the only two places downstream of the DTO change.** Verified by full-solution build.
- **Filter is a transitional artefact.** Once production logs show zero `WARN payments.shipping-cost-tampering-attempt` events for a sustained window (suggest ≥ 4 weeks after FE deploys an updated DTO), the filter and its registration can be removed in a follow-up. Mark this in the dev-side cleanup backlog.
- **The validator's required `ShippingAddress.County` matches the production model.** `SavedAddressValidator` already validates the same field name; bolt 034's validator is consistent with the existing pattern.
- API project builds clean (0 errors, 3 pre-existing warnings). Test project builds clean (0 errors, 1 pre-existing warning).
- Smoke check: full `OrderServiceTests` + `PaymentControllerIntegrationTests` filtered run reports 38 / 38 passed.
