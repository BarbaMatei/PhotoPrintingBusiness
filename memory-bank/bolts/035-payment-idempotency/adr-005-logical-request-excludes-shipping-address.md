---
bolt: 035-payment-idempotency
created: 2026-05-25T13:36:00Z
status: accepted
superseded_by: null
---

# ADR-005: Idempotency Equality (LogicalRequest) Excludes Shipping Address

## Context

Bolt 035 detects whether a repeat payment-intent call carrying the same `Idempotency-Key` is the *same* operation (→ replay the original order) or a *different* one (→ 409 conflict). This requires defining what "same operation" means.

A `CreateOrderRequest` carries: `PaymentProcessor`, `DeliveryType`, `EasyboxLockerId?`, and `ShippingAddress?`. The server additionally resolves `TotalRon` (shipping cost server-side, per bolt 034). We must choose which of these fields participate in the equality check that distinguishes "replay" from "conflict."

The tension: a customer who clicks "Pay", sees a validation hiccup or a slow response, corrects a **postal-code typo** in their courier address, and clicks "Pay" again — with the browser reusing the same `Idempotency-Key` — is, intuitively, performing the *same* purchase. If `ShippingAddress` participates in equality, that benign correction yields a **409 conflict** and a confusing dead-end. If it does not, the original order (with the *original*, typo'd address) is replayed — which is also surprising, because the corrected address silently does not take effect.

Neither choice is free. The decision is which surprise is less harmful.

## Decision

`LogicalRequest` equality is computed over **`(PaymentProcessor, DeliveryType, EasyboxLockerId, TotalRon)` only**. `ShippingAddress` is **excluded**.

Consequence made explicit: when a customer changes only their shipping address and retries with the same key within the 24 h window, they receive the **original** order (original address). To change the address, the client must use a **new** `Idempotency-Key` (which the FE should generate per distinct "Pay" intent, not per page-load).

## Rationale

Idempotency exists to make retries safe. The dominant real-world retry is a network/timeout retry of the *same* intent, where the body is byte-identical or trivially different. Optimising the equality check to treat address edits as "different" turns the common safe-retry into a conflict, which is the opposite of what idempotency is for.

The `(processor, deliveryType, locker, total)` tuple captures everything that affects **money and fulfilment routing** — the fields where a silent divergence would actually harm the customer (wrong charge, wrong delivery method). An address typo affects neither the amount charged nor the locker/courier choice at the granularity we model; it's a fulfilment-detail edit that belongs to a *new* checkout intent, signalled by a new key.

The FE contract is the mitigation: generate a fresh `Idempotency-Key` whenever the user meaningfully re-initiates checkout (e.g. after editing the cart or address), and reuse the key only for automatic/manual retries of the *same* submission.

### Alternatives Considered

| Alternative | Pros | Cons | Why Rejected |
|-------------|------|------|--------------|
| Include `ShippingAddress` in equality (strict byte-equality) | Address edits always take effect; no silent staleness | A postal-code typo correction → 409 dead-end on the common retry path; brittle against whitespace/casing differences | Rejected — penalises the exact scenario idempotency should protect |
| Include a *normalized* address (trim/casing-folded) | Tolerates trivial formatting diffs | Still 409s on a genuine correction; normalization rules become their own maintenance burden | Rejected — complexity without solving the core tension |
| Exclude `ShippingAddress` (chosen) | Common retry path stays a clean replay; equality reflects money + routing | A real address correction under the same key is silently ignored | **Accepted** — paired with the FE "new key per new intent" contract |
| No logical comparison at all (any key match = replay) | Simplest | A genuinely different order (different total!) under a reused key would be silently replayed — a real defect | Rejected — `TotalRon` divergence must be a conflict, so some comparison is mandatory |

## Consequences

### Positive

- The dominant safe-retry path (same intent, network retry) is always a clean replay.
- Equality is computed over stable, server-authoritative fields (`TotalRon` comes from bolt 034's server-side resolution, not the client).
- No fragile address normalization logic.

### Negative

- A customer who edits only their address and reuses the same key gets the original address silently. Surprising if it happens.
- Correct behaviour depends on the FE honouring the "new key per new checkout intent" contract — a cross-team coupling that must be documented in the API contract.

### Risks

- **Risk**: FE reuses one `Idempotency-Key` for the entire checkout page lifetime, so address edits never take effect. **Mitigation**: document the key-generation contract in OpenAPI + FE onboarding; recommend a fresh UUID v4 on each "Pay" click that follows a cart/address mutation.
- **Risk**: business later decides address edits *must* take effect on retry. **Mitigation**: `LogicalRequest` is a value object — adding `ShippingAddress` to its equality set is a localized change; this ADR would then be superseded.

## Related

- **Stories**: 002-stripe-intent-idempotency, 003-euplatesc-initiate-idempotency
- **Standards**: `api-conventions.md` (document the `Idempotency-Key` generation contract)
- **Previous ADRs**: ADR-004 (state conflict → 409) — defines the response when `LogicalRequest` equality fails
