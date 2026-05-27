---
bolt: 035-payment-idempotency
created: 2026-05-25T13:35:00Z
status: accepted
superseded_by: null
---

# ADR-004: State Conflicts Return HTTP 409, Distinct from Validation's 422

## Context

[ADR-002](../001-error-handling-logging/adr-002-validation-filter-422.md) established `422 Unprocessable Entity` as the project-wide response for **validation** failures, returned by the custom `ValidationFilter` in the shape `{ errors: [{field, message}] }`. Every malformed or rule-violating request body produces a 422.

Bolt 035 introduces a new class of failure that is **not** a validation failure: an `Idempotency-Key` that is structurally valid and attached to a request whose body is structurally valid, but which collides with an **already-existing order** created under the same key with a *different* logical request (different processor, delivery type, locker, or total).

The request passed validation. The body is well-formed. What failed is a check against **persisted state**. Modelling this as 422 would conflate two genuinely different conditions:

- "Your input is wrong" (422 — fix the request).
- "Your input is fine, but it conflicts with something that already exists" (the customer should not silently get a second charge; the client tooling needs to distinguish this to decide whether to retry, surface an error, or rotate the key).

This is the first state-conflict surface in the codebase, but not the last — coupon double-redemption (intent 022), invoice-number collisions (intent 016), and any future "this resource already exists in an incompatible state" check share the same semantics.

## Decision

State conflicts return **`HTTP 409 Conflict`** as RFC 7807 ProblemDetails, distinct from validation's 422. For idempotency specifically:

```json
{
  "type": "https://fototipar.ro/problems/idempotency-conflict",
  "title": "Idempotency conflict",
  "status": 409,
  "detail": "The Idempotency-Key is already associated with a different request.",
  "divergentFields": ["paymentProcessor", "totalRon"],
  "correlationId": "…"
}
```

`409` is produced by a dedicated `IdempotencyConflictException` mapped in `ExceptionHandlerMiddleware`, parallel to the existing exception→status mappings (`NotFoundException`→404, `ConflictException`→409, etc.). The `divergentFields` array names **field names only** — never values — to avoid leaking PII or order amounts into client-visible errors.

## Rationale

422 and 409 answer different questions for the client. A client receiving 422 should fix and resubmit. A client receiving 409 on an idempotency key should NOT blindly resubmit the same key — doing so loops. The status code is the client's signal for which recovery path to take. Folding both into 422 would force clients to parse the body to disambiguate, defeating the purpose of HTTP status semantics.

### Alternatives Considered

| Alternative | Pros | Cons | Why Rejected |
|-------------|------|------|--------------|
| Return 422 like all other failures | One status code to remember; reuses `ValidationFilter` shape | Conflates "fix your input" with "this conflicts with existing state"; client cannot distinguish without parsing body; encourages retry loops | Rejected — semantically wrong; 422 means the *representation* is unprocessable, not that state conflicts |
| Return 200 with the existing order (treat conflict as replay) | Simplest client story | Silently returns a DIFFERENT order than the one the client described — masks a real client bug (key reuse with changed body) | Rejected — hides a defect; the client asked for X and would receive Y |
| Return 400 Bad Request | Familiar generic error | Even less specific than 422; loses the "conflict with existing state" meaning entirely | Rejected — 409 exists precisely for this |
| Use existing `ConflictException` (→409) without a dedicated type | Reuses plumbing | Loses the `divergentFields` payload and a clear, greppable exception name | Partially adopted — we add `IdempotencyConflictException` but it maps to 409 exactly like `ConflictException` |

## Consequences

### Positive

- Clients get an unambiguous signal: 422 = fix input, 409 = key/state conflict, do not blind-retry.
- Establishes a reusable project precedent for all future state-conflict surfaces (coupons, invoices).
- `divergentFields` gives client/debugging tooling actionable detail without leaking values.

### Negative

- A second 4xx semantic for developers to learn alongside the 422 convention from ADR-002.
- Requires a new exception type + middleware mapping (small, but non-zero surface).

### Risks

- **Risk**: a future contributor adds a state-conflict check and reaches for 422 out of habit. **Mitigation**: this ADR + `api-conventions.md` update should codify "409 for state conflict, 422 for validation." Recommend promoting to `api-conventions.md` in a follow-up.
- **Risk**: clients written before this bolt only handle 422. **Mitigation**: idempotency is opt-in via the `Idempotency-Key` header during the transition; clients that don't send the header never see 409.

## Related

- **Stories**: 002-stripe-intent-idempotency, 003-euplatesc-initiate-idempotency
- **Standards**: `api-conventions.md` (should gain a "409 vs 422" subsection in a follow-up)
- **Previous ADRs**: ADR-002 (validation → 422) — this ADR is the state-conflict counterpart, not a replacement
