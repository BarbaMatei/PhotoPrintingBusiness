---
unit: 002-payment-idempotency
bolt: 035-payment-idempotency
stage: model
status: complete
updated: 2026-05-25T13:15:00Z
---

# Static Model — Payment Idempotency

## Bounded Context

**Payment processing within the e-commerce checkout flow.** This unit augments the existing `Order` aggregate with idempotent-creation semantics across both supported payment processors (Stripe, EuPlatesc). It does not introduce a new bounded context — it tightens an invariant on the existing one.

**Relevant prior decisions**:

- [`ADR-002`](../001-error-handling-logging/adr-002-validation-filter-422.md) — *Custom ValidationFilter overrides [ApiController] 400 behavior with 422.* Idempotency conflicts are intentionally **not** modeled as validation errors. The 4-arg `CreateOrderRequest` is structurally valid; the conflict lies in the *state of an existing order* that holds the same key. This bolt distinguishes the two as separate HTTP semantics: 422 (validation, per ADR-002) versus 409 (state conflict, this bolt).

---

## Domain Entities

| Entity | Properties (additive only) | Business Rules |
|--------|----------------------------|----------------|
| `Order` (aggregate root, existing) | `IdempotencyKey: IdempotencyKey?` — nullable; set once at creation, never modified thereafter | An order may participate in at most one idempotency contract; the key is immutable after persist; `Order.CreatedAt` defines the start of the idempotency window |

The `Order` aggregate is otherwise unchanged. The new property is **additive** — all existing invariants on `Order` (price snapshots, status machine, shipping resolution from bolt 034) are preserved.

---

## Value Objects

| Value Object | Properties | Constraints |
|--------------|------------|-------------|
| `IdempotencyKey` | `Value: string` | Non-empty; length 1..80; opaque token from the client; UUID v4 strongly recommended (documented in OpenAPI) but format is not enforced server-side; equality is case-sensitive string equality |
| `LogicalRequest` | `PaymentProcessor: PaymentProcessor`, `DeliveryType: DeliveryType`, `EasyboxLockerId: Guid?`, `TotalRon: decimal` | Snapshot of the order-defining attributes derived from a `CreateOrderRequest` + the server-resolved totals (from bolt 034). Two `LogicalRequest`s are equal iff **all** fields match. `ShippingAddress` is intentionally excluded — courier addresses can be typo-corrected on retry without changing the logical intent |
| `ResolutionOutcome` (discriminated union) | One of: `NewOrder`, `Replay(existingOrder)`, `Conflict(divergentFields)` | Total — every key/request pair maps to exactly one outcome |

**Rationale on `LogicalRequest.ShippingAddress` exclusion**: a customer who hits "Pay", corrects a postal-code typo, and clicks "Pay" again with the same `Idempotency-Key` is doing what idempotency is for — they want the same charge. Including the address in equality would force a 409 on a benign correction. The trade-off is documented; if business chooses to be stricter later, the value object can be extended without changing the aggregate.

---

## Aggregates

| Aggregate Root | Members | Invariants |
|----------------|---------|------------|
| `Order` (existing) | All existing members + `IdempotencyKey?` | (1) **Uniqueness**: At most one `Order` row in the database carries any given non-null `IdempotencyKey` (enforced by the filtered unique index). (2) **Immutability of key**: Once `IdempotencyKey` is set on an order, it must never be re-assigned. (3) **Window-bound visibility**: For lookup purposes, an order's `IdempotencyKey` is considered "active" only when `Order.CreatedAt + 24h > UtcNow`. After 24 h the key is treated as expired and may be reused for a new order |

The 24-hour window is a domain rule, not a storage detail — it caps how long a retry is honoured. Stale keys remain on the row (audit value), but they no longer participate in lookup.

> **As-built caveat (REQ-1, review 035-v8):** the "may be reused after 24 h" reuse is **owner-scoped**, not global. Reclamation (nulling the stale row's key so the index frees it) only runs when the **original owner** resubmits — because both the lookup and the free are scoped to the caller (SEC-1). A *different* caller presenting the same stale key finds nothing in their scoped lookup, then collides on the **global** single-column unique index (which still holds the stale key) → 409. So across callers the key is effectively reserved for as long as the stale row exists, not freed at 24 h. This is a consequence of the global single-column index + owner-scoped reclamation; making the window truly per-caller needs the composite `(owner, key)` index tracked under the deferred SEC-1. In practice keys are unpredictable GUIDs so cross-caller reuse is a non-scenario; documented so the contract matches the code.

---

## Domain Events

| Event | Trigger | Payload |
|-------|---------|---------|
| `IdempotentReplayDetected` | A `Resolve(...)` call returns `Replay(existingOrder)` | `{ idempotencyKey, orderId, originalCreatedAt }` |
| `IdempotencyConflictDetected` | A `Resolve(...)` call returns `Conflict(divergentFields)` | `{ idempotencyKey, existingOrderId, divergentFields: list<string> }` |
| `MissingIdempotencyKeyObserved` | A request reaches the payment endpoint without an `Idempotency-Key` header | `{ endpoint, correlationId }` |

**Publication policy for this bolt**: events are *implicit* — recorded as structured log lines (`payments.idempotency.replay`, `payments.idempotency.conflict`, `payments.idempotency.missing-key`) rather than dispatched to an event bus. The bolt does not introduce an event-bus dependency. The names are reserved so that intent 020 (observability) can later wire them as metrics without re-modelling.

---

## Domain Services

| Service | Operations | Dependencies |
|---------|------------|--------------|
| `IdempotencyResolver` | `ResolveAsync(key: IdempotencyKey, current: LogicalRequest) -> ResolutionOutcome` | `IOrderRepository` (read-only lookup); a clock abstraction (`DateTimeOffset.UtcNow` in practice) for the 24 h window |

**Resolver decision table** (canonical):

| Lookup result | Order age | Logical match | Outcome |
|---|---|---|---|
| no order with this key | — | — | `NewOrder` |
| order found | ≤ 24 h | yes | `Replay(existingOrder)` |
| order found | ≤ 24 h | no | `Conflict(divergentFields)` |
| order found | > 24 h | — (key is stale) | `NewOrder` (proceed to insert; the old row keeps its key for audit but no longer dedupes) |

**Concurrency note (race window)**: the filtered unique index is the ultimate authority. If two simultaneous requests with the same key both pass the lookup and both attempt insert, the database rejects the second with a unique-violation. The application catches this as `Conflict(...)` and the second caller retries (or sees a 409). The resolver itself does not lock; the DB does. Documented as a deliberate "optimistic-then-DB-arbitrated" pattern.

---

## Repository Interfaces

| Repository | Entity | Methods (additive — existing methods unchanged) |
|------------|--------|------------------------------------------------|
| `IOrderService` (existing) | `Order` | ~~`GetByIdempotencyKeyAsync(...)`~~ **removed as dead code (QUAL-1, review 035-v8)** — idempotency resolution lives entirely inside `CreateFromCartAsync` via the private `FindKeyHolderAsync`; the standalone public lookup had no production caller. `IsSameLogicalRequest(Order existing, LogicalRequest current) -> bool` — pure comparison; no I/O (as-built: `DivergentFields`) |

The Stripe `ClientSecret` already lives on the `Order` aggregate (no new field) so `Replay` can return it directly. EuPlatesc has no equivalent secret — the redirect URL is reconstructed deterministically from the persisted `Order` (HMAC-MD5 of stable fields).

---

## Ubiquitous Language

| Term | Definition |
|------|------------|
| **Idempotency Key** | A client-supplied opaque token (UUID v4 recommended) carried as the `Idempotency-Key` HTTP header. Identifies a single logical payment-intent creation across retries. |
| **Logical Request** | The subset of a `CreateOrderRequest` plus server-resolved totals that defines the *intent* of an order: `(PaymentProcessor, DeliveryType, EasyboxLockerId, TotalRon)`. Two requests with the same logical-request hash are considered the same operation. |
| **Replay** | The successful path of a repeat call: the existing order is returned unchanged, no new database row, no new Stripe `PaymentIntent`. |
| **Conflict** | A repeat call with the same key but a divergent logical request. Yields 409 ProblemDetails. The client must either change the key or change the request. |
| **Idempotency Window** | The 24-hour interval starting at `Order.CreatedAt` during which the key dedupes. After the window, the key is *stale* — it remains on the row for audit but no longer matches in lookup. |
| **Missing Key (transitional)** | A request without the `Idempotency-Key` header. The endpoint accepts it (preserving current behaviour during the FE migration window) and logs `INFO payments.idempotency.missing-key` (OBS-3, review 035-v8: Information, not Warning — it is the expected transitional state on ~100% of requests, so a Warning is constant alert noise). After the FE adopts the header globally, missing-key should escalate to 400 (and the log back to Warning) — that decision is out of scope for this bolt. |
| **Replay Token** | The Stripe `ClientSecret` returned to a replay caller. Identical bytes to the original response. Equivalent for EuPlatesc is the redirect URL. |

---

## Acceptance-criteria coverage

| Story | AC | Modelled by |
|-------|----|-------------|
| 001 | Migration adds `IdempotencyKey` nullable `varchar(80)` + filtered unique index | Aggregate invariant (1) "Uniqueness" + value object constraint "length 1..80" |
| 001 | Down-migration drops index and column | Implicit from additive-only modelling; reversibility is a stage-2 / migration concern |
| 002 | Two consecutive calls with same key + identical body → same OrderId + ClientSecret, one DB row, one Stripe PaymentIntent | `Replay(existingOrder)` outcome + Stripe SDK `RequestOptions.IdempotencyKey` (stage-2 wiring) |
| 002 | Same key + divergent body → 409 "Idempotency conflict" naming divergent fields | `Conflict(divergentFields)` outcome → 409 ProblemDetails (NOT 422, per ADR-002) |
| 002 | Missing key behaves as today + Information log (OBS-3, v8 — was Warning) | `MissingIdempotencyKeyObserved` event (logged) + resolver short-circuit (treat as `NewOrder`) |
| 002 | Stripe SDK `RequestOptions.IdempotencyKey` set | Stage-2 wiring; modelled here as "Replay Token" parity requirement |
| 003 | Same key on EuPlatesc → same redirect URL + OrderId | `Replay(existingOrder)` outcome; EuPlatesc redirect URL is deterministic from the persisted order |
| 003 | First-call failure before persist → retry allowed | Resolver returns `NewOrder` because no row was ever written; no special state to clean up |

---

## Out of scope

- Cross-instance idempotency cache (deferred to intent 021 — Redis backplane).
- Refund-flow idempotency (admin path, different aggregate root).
- IPN-handler changes (already idempotent via signature + amount check).
- Cleaning up stale keys (the row keeps the key after 24 h for audit; no garbage collection).
- Distinguishing "Stripe gateway returned an idempotency conflict to us" from "our DB returned a conflict to us" — both surface as 409 with the same shape. Stage 2 may refine.
