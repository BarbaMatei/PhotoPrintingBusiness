---
unit: 002-payment-idempotency
bolt: 035-payment-idempotency
stage: design
status: complete
updated: 2026-05-25T13:25:00Z
---

# Technical Design — Payment Idempotency

## Architecture Pattern

**Existing layered architecture preserved** (per the project's `system-architecture.md` and the implementations behind bolts 001–034):

- **Presentation**: ASP.NET Core controllers + filters.
- **Application/Domain**: services (`OrderService`, `IStripePaymentGateway`, `IEuPlatescService`) + validators (FluentValidation).
- **Infrastructure**: EF Core (`PhotoPrintDbContext`) + external SDKs (Stripe.NET).

**No new architectural pattern introduced.** This bolt adds one domain service (`IdempotencyResolver`) and extends existing services and the `Order` entity. The pattern choice is **"optimistic application-layer lookup + DB-arbitrated unique constraint"**:

1. The controller asks `OrderService` for an existing order by key.
2. If found and matching → return it (Replay).
3. If found and divergent → 409.
4. If not found → create. The filtered unique index on `Orders.IdempotencyKey` is the ultimate authority; any race that slips past the lookup is rejected by the database and surfaced as 409.

This pattern is deliberate over a distributed lock or Redis cache (out of scope per intent 021).

---

## Layer Structure

```text
┌────────────────────────────────────────────────────────┐
│  Presentation                                          │
│  ─ PaymentsController                                   │
│     • [FromHeader] Idempotency-Key                     │
│     • [ServiceFilter] DetectLegacyShippingCostFilter   │
│                       (existing, bolt 034)              │
└────────────────────────────────────────────────────────┘
                          │
                          ▼
┌────────────────────────────────────────────────────────┐
│  Application                                           │
│  ─ IOrderService                                        │
│     • CreateFromCartAsync(... , idempotencyKey?, ct)   │
│     • GetByIdempotencyKeyAsync(key, ct)   ← NEW         │
│     • IsSameLogicalRequest(order, request) ← NEW        │
│  ─ IStripePaymentGateway                                │
│     • CreatePaymentIntentAsync(..., idempotencyKey?, ct)│
│       (signature extended)                              │
│  ─ IEuPlatescService                                    │
│     • BuildInitiateUrl(order) — already deterministic   │
└────────────────────────────────────────────────────────┘
                          │
                          ▼
┌────────────────────────────────────────────────────────┐
│  Domain                                                │
│  ─ Order  +  IdempotencyKey?  (additive)                │
│  ─ IdempotencyConflictException                         │
│    (caught in controller → 409 ProblemDetails)          │
└────────────────────────────────────────────────────────┘
                          │
                          ▼
┌────────────────────────────────────────────────────────┐
│  Infrastructure                                        │
│  ─ EF Core                                              │
│     • Filtered unique index on Orders.IdempotencyKey   │
│     • Configured per provider                          │
│       (Postgres: HasFilter; SQLite: plain unique)      │
│  ─ Stripe SDK                                           │
│     • RequestOptions.IdempotencyKey forwarded          │
└────────────────────────────────────────────────────────┘
```

---

## API Design

### `POST /api/payments/stripe/intent`

**Request headers**:

- `Idempotency-Key: <opaque-string-1..80-chars>` — optional during transitional period, recommended UUID v4.
- `Authorization: Bearer <jwt>` OR `X-Guest-Token: <guest-id>` (existing dual-auth, unchanged).

**Request body**: `CreateOrderRequest` (4-arg, unchanged from bolt 034).

**Response 200** (new replay path collapses to same shape):

```json
{ "clientSecret": "pi_xxx_secret_yyy", "orderId": "<guid>" }
```

**Response 409** (new — idempotency conflict):

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.8",
  "title": "Idempotency conflict",
  "status": 409,
  "detail": "An order with this Idempotency-Key already exists with different parameters.",
  "divergentFields": ["paymentProcessor", "totalRon"],
  "correlationId": "<guid>"
}
```

**Response 422** (existing — validation): unchanged. Idempotency conflicts do **not** flow through the validation pipeline.

### `POST /api/payments/euplatesc/initiate`

Identical contract additions. Response body is `{ redirectUrl, orderId }` instead of `{ clientSecret, orderId }`. The replay path returns the same `redirectUrl` for the same key (deterministic HMAC from the persisted order).

### Behavioural matrix

| Scenario | Header `Idempotency-Key` | Existing order in 24h window | Logical match | HTTP status | Body | Side effects |
|---|---|---|---|---|---|---|
| Fresh call | absent | — | — | 200 | new order | 1 row inserted; 1 Stripe intent; **WARN log** `missing-idempotency-key` |
| Fresh call | present | none | — | 200 | new order | 1 row inserted; 1 Stripe intent; key persisted on order; **Stripe `RequestOptions.IdempotencyKey` set** |
| Replay | present | exists | yes | 200 | replay (same order, same `clientSecret`) | **zero new rows; zero new Stripe calls**; INFO log `replay` |
| Conflict | present | exists | no | 409 | ProblemDetails with `divergentFields` | zero state mutation; INFO log `conflict` |
| Race (two simultaneous fresh calls, same key) | present | (both pass lookup) | — | one 200, one 409 | second caller receives ProblemDetails | 1 row inserted; second insert rejected by unique index; the rejection is caught and translated to 409 |
| Stale (>24h) | present | exists, > 24h old | — | 200 | new order (the stale row keeps its key for audit) | 1 row inserted with the same key value but **only the new one is "active"** because `CreatedAt + 24h > UtcNow` filter excludes the old |

**The stale case requires careful index handling**: the filtered unique index covers ALL non-null keys, not "active" keys. Therefore, when we insert a new row with a previously-used (stale) key, the unique index will reject it. **Design choice**: at insert time, if the resolver returns `NewOrder` because the existing row is stale, we **null out the stale row's `IdempotencyKey`** before inserting the new row. Documented in the Data Model section.

> **Implementation note (DOC-1, review 035-v5):** the as-built code frees the stale key in **its own `SaveChangesAsync`**, then inserts the new order in a **separate save** — deliberately two saves, *not* one wrapping transaction. A single transaction would still violate the unique index per-statement (both Postgres and SQLite enforce it mid-transaction), so the free must commit before the insert. The free+insert is therefore non-atomic; this is benign because the spec already accepts losing the stale row's key for audit (below), and the new insert is still arbitrated by the unique index. The wording "same transaction" in this section and the Data Model section below is the original sketch, not the shipped behaviour.

---

## Data Model

### Migration `20260526_AddOrderIdempotencyKey`

```sql
-- Up
ALTER TABLE "Orders"
  ADD COLUMN "IdempotencyKey" varchar(80) NULL;

-- Postgres branch (filtered unique)
CREATE UNIQUE INDEX "ix_orders_idempotency_key"
  ON "Orders" ("IdempotencyKey")
  WHERE "IdempotencyKey" IS NOT NULL;

-- SQLite branch (no filtered indexes; partial index on expression)
-- EF Core 8 emits this when configured via HasFilter() — provider-aware
CREATE UNIQUE INDEX "ix_orders_idempotency_key"
  ON "Orders" ("IdempotencyKey")
  WHERE "IdempotencyKey" IS NOT NULL;
-- SQLite 3.8+ supports partial indexes natively; this works on both providers.

-- Down
DROP INDEX "ix_orders_idempotency_key";
ALTER TABLE "Orders" DROP COLUMN "IdempotencyKey";
```

### EF Core model builder addition

In the existing `Order` configuration block inside `PhotoPrintDbContext.OnModelCreating`:

```text
- Column: IdempotencyKey, nullable, max length 80.
- Index: HasIndex(o => o.IdempotencyKey)
         .IsUnique()
         .HasFilter("\"IdempotencyKey\" IS NOT NULL")
         .HasDatabaseName("ix_orders_idempotency_key");
```

The `HasFilter` string is passed verbatim; Postgres respects it, SQLite (3.8+) also supports partial indexes with the same syntax.

### Stale-row handling on reuse

When `IdempotencyResolver` returns `NewOrder` because an existing row's key is stale (> 24h old), `OrderService.CreateFromCartAsync(... , idempotencyKey, ct)` frees the key by nulling it on the stale row, then inserts the new row. This frees the key for the new row and preserves the old order's audit trail (the historical key value is no longer queryable but its presence/absence isn't audit-relevant once the order ships).

> **As-built (DOC-1, review 035-v5):** this is **two saves**, not one transaction. The stale row's key is nulled and committed in its own `SaveChangesAsync` *first*, because the unique index is enforced per-statement (a single transaction would still collide). Only an owner-scoped stale row is freed (SEC-1). The free+insert pair is intentionally non-atomic — see the behaviour-matrix note above.

Trade-off: the stale row's idempotency key is lost. If audit ever requires it, a future migration can introduce `Orders.HistoricalIdempotencyKey` text column. Out of scope here.

---

## Application-layer changes

### `IOrderService` additions

```text
Task<Order?> GetByIdempotencyKeyAsync(string key, CancellationToken ct);
  - Filters: IdempotencyKey == key AND CreatedAt + 24h > UtcNow.
  - Returns null when no row matches or all matches are stale.

bool IsSameLogicalRequest(Order existing, CreateOrderRequest request, decimal currentTotalRon);
  - Pure comparison; no I/O.
  - Compares: PaymentProcessor, DeliveryType, EasyboxLockerId, TotalRon.
  - Does NOT compare: ShippingAddress (per domain model decision).
  - Returns true iff all compared fields match.
```

### `CreateFromCartAsync` signature change

```text
Before: CreateFromCartAsync(Guid? userId, Guid? guestSessionId, CreateOrderRequest req, CancellationToken ct)
After:  CreateFromCartAsync(Guid? userId, Guid? guestSessionId, CreateOrderRequest req, string? idempotencyKey, CancellationToken ct)
```

Inside the method, after the existing logic produces `order` but before `_db.Orders.Add(order)`:

1. If `idempotencyKey is not null`: assign `order.IdempotencyKey = idempotencyKey`.
2. If there is a stale row with the same key (a `Replay` was NOT possible but the key exists): clear that row's key in the same transaction.
3. Try `SaveChangesAsync`. If the DB raises a unique-violation on `ix_orders_idempotency_key`, throw a typed `IdempotencyConflictException` carrying the offending key (the resolver missed the row in the read window — a benign race).

### New domain exception

```text
namespace PhotoPrint.API.Exceptions

public sealed class IdempotencyConflictException(string key, IReadOnlyList<string> divergentFields)
    : Exception($"Idempotency key '{key}' is bound to an order with divergent parameters.");

  Properties: Key (string), DivergentFields (IReadOnlyList<string>).
```

Mapped in `ExceptionHandlerMiddleware` to **409 ProblemDetails** with `title: "Idempotency conflict"`, `divergentFields` echoed in the body, correlation id from the existing context.

### `IStripePaymentGateway.CreatePaymentIntentAsync` signature change

```text
Before: CreatePaymentIntentAsync(long amountBani, string currency, string orderIdMetadata, CancellationToken ct)
After:  CreatePaymentIntentAsync(long amountBani, string currency, string orderIdMetadata, string? idempotencyKey, CancellationToken ct)
```

Real implementation passes `new RequestOptions { IdempotencyKey = idempotencyKey }` into the Stripe SDK call when `idempotencyKey is not null`.
`FakeStripePaymentGateway` (test double) accepts the new arg and ignores it.

### Controller

```text
[HttpPost("stripe/intent")]
public async Task<IActionResult> CreateStripeIntent(
    [FromBody] CreateOrderRequest req,
    [FromHeader(Name = "Idempotency-Key")] string? key,
    CancellationToken ct)
{
    if (string.IsNullOrEmpty(key))
        _logger.LogWarning("payments.idempotency.missing-key endpoint=stripe/intent");

    if (!string.IsNullOrEmpty(key))
    {
        var existing = await _orderService.GetByIdempotencyKeyAsync(key, ct);
        if (existing is not null)
        {
            // Resolver invocation inlined for clarity; can become a proper service later.
            // Cart subtotal recomputation is required to derive the *current* TotalRon.
            // We use the same OrderService primitive that creates orders, just in dry-run.
            // Simpler: compare against the persisted Order's fields, which already snapshot
            // the resolved server-side shipping cost.
            if (!_orderService.IsSameLogicalRequest(existing, req, existing.TotalRon))
                throw new IdempotencyConflictException(key, /* divergent fields */ ...);

            _logger.LogInformation("payments.idempotency.replay key={Key} order={OrderId}", key, existing.Id);
            return Ok(new StripeIntentResponse(existing.StripeClientSecret!, existing.Id));
        }
    }

    var order = await _orderService.CreateFromCartAsync(userId, guestSessionId, req, key, ct);
    var (clientSecret, paymentIntentId) =
        await _stripeGateway.CreatePaymentIntentAsync(
            (long)(order.TotalRon * 100), "ron", order.Id.ToString(), key, ct);

    order.StripeClientSecret = clientSecret;   // NEW persisted field — see Data Model addendum below
    order.PaymentIntentId    = paymentIntentId;
    await _db.SaveChangesAsync(ct);

    return Ok(new StripeIntentResponse(clientSecret, order.Id));
}
```

**`Order.StripeClientSecret` (new persisted field)** — required so a replay caller receives the **identical** `ClientSecret` without a Stripe round-trip. Adding it to the same migration: `varchar(255) NULL`. Documented in the Data Model addendum.

The EuPlatesc controller method is structurally identical but its replay path calls `_euPlatescService.BuildInitiateUrl(existing)` to reconstruct the URL (no persisted secret needed — already deterministic from order fields).

### Behaviour during Stripe-gateway idempotency conflict

If Stripe itself rejects with an idempotency mismatch (the gateway saw the same key with a different payload before we did — possible if a previous request errored after we sent to Stripe but before we persisted), the SDK throws `StripeException` with `StripeError.Code == "idempotency_error"`. The controller catches this specific case and surfaces it as the same 409 ProblemDetails. Documented for the test plan.

---

## Security Design

| Concern | Approach |
|---|---|
| **Cross-user key reuse** | The filtered unique index applies database-wide. If user A and user B race the same opaque key (statistically improbable with UUID v4 but possible), the second insert is rejected — both see a 409. Not a security hole; the key is opaque and not a credential. |
| **Tampered logical request via same key** | `IsSameLogicalRequest` runs server-side against the persisted snapshot. The client cannot widen what's compared. |
| **Stripe ClientSecret exposure on replay** | Already controlled by JWT/guest auth on `/api/payments/stripe/intent`. Replay returns the same secret only to a caller already authorised to create the order. |
| **Log injection** | Idempotency key included in logs is bounded to 80 chars and is structured-logged via Serilog templating, not string interpolation. |
| **DOS via key brute-force** | Each lookup is O(1) via the index. Rate limiting on the auth-policy endpoints (existing 100 req/min/IP) covers brute-force risk. |
| **PII in conflict response** | `divergentFields` returns field *names* only (e.g. `"totalRon"`, `"paymentProcessor"`), never the values. No PII leak. |

---

## NFR Implementation

| NFR | Target | Design approach |
|---|---|---|
| Idempotency lookup latency | p95 < 5 ms added | Filtered unique index → single B-tree lookup. Avoids any extra query when no key is sent. |
| Replay throughput | No degradation vs. fresh call | Replay path skips order creation + Stripe call entirely — strictly cheaper than a fresh call. |
| Migration safety | Apply cleanly to running prod with no downtime | `IdempotencyKey` and `StripeClientSecret` both nullable; index creation does NOT lock the table on Postgres (`CREATE INDEX CONCURRENTLY` not needed at MVP scale, but documented as a future option). |
| Observability | Replay/conflict/missing-key counts | Logged as structured events with reserved names. Intent 020 will lift them to Prometheus counters without re-coding. |
| Backwards compat | FE-without-key still works for one release | Endpoint accepts missing header; logs Warning. Migration in two phases: deploy server, then FE adopts; finally a follow-up bolt makes the header required. |

---

## Integration Points

### Stripe SDK

- `Stripe.RequestOptions.IdempotencyKey` set on every `PaymentIntentService.CreateAsync(...)` invocation.
- **The gateway is keyed by `order.Id` (server-generated), NOT the client `Idempotency-Key` header (DOC-2 / BUG-4, review 035).** The client key arbitrates *our* order row (the filtered unique index); Stripe is keyed by the stable order id so a recycled or replayed client key can never collide a different order at Stripe, and a re-call for the same order (e.g. the recovery-replay path, OBS-3) returns the same PaymentIntent rather than double-charging. The sketch below that forwarded the client `key` to Stripe is superseded by this choice.
- Stripe enforces gateway-side dedupe for 24 h (matches our window — convenient alignment).
- On Stripe-side conflict (`StripeError.Code == "idempotency_error"`), translate to our `IdempotencyConflictException`.

### EuPlatesc

- No gateway-side primitive. The redirect URL is reconstructed deterministically from the persisted order via the existing `BuildInitiateUrl(order)`. Replay returns the same URL because the same `Order` row produces the same HMAC.
- No SDK options to forward.

### Existing `DetectLegacyShippingCostFilter` (bolt 034)

- Untouched. Runs before model binding, has no interaction with the new header-bound `Idempotency-Key` parameter.

---

## Test Plan Preview (full plan in Stage 5)

- **Unit**: `IsSameLogicalRequest` true/false matrix; resolver decision-table (4 rows) on in-memory fixtures; `IdempotencyConflictException` payload shape.
- **Integration — Stripe**: replay returns same body; conflict returns 409 with `divergentFields`; missing-key logs Warning + still succeeds; Stripe `RequestOptions.IdempotencyKey` verified via test-double assertion that the key bytes reach the SDK.
- **Integration — EuPlatesc**: replay returns same `redirectUrl`; conflict returns 409.
- **Concurrency**: two parallel calls with the same key, no body divergence — exactly one new order, one 200, one 409 (DB-arbitrated). Captured via xUnit `await Task.WhenAll(...)` against the in-memory DB; the unique index is what makes this work.

---

## ADR-worthy decisions (presented at Stage 3)

The Stage 1 model flagged two; one more emerged during this design:

1. **State-conflict semantic = 409, not 422.** Distinguishes structural-validation errors (ADR-002) from state-conflict errors. Lives across this entire bolt and future API surfaces.
2. **`LogicalRequest` excludes `ShippingAddress`.** A trade-off favouring retry-friendliness over strict equality. Worth recording because it shapes future cart-modification policies.
3. **Stale-row handling: null the old key on reuse, in the same transaction.** Affects what the audit trail looks like for an `Order` whose key was once present but is now null. Alternative was "leave the stale row alone and accept the unique-index conflict, then retry with a fresh key for the new order" — heavier and surprising.

I'll surface these at the Stage 3 checkpoint with "create / skip" prompts.

---

## Open Items

- **Cart-subtotal recomputation on replay**: We compare against the persisted `Order.TotalRon` — that's a snapshot at original creation time. If the customer added items to the cart between the original call and the retry, the cart subtotal would differ but `TotalRon` would not. The resolver's `IsSameLogicalRequest(existing, req, existing.TotalRon)` compares the request's intent against the existing snapshot, NOT against a freshly recomputed cart total. This is intentional — idempotency is about the *original* request, not the live cart. Documented for the test plan.
- **`Order.StripeClientSecret` lifetime**: Stripe `ClientSecret`s are valid only as long as the underlying `PaymentIntent` is in a state where the customer can pay. We persist the secret to enable replay; we do NOT attempt to refresh it if Stripe later expires it. A replay caller who waits a long time may receive a stale secret. Acceptable for MVP; an intent-021 follow-up could re-fetch from Stripe via `PaymentIntent.Id`.
- **Cleanup of long-stale keys** (> 24h): no scheduled job introduced. Keys remain on rows forever. Storage cost is negligible (80 bytes per row). If retention becomes a concern, a follow-up bolt can NULL them in batches.
