---
stage: implement
bolt: 035-payment-idempotency
created: 2026-05-25T13:55:00Z
---

## Implementation Walkthrough: 002-payment-idempotency

### Summary

End-to-end idempotent payment-intent creation. A repeat `POST` with the same `Idempotency-Key` within 24 h replays the original order (same `OrderId`, same Stripe `ClientSecret` / EuPlatesc redirect URL, no new row, no new gateway charge). A divergent body under the same key returns 409. Enforced at three layers: DB (unique index), application (resolver in `OrderService`), and Stripe (`RequestOptions.IdempotencyKey`).

### Completed Work

- [x] `Models/Order.cs` — added `IdempotencyKey?`, `StripeClientSecret?`, `EuPlatescRedirectUrl?` (all nullable, additive).
- [x] `Data/PhotoPrintDbContext.cs` — column lengths (80 / 255 / 1000) + unique index `ix_orders_idempotency_key`; `HasFilter("IdempotencyKey IS NOT NULL")` applied on Postgres only.
- [x] `Migrations/20260527075359_AddOrderIdempotencyKey.cs` — generated via `dotnet ef migrations add`; three columns + unique index.
- [x] `Exceptions/IdempotencyConflictException.cs` — carries `DivergentFields` (names only). New.
- [x] `Middleware/ExceptionHandlerMiddleware.cs` — maps `IdempotencyConflictException` → 409 "Idempotency conflict"; adds `divergentFields` to the ProblemDetails extensions.
- [x] `Services/IStripePaymentGateway.cs` + `StripePaymentGateway.cs` — added optional `idempotencyKey` → `RequestOptions.IdempotencyKey`.
- [x] `Services/IOrderService.cs` + `OrderService.cs` — `CreateFromCartAsync` now takes `idempotencyKey` and returns `OrderCreationResult`; added `GetByIdempotencyKeyAsync` (24h-windowed) + private `DivergentFields`; stale-key null-out before insert.
- [x] `DTOs/Orders/OrderCreationResult.cs` — `(Order, bool WasIdempotentReplay)`. New.
- [x] `Controllers/PaymentsController.cs` — `Idempotency-Key` header on both endpoints; replay short-circuit (Stripe returns stored secret, EuPlatesc returns stored URL); missing-key Warning; injected `ILogger`.
- [x] `Tests` — `FakeStripePaymentGateway` matches new signature + records `CreateCallCount` / `LastIdempotencyKey`; `OrderServiceTests` call sites use `.Order`.

### Key Decisions

- **Resolution lives in `OrderService`, not the controller.** The story sketched a controller-side `GetByIdempotencyKeyAsync` + `IsSameLogicalRequest` flow, but the `TotalRon` comparison needs the server-resolved total, which is computed inside `CreateFromCartAsync` (after cart load + shipping resolution from bolt 034). Keeping resolution in the service makes the total comparison feasible without recomputing the cart in the controller. The controller distinguishes replay-vs-new via `OrderCreationResult.WasIdempotentReplay`. `GetByIdempotencyKeyAsync` stays public for testing/future use.
- **Replay parity via stored secrets.** `Order.StripeClientSecret` and `Order.EuPlatescRedirectUrl` are persisted so a replay returns byte-identical tokens with no gateway round-trip.
- **Stale-key null-out is a separate `SaveChanges`** before the new insert — Postgres checks unique constraints per-statement, so freeing the key in its own save avoids a transient violation.

### Deviations from Design

1. **EuPlatesc redirect URL is persisted, not reconstructed.** Stage 2 assumed the URL was deterministically reproducible. It is not — `BuildInitiateUrl` embeds `DateTime.UtcNow` + a random nonce. So replay returns the **stored** `Order.EuPlatescRedirectUrl` instead of rebuilding. This is the option story 003 explicitly left open ("persist… or reconstruct"); persisting is the only way to honour the "same redirect URL" AC literally. Added `Order.EuPlatescRedirectUrl` for this.
2. **Migration is SQLite-flavoured (`TEXT`, plain unique index).** `dotnet ef` resolved the SQLite design-time provider (dev default `DatabaseProvider: Sqlite`), matching the project's entire existing migration history (e.g. `AddFinishNameToCartItem` is also `TEXT`). `TEXT` is valid on Postgres; a plain unique index on a nullable column allows multiple NULLs on both providers, so it is behaviourally equivalent to the filtered index. The `HasFilter` optimisation is expressed in the DbContext model config for Postgres runtime/EnsureCreated.

### Dependencies Added

None. No new NuGet packages. `Stripe.RequestOptions` already available via the existing Stripe.net reference.

### Developer Notes

- `IShippingService` / bolt-034 server-side total resolution feeds the `LogicalRequest` total comparison — the two payment-hardening units compose cleanly.
- Regression gate: full suite **449/449 passed** after implementation, before any new idempotency tests (those are Stage 5).
- The missing-key path logs `WARN payments.idempotency.missing-key` and proceeds as before — transitional until the FE always sends the header (then escalate to 400, out of scope).
- Reserved log names for intent 020: `payments.idempotency.replay`, `payments.idempotency.missing-key`. Conflict is surfaced as the 409 exception (logged by the existing middleware warning path).
