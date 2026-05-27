---
stage: plan
bolt: 034-shipping-cost-server-side
created: 2026-05-25T12:30:00Z
---

## Implementation Plan: 001-shipping-cost-server-side

### Objective

Eliminate the client's ability to set `ShippingCostRon`. The server resolves the cost from the chosen `DeliveryType` via the existing `IShippingService`. Mismatched delivery configurations are rejected (422) before any DB write or Stripe call.

### Deliverables

1. `src/PhotoPrint.API/DTOs/Payments/CreateOrderRequest.cs` — drop the `ShippingCostRon` field. The DTO becomes a 4-arg record.
2. `src/PhotoPrint.API/Services/OrderService.cs` — inject `IShippingService` in the constructor; in `CreateFromCartAsync`, replace `request.ShippingCostRon` with the server-resolved cost from `_shipping.GetShippingCostAsync(request.DeliveryType.ToString(), ct)`.
3. `src/PhotoPrint.API/Validators/Payments/CreateOrderRequestValidator.cs` — **new** file. FluentValidation rules for delivery-type conditional fields (Easybox requires `EasyboxLockerId`; Courier requires `ShippingAddress` with non-empty `City`, `County`, `PostalCode`). `PaymentProcessor` and `DeliveryType` must be defined enum values.
4. `src/PhotoPrint.API/Filters/DetectLegacyShippingCostFilter.cs` — **new**. `IAsyncResourceFilter` applied via `[ServiceFilter]` to the two payment endpoints. Reads the JSON body once (`Request.EnableBuffering()` + rewind), parses for a `shippingCostRon` key (case-insensitive), logs `WARN payments.shipping-cost-tampering-attempt processor={p} delivery={d}` if found, then rewinds the stream for the model binder. No exception thrown — purely observational.
5. `src/PhotoPrint.API/Controllers/PaymentsController.cs` — apply `[ServiceFilter(typeof(DetectLegacyShippingCostFilter))]` to both actions.
6. `src/PhotoPrint.API/Program.cs` — register the resource filter with DI.
7. `src/PhotoPrint.Tests/Unit/Services/OrderServiceTests.cs` — update `OrderService` constructor call to pass a mocked `IShippingService` that returns `new ShippingCostDto(20.00m)` for `"Easybox"`; remove `ShippingCostRon` from the `MakeRequest` helper.
8. **New** `src/PhotoPrint.Tests/Integration/PaymentControllerIntegrationTests.cs` integration test (or append to the existing file if one exists): post a payment-intent request with a body that includes the legacy `ShippingCostRon: -100` JSON field; assert the persisted `Order.TotalRon` equals `subtotal + 20.00 RON` (Easybox server-resolved cost), the field was silently ignored, and a warning was logged.
9. **New** validator unit test file `src/PhotoPrint.Tests/Unit/Validators/CreateOrderRequestValidatorTests.cs` — covers the 4 acceptance criteria of story 002.

### Dependencies

- No new NuGet packages. FluentValidation, Moq, FluentAssertions all already in the test project.
- `IShippingService` already DI-registered (`StaticShippingService` in `Program.cs`); no service-registration change needed except the new resource filter.

### Technical Approach

**1. `CreateOrderRequest` shape change**

```csharp
// Before
public record CreateOrderRequest(
    PaymentProcessor PaymentProcessor,
    DeliveryType     DeliveryType,
    Guid?            EasyboxLockerId,
    ShippingAddressSnapshot? ShippingAddress,
    decimal          ShippingCostRon);

// After
public record CreateOrderRequest(
    PaymentProcessor PaymentProcessor,
    DeliveryType     DeliveryType,
    Guid?            EasyboxLockerId,
    ShippingAddressSnapshot? ShippingAddress);
```

The `PaymentsController` calls don't reference `ShippingCostRon` so no controller diff is needed for the DTO change itself.

**2. `OrderService.CreateFromCartAsync` server-side resolution**

Constructor gains an `IShippingService _shipping` parameter. Inside `CreateFromCartAsync`:

```csharp
// Replace existing line 69 + line 94 with:
var shipping = await _shipping.GetShippingCostAsync(request.DeliveryType.ToString(), ct);

// Then:
var total = subtotal + shipping.CostRon;
// ...
order.ShippingCostRon = shipping.CostRon;
order.TotalRon        = total;
```

The `DeliveryType.ToString()` produces `"Easybox"` / `"Courier"` — exact strings the existing `StaticShippingService.GetShippingCostAsync` switch case expects.

**3. `CreateOrderRequestValidator` (new)**

```csharp
// src/PhotoPrint.API/Validators/Payments/CreateOrderRequestValidator.cs
public sealed class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(x => x.PaymentProcessor).IsInEnum();
        RuleFor(x => x.DeliveryType).IsInEnum();

        When(x => x.DeliveryType == DeliveryType.Easybox, () =>
            RuleFor(x => x.EasyboxLockerId).NotNull()
                .WithMessage("Locker ID is required for Easybox delivery"));

        When(x => x.DeliveryType == DeliveryType.Courier, () =>
        {
            RuleFor(x => x.ShippingAddress).NotNull()
                .WithMessage("Shipping address is required for courier delivery");

            When(x => x.ShippingAddress != null, () =>
            {
                RuleFor(x => x.ShippingAddress!.City).NotEmpty();
                RuleFor(x => x.ShippingAddress!.County).NotEmpty();
                RuleFor(x => x.ShippingAddress!.PostalCode).NotEmpty();
            });
        });
    }
}
```

Registered automatically via `AddValidatorsFromAssemblyContaining<Program>()` already in `Program.cs`. No new wiring required.

**4. `DetectLegacyShippingCostFilter` (new)**

```csharp
// src/PhotoPrint.API/Filters/DetectLegacyShippingCostFilter.cs
public sealed class DetectLegacyShippingCostFilter : IAsyncResourceFilter
{
    private readonly ILogger<DetectLegacyShippingCostFilter> _logger;

    public DetectLegacyShippingCostFilter(ILogger<DetectLegacyShippingCostFilter> logger)
        => _logger = logger;

    public async Task OnResourceExecutionAsync(
        ResourceExecutingContext context,
        ResourceExecutionDelegate next)
    {
        context.HttpContext.Request.EnableBuffering();
        var body = await new StreamReader(
            context.HttpContext.Request.Body,
            leaveOpen: true).ReadToEndAsync();
        context.HttpContext.Request.Body.Position = 0;

        if (ContainsLegacyShippingCostKey(body))
            _logger.LogWarning(
                "payments.shipping-cost-tampering-attempt path={Path}",
                context.HttpContext.Request.Path);

        await next();
    }

    private static bool ContainsLegacyShippingCostKey(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return false;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return false;
            foreach (var prop in doc.RootElement.EnumerateObject())
                if (string.Equals(prop.Name, "shippingCostRon", StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }
        catch (JsonException) { return false; }
    }
}
```

Registered in `Program.cs` as `services.AddSingleton<DetectLegacyShippingCostFilter>()`. Applied to the two payment endpoints via `[ServiceFilter(typeof(DetectLegacyShippingCostFilter))]`.

**5. `PaymentsController` annotation**

Single-line `[ServiceFilter(typeof(DetectLegacyShippingCostFilter))]` attribute on both action methods (or on the class — applies to all actions, either is fine; class-level is cleaner since both actions are payments-create paths).

**6. Tests**

- `OrderServiceTests.MakeRequest` becomes a 4-arg constructor; constructor in `OrderServiceTests` ctor sets up a `Mock<IShippingService>` returning `ShippingCostDto(20.00m)` for any string. Existing 7 OrderServiceTests assertions adapted to expect server-resolved cost (was 20.00m by the helper default, still 20.00m by mock — no semantic change).
- New `CreateOrderRequestValidatorTests` — 4 tests covering AC 1–4 of story 002.
- New integration test verifying the tampered-body scenario and the silent-ignore + warning behaviour.

### Deviations from Stories

1. **`IShippingService.GetShippingCostAsync` signature.** The story tech notes referenced `_shipping.GetShippingCostAsync(request.DeliveryType, request.ShippingAddress?.CountyCode, ct)` — a 3-arg overload taking an enum + a county code. The actual signature is `(string type, CancellationToken ct)` with no county dependency (flat rates from `appsettings.json`). Plan calls `_shipping.GetShippingCostAsync(request.DeliveryType.ToString(), ct)` and uses `.CostRon`. The per-county shipping rate is out of scope for bolt 034 — it lands in intent 015 (Sameday integration) which replaces the static service.

2. **`ShippingAddressSnapshot.County` not `CountyCode`.** Story 002 tech notes referenced `ShippingAddress.CountyCode`. Actual field is `County`. The validator validates `County` per the real model. Same field; just a label difference in the story.

### Acceptance Criteria (consolidated)

**From story 001:**

- [ ] Tampered `ShippingCostRon: -100` in body does NOT reduce `order.TotalRon`. Final total = `subtotal + 20.00 RON` (Easybox).
- [ ] `DeliveryType: Courier` with a valid address yields `subtotal + 25.00 RON`.
- [ ] `CreateOrderRequest` no longer compiles with a `ShippingCostRon` member. Test helper updated.
- [ ] Legacy `shippingCostRon` body field is silently ignored; a Warning log is emitted naming the request path.

**From story 002:**

- [ ] `DeliveryType: Easybox` + `EasyboxLockerId: null` → 422 with `{field:"EasyboxLockerId", message}`.
- [ ] `DeliveryType: Courier` + `ShippingAddress: null` → 422.
- [ ] `DeliveryType: Courier` + `ShippingAddress.PostalCode: ""` → 422 with the nested field path.
- [ ] Unknown `PaymentProcessor` enum value → 422.
- [ ] Validator auto-registered via existing `AddValidatorsFromAssemblyContaining<Program>()`.

### Risk Notes

- **`Request.EnableBuffering()` semantics.** Resource filters run before model binding, which is exactly where buffering must be enabled. Reading the body in a resource filter and rewinding is a standard ASP.NET Core pattern. Behaviour validated by the integration test.
- **`OrderServiceTests` blast radius.** All 7 existing tests share the `MakeRequest` helper. Constructor + helper update fixes them all in one edit. No test logic re-derivation needed.
- **Integration test environment.** `PaymentControllerIntegrationTests` (if it exists) likely uses `WebApplicationFactory`. Bolt-049 work confirmed the full test project builds clean. The new integration test should land cleanly. If the file doesn't exist, the new file follows the same `IClassFixture<PaymentFactory>` pattern as the cart integration tests.
- **`ShippingCostRon` is also persisted on `Order`.** It needs to stay on the **entity** (legitimate persisted column) but must be set by the server, not the request. The plan keeps the column on `Order` and writes it from the resolved value.
