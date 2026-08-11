---
type: resolution
target: 015-sameday-shipping
version: 5
answers: review-v5.md
status: resolved
fixed_commit: 1816f5f
closed: 2026-07-29
---

# Resolution v5 — 015-sameday-shipping

## Findings

| ID | Status | Commit | Note |
|---|---|---|---|
| PPW-294 | fixed | `3764fa0` | Easybox address fields length-capped, since the locker supplies the address; the block field is capped in both delivery types. Test: an oversized Easybox street fails validation. |
| PPW-295 | fixed | `66c6d50` | The label link and both timestamps added to the admin order-detail response and projection; a test asserts they surface. The fetch method is kept as the authenticated path — see Decisions. |
| PPW-296 | fixed | `c75003d` | A test seeds a claim older than the lifetime and asserts the creator reclaims it and creates the label, which is the crashed-worker recovery path. |
| PPW-297 | fixed | `c75003d` | A test asserts the claim is released on a definitive failure, complementing the PPW-307 preserve-on-timeout case. |
| PPW-298 | fixed | `c611a23` | Specs cover guest-session prefill and malformed stored data in the Easybox contact prefill. |
| PPW-299 | fixed | `c75003d` | An over-length vendor label link is dropped with a warning before the persist, so the AWB number always records and the re-bill loop cannot start; the column widened from 500 to 2048 by a new migration. Approach-checked. |
| PPW-300 | fixed | `3764fa0` | The phone rule requires 9 to 15 real digits, not just an allowed character set. Tests: "1-2-3-4" and a separator-only value both fail. |
| PPW-301 | fixed | `56320c0` | The vendor rejection body, truncated to limit personal data, is logged on a permanent label failure. |
| PPW-302 | fixed | `16d065b` | Authentication and protocol faults are caught before the base catch and logged at Error, deduplicated per outage window; per-order detail drops to Debug. Approach-checked: a per-order Error would storm. |
| PPW-303 | fixed | `c611a23` | Choosing a method resets the selected locker, with a no-op guard on re-clicking the same one, so a stale locker cannot reach payment as null. Test covers the Easybox, Courier, Easybox switch. Approach-checked. |
| PPW-304 | fixed | `fd59bf2` | The auth handler resolves the token provider lazily through the service provider, breaking the constructor-time cycle. Registration moved into one extension; a test resolves the enabled root. The approach-check confirmed a real cycle. |
| PPW-305 | fixed | `56320c0` | The clearer give-up log from PPW-301 surfaces stale-locker failures, which is the finding's own alternative. Keeping the locker table in step is a pre-enable operational task — see Decisions. |
| PPW-306 | fixed | `16d065b` | A tick-start clock stamp and an interval-minus-buffer eligibility window, so orders poll about every interval rather than every other tick. Approach-checked, keeping the cross-replica band. |
| PPW-307 | fixed | `c75003d` | The claim is preserved on timeout, on a post-create persist failure and, after the micro-review (1816f5f), on a retryable 5xx; a transport failure with no status still releases. Approach-checked. |
| PPW-308 | fixed | `c611a23` | The client phone control mirrors the server character set and the 9-to-15-digit rule on both forms. Test: a digit-poor phone keeps Continue disabled. |
| PPW-309 | fixed | `56320c0` | The vendor client caps buffered responses at 10 MB; the label PDF still streams. |
| PPW-310 | fixed | `56320c0` | The transport backoff is the intended 1, 4 and 16 seconds through an explicit delay generator, because the library default is base 2. |
| PPW-311 | deferred | — | The shipped-timestamp backfill is moot before deployment: no legacy Shipped orders exist and the admin transition stamps it going forward. See Decisions. |
| PPW-312 | fixed | `aa995c1` | The retry sweep logs the order id on each re-enqueue, so a single order's attempts can be followed. |
| PPW-313 | fixed | `c611a23` | The prefill reads the guest session through the shared accessor instead of an inline local-storage parse. |
| PPW-314 | fixed | `56320c0` | One status-check chokepoint in the client, sharing the policy helper's classification, so the four ladders cannot drift. |
| PPW-315 | fixed | `16d065b` | A two-order tick test exercises the parallel per-order scope fan-out. |
| PPW-316 | fixed | `aa995c1` | The retry-sweep tests moved to SQLite, with fresh-claim skip and stale-claim re-drive cases added. |
| PPW-317 | fixed | `c611a23` | Tests: selecting a locker preserves the Easybox contact, and the review step renders no address line for an Easybox order. |
| PPW-318 | fixed | `c611a23` | The token service adds a name claim and the auth service populates the current-user stream from it on login and on session restore, so the signed-in prefill is live rather than dead code. |
| PPW-319 | fixed | `c611a23` | A transient locker-search error sets a distinct error signal, reset per fetch, with a retry, instead of showing "no easybox here". Test covers error then recovery. Approach-checked. |
| PPW-320 | deferred | — | Service-id validation is the parked pre-enable configuration task; the feature is dormant, so no boot-time guard was added now. See Decisions. |
| PPW-321 | fixed | `aa995c1` | The dispatcher re-enqueue is unit-testable through a pure delay computation and a clock-based delayed re-enqueue; tests assert the schedule, the lifetime floor and the attempt increment. |
| PPW-322 | wont-fix | — | The bundled locker-map behaviour is intentional and is now covered by tests from this round; a retrospective story adds nothing. See Decisions. |
| PPW-323 | fixed | `c75003d` | The creator loads the order untracked, since all writes go through direct updates. |
| PPW-324 | fixed | `16d065b` | The tracking poll loads the order untracked. |
| PPW-325 | fixed | `3764fa0` | Recipient name and phone rules extracted into one shared method used by both blocks; the pattern hoisted to a constant. |
| PPW-326 | fixed | `aa995c1` | The sweep's outside-window floor is derived from the registry entry lifetime, so the deduplication window and the query window cannot drift. |
| PPW-327 | false-positive | — | Refuted at the certification pass: Npgsql maps any-offset values to the UTC instant, so there is no timestamp write bug. No fix. |
| PPW-328 | wont-fix | — | The label-fetch method is not dead: it is the authenticated path kept for a pre-enable admin label-proxy endpoint. See Decisions. |
| PPW-259 | fixed | `56320c0` | A separate requests-per-second setting decouples the transport rate from the concurrency gate, defaulting to it so behaviour is preserved. |
| PPW-263 | fixed | `56320c0` | The client no longer fabricates a wall-clock observation time; the snapshot value is nullable and the job supplies its poll clock for the delivered timestamp. Test updated. |
| PPW-264 | fixed | `56320c0` | The token expiry is normalized to UTC. |
| PPW-266 | fixed | `16d065b` | The non-delivered sync write is guarded on Status = Shipped and a forward stamp, so it is monotonic and never touches a delivered row. |
| PPW-268 | fixed | `56320c0` | Retries log through a retry callback carrying attempt, delay and outcome. |
| PPW-269 | fixed | `56320c0` | Resolved the other way: the design document was corrected, because the generic health writer never delivered the documented Sameday field; the field itself was dropped as out of scope. |
| PPW-272 | false-positive | — | Obsolete: the tick loads ids only and each poll loads its order once on its own scope, which the parallel design requires. The unused variable the finding cites does not exist. See Decisions. |
| PPW-274 | fixed | `c611a23` | The locker list is primed lazily through the search stream only while Easybox is active, so a courier-only customer triggers no fetch. Test asserts no fetch at init for courier. |
| PPW-276 | fixed | `56320c0` | The unreferenced redaction helper removed. |
| PPW-277 | fixed | `16d065b` | The give-up and tracking-stop registries share one base type. |
| PPW-278 | fixed | `fd59bf2` | The shipping service injects the static shipping service, registered scoped, instead of constructing it, and drops the two constructor parameters. |
| PPW-279 | deferred | — | Pre-existing bolt-035 model and designer drift on a secret's length. The current snapshot and every later designer carry 512; only the two June designers hold 255. See Decisions. |
| PPW-289 | fixed | `aa995c1` | The dispatcher orchestration is unit-tested through the pure delay computation and the delayed re-enqueue, so the deferred coverage gap is closed. |

## Scope

| Cluster | Findings | Files | Approach-check |
|---|---|---|---|
| A — Validation rules and shared rule extraction (`3764fa0`) | PPW-294, PPW-300, PPW-325 | `Validators/Payments/CreateOrderRequestValidator.cs` | not needed (rules only) |
| B — Claim lifecycle, label-link clamp, creator tests (`c75003d`) | PPW-296, PPW-297, PPW-299, PPW-307, PPW-323 | `Services/Sameday/AwbCreator.cs`, `Migrations/`, `Tests/…/AwbCreatorTests.cs` | run before implementation — sound-with-changes, folded in |
| C — Client hardening and transport behaviour (`56320c0`) | PPW-301, PPW-305, PPW-309, PPW-310, PPW-314, PPW-259, PPW-263, PPW-264, PPW-268, PPW-269, PPW-276 | `Services/Sameday/SamedayClient.cs`, `SamedayPolicies.cs` | not needed (classification, limits and log content) |
| D — Tracking poll clock, outage dedup, untracked reads (`16d065b`) | PPW-302, PPW-306, PPW-315, PPW-324, PPW-266, PPW-277 | `BackgroundJobs/ShipmentTrackingJob.cs`, registries | run before implementation — sound-with-changes, folded in |
| E — Checkout state, prefill, locker search (`c611a23`) | PPW-298, PPW-303, PPW-308, PPW-313, PPW-317, PPW-318, PPW-319, PPW-274 | `UI/…/delivery-step.ts`, `checkout-state.service.ts`, specs | run before implementation — sound-with-changes, folded in |
| F — Enabled-root wiring and container registration (`fd59bf2`) | PPW-304, PPW-278 | `Program.cs`, `Extensions/SamedayServiceCollectionExtensions.cs` | run before implementation — confirmed a real resolution cycle |
| G — Dispatcher and retry-sweep testability (`aa995c1`) | PPW-312, PPW-316, PPW-321, PPW-326, PPW-289 | `BackgroundJobs/AwbDispatcher.cs`, `AwbRetryJob.cs` | not needed (extracting pure functions to test) |
| H — Admin label surface (`66c6d50`) | PPW-295 | `DTOs/Admin/AdminOrderDtos.cs` | not needed (a projection and a DTO) |
| I — Micro-review follow-up: preserve the claim on a retryable 5xx (`1816f5f`) | PPW-307 | `Services/Sameday/AwbCreator.cs` | covered by cluster B's check |
| J — Not fixed | PPW-311, PPW-320, PPW-279, PPW-322, PPW-328, PPW-327, PPW-272 | — | not needed (no code changed) |

## Decisions

### The shipped-timestamp backfill is deferred

No deployed data exists, so there are no legacy Shipped orders with an empty shipped timestamp, and
the admin transition stamps it going forward. A one-time backfill belongs in the deploy runbook if
the application ever ships with pre-integration Shipped orders. Adding a data migration now, for
zero current rows, is churn.

### Service-id validation is deferred to the pre-enable step (PPW-320, PPW-305)

The real Sameday service ids are a parked pre-enable configuration task by owner decision. The
feature is dormant, so no boot-time guard was added now; configuring the real ids before flipping
the flag is the existing pre-enable step. The same reasoning covers PPW-305's locker-table freshness:
this round improved the give-up log so a stale locker is diagnosable, and keeping the locker table
in step with the vendor stays an operational task, not code.

### The secret-length drift stays deferred

Pre-existing bolt-035 drift, not Sameday. The current model snapshot and every migration designer
after June carry 512; only the two June designers hold the historical 255, and those are immutable
history. Stripe client secrets are about 66 characters, so any gap between database and model is
harmless. Aligning the actual Postgres column is a bolt-035 groom item.

### Two cleanup rows ruled wont-fix (PPW-322, PPW-328)

PPW-322: the locker-map behaviours that rode into the diff — prime on init, clear restores the list,
survive a search error — are intentional and are now covered by the delivery-step specs this round
added, so a retrospective story adds no value. PPW-328: the label-fetch method is not dead code. It is
the authenticated way to fetch the label by AWB number, kept for a pre-enable admin label-proxy
endpoint, because the raw vendor link that PPW-295 now surfaces may need the bearer token to open.

### Two rows ruled false-positive (PPW-272, PPW-327)

PPW-272: the tick selects ids only and untracked, then each poll loads its own order on its own scoped
context, which the parallel fan-out requires rather than a wasteful re-query; the unused variable
the finding cites does not exist in the current code. PPW-327 was refuted during the certification pass
itself: Npgsql maps any-offset values to the UTC instant.

### The vendor-idempotency residual is unchanged

Not in this round's scope and not touched by it. Avoiding a double courier charge after a crash
between the vendor call and the persist still rests on Sameday deduplicating on the reference we
send. Confirm that with the vendor before enabling the jobs, per ADR-015.
