---
last_updated: 2026-06-03T12:30:00Z
total_decisions: 24
---

# Decision Index

This index tracks all Architecture Decision Records (ADRs) created during Construction bolts.
Use this to find relevant prior decisions when working on related features.

## How to Use

**For Agents**: Scan the "Read when" fields below to identify decisions relevant to your current task. Before implementing new features, check if existing ADRs constrain or guide your approach. Load the full ADR for matching entries.

**For Humans**: Browse decisions chronologically or search for keywords. Each entry links to the full ADR with complete context, alternatives considered, and consequences.

---

## Decisions

### ADR-024: Implicit Attempt Count from `(now - CreatedAt)`, No Persisted `RejectionCount` Column
- **Status**: accepted
- **Date**: 2026-06-03
- **Bolt**: 039-efactura-anaf (efactura-generation-and-anaf)
- **Path**: `bolts/039-efactura-anaf/adr-024-implicit-attempt-count-from-updatedat-no-persisted-counter.md`
- **Summary**: `InvoiceUploadJob` enforces the `1h/4h/16h/64h then Failed` retry budget by comparing `(now - Invoice.CreatedAt)` against the cumulative backoff sum (85h default). No `Invoice.RejectionCount` column is persisted. Trade-off: clock skew at the 85h boundary can shift the give-up decision by seconds (CAS resolves the race; same-minute either-way outcome is noise), and admin retry doesn't extend the budget. Wins: no migration in bolt 039; clean state machine (5 statuses, no counter shadow); behaviour fully derivable from the persisted row. If the column is ever needed for ops queries or budget-extension semantics, the addition must engage with this ADR's trade-off rather than silently "fix" what looks like missing state.
- **Read when**: working on `InvoiceUploadJob.PollSubmitted` or the backoff schedule logic; reviewing PRs that add a counter column to `Invoices`; reviewing PRs that change `Anaf:BackoffHours`; debugging "why did this invoice escalate to Failed at hour 86"; designing admin-retry behaviour for similar lifecycle workers; reasoning about the regulated 5-business-day SLA vs the worker's give-up boundary.

### ADR-023: `InvoiceUploadJob` Uses DB Polling, Not In-Process `Channel<T>`
- **Status**: accepted
- **Date**: 2026-06-03
- **Bolt**: 039-efactura-anaf (efactura-generation-and-anaf)
- **Path**: `bolts/039-efactura-anaf/adr-023-worker-dispatch-db-polling-not-in-process-channel.md`
- **Summary**: The ANAF invoice-upload worker uses a `PeriodicTimer`-driven DB poll every 30 minutes, NOT an in-process `Channel<T>`. Explicitly diverges from ADR-010 (which chose `Channel<T>` for the photo-promotion worker). ADR-010's load-bearing reasons — sub-second reaction latency, polling-table DB load, simpler code — all flip for the ANAF worker: the 5-business-day SLA tolerates 30-min cadence (240× headroom); the `Invoices` table is cold; polling is the *simpler* shape because it removes producer-side coupling (Stripe webhook, admin retry, future replay tools all just write to DB without notifying anyone). Admin retry becomes one UPDATE; multi-replica safety comes from ADR-015 + ADR-016 (CAS); ANAF outages absorb naturally as DB backlog. Future bolt 046 (Redis) may revisit with leader election.
- **Read when**: working on `InvoiceUploadJob`; reviewing PRs that add invoice-creation paths (need to remember: just write to DB, no notification needed); reviewing PRs that touch dispatch cadence; debugging "why didn't my admin retry kick off immediately"; planning multi-replica scaling (bolt 046); designing the next BackgroundService and choosing between polling and `Channel<T>` (the SLA distinction is the rule).

### ADR-022: Dual-Write Rollout for Regulated Integrations via Feature Flag, Not Branch Deploy
- **Status**: accepted
- **Date**: 2026-06-03
- **Bolt**: 039-efactura-anaf (efactura-generation-and-anaf)
- **Path**: `bolts/039-efactura-anaf/adr-022-dual-write-rollout-via-feature-flag.md`
- **Summary**: Regulated integrations (e-Factura today; credit notes, e-receipts later) are rolled out via a config feature flag that suppresses the customer-facing side effect while the full pipeline runs. For bolt 039 the pattern is only half-built: `Invoicing:CustomerEmailAttachments:Enabled` (default `false`) gates nothing customer-visible yet, because no email send path exists — and the XML build, ANAF upload, PDF render and storage write it describes are themselves gated by `Anaf:Enabled`, not unconditional. Flipped to `true` after a one-week inspection window. Wins over branch-deploy approach: reversibility (config-only rollback), production code path identical to inspection week, clean audit trail. Pattern is intended to recur for the next regulated integration. The flag is one if-statement of permanent code surface; deletion after permanent rollout is a tracked cleanup.
- **Read when**: planning a rollout of any new regulated integration (credit notes, e-receipts, anything ANAF-adjacent); reviewing PRs that add a new "off by default" feature flag; reviewing PRs that read `Invoicing:CustomerEmailAttachments`; flipping the flag in production (use this ADR to recall what side effect is gated and what's NOT gated); cleaning up unused feature flags after a permanent rollout.

### ADR-021: PDF Library — QuestPDF, Not PuppeteerSharp
- **Status**: accepted
- **Date**: 2026-06-03
- **Bolt**: 039-efactura-anaf (efactura-generation-and-anaf)
- **Path**: `bolts/039-efactura-anaf/adr-021-pdf-library-questpdf-not-puppeteersharp.md`
- **Summary**: `InvoicePdfRenderer` uses QuestPDF (pure-managed C# DSL, ~15MB DLL, sub-100ms cold render, Community License free under $1M revenue). PuppeteerSharp is explicitly forbidden in this codebase without a superseding ADR. The decision is operational-cost-driven: PuppeteerSharp adds ~200MB Chromium to the prod image, cold-start latency, version-drift exposure, and per-host browser cache management. QuestPDF's downsides — DSL learning curve, future ~$100/year commercial-license fee above $1M revenue — are bounded and well-known. The PDF document tree lives as C# in `Services/Invoicing/InvoicePdfDocument.cs`; no Razor `.cshtml` intermediate. Community License declared at process startup in `Program.cs`.
- **Read when**: working on `InvoicePdfRenderer` or `InvoicePdfDocument`; reviewing PRs that touch the PDF rendering path; reviewing PRs that add a new PDF use case (refund receipt, customer statement); evaluating PDF library alternatives; reviewing the DEPLOYMENT.md License Obligations section; debugging "this PDF looks different on prod than dev" (font drift); planning a Chromium-based feature (HTML email previews) that might tempt a PuppeteerSharp adoption.

### ADR-020: Postgres `SEQUENCE` for Invoice Numbering — Accept Gap-on-Rollback
- **Status**: accepted
- **Date**: 2026-06-03
- **Bolt**: 038-vat-calculation (vat-calculation)
- **Path**: `bolts/038-vat-calculation/adr-020-postgres-sequence-for-invoice-numbering-accept-gap-on-rollback.md`
- **Summary**: Invoice numbering uses Postgres `SEQUENCE` per `(series, year)` partition with `CREATE SEQUENCE IF NOT EXISTS` + `nextval()`. Atomic, concurrent, idiomatic — but gaps on transaction rollback by Postgres design. The counter-table alternative (`FOR UPDATE` + increment + INSERT in one transaction) was considered and rejected: it eliminates gaps at the cost of row-level lock contention on the Paid path. Rollback is extraordinarily rare in our flow (single SaveChanges, no external I/O inside the transaction); mitigation is a quarterly audit query that surfaces any gap for the accountant. Composite unique index `(Series, year, Number)` is the last-line-of-defence against the database-restore-error case.
- **Read when**: working on `IInvoiceNumberingService` or `Invoice` insertion; reviewing PRs that touch the Paid transition's transactional scope; designing the bolt-039 worker that creates invoices; tempted to "harden" the numbering by switching to a counter table (don't, without re-engaging with this ADR's trade-off); debugging "why is there a gap between `FT-2026-00042` and `FT-2026-00044`?"; auditing invoices for a fiscal period; planning Redis-backed alternatives at scale.

### ADR-019: `MidpointRounding.AwayFromZero` for Legal / Regulatory Decimal Math
- **Status**: accepted
- **Date**: 2026-06-03
- **Bolt**: 038-vat-calculation (vat-calculation)
- **Path**: `bolts/038-vat-calculation/adr-019-decimal-rounding-away-from-zero-for-regulatory-math.md`
- **Summary**: All decimal rounding in legal / regulatory code paths uses `MidpointRounding.AwayFromZero`. The default `decimal.Round(x, 2)` overload (no mode argument) is FORBIDDEN in any path that produces a value written to an invoice, submitted to ANAF, or reported to a customer as a tax amount. The default uses banker's rounding (`ToEven`) which disagrees with Romanian accountancy convention + ANAF tooling; small per-row, accumulates across many invoices, audit-time finding. `VatCalculator.ExtractBreakdown` is the canonical reference; future tax-adjacent classes follow it. Unit test pins the contract against the .NET default.
- **Read when**: writing any code that rounds a `decimal` to a fixed number of decimal places in a financial / regulatory context; reviewing PRs that touch VAT, totals, discounts, refunds, or any value written to an invoice or report; debugging "why does my invoice's `VatRon` disagree with ANAF's recomputation?"; tempted to "simplify" a `decimal.Round(x, 2, MidpointRounding.AwayFromZero)` call by dropping the mode argument (don't — it's load-bearing); designing similar rounding rules for other regulatory domains.

### ADR-018: `/metrics` Uses IP Allow-List, Not JWT
- **Status**: accepted
- **Date**: 2026-06-03
- **Bolt**: 044-tracing-and-metrics (tracing-and-metrics)
- **Path**: `bolts/044-tracing-and-metrics/adr-018-metrics-endpoint-ip-allow-list-not-jwt.md`
- **Summary**: `GET /metrics` deliberately deviates from the project's JWT-everywhere posture (intent 002). The endpoint is gated by `MetricsEndpointIpAllowListMiddleware` on **two** conditions: the request must arrive on the scrape listener (`Observability:Metrics:ScrapePort`, else 404) and its peer address must be in `Observability:Metrics:AllowedScrapeIps` (plain IPs or CIDR; default `["127.0.0.1", "::1"]` for local dev, production override required; else 403). JWT is the wrong primitive for server-to-server scrape: tokens have to be issued, rotated, and revoked manually, and the failure mode (expired token → dashboards go dark) is silent. The **2026-07-31 amendment** added the scrape-port gate: behind a reverse proxy the peer address is always the proxy's, so an allow-list alone could only be made to work by allow-listing the proxy — which opens the endpoint to the internet. `X-Forwarded-For` was rejected as a substitute. The shipped `Caddyfile` also refuses `/metrics*` at the edge. The **2026-09-03 amendment** holds that rejection now that `ForwardedHeadersMiddleware` exists for the rest of the pipeline: a scrape request is excluded from it — the `UseWhen` predicate skips a request only when it is both on the scrape listener and for the metrics path, each conjunct guarding a different way the other could be misconfigured — so the gate still judges the true peer, and a request there carrying an allow-listed address in `X-Forwarded-For` still gets 403. Removing the exclusion returns 200 and publishes the metric store.
- **Read when**: adding any server-to-server endpoint without a user identity (push-gateways, internal admin APIs); reviewing PRs that touch `/metrics` or its middleware; tempted to add `[Authorize]` to `/metrics` "for consistency"; putting a new reverse proxy, ingress or service mesh in front of the API; designing a NetworkPolicy / security group that bounds traffic to the API; changing which requests `ForwardedHeadersMiddleware` sees; debugging "why does the scraper get 403 (or 404)"; reasoning about defence-in-depth for the observability stack.

### ADR-017: Deterministic Trace-ID Sampling, Not Random
- **Status**: accepted
- **Date**: 2026-06-03
- **Bolt**: 044-tracing-and-metrics (tracing-and-metrics)
- **Path**: `bolts/044-tracing-and-metrics/adr-017-deterministic-trace-id-sampling.md`
- **Summary**: `DeterministicTraceIdSampler` derives its sampling decision from a deterministic hash of the trace_id (lower 63 bits normalised against `long.MaxValue`), not from `Random.NextDouble()`. The same trace_id + same rate always yields the same decision. Required so a single request's spans (HTTP server → EF queries → outbound Stripe/Sameday) are either all sampled or all dropped — never partial. Also gives cross-service trace consistency under W3C trace-context propagation, since downstream services using the same OTel-spec-recommended algorithm make the same decision. Random sampling produces frankenstein traces and is silently wrong. Two **2026-08-03 amendments**: (1) per-route rates left the sampler — it runs before routing resolves an endpoint and is handed no tags at all, so `Observability:Sampling:Routes` could never match and is gone; one service-wide `Sampling:Default` remains, per-route rates are a collector (tail-sampling) concern. (2) An out-of-rate span is sampled `RecordOnly`, not `Drop`, because the SDK skips `OnEnd` for dropped spans and the "errors are always sampled" override could never fire; a promoted error span is exported alone (its children were dropped at start) and carries `fototipar.sampling.error_override`.
- **Read when**: implementing or modifying any sampler in `Observability/Sampling/`; reviewing PRs that touch the sampling path; debugging "why does this trace_id exist but its EF spans don't"; wondering where per-route sample rates went, or why an error trace has no child spans; reasoning about cross-service trace completeness; tempted to use `Random.NextDouble` "for simplicity"; designing similar deterministic-by-id decisions in other domains (feature flag rollouts, A/B test bucketing).

### ADR-016: Compare-and-Swap via `ExecuteUpdateAsync` for Multi-Replica-Safe `Order.Status` Transitions
- **Status**: accepted
- **Date**: 2026-06-02
- **Bolt**: 037-awb-and-tracking-jobs (awb-and-tracking-jobs)
- **Path**: `bolts/037-awb-and-tracking-jobs/adr-016-cas-execute-update-for-multi-replica-status-transitions.md`
- **Summary**: Background workers that transition `Order.Status` use EF 8's `ExecuteUpdateAsync` with a `WHERE` clause that pins the expected source state — a database-native compare-and-swap. The affected-row count is the success signal: `affected == 0` is a legitimate, expected outcome (race lost; another replica or admin already moved the row) logged at Info level. No new column (no `RowVersion`), no transactions wrapping the outbound HTTP call, no Redis. Generalises beyond bolt 037: any future job that mutates a status column should adopt the same shape.
- **Read when**: writing any `BackgroundService` that mutates `Order.Status`; reviewing PRs that add `ExecuteUpdateAsync` calls on `Orders`; designing concurrency for an aggregate with an enum-style status column; debugging "did the wrong replica win this race?"; reasoning about whether to introduce a `RowVersion` column (don't, unless the *value* semantics — not just the transition — need protecting).

### ADR-015: Accept Duplicate `CreateAwb` Calls on Multi-Replica (Rely on Vendor Idempotency + DB Re-Check)
- **Status**: accepted
- **Date**: 2026-06-02
- **Bolt**: 037-awb-and-tracking-jobs (awb-and-tracking-jobs)
- **Path**: `bolts/037-awb-and-tracking-jobs/adr-015-accept-duplicate-awb-create-on-multi-replica.md`
- **Summary**: `AwbRetryJob` running on multiple replicas will enqueue (and dispatch) the same order ID more than once. Rather than introduce leader election / Redis locks before bolt 046, we accept duplicate `POST /api/awb` calls. Correctness rests on two load-bearing properties: (a) Sameday's `awbPayment` external reference makes the second call idempotent on the vendor side, and (b) `IAwbCreator.CreateForOrderAsync` re-checks `Status == Paid AND AwbNumber IS NULL` before persisting. A future PR that breaks either property silently breaks correctness — this ADR makes the dependency loud.
- **Read when**: working on `AwbRetryJob`, `AwbDispatcher`, or `IAwbCreator`; modifying the `AwbNumber` write path; refactoring or removing the `Status == Paid AND AwbNumber IS NULL` re-check (don't — it's the load-bearing half); reasoning about scale-out before bolt 046's Redis introduction; vendor behaviour drift post-mortem; debugging "why are there two `sameday.awb.created` logs for the same order id?"; designing similar acceptance-of-duplication trade-offs for other vendor integrations.

### ADR-014: 401 Retry-Once Lives in `SamedayAuthHandler`, Not in Polly
- **Status**: accepted
- **Date**: 2026-06-02
- **Bolt**: 036-sameday-api-client (sameday-api-client)
- **Path**: `bolts/036-sameday-api-client/adr-014-401-retry-in-auth-handler-not-polly.md`
- **Summary**: The Sameday HTTP pipeline keeps "session expiry" (401 → invalidate token → re-auth → retry once → `SamedayAuthException` on a second 401) in a dedicated `DelegatingHandler` *outside* Polly. Polly retains exclusive ownership of transient-failure retries (5xx / 408 / 429 / network) on its own budget. 401 is never in Polly's retryable status set. Trade-off: two retry layers to reason about, but each is independently testable, retry budgets don't collide, and Polly's exponential backoff doesn't waste a second on session refresh.
- **Read when**: working on the Sameday HTTP pipeline or `SamedayAuthHandler`; debugging "why was this request retried N times"; tempted to fold 401 into Polly's retry list; adding a new outbound endpoint to `SamedayClient`; designing retry semantics for *another* upstream that has token-expiry behaviour.

### ADR-013: In-Process Singleton Token Cache for the Sameday API
- **Status**: accepted
- **Date**: 2026-06-02
- **Bolt**: 036-sameday-api-client (sameday-api-client)
- **Path**: `bolts/036-sameday-api-client/adr-013-in-process-sameday-token-cache.md`
- **Summary**: The Sameday bearer token is cached in-process on a singleton `SamedayTokenProvider`, gated by `SemaphoreSlim(1,1)` against thundering-herd, with a 60 s pre-expiry safety window. No Redis, no Postgres-backed token row. Mirrors ADR-010's "in-process now, durable later" stance for the photo-promotion queue. Cross-instance sharing is deferred to intent 021 (when Redis lands for other reasons). Each replica re-authenticates independently; cost is well within Sameday's rate budget.
- **Read when**: working on `SamedayTokenProvider` or the Sameday auth flow; reviewing token caching during an outage post-mortem; planning horizontal scale-out *before* intent 021 lands Redis; tempted to persist the token in Postgres; rotating Sameday credentials and asking "do I need to restart all replicas?"

### ADR-012: Retention Anchor = `Order.PaidAt`
- **Status**: accepted
- **Date**: 2026-05-29
- **Bolt**: 052-archive-retention (archive-retention)
- **Path**: `bolts/052-archive-retention/adr-012-retention-anchor-paid-at.md`
- **Summary**: The intent-024 retention job (large preview + thumbnail cleanup after the configurable window, default 12 months) measures the window from `Order.PaidAt` — not from a new `CompletedAt` column, not from `UpdatedAt`, not from delivery time. Chosen because `PaidAt` is always set on any order whose photos reached the cloud (including `Paid → Cancelled`), is never re-set, and is index-friendly. Trade-off: a slow fulfilment shortens customer-visible archive lifetime. Net schema change: zero.
- **Read when**: Working on `ArchiveRetentionJob` or anything time-anchored in intent 024; tempted to add an `Order.CompletedAt` / `DeliveredAt` column; debugging "why aren't these old photos getting cleaned up?"; designing the customer-facing copy ("12 months from when you paid us"); planning courier-IPN-driven delivery confirmation (would supersede this ADR).

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
