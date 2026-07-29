---
type: findings
target: 015-sameday-shipping
version: 5
commit: 5fc330b
pass-type: certification
date: 2026-07-28
---

# Findings v5 — 015-sameday-shipping certification

Per-finding detail behind [review-v5.md](review-v5.md). Verdicts from the workflow's adversarial
verify stage (trace-constructor / guard-hunt). Convergence = independent lenses that found it.
Dormant feature: nothing here is reachable in production until both `Sameday:*` flags flip.

## Medium (17)

**D55 — Easybox address fields uncapped → storage-exhaustion DoS** · `CreateOrderRequestValidator.cs:26`
· security · conv 1 · **confirmed**. Guest POSTs Easybox order with a valid locker and
`ShippingAddress.Street` ~28 MB; only RecipientName+Phone are validated for Easybox, so it passes and
OrderService persists the whole JSON snapshot (column has `HasConversion`, no `HasMaxLength`). Under
Kestrel's 30 MB default, repeated unpaid orders bloat `Orders`. Fix: cap Street/Number/Block/City/
County/PostalCode in the Easybox `When` block (Block is uncapped in both branches).

**D56 — Downloadable label never surfaced to admin** · `AdminOrderDtos.cs:44` · requirements · conv 2 ·
**confirmed**. `AwbCreator` persists `AwbLabelUrl` and `GetLabelPdfAsync` is fully built, but no DTO/
endpoint returns the label and the method has no production caller. Intent 015's "downloadable label"
Must goal is stored, never delivered. Fix: add `AwbLabelUrl`/`DeliveredAt` to the admin projection or
wire `GetLabelPdfAsync` to an endpoint; else track an explicit follow-up story. (Ties to D89.)

**D57 — Stale-claim reclaim path untested** · `AwbCreatorTests.cs:250` · tests · conv 1 · **confirmed**.
Only a fresh claim is tested. Dropping `|| AwbClaimedAt < now-claimTtl` (AwbCreator.cs:76) keeps every
test green, yet a crashed-worker order is Skipped forever. Fix: seed `AwbClaimedAt = Now-(TTL+margin)`,
assert Created + one vendor call.

**D58 — Claim-release-after-failure untested** · `AwbCreatorTests.cs:326` · tests · conv 1 · **confirmed**.
No-op the release ExecuteUpdate (AwbCreator.cs:93) and all RetryLater/GiveUp tests still pass (they
assert only outcome type); a within-TTL in-process retry is then wrongly Skipped, defeating the
30/120/300 s backoff. Fix: after a transient RetryLater, read back and assert `AwbClaimedAt` is null.

**D59 — prefillEasyboxContact branches untested** · `delivery-step.spec.ts` · tests · conv 2 · **confirmed**
(hinted). Specs clear localStorage and supply no signed-in user, so neither the guest-session nor the
displayName prefill branch (delivery-step.ts:377) ever runs with data. A wrong key / swapped name-phone
/ bad-JSON throw ships green — the guest-state defect cluster (CLAUDE.md class 11). Fix: seed a
`guestSession` and a mock user; assert prefill; assert malformed JSON doesn't throw.

**D60 — Vendor pdfLink > 500 overflows Postgres varchar(500)** · `AwbCreator.cs:156` · db-parity +
input-validation · conv 2 · **confirmed**. A signed `pdfLink` > 500 chars is persisted verbatim into
`AwbLabelUrl` (`character varying(500)`); on Postgres the ExecuteUpdate throws "value too long" AFTER
the billable AWB exists → caught as transient RetryLater → `AwbNumber` stays null → retry sweep
re-calls the vendor each cycle. SQLite/InMemory never enforce the cap. Fix: validate/cap LabelUrl (or
store as text) before persist; add a length-enforcing-provider test.

**D61 — Phone regex over-accepts digit-poor input** · `CreateOrderRequestValidator.cs:28` ·
input-validation · conv 1 · **confirmed**. `^[0-9+\s().\-]{6,20}$` checks charset+length only. `"1-2-3-4"`
or `"()-. ()"` passes → order created Paid → AWB call → Sameday 4xx → GiveUp, order stuck Paid, no AWB.
Fix: strip separators and require a real digit count (e.g. RO `^0?7\d{8}$` / 9–15 digits).

**D62 — Vendor rejection body never logged** · `AwbCreator.cs:136` · observability · conv 1 · **confirmed**.
On a Sameday 400, `SamedayValidationException.ResponseBody` holds the field-level reason but the GiveUp
path logs only `ex.Message` ("HTTP 400 at {endpoint}"). `ResponseBody` is set (3 sites) and never read.
Ops can't diagnose why the label failed. Fix: log `ResponseBody` (truncated/PII-redacted) on GiveUp.

**D63 — Systemic tracking failure collapsed into per-order Warning** · `ShipmentTrackingJob.cs:148` ·
observability · conv 1 · **confirmed**. A rotated Sameday password makes every poll throw
`SamedayAuthException`, caught by the broad `catch (SamedayException)` and logged at Warning, identical
to a benign one-off. All delivery detection is dead but no Error fires. Fix: catch Auth/Protocol before
the base catch and log at Error with a distinct event name.

**D64 — selectMethod never resets selectedLockerId → 400 dead-end** · `delivery-step.ts:399` · frontend-ux
· conv 1 · **confirmed**. Pick Easybox+locker, switch to Courier, back to Easybox: `setMethod` clears
`state.lockerId` but the `selectedLockerId` signal stays set, so `canContinue` reads true; payment
builds a request with `easyboxLockerId=null` → server 400 (ReviewStep.proceed has no isDeliveryComplete
gate). Fix: `selectedLockerId.set(null)` (and clear the forms) in `selectMethod`.

**D65 — Enabled=true composition root never booted (DI-cycle risk)** · `Program.cs:146` ·
completeness-critic · conv 1 · **confirmed**. No test boots with `Enabled=true`. `SamedayTokenProvider`
(singleton) → `ISamedayAuthenticator` → `ISamedayClient` whose pipeline includes `SamedayAuthHandler`
needing `ISamedayTokenProvider` — a reentrant-resolution/captive-dependency risk `ValidateOnStart`
won't surface. First flag flip in staging is the first execution. Fix: add a WebApplicationFactory
DI-resolution test with Enabled=true (+ Jobs) resolving `ISamedayClient` and the 3 hosted services.

**D66 — Locker SamedayId freshness assumed, no sync** · `OrderToAwbRequestMapper.cs:48` ·
completeness-critic · conv 1 · **confirmed**. `GetLockersAsync` reads the local `EasyboxLockers` table;
AWB creation trusts `order.EasyboxLocker.SamedayId`. A renamed/removed Sameday locker → vendor 4xx →
permanent GiveUp, no label, retries repeat the same 4xx until give-up. Fix: verify how `SamedayId` is
populated/refreshed; add a sync check or a clearer give-up log.

**(plausible, verify-refuted-as-code-defect)**
**id24 / id25 → D45 residual (accepted).** Resilience pipeline retries the non-idempotent AWB POST; on
crash/timeout between vendor-create and persist, no-double-bill rests on unverified vendor dedup. No
code-only failing trace (the code guards every in-process path). = the documented ADR-015 residual.
**id26 → refuted.** DeliveredAt non-UTC `timestamptz` write — Npgsql converts any-offset `DateTimeOffset`
to the UTC instant; premise false. Dropped.
**id41 → D89 (cleanup).** `GetLabelPdfAsync` no production caller — plausible; no exploit path since
`AwbLabelUrl` isn't exposed anywhere. Dead code tied to the D56 label story.

## Low (19) — ledger D67–D82 / D79–D81

- **D67** poll-throttle window = tick interval, so orders poll every *other* tick (ε>0 latency makes the
  guard skip alternate ticks) · `ShipmentTrackingJob.cs:74` · confirmed.
- **D68** durable claim released on vendor-call *timeout* — the one outcome where AWB state is unknown;
  re-attempt in ~30 s (< TTL) risks a 2nd label if the vendor didn't dedup · `AwbCreator.cs:90` · confirmed.
- **D69** client Easybox phone validation presence-only, weaker than server regex → bad phone still 400s
  at CreateOrder (same gap in Courier `addressForm`) · `delivery-step.ts:321` · conv 2 · confirmed.
- **D70** no `MaxResponseContentBufferSize` on untrusted Sameday responses → OOM from a hijacked/MITM
  multi-GB body, ×MaxConcurrentSamedayCalls · `SamedayClient.cs:218` · confirmed.
- **D71** Polly backoff is 1/2/4 s (exponential factor 2), not FR-3's 1/4/16 s; the `// 1s,4s,16s`
  comment is wrong · `SamedayPolicies.cs:50` · confirmed.
- **D72** new `ShippedAt` column has no backfill (design called it "existing"); a pre-integration Shipped
  order has `ShippedAt=null` → never polled, never flagged for manual closure (FR-5) · migration
  `20260602190046:21` · confirmed.
- **D73** FR-4 per-attempt logging partial: retry sweep logs only aggregate count, and no correlation id
  is threaded into any BackgroundService · `AwbRetryJob.cs:95` · confirmed.
- **D74** `prefillEasyboxContact` re-implements `GuestAuthService.getStoredSession()` (hardcoded key +
  inline parse) → silent drift on key/shape change · `delivery-step.ts:382` · confirmed.
- **D75** HTTP status classification duplicated 4× in `SamedayClient` and drifts from
  `SamedayPolicies.IsRetryableStatus` (`>=500` unbounded vs `>=500 && <600`) · `SamedayClient.cs:65` ·
  confirmed.
- **D76** parallel multi-order poll fan-out never exercised (every test seeds 1 order), so a per-order
  DbContext-scope regression ships green · `ShipmentTrackingJobTests.cs:117` · confirmed.
- **D77** retry sweep tested only on EF InMemory; fresh-claim skip clause never exercised (all seeds
  leave `AwbClaimedAt=null`, OR short-circuits) · `AwbRetryJobTests.cs:23` · confirmed.
- **D78** `setLocker` preserving existing contact + review-step Easybox display untested (reverts stay
  green) · `checkout-state.service.spec.ts:45` · confirmed.
- **D79** signed-in recipient-name prefill is dead code — `currentUser$$` is only ever set to null
  (never on login) · `delivery-step.ts:392` · confirmed.
- **D80** transient locker-search 500 rendered as "Niciun easybox găsit pentru acest oraș" — user thinks
  city unserved · `delivery-step.ts:371` · confirmed.
- **D81** `LockerServiceId`/`CourierServiceId` default to placeholder `7` and are unvalidated when
  Enabled → every AWB under the wrong service · `SamedaySettingsValidator.cs:38` · confirmed.
  (Configuring real values is already the parked pre-enable task.)
- **D82** dispatcher-backoff vs 60-min retry-sweep double-enqueue window: claim released for the whole
  backoff (up to 3600 s), so a sweep can enqueue a 2nd in-flight job with attempt reset to 1 (CAS still
  prevents a double vendor call) — untested · `AwbCreator.cs:129` · confirmed.

**Re-raises (decided, re-affirmed):** id39→**D50** dispatcher orchestration untested (deferred, harness);
id44→**D23** Postgres migration DDL never exercised (deferred); id34→**D29** Polly `OnRetry` logging
(deferred); id12→**D39** StaticShippingService DI (deferred, cleanup).

## Cleanup (6) — ledger D83–D88

- **D83** undocumented scope: locker prime-all-on-init + clear-restores-list + search-error survival
  bundled into the diff with no story/AC · `delivery-step.ts:366`.
- **D84** `AwbCreator` loads the order tracked (Include Items+EasyboxLocker) but only reads it; all
  writes use ExecuteUpdate → add `AsNoTracking()` · `AwbCreator.cs:42`.
- **D85** tracking poll loads the order tracked (Include User) but only reads it → `AsNoTracking()` ·
  `ShipmentTrackingJob.cs:129`.
- **D86** recipient phone rule + regex literal duplicated across Easybox/Courier blocks · `CreateOrder
  RequestValidator.cs:40`.
- **D87** magic day-count query floors (30/60 days) implicitly coupled to registry lifetimes (32/60),
  unnamed · `AwbRetryJob.cs:252`.
- **D88** DeliveredAt-timestamptz finding recorded refuted (Npgsql handles DateTimeOffset). (No fix.)
- **D89** `GetLabelPdfAsync` dead code — no production caller (see D56).
