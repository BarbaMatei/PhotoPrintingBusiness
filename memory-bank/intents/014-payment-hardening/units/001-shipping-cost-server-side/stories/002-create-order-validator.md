---
id: 002-create-order-validator
unit: 001-shipping-cost-server-side
intent: 014-payment-hardening
status: implemented
priority: must
created: 2026-05-25T10:05:00Z
assigned_bolt: 034-shipping-cost-server-side
implemented: true
implemented_at: 2026-05-25T13:00:00Z
---

# Story: 002-create-order-validator

## User Story

**As** a backend developer
**I want** `CreateOrderRequest` validated declaratively
**So that** mismatched delivery configurations are rejected before any order or PaymentIntent is created

## Acceptance Criteria

- [ ] **Given** `DeliveryType: "Easybox"` and `EasyboxLockerId: null`, **When** the controller is called, **Then** the response is 422 with `errors:[{field:"EasyboxLockerId", message:"Locker ID is required for Easybox delivery"}]` and no DB write occurs.
- [ ] **Given** `DeliveryType: "Courier"` and `ShippingAddress: null`, **Then** 422 with `errors:[{field:"ShippingAddress", message:"Shipping address is required for courier delivery"}]`.
- [ ] **Given** `DeliveryType: "Courier"` and `ShippingAddress.PostalCode: ""`, **Then** 422 listing the nested field path `ShippingAddress.PostalCode`.
- [ ] **Given** an unknown `PaymentProcessor` enum value, **Then** 422.
- [ ] Validator is registered via FluentValidation auto-discovery (matches the project's existing pattern from bolt 001).

## Technical Notes

```csharp
// Validators/Payments/CreateOrderRequestValidator.cs
public sealed class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(x => x.PaymentProcessor).IsInEnum();
        RuleFor(x => x.DeliveryType).IsInEnum();

        When(x => x.DeliveryType == DeliveryType.Easybox, () =>
        {
            RuleFor(x => x.EasyboxLockerId).NotNull()
                .WithMessage("Locker ID is required for Easybox delivery");
        });

        When(x => x.DeliveryType == DeliveryType.Courier, () =>
        {
            RuleFor(x => x.ShippingAddress).NotNull()
                .WithMessage("Shipping address is required for courier delivery");

            When(x => x.ShippingAddress != null, () =>
            {
                RuleFor(x => x.ShippingAddress!.PostalCode).NotEmpty();
                RuleFor(x => x.ShippingAddress!.CountyCode).NotEmpty();
                RuleFor(x => x.ShippingAddress!.City).NotEmpty();
            });
        });
    }
}
```

## Dependencies

### Requires
- 001-remove-client-shipping-cost (DTO shape stabilised)

### Enables
- All later payment work (clean inputs)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Both delivery type fields populated | Pass; only the relevant block enforces |
| Empty body | 422 from existing ValidationFilter (ADR-002) |

## Out of Scope

- VAT identifier validation (intent 016 owns invoicing).
