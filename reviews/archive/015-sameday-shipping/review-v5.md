---
type: review
target: 015-sameday-shipping
version: 5
supersedes: 4
commit: 5fc330b
branch: feat/bolt-036-sameday-api-client
pass-type: certification
subtype: single-pass recorded deviation
date: 2026-07-28
reviewer: discovery-review.wf.js (11 blinded lenses → dedup → convergence-weighted adversarial verify)
verdict: approve-with-followups
outcome: CERTIFIED
findings: { high: 0, medium: 17, low: 19, cleanup: 6, confirmed: 30, plausible: 3, re-raise: 4, refuted: 1 }
tests: { dotnet: "898/898 (+10 skipped MinIO)", frontend: "452/452" }
cost: { agents: 48, tokens: 2904079, note: "first run hit a platform-wide API 500 wave (34/64 agents); resumed from cache — 30 lens results replayed, verify+dedup+correctness re-ran clean" }
---

# Review v5 — 015-sameday-shipping · CERTIFICATION (single-pass recorded deviation)

One fresh blinded full-manifest pass (11 lenses) against the frozen fix code `5fc330b`. The prior
two blinded passes (the v3 pair) ran today on near-identical code and the fix round since was small
and independently verified (review-v4), so this single pass satisfies the recorded-deviation rule
([README router](../README.md#the-router), note ²; precedent 043-v9).

## Result: no serious defect survives → **CERTIFIED**

**0 High. 0 fix-caused regression. 0 reopened fix.** Nothing re-arms the loop. The pass surfaced
17 medium + 19 low + 6 cleanup — all validation/coverage/observability gaps or risks that only bite
once the two `false` flags flip. Verdict is capped at `approve-with-followups` (residuals remain);
outcome is **Certified** (no 🔴, per the severity-based stop rule).

Adversarial verify: 30 confirmed (trace built), 3 plausible, 4 re-raises of already-deferred items,
1 refuted. Max cross-lens convergence 2.

## The follow-up list — a pre-enable checklist, none live today

The feature is dormant (`Sameday:Enabled`=false, `Sameday:Jobs:Enabled`=false); none of these can
be hit in production now. **Work this list before flipping either flag.** Full detail in
[findings-v5.md](findings-v5.md); ledger IDs D55–D82.

### Medium — address before enabling (12, all confirmed)

| D# | Finding | Site |
|----|---------|------|
| D55 | Easybox order leaves Street/Block/etc. uncapped → 28 MB body storage-exhaustion DoS (only Name+Phone validated) | `CreateOrderRequestValidator.cs:26` |
| D56 | AwbLabelUrl persisted but never surfaced to admin; `GetLabelPdfAsync` has no caller — the "downloadable label" Must goal is stored, not delivered | `AdminOrderDtos.cs:44` |
| D57 | Stale-claim (crashed-worker) reclaim path untested — drop the TTL clause and the suite stays green | `AwbCreatorTests.cs:250` |
| D58 | Claim-release-after-failure untested — a broken release strands in-process retries, green | `AwbCreatorTests.cs:326` |
| D59 | `prefillEasyboxContact` guest-session + signed-in branches untested (guest-state = repo's top defect class) | `delivery-step.spec.ts` |
| D60 | Vendor `pdfLink` > 500 chars overflows Postgres `varchar(500)` → persist throws → retry re-bills each cycle; SQLite/InMemory never enforce the cap | `AwbCreator.cs:156` |
| D61 | Phone regex validates charset+length only (no digit count) → `"1-2-3-4"` reaches the paid AWB call → GiveUp, order stuck Paid | `CreateOrderRequestValidator.cs:28` |
| D62 | `SamedayValidationException.ResponseBody` (the vendor's field-level reason) captured but never logged on permanent AWB fail | `AwbCreator.cs:136` |
| D63 | Systemic tracking failure (rotated credentials) logged per-order at Warning, never Error — no alert signal | `ShipmentTrackingJob.cs:148` |
| D64 | `selectMethod` never resets `selectedLockerId`; Easybox→Courier→Easybox proceeds to payment with lockerId=null → 400 dead-end | `delivery-step.ts:399` |
| D65 | `Sameday:Enabled=true` composition root never booted — token-provider ↔ auth-handler DI cycle risk unverified; first flag flip in staging is the first execution | `Program.cs:146` |
| D66 | Local `EasyboxLockers.SamedayId` freshness assumed; a stale locker code → permanent GiveUp with no label | `OrderToAwbRequestMapper.cs:48` |

### Low (19) & Cleanup (6) — ledger backlog D67–D82

Notable lows: poll-throttle window equals the tick interval so orders poll every *other* tick (D67);
client phone gate weaker than server (D69); no response-size cap on untrusted Sameday bodies → OOM
(D70); Polly backoff is 1/2/4 s not the FR-3 documented 1/4/16 s, comment wrong (D71); new `ShippedAt`
column has no backfill so pre-integration Shipped orders never poll (D72); signed-in name prefill is
dead code — `currentUser$` never emits (D79); a transient locker-search 500 renders as "no easybox in
this city" (D80); service-id defaults (placeholder `7`) unvalidated when Enabled (D81). Cleanups: two
jobs load entities tracked but only read them, HTTP status-classification duplicated 4× and drifting
from `SamedayPolicies`, an undocumented locker-map UX bundled into the diff.

### Re-affirmed deferrals (no new decision)

D50 (dispatcher-runtime test — harness needed), D23 (Postgres migration DDL never exercised), D29
(Polly `OnRetry` logging), D39 (StaticShippingService DI) — all re-raised by this pass with their
prior decisions attached; all stand as deferred.

### The D45 vendor-idempotency residual — re-confirmed, already accepted

The race + correctness lenses independently re-surfaced it (findings 24/25): the AWB-create POST is
auto-retried by the transport pipeline and, on a crash/timeout between vendor-create and DB-persist,
no-double-bill rests entirely on Sameday deduping on `ClientInternalReference`. The skeptic could
build **no code-only failing trace** — the code guards every in-process path; the residual is purely
the unverified vendor contract. This is the honest residual [ADR-015](../../memory-bank/bolts/037-awb-and-tracking-jobs/adr-015-accept-duplicate-awb-create-on-multi-replica.md)
already documents (accepted + Error-alerted). **Verify Sameday's create-idempotency before enabling.**

### Refuted (1)

DeliveredAt written from a non-UTC vendor `DateTimeOffset` to `timestamptz` — the skeptic showed
Npgsql maps any-offset `DateTimeOffset` to the UTC instant (the restriction is `DateTime`-only), so
the premise is false. Dropped.

## Build & tests (at `5fc330b`)

- **.NET** `898/898` (+10 skipped MinIO) · **Frontend (Vitest)** `452/452`.

## Loop state

Certification quiet (no 🔴, no regression, no reopen) → **the review loop for 015-sameday-shipping is
complete; verdict Certified.** D55–D82 enter the ledger backlog as the pre-enable checklist.
