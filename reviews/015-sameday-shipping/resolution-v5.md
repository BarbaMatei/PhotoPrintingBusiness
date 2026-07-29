---
type: resolution
target: 015-sameday-shipping
version: 5
status: resolved
review_commit: 5fc330b
fixed_commit: 1816f5f
closed: 2026-07-29
findings:
  # v5 mediums (D55–D66)
  D55: { status: fixed, commit: 3764fa0, note: "Easybox address fields length-capped (locker supplies the address); Block capped in both types. Test: oversized Easybox Street fails." }
  D56: { status: fixed, commit: 66c6d50, note: "AwbLabelUrl + ShippedAt + DeliveredAt added to the admin order-detail DTO + projection; test asserts they surface. GetLabelPdfAsync (D89) kept as the auth-safe fetch path for a pre-enable label-proxy endpoint." }
  D57: { status: fixed, commit: c75003d, note: "Test seeds a claim older than the TTL, asserts the creator reclaims + creates (crashed-worker recovery)." }
  D58: { status: fixed, commit: c75003d, note: "Test: on a definitive (unreachable) failure the claim is released; complements D68's preserve-on-timeout." }
  D59: { status: fixed, commit: c611a23, note: "Specs: guest-session prefill and malformed-JSON safety in prefillEasyboxContact." }
  D60: { status: fixed, commit: c75003d, note: "Over-length vendor label url dropped (null + warning) before persist so the AWB number always records — breaks the re-bill loop; column widened 500→2048 via a new migration; test covers it. Approach-checked." }
  D61: { status: fixed, commit: 3764fa0, note: "Phone requires 9-15 real digits (HasEnoughDigits Must), not just charset. Tests: '1-2-3-4' / '()-. ()' fail." }
  D62: { status: fixed, commit: 56320c0, note: "Vendor rejection body (truncated to limit PII) logged on permanent AWB failure." }
  D63: { status: fixed, commit: 16d065b, note: "Auth/Protocol caught before the base catch, logged at Error, deduped per outage window via TrackingStopRegistry.MarkOutageOnce; per-order detail at Debug. Approach-checked (per-order Error would storm)." }
  D64: { status: fixed, commit: c611a23, note: "selectMethod resets selectedLockerId (+ no-op re-click guard) so a stale locker can't reach payment as null. Test covers the Easybox→Courier→Easybox switch. Approach-checked." }
  D65: { status: fixed, commit: fd59bf2, note: "SamedayAuthHandler resolves ISamedayTokenProvider lazily via IServiceProvider — breaks the ctor-time resolution cycle. Registration extracted to AddSamedayIntegration; a test resolves the Enabled=true root (client + creator + 3 jobs). Confirmed a real cycle by approach-check." }
  D66: { status: fixed, commit: 56320c0, note: "Clearer give-up log (D62's vendor-reason logging) surfaces stale-locker failures — the finding's give-up-log alternative. Locker-table sync is a pre-enable operational task (no live sync); see decisions." }
  # v5 lows (D67–D82)
  D67: { status: fixed, commit: 16d065b, note: "Tick-start-clock stamp + interval-minus-buffer eligibility window → polls ~every interval, not every other tick. Test covers a one-interval-ago order. Approach-checked (kept the cross-replica dedup band)." }
  D68: { status: fixed, commit: c75003d, note: "PreserveClaim on timeout + post-create persist-fail + (micro-review, 1816f5f) a retryable 5xx/408/429 on the create call — all 'AWB may be billed' cases hold the claim through its TTL; a status-less transport failure still releases. Dispatcher defers re-enqueue past the TTL (floor clamps the TTL). Tests cover timeout, create-5xx-preserve, transport-release. Approach-checked." }
  D69: { status: fixed, commit: c611a23, note: "Client phone control mirrors the server charset + 9-15 digit rule (both forms). Test: digit-poor phone keeps Continue disabled." }
  D70: { status: fixed, commit: 56320c0, note: "Sameday HttpClient caps buffered responses at 10 MB; label PDF still streams (ResponseHeadersRead)." }
  D71: { status: fixed, commit: 56320c0, note: "Transport backoff now the intended 1/4/16 s via an explicit DelayGenerator (Polly Exponential is base-2)." }
  D72: { status: deferred, commit: null, note: "ShippedAt backfill moot pre-deploy (no legacy Shipped orders exist); the admin transition stamps ShippedAt going forward. A one-time backfill belongs in the deploy runbook if legacy data ever exists. See decisions." }
  D73: { status: fixed, commit: aa995c1, note: "Retry sweep logs order_id per re-enqueue (per-order traceability)." }
  D74: { status: fixed, commit: c611a23, note: "Prefill reads the guest session via GuestAuthService.getStoredSession() instead of an inline localStorage parse." }
  D75: { status: fixed, commit: 56320c0, note: "One EnsureSuccessOrThrowAsync chokepoint in SamedayClient, sharing SamedayPolicies.IsRetryableStatus so the 4 ladders can't drift." }
  D76: { status: fixed, commit: 16d065b, note: "Two-order tick test exercises the parallel per-order-scope fan-out." }
  D77: { status: fixed, commit: aa995c1, note: "AwbRetryJobTests ported to SQLite; fresh-claim (skip) + stale-claim (re-drive) cases added." }
  D78: { status: fixed, commit: c611a23, note: "Tests: setLocker preserves the Easybox contact; review-step renders no address line for an Easybox order." }
  D79: { status: fixed, commit: c611a23, note: "TokenService adds a `name` claim; auth.service populates currentUser$ from it on login + restore, so the signed-in recipient prefill is live (was dead code)." }
  D80: { status: fixed, commit: c611a23, note: "Transient locker-search error sets a distinct lockerSearchError signal (reset per fetch) + retry, instead of showing 'no easybox here'. Test covers error→recover. Approach-checked." }
  D81: { status: deferred, commit: null, note: "Service-id validation is the parked pre-enable config task (owner deferred setting real Sameday service ids); the feature is dormant, so no boot-time guard added now. See decisions + [[project_sameday_service_ids_parked]]." }
  D82: { status: fixed, commit: aa995c1, note: "Dispatcher re-enqueue is unit-testable (ComputeReEnqueueDelay + clock-based DelayedReEnqueueAsync); tests assert schedule, TTL floor, attempt+1." }
  # v5 cleanups (D83–D89)
  D83: { status: wont-fix, commit: null, note: "The bundled locker-map UX (prime-all-on-init, clear-restores-list, search-error survival) is intentional and is now covered by tests this round; not worth a separate story. See decisions." }
  D84: { status: fixed, commit: c75003d, note: "AwbCreator loads the order AsNoTracking (all writes use ExecuteUpdate)." }
  D85: { status: fixed, commit: 16d065b, note: "Tracking poll loads the order AsNoTracking." }
  D86: { status: fixed, commit: 3764fa0, note: "Recipient name+phone rules extracted to AddRecipientRules() shared by both blocks; regex hoisted to a const." }
  D87: { status: fixed, commit: aa995c1, note: "Retry sweep's outside-window floor derived from AwbGiveUpRegistry.EntryLifetime so the dedup + query window can't drift." }
  D88: { status: false-positive, commit: null, note: "Refuted at cert: Npgsql maps any-offset DateTimeOffset to the UTC instant; no timestamptz write bug. No fix." }
  D89: { status: wont-fix, commit: null, note: "GetLabelPdfAsync is not dead — it is the auth-safe fetch path retained for a pre-enable admin label-proxy endpoint (the raw vendor url may require the bearer token). See decisions." }
  # folded backlog (owner chose 'Everything')
  D20: { status: fixed, commit: 56320c0, note: "MaxRequestsPerSecond decouples the transport rate from the concurrency gate (defaults to it, preserving behaviour)." }
  D24: { status: fixed, commit: 56320c0, note: "Client no longer fabricates a wall-clock observedAt; TrackingSnapshot.ObservedAt is nullable and the job supplies its poll clock for DeliveredAt. Test updated." }
  D25: { status: fixed, commit: 56320c0, note: "Token expiry normalized with ToUniversalTime()." }
  D27: { status: fixed, commit: 16d065b, note: "Non-delivered LastTrackingSyncAt write guarded (Status=Shipped AND stamp < now) — monotonic, never touches a Delivered row." }
  D29: { status: fixed, commit: 56320c0, note: "Retries log via an OnRetry callback (attempt/delay/outcome)." }
  D30: { status: fixed, commit: 56320c0, note: "Corrected ddd-02: the documented /health sameday field was never delivered by the generic writer; dropped as out of scope." }
  D33: { status: false-positive, commit: null, note: "Obsolete: the tick loads IDs only (AsNoTracking) then PollOneAsync loads each order once on its own scoped context — required by the parallel design, not a wasteful re-query; no unused 'inWindow' variable exists in the current code. See decisions." }
  D35: { status: fixed, commit: c611a23, note: "Locker list primed lazily through the search stream only when Easybox is active — a courier-only user never triggers a fetch. Test asserts no init fetch for courier." }
  D37: { status: fixed, commit: 56320c0, note: "Removed the unreferenced LogRedactor." }
  D38: { status: fixed, commit: 16d065b, note: "AwbGiveUpRegistry + TrackingStopRegistry share a MemoryCacheOnceRegistry base." }
  D39: { status: fixed, commit: fd59bf2, note: "SamedayShippingService injects StaticShippingService (registered scoped) instead of new-ing it; drops the db/config ctor params." }
  D40: { status: deferred, commit: null, note: "Pre-existing bolt-035 model/designer drift (StripeClientSecret 255 vs 512): the current model snapshot + all post-June migration designers carry 512; only the two June designers hold the point-in-time 255 (immutable history). Harmless — Stripe secrets are ~66 chars. Align the DB in a bolt-035 groom, not this round. See decisions." }
  D50: { status: fixed, commit: aa995c1, note: "AwbDispatcher orchestration unit-tested (ComputeReEnqueueDelay + DelayedReEnqueueAsync) — the deferred coverage gap is closed." }
---

# Resolution v5 — 015-sameday-shipping

Fixer response to [review-v5.md](review-v5.md) (certification; all findings dormant behind the two
`false` flags). Owner chose the widest scope: all v5 findings **plus** the foldable backlog. Worked
in file-clusters. Design/mechanism/UI-state changes got adversarial approach-checks before
implementation (D68/D60, D65/D39, D64/D35/D80, D67/D27/D63). A fresh-eyes fix-diff micro-review (3
Explore agents) ran before hand-back; it confirmed the DI-cycle break, the client refactor, and the
frontend stream, and found one class-vs-instance gap — a retryable 5xx on the AWB create call also
leaves a possibly-billed AWB, so `PreserveClaim` was extended to it (1816f5f) — plus minor tightenings
(TTL floor clamp, outage-window constant, extra tests). All folded in.

Backend 914 / frontend 457 green; 10 MinIO tests skipped. Nothing pushed.

## Outcome counts

- **fixed:** 30 (all mediums except D66-note; all actioned lows/cleanups; 9 folded backlog).
- **deferred:** D72, D81, D40 (+ D50 was the prior deferral — now fixed).
- **wont-fix:** D83, D89.
- **false-positive:** D88 (refuted at cert), D33 (obsolete).

## Decisions & rationale (non-`fixed`)

- **D72 (deferred) — ShippedAt backfill.** No deployed data exists, so there are no legacy Shipped
  orders with a null ShippedAt; the admin transition stamps it going forward. A one-time backfill
  (`ShippedAt = UpdatedAt` for legacy Shipped rows) belongs in the deploy runbook if the app ever
  ships with pre-integration Shipped orders. Adding a data-migration now for zero current rows is
  churn.
- **D81 (deferred) — service-id validation.** The real Sameday service ids are a parked pre-enable
  configuration task (owner decision). The feature is dormant, so no boot-time guard is added now;
  configuring real ids before flipping the flag is the existing pre-enable step.
- **D40 (deferred) — StripeClientSecret 255-vs-512.** Pre-existing bolt-035 drift, not Sameday. The
  current model snapshot and every post-June migration designer carry 512; only the two June
  designers hold the historical 255. Stripe client secrets are ~66 chars, so any DB-vs-model gap is
  harmless. Aligning the actual Postgres column is a bolt-035 groom item.
- **D83 (wont-fix) — bundled locker-map UX.** The prime-all-on-init / clear-restores-list /
  search-error-survival behaviours are intentional UX and are now covered by delivery-step specs;
  a retro-story adds no value.
- **D89 (wont-fix) — GetLabelPdfAsync no caller.** Not dead: it is the auth-safe way to fetch the
  label PDF by AWB number, retained for a pre-enable admin label-proxy endpoint (the raw vendor
  `AwbLabelUrl`, now surfaced by D56, may require the bearer token to open).
- **D33 (false-positive) — tracking re-query.** The current tick selects IDs only (`AsNoTracking`),
  then each `PollOneAsync` loads its order once on its own scoped context — mandatory for the
  parallel fan-out, not a wasteful re-query of an already-loaded entity; the "unused inWindow" the
  finding cited does not exist in the current code.
- **D88 (false-positive)** — refuted during certification (Npgsql handles any-offset DateTimeOffset).

## Follow-ups for the re-reviewer (new surface this round)

- **New mechanisms** (owning lens in parentheses): durable-claim `PreserveClaim` + dispatcher TTL-floor
  re-enqueue (race); label-url clamp + `AlterAwbLabelUrlLength` migration (db-parity); tracking
  systemic-failure outage dedup via `MarkOutageOnce` + `MemoryCacheOnceRegistry` base (observability);
  `SamedayAuthHandler` lazy token-provider resolution + `AddSamedayIntegration` extension (correctness/DI);
  frontend `primeLockers$` stream + `lockerSearchError` signal (frontend-ux).
- **Accepted residual (unchanged):** D45 vendor-idempotency crash-window — still rests on Sameday
  deduping on `ClientInternalReference`; verify with the vendor before enabling (ADR-015).
