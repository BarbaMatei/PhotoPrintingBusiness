---
id: 002-stripe-intent-idempotency
unit: 002-payment-idempotency
intent: 014-payment-hardening
status: draft
priority: must
created: 2026-05-25T10:05:00Z
assigned_bolt: 035-payment-idempotency
implemented: false
---

# Story: 002-stripe-intent-idempotency

## User Story

**As** a checkout user
**I want** my second click on "Pay" to reuse the same order and Stripe charge
**So that** I am never billed twice for the same purchase

## Acceptance Criteria

- [ ] **Given** two consecutive `POST /api/payments/stripe/intent` calls with header `Idempotency-Key: <uuid>` and identical bodies, **When** both succeed, **Then** both responses contain the same `OrderId` and `ClientSecret`, the `Orders` table has exactly one new row, and Stripe shows exactly one `PaymentIntent`.
- [ ] **Given** two calls with the same `Idempotency-Key` and **different** `PaymentProcessor` or `TotalRon`, **Then** the second call returns 409 ProblemDetails with `title: "Idempotency conflict"` and detail naming the divergent field(s).
- [ ] **Given** a call **without** an `Idempotency-Key` header, **Then** the endpoint behaves as today and logs `WARN payments.stripe.intent missing-idempotency-key correlation_id=...`.
- [ ] The Stripe SDK call uses `RequestOptions.IdempotencyKey = key` so duplicate Stripe charges are blocked at the gateway too.

## Technical Notes

```csharp
// Controllers/PaymentsController.cs
[HttpPost("stripe/intent")]
public async Task<IActionResult> CreateStripeIntent(
    [FromBody] CreateOrderRequest req,
    [FromHeader(Name = "Idempotency-Key")] string? key,
    CancellationToken ct)
{
    if (key is not null)
    {
        var existing = await _orders.GetByIdempotencyKeyAsync(key, ct);
        if (existing is not null)
        {
            if (!_orders.IsSameLogicalRequest(existing, req))
                return Conflict(new ProblemDetails { Title = "Idempotency conflict" });
            return Ok(new { OrderId = existing.Id, ClientSecret = existing.StripeClientSecret });
        }
    }

    var order = await _orderService.CreateFromCartAsync(req, idempotencyKey: key, ct);
    var intent = await _stripe.CreatePaymentIntentAsync(
        order, new RequestOptions { IdempotencyKey = key }, ct);

    return Ok(new { OrderId = order.Id, intent.ClientSecret });
}
```

- 24 h window enforced by clearing the key on background cleanup OR by comparing `Order.CreatedAt + 24h` at lookup time (recommend the latter — simpler).
- Persist `StripeClientSecret` on `Order` if not already; otherwise the lookup must reconstruct it from `PaymentIntent.Id` via the SDK.

## Dependencies

### Requires
- 001-idempotency-key-migration

### Enables
- 003-euplatesc-initiate-idempotency (shares lookup helper)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Key reuse > 24 h later | Treated as new request (allow new order) |
| Key collision across users | Filtered unique index → second user gets 409; safe |
| Stripe returns idempotency conflict | Surface a 409 with Stripe's reason |

## Out of Scope

- Cross-instance idempotency cache (intent 021 Redis).
