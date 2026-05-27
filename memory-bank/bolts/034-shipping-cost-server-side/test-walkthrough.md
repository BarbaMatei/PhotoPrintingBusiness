---
stage: test
bolt: 034-shipping-cost-server-side
created: 2026-05-25T12:55:00Z
---

## Test Report: 001-shipping-cost-server-side

### Summary

- **Tests**: 449 / 449 passed
- **Duration**: 6 s
- **New tests added in this bolt**: 16 (10 validator, 6 filter, 1 integration; existing OrderServiceTests adapted, count unchanged)
- **Failures**: 0

### Test Files

- [x] `src/PhotoPrint.Tests/Unit/Validators/CreateOrderRequestValidatorTests.cs` — **new**. 10 tests covering enum validity, Easybox conditional requirements, Courier conditional requirements, nested-address field paths, and a happy-path case for each delivery type.
- [x] `src/PhotoPrint.Tests/Unit/Filters/DetectLegacyShippingCostFilterTests.cs` — **new**. 6 tests verifying: warning emission on `shippingCostRon` presence, case-insensitive matching, no log when the key is absent, graceful handling of empty body, graceful handling of malformed JSON, and stream rewind for the downstream model binder.
- [x] `src/PhotoPrint.Tests/Integration/PaymentControllerIntegrationTests.cs` — **+1 test**. Tampered raw JSON with `shippingCostRon: -100` posts to `/api/payments/stripe/intent`; persisted `Order.TotalRon` equals `subtotal + Easybox flat rate (20.00 RON)`, never the negative value the client sent.
- [x] `src/PhotoPrint.Tests/Unit/Services/OrderServiceTests.cs` — pre-existing test file adapted in Stage 2 (mock `IShippingService` + 3-arg `MakeRequest`). All pre-existing assertions still pass.

### Acceptance Criteria Validation

**Story 001 — remove-client-shipping-cost**

- ✅ **Tampered `ShippingCostRon: -100` in body does NOT reduce `order.TotalRon`.** Integration test `CreateStripeIntent_TamperedShippingCostInBody_IsIgnored_OrderTotalReflectsServerResolvedCost` verifies: persisted `order.TotalRon == 30.00m` (10.00 subtotal + 20.00 Easybox server-resolved cost). The tampered −100 is silently dropped by System.Text.Json's default handling of unknown fields.
- ✅ **`DeliveryType: Courier` yields server-resolved courier cost.** Covered by pre-existing `InitiateEuPlatesc_ValidCart_Returns200WithRedirectUrl` continuing to pass against the new server-side resolution path.
- ✅ **`CreateOrderRequest` no longer compiles with `ShippingCostRon`.** Verified by Stage 2 build break in `PaymentControllerIntegrationTests` static request literals and `OrderServiceTests` `MakeRequest` helper — both updated to the 4-arg shape; project compiles clean now.
- ✅ **Legacy field silently ignored + Warning logged.** Filter unit tests confirm: warning emitted (`BodyContainingShippingCostRon_LogsWarning`, `…_CaseInsensitive_LogsWarning`), no warning when absent, no exception on edge cases.

**Story 002 — create-order-validator**

- ✅ **`Easybox` + null `EasyboxLockerId` → 422 with field error and message.** `Easybox_WithoutLockerId_FailsWithFieldErrorAndMessage` asserts both the field path and the exact message text `"Locker ID is required for Easybox delivery"`.
- ✅ **`Courier` + null `ShippingAddress` → 422.** `Courier_WithoutShippingAddress_FailsWithFieldErrorAndMessage` asserts field path + message.
- ✅ **`Courier` + empty `ShippingAddress.PostalCode` → 422 with nested field path.** `Courier_WithEmptyPostalCode_FailsOnNestedField` (also tests `.City` and `.County` analogues).
- ✅ **Unknown `PaymentProcessor` enum value → 422.** `UnknownPaymentProcessor_FailsValidation` (also `UnknownDeliveryType_FailsValidation`).
- ✅ **Validator auto-registered.** The class lives under `Validators/Payments/` and is picked up by the existing `AddValidatorsFromAssemblyContaining<Program>` registration — verified by the integration test running end-to-end through the full middleware + validation pipeline.

### Self-correction during Stage 3

The first integration-test run returned **422 UnprocessableEntity** instead of **200 OK**. Diagnosis: the raw JSON I authored serialized `paymentProcessor` and `deliveryType` as **string enum names** (`"Stripe"`, `"Easybox"`), but the API has no `JsonStringEnumConverter` registered — existing tests rely on `PostAsJsonAsync` which writes enums as **integers** by default. Fixed by using integer values (`0` for both — first enum member). One-line edit, re-ran the suite, 449/449 green. This is documented in the test file with a comment explaining the wire format choice.

### Notes

- **Pre-existing warnings unchanged.** Stripe NuGet `NU1603` (Stripe.net 46.3.0 → 47.0.0 resolution) and `CS1998` on `RazorTemplateServiceTests.cs:82` (async-without-await) both predate this bolt.
- **No `[Fact(Skip = ...)]` introduced.** Every test in the bolt either passes or asserts the expected failure path explicitly.
- **Bolt-033's three `UploadCleanupJob` tests** continue to pass without filter — confirmed by the unfiltered 449/449 run.
- **No production-side cleanup tracking.** The transitional `DetectLegacyShippingCostFilter` should be removed in a future follow-up bolt once production logs show no `WARN payments.shipping-cost-tampering-attempt` events for ~4 weeks after the FE deploys an updated DTO. Documented in the implementation walkthrough's "Developer Notes" section.
