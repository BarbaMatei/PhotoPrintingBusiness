---
last_updated: 2026-05-25T14:45:00Z
total_decisions: 6
---

# Decision Index

This index tracks all Architecture Decision Records (ADRs) created during Construction bolts.
Use this to find relevant prior decisions when working on related features.

## How to Use

**For Agents**: Scan the "Read when" fields below to identify decisions relevant to your current task. Before implementing new features, check if existing ADRs constrain or guide your approach. Load the full ADR for matching entries.

**For Humans**: Browse decisions chronologically or search for keywords. Each entry links to the full ADR with complete context, alternatives considered, and consequences.

---

## Decisions

### ADR-006: Accept the Historical Key Leak and Mitigate by Rotation (No History Rewrite)
- **Status**: accepted
- **Date**: 2026-05-25
- **Bolt**: 041-secrets-management (secrets-rotation-and-guardrails)
- **Path**: `bolts/041-secrets-management/adr-006-secret-history-accept-and-rotate.md`
- **Summary**: A real dev RSA JWT key was committed in the initial commit and remains in git history. Rather than rewrite history (force-push, full re-clone), accept its presence and neutralize it by rotating the key out of all environments. Pre-commit hook + CI gitleaks scan prevent recurrence.
- **Read when**: Handling leaked credentials, deciding whether to rewrite git history, JWT key rotation, secret-scanning setup, or onboarding secrets for a new environment.

### ADR-005: Idempotency Equality (LogicalRequest) Excludes Shipping Address
- **Status**: accepted
- **Date**: 2026-05-25
- **Bolt**: 035-payment-idempotency (payment-idempotency)
- **Path**: `bolts/035-payment-idempotency/adr-005-logical-request-excludes-shipping-address.md`
- **Summary**: Idempotency "same operation" equality is computed over `(PaymentProcessor, DeliveryType, EasyboxLockerId, TotalRon)` only; `ShippingAddress` is excluded. A retry that changes only the address (same key) replays the original order; a new checkout intent must use a new `Idempotency-Key`.
- **Read when**: Working on payment-intent creation, idempotency-key handling, the `Idempotency-Key` FE contract, replay-vs-conflict logic, or deciding which request fields define operation equality.

### ADR-004: State Conflicts Return HTTP 409, Distinct from Validation's 422
- **Status**: accepted
- **Date**: 2026-05-25
- **Bolt**: 035-payment-idempotency (payment-idempotency)
- **Path**: `bolts/035-payment-idempotency/adr-004-state-conflict-409.md`
- **Summary**: A structurally-valid request that conflicts with existing persisted state returns `409 Conflict` (RFC 7807), not the `422` used for validation failures (ADR-002). Idempotency conflicts carry a `divergentFields` array of field names only (no values/PII). Establishes the project precedent for all state-conflict surfaces.
- **Read when**: Choosing an HTTP status for "already exists / conflicts with existing state" errors, implementing idempotency, coupon double-redemption, invoice-number collisions, or any check against persisted state. Also read when deciding 409 vs 422 vs 400.

### ADR-003: Trust Client-Provided X-Correlation-Id (Validate, Don't Reject)
- **Status**: accepted
- **Date**: 2026-05-05
- **Bolt**: 001-error-handling-logging (error-handling-logging)
- **Path**: `bolts/001-error-handling-logging/adr-003-correlation-id-trust.md`
- **Summary**: The `CorrelationIdMiddleware` accepts a client-provided `X-Correlation-Id` header if it is a valid GUID, otherwise generates a fresh one. Accept if valid GUID, generate if missing or malformed — never reject the request.
- **Read when**: Working on middleware, request tracing, correlation IDs, distributed tracing, logging enrichment, or any code that reads/sets the `X-Correlation-Id` header.

### ADR-002: Custom ValidationFilter Overrides [ApiController] 400 Behavior
- **Status**: accepted
- **Date**: 2026-05-05
- **Bolt**: 001-error-handling-logging (error-handling-logging)
- **Path**: `bolts/001-error-handling-logging/adr-002-validation-filter-422.md`
- **Summary**: `[ApiController]`'s automatic 400 ModelState response is suppressed via `ApiBehaviorOptions.SuppressModelStateInvalidFilter = true`; a custom `ValidationFilter` returns 422 with `{ errors: [{field, message}] }`. All validation must use FluentValidation — data annotation validators (`[Required]`, `[MaxLength]`) are prohibited.
- **Read when**: Working on request validation, adding validators, implementing new controllers, handling ModelState errors, or configuring FluentValidation. Also read when encountering 400 vs 422 response code questions.

### ADR-001: Health Endpoint Always Returns HTTP 200
- **Status**: accepted
- **Date**: 2026-05-05
- **Bolt**: 001-error-handling-logging (error-handling-logging)
- **Path**: `bolts/001-error-handling-logging/adr-001-health-endpoint-200.md`
- **Summary**: The `/health` endpoint always returns `HTTP 200 OK` regardless of health check results; the `status` field in the JSON body conveys actual health state. This decouples transport-level reachability from application-level health.
- **Read when**: Working on health checks, monitoring configuration, load balancer setup, uptime monitoring, Docker healthcheck configuration, or any endpoint that reports system operational status.
