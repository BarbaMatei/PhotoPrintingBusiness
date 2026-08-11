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

| D# | Status | Commit | Note |
|---|---|---|---|
| D55 | fixed | `3764fa0` | Easybox address fields length-capped, since the locker supplies the address; the block field is capped in both delivery types. Test: an oversized Easybox street fails validation. |
| D56 | fixed | `66c6d50` | The label link and both timestamps added to the admin order-detail response and projection; a test asserts they surface. The fetch method is kept as the authenticated path — see Decisions. |
| D57 | fixed | `c75003d` | A test seeds a claim older than the lifetime and asserts the creator reclaims it and creates the label, which is the crashed-worker recovery path. |
| D58 | fixed | `c75003d` | A test asserts the claim is released on a definitive failure, complementing the D68 preserve-on-timeout case. |
| D59 | fixed | `c611a23` | Specs cover guest-session prefill and malformed stored data in the Easybox contact prefill. |
| D60 | fixed | `c75003d` | An over-length vendor label link is dropped with a warning before the persist, so the AWB number always records and the re-bill loop cannot start; the column widened from 500 to 2048 by a new migration. Approach-checked. |
| D61 | fixed | `3764fa0` | The phone rule requires 9 to 15 real digits, not just an allowed character set. Tests: "1-2-3-4" and a separator-only value both fail. |
| D62 | fixed | `56320c0` | The vendor rejection body, truncated to limit personal data, is logged on a permanent label failure. |
| D63 | fixed | `16d065b` | Authentication and protocol faults are caught before the base catch and logged at Error, deduplicated per outage window; per-order detail drops to Debug. Approach-checked: a per-order Error would storm. |
| D64 | fixed | `c611a23` | Choosing a method resets the selected locker, with a no-op guard on re-clicking the same one, so a stale locker cannot reach payment as null. Test covers the Easybox, Courier, Easybox switch. Approach-checked. |
| D65 | fixed | `fd59bf2` | The auth handler resolves the token provider lazily through the service provider, breaking the constructor-time cycle. Registration moved into one extension; a test resolves the enabled root. The approach-check confirmed a real cycle. |
| D66 | fixed | `56320c0` | The clearer give-up log from D62 surfaces stale-locker failures, which is the finding's own alternative. Keeping the locker table in step is a pre-enable operational task — see Decisions. |
| D67 | fixed | `16d065b` | A tick-start clock stamp and an interval-minus-buffer eligibility window, so orders poll about every interval rather than every other tick. Approach-checked, keeping the cross-replica band. |
| D68 | fixed | `c75003d` | The claim is preserved on timeout, on a post-create persist failure and, after the micro-review (1816f5f), on a retryable 5xx; a transport failure with no status still releases. Approach-checked. |
| D69 | fixed | `c611a23` | The client phone control mirrors the server character set and the 9-to-15-digit rule on both forms. Test: a digit-poor phone keeps Continue disabled. |
| D70 | fixed | `56320c0` | The vendor client caps buffered responses at 10 MB; the label PDF still streams. |
| D71 | fixed | `56320c0` | The transport backoff is the intended 1, 4 and 16 seconds through an explicit delay generator, because the library default is base 2. |
| D72 | deferred | — | The shipped-timestamp backfill is moot before deployment: no legacy Shipped orders exist and the admin transition stamps it going forward. See Decisions. |
| D73 | fixed | `aa995c1` | The retry sweep logs the order id on each re-enqueue, so a single order's attempts can be followed. |
| D74 | fixed | `c611a23` | The prefill reads the guest session through the shared accessor instead of an inline local-storage parse. |
| D75 | fixed | `56320c0` | One status-check chokepoint in the client, sharing the policy helper's classification, so the four ladders cannot drift. |
| D76 | fixed | `16d065b` | A two-order tick test exercises the parallel per-order scope fan-out. |
| D77 | fixed | `aa995c1` | The retry-sweep tests moved to SQLite, with fresh-claim skip and stale-claim re-drive cases added. |
| D78 | fixed | `c611a23` | Tests: selecting a locker preserves the Easybox contact, and the review step renders no address line for an Easybox order. |
| D79 | fixed | `c611a23` | The token service adds a name claim and the auth service populates the current-user stream from it on login and on session restore, so the signed-in prefill is live rather than dead code. |
| D80 | fixed | `c611a23` | A transient locker-search error sets a distinct error signal, reset per fetch, with a retry, instead of showing "no easybox here". Test covers error then recovery. Approach-checked. |
| D81 | deferred | — | Service-id validation is the parked pre-enable configuration task; the feature is dormant, so no boot-time guard was added now. See Decisions. |
| D82 | fixed | `aa995c1` | The dispatcher re-enqueue is unit-testable through a pure delay computation and a clock-based delayed re-enqueue; tests assert the schedule, the lifetime floor and the attempt increment. |
| D83 | wont-fix | — | The bundled locker-map behaviour is intentional and is now covered by tests from this round; a retrospective story adds nothing. See Decisions. |
| D84 | fixed | `c75003d` | The creator loads the order untracked, since all writes go through direct updates. |
| D85 | fixed | `16d065b` | The tracking poll loads the order untracked. |
| D86 | fixed | `3764fa0` | Recipient name and phone rules extracted into one shared method used by both blocks; the pattern hoisted to a constant. |
| D87 | fixed | `aa995c1` | The sweep's outside-window floor is derived from the registry entry lifetime, so the deduplication window and the query window cannot drift. |
| D88 | false-positive | — | Refuted at the certification pass: Npgsql maps any-offset values to the UTC instant, so there is no timestamp write bug. No fix. |
| D89 | wont-fix | — | The label-fetch method is not dead: it is the authenticated path kept for a pre-enable admin label-proxy endpoint. See Decisions. |
| D20 | fixed | `56320c0` | A separate requests-per-second setting decouples the transport rate from the concurrency gate, defaulting to it so behaviour is preserved. |
| D24 | fixed | `56320c0` | The client no longer fabricates a wall-clock observation time; the snapshot value is nullable and the job supplies its poll clock for the delivered timestamp. Test updated. |
| D25 | fixed | `56320c0` | The token expiry is normalized to UTC. |
| D27 | fixed | `16d065b` | The non-delivered sync write is guarded on Status = Shipped and a forward stamp, so it is monotonic and never touches a delivered row. |
| D29 | fixed | `56320c0` | Retries log through a retry callback carrying attempt, delay and outcome. |
| D30 | fixed | `56320c0` | Resolved the other way: the design document was corrected, because the generic health writer never delivered the documented Sameday field; the field itself was dropped as out of scope. |
| D33 | false-positive | — | Obsolete: the tick loads ids only and each poll loads its order once on its own scope, which the parallel design requires. The unused variable the finding cites does not exist. See Decisions. |
| D35 | fixed | `c611a23` | The locker list is primed lazily through the search stream only while Easybox is active, so a courier-only customer triggers no fetch. Test asserts no fetch at init for courier. |
| D37 | fixed | `56320c0` | The unreferenced redaction helper removed. |
| D38 | fixed | `16d065b` | The give-up and tracking-stop registries share one base type. |
| D39 | fixed | `fd59bf2` | The shipping service injects the static shipping service, registered scoped, instead of constructing it, and drops the two constructor parameters. |
| D40 | deferred | — | Pre-existing bolt-035 model and designer drift on a secret's length. The current snapshot and every later designer carry 512; only the two June designers hold 255. See Decisions. |
| D50 | fixed | `aa995c1` | The dispatcher orchestration is unit-tested through the pure delay computation and the delayed re-enqueue, so the deferred coverage gap is closed. |

## Scope

| Cluster | Findings | Files | Approach-check |
|---|---|---|---|
| A — Validation rules and shared rule extraction (`3764fa0`) | D55, D61, D86 | `Validators/Payments/CreateOrderRequestValidator.cs` | not needed (rules only) |
| B — Claim lifecycle, label-link clamp, creator tests (`c75003d`) | D57, D58, D60, D68, D84 | `Services/Sameday/AwbCreator.cs`, `Migrations/`, `Tests/…/AwbCreatorTests.cs` | run before implementation — sound-with-changes, folded in |
| C — Client hardening and transport behaviour (`56320c0`) | D62, D66, D70, D71, D75, D20, D24, D25, D29, D30, D37 | `Services/Sameday/SamedayClient.cs`, `SamedayPolicies.cs` | not needed (classification, limits and log content) |
| D — Tracking poll clock, outage dedup, untracked reads (`16d065b`) | D63, D67, D76, D85, D27, D38 | `BackgroundJobs/ShipmentTrackingJob.cs`, registries | run before implementation — sound-with-changes, folded in |
| E — Checkout state, prefill, locker search (`c611a23`) | D59, D64, D69, D74, D78, D79, D80, D35 | `UI/…/delivery-step.ts`, `checkout-state.service.ts`, specs | run before implementation — sound-with-changes, folded in |
| F — Enabled-root wiring and container registration (`fd59bf2`) | D65, D39 | `Program.cs`, `Extensions/SamedayServiceCollectionExtensions.cs` | run before implementation — confirmed a real resolution cycle |
| G — Dispatcher and retry-sweep testability (`aa995c1`) | D73, D77, D82, D87, D50 | `BackgroundJobs/AwbDispatcher.cs`, `AwbRetryJob.cs` | not needed (extracting pure functions to test) |
| H — Admin label surface (`66c6d50`) | D56 | `DTOs/Admin/AdminOrderDtos.cs` | not needed (a projection and a DTO) |
| I — Micro-review follow-up: preserve the claim on a retryable 5xx (`1816f5f`) | D68 | `Services/Sameday/AwbCreator.cs` | covered by cluster B's check |
| J — Not fixed | D72, D81, D40, D83, D89, D88, D33 | — | not needed (no code changed) |

## Decisions

### The shipped-timestamp backfill is deferred (D72)

No deployed data exists, so there are no legacy Shipped orders with an empty shipped timestamp, and
the admin transition stamps it going forward. A one-time backfill belongs in the deploy runbook if
the application ever ships with pre-integration Shipped orders. Adding a data migration now, for
zero current rows, is churn.

### Service-id validation is deferred to the pre-enable step (D81, D66)

The real Sameday service ids are a parked pre-enable configuration task by owner decision. The
feature is dormant, so no boot-time guard was added now; configuring the real ids before flipping
the flag is the existing pre-enable step. The same reasoning covers D66's locker-table freshness:
this round improved the give-up log so a stale locker is diagnosable, and keeping the locker table
in step with the vendor stays an operational task, not code.

### The secret-length drift stays deferred (D40)

Pre-existing bolt-035 drift, not Sameday. The current model snapshot and every migration designer
after June carry 512; only the two June designers hold the historical 255, and those are immutable
history. Stripe client secrets are about 66 characters, so any gap between database and model is
harmless. Aligning the actual Postgres column is a bolt-035 groom item.

### Two cleanup rows ruled wont-fix (D83, D89)

D83: the locker-map behaviours that rode into the diff — prime on init, clear restores the list,
survive a search error — are intentional and are now covered by the delivery-step specs this round
added, so a retrospective story adds no value. D89: the label-fetch method is not dead code. It is
the authenticated way to fetch the label by AWB number, kept for a pre-enable admin label-proxy
endpoint, because the raw vendor link that D56 now surfaces may need the bearer token to open.

### Two rows ruled false-positive (D33, D88)

D33: the tick selects ids only and untracked, then each poll loads its own order on its own scoped
context, which the parallel fan-out requires rather than a wasteful re-query; the unused variable
the finding cites does not exist in the current code. D88 was refuted during the certification pass
itself: Npgsql maps any-offset values to the UTC instant.

### The vendor-idempotency residual is unchanged (D45)

Not in this round's scope and not touched by it. Avoiding a double courier charge after a crash
between the vendor call and the persist still rests on Sameday deduplicating on the reference we
send. Confirm that with the vendor before enabling the jobs, per ADR-015.
