# US-305 — Payment — Stripe Backend

## Story
**As a** system  
**I want to** create Stripe PaymentIntents and confirm orders only after Stripe webhook verification

## Type
BACKEND — ASP.NET Core

## Epic
EPIC-3 | Checkout & Plată

## Dependencies
- US-206 (Cart must exist to calculate amount)
- US-105/US-109 (Auth)
- US-801 (Error handling)

## Acceptance Criteria

1. **`POST /api/payments/stripe/intent`** — creates PaymentIntent for cart total; returns `{clientSecret}`
2. **Creates PendingOrder** with `Status=AwaitingPayment`, `PaymentProcessor=Stripe`, `PaymentIntentId`
3. **`POST /api/webhooks/stripe`** — validates `Stripe-Signature` header with `STRIPE_WEBHOOK_SECRET`
4. **`payment_intent.succeeded`** → Order `status=Paid`; fires `OrderConfirmedEmail`; associates uploads to order
5. **`payment_intent.payment_failed`** → Order `status=PaymentFailed`
6. **Idempotent**: duplicate events ignored by `PaymentIntentId` uniqueness check

## Technical Notes

### Endpoints
```
POST /api/payments/stripe/intent
Authorization: Bearer {jwt} OR X-Guest-Token: {uuid}
→ 200 { "clientSecret": "pi_xxx_secret_xxx", "orderId": "uuid" }
→ 400 { "message": "Coșul este gol" }
```

```
POST /api/webhooks/stripe
Stripe-Signature: t=xxx,v1=xxx
(raw body)
→ 200
→ 400 (signature invalid)
```

### Implementation Details
- Install `Stripe.net` NuGet package
- Create PaymentIntent: amount = cart subtotal + shipping cost (in bani = RON × 100); currency = "ron"
- Create Order entity simultaneously:
  - `OrderNumber`: format `FT-{YYYY}{NNNN}` (auto-incrementing per year)
  - `Status = AwaitingPayment`
  - `PaymentProcessor = Stripe`
  - `PaymentIntentId = pi_xxx`
  - Copy cart items → OrderItems (snapshot unit price at time of order)
  - `ProductSnapshot`: JSON of product details at order time (for historical accuracy)
- Webhook handler:
  - Read raw request body (NOT parsed JSON)
  - Validate signature using `EventUtility.ConstructEvent(body, signature, webhookSecret)`
  - Handle `payment_intent.succeeded`: find order by PaymentIntentId → set Paid → fire email
  - Handle `payment_intent.payment_failed`: find order → set PaymentFailed
  - Idempotency: if order already Paid, return 200 without processing
- Config: `Stripe:SecretKey`, `Stripe:WebhookSecret` in environment variables

### Database
- `Orders` table (see Appendix A)
- `OrderItems` table (see Appendix A)
- Unique index on `PaymentIntentId`

## Files to Create/Modify
- `src/PhotoPrint.API/Controllers/PaymentsController.cs` (StripeCreateIntent)
- `src/PhotoPrint.API/Controllers/WebhooksController.cs` (StripeWebhook)
- `src/PhotoPrint.API/DTOs/Payment/StripeIntentResponse.cs`
- `src/PhotoPrint.API/Models/Order.cs`
- `src/PhotoPrint.API/Models/OrderItem.cs`
- `src/PhotoPrint.API/Models/OrderStatus.cs` (enum)
- `src/PhotoPrint.API/Services/IPaymentService.cs` + `StripePaymentService.cs`
- `src/PhotoPrint.API/Services/IOrderService.cs` + `OrderService.cs`
- EF Core migration for Orders + OrderItems

## Testing
- Unit test: PaymentIntent creation with correct amount
- Unit test: Order creation from cart
- Unit test: webhook signature validation
- Unit test: payment_intent.succeeded → order status Paid
- Unit test: payment_intent.payment_failed → order status PaymentFailed
- Unit test: idempotent webhook handling
- Integration test: full Stripe payment flow with mocked Stripe
