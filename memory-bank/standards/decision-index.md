---
last_updated: 2026-05-29T10:20:00Z
total_decisions: 11
---

# Decision Index

This index tracks all Architecture Decision Records (ADRs) created during Construction bolts.
Use this to find relevant prior decisions when working on related features.

## How to Use

**For Agents**: Scan the "Read when" fields below to identify decisions relevant to your current task. Before implementing new features, check if existing ADRs constrain or guide your approach. Load the full ADR for matching entries.

**For Humans**: Browse decisions chronologically or search for keywords. Each entry links to the full ADR with complete context, alternatives considered, and consequences.

---

## Decisions

### ADR-011: Per-Upload Atomicity with Confirmed-Write-Then-Delete
- **Status**: accepted
- **Date**: 2026-05-29
- **Bolt**: 051-order-photo-promotion (order-photo-promotion)
- **Path**: `bolts/051-order-photo-promotion/adr-011-per-upload-atomicity-confirmed-write-then-delete.md`
- **Summary**: Promotion atomicity is per-upload, not per-order, and side effects within an upload are applied strictly in the order: cloud writes → DB row update → local file deletes ("Confirmed-Write-Then-Delete"). `Upload.StorageLocation` is the single source of truth; partial promotion states are normal and recoverable. Wraps `OrderPhotoPromoter` and binds units 002 (purge) and 003 (viewing) to the same invariant.
- **Read when**: Working on `OrderPhotoPromoter` or anything in intent 024; modifying upload-to-cloud transitions; designing the unit-002 purge or unit-003 viewing paths; debugging "where do these bytes live?" issues; reasoning about crash recovery for promotion; tempted to wrap the order loop in a DB transaction.

### ADR-010: In-Process `Channel<T>` + Startup Recovery Scan Instead of a Durable Work-Queue Table
- **Status**: accepted
- **Date**: 2026-05-29
- **Bolt**: 051-order-photo-promotion (order-photo-promotion)
- **Path**: `bolts/051-order-photo-promotion/adr-010-in-process-promotion-queue.md`
- **Summary**: Promotion queueing uses an in-memory `Channel<PromotionJob>` consumed by a single `BackgroundService`, with crash-safety provided by a startup `PromotionRecoveryScanner` that re-derives pending work from `Upload.StorageLocation`. No durable `PromotionJobs` table. Trade-off: simpler code + single source of truth, in exchange for blocking multi-VM scale-out until the queue is replaced (likely alongside bolt 046's Redis introduction).
- **Read when**: Working on the promotion worker, recovery scanner, backfill CLI, or anything in intent 024; planning multi-VM scale-out; tempted to add a `PromotionJobs` table; debugging "why is order X not getting promoted?"; introducing a new producer of promotion work.

### ADR-009: Cloudflare R2 as the Recommended Concrete Cloud Target
- **Status**: accepted
- **Date**: 2026-05-28
- **Bolt**: 043-cloud-storage-provider (cloud-storage-provider)
- **Path**: `bolts/043-cloud-storage-provider/adr-009-cloudflare-r2-recommended-cloud-target.md`
- **Summary**: `S3StorageService` is vendor-neutral; this records the production recommendation of Cloudflare R2 over AWS S3, based on $0 egress (decisive for image serving), Cloudflare-edge proximity to the Romanian audience, and lower storage cost. AWS S3 and MinIO remain fully supported via the same code path; only config changes.
- **Read when**: Choosing or configuring a cloud storage backend, writing/updating `docs/DEPLOYMENT.md` storage section, debugging R2-specific quirks (`Region="auto"`, `ForcePathStyle=true`), reasoning about CDN cache rules and egress cost, or evaluating storage cost.

### ADR-008: Two-Tier Storage with Per-Upload StorageLocation and IStorageRouter
- **Status**: accepted
- **Date**: 2026-05-28
- **Bolt**: 043-cloud-storage-provider (cloud-storage-provider)
- **Path**: `bolts/043-cloud-storage-provider/adr-008-two-tier-storage-with-storage-location.md`
- **Summary**: Storage runs as two tiers — local (always available) and cloud (when configured) — with per-upload routing via `Upload.StorageLocation` and `IStorageRouter`. `Storage:Provider` is repurposed to "cloud tier on/off." The preview endpoint branches per upload. Driven by the intent-024 promote-on-payment lifecycle and GDPR data minimization; trades multi-replica scaling for the pre-payment phase.
- **Read when**: Working on upload/preview/promotion code paths, adding new storage callers, debugging where an upload's bytes live, planning multi-replica scale-out (pre-payment serving), or reading anything in intent 024.

### ADR-007: Storage Adapter Persists Bytes at Caller-Supplied Keys (Naming is an Application Concern)
- **Status**: accepted
- **Date**: 2026-05-28
- **Bolt**: 043-cloud-storage-provider (cloud-storage-provider)
- **Path**: `bolts/043-cloud-storage-provider/adr-007-storage-adapter-caller-supplied-keys.md`
- **Summary**: `IStorageService.SaveAsync` accepts an explicit, caller-supplied `string key` rather than inventing one. Storage key/naming policy lives in an application-layer `StorageKeys` helper, not in the adapter. Adapters perform byte persistence only.
- **Read when**: Modifying `IStorageService` or any of its implementations (`LocalStorageService`, `S3StorageService`, `FakeStorageService`); adding new asset kinds (e.g. `previews/`); writing tests that mock storage; implementing the intent-024 promoter/backfill; debugging storage key drift.

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
