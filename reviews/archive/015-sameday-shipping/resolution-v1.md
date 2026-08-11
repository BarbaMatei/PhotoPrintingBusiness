---
type: resolution
target: 015-sameday-shipping
version: 1
answers: review-v1.md
status: resolved
fixed_commit: 835e932
closed: 2026-07-27
---

# Resolution v1 — 015-sameday-shipping

## Findings

| D# | Status | Commit | Note |
|---|---|---|---|
| D1 | fixed | `edd49f7` | The vendor reference is the order number, per order, not the shop-wide pickup-point id; a wire test pins it. The design got an adversarial approach-check first. |
| D2 | fixed | `edd49f7` | Guarded update (AwbNumber IS NULL AND Status != Cancelled) so one writer wins the row; the per-order key from D1 covers the vendor side. The client has no void endpoint, so a genuine orphan is logged. |
| D3 | fixed | `d6744f1` | Scope and DbContext per order, opened after the semaphore gate, so live contexts never exceed the concurrency cap; tick reads are untracked projections. Approach-checked. |
| D4 | fixed | `835e932` | Easybox contact captured at checkout, prefilled from the account or the guest session, required on the server (e8d4b53) and re-checked in the mapper (edd49f7), so a blank recipient now fails locally. |
| D5 | fixed | `edd49f7` | The request carries the delivery-type service id and the locker's vendor id, and the client sends both. Service ids became per-merchant configuration — see Decisions. |
| D6 | fixed | `e8d4b53` | A recording notifier double plus a Stripe webhook enqueue test; deleting the notifier call now reddens the suite. |
| D7 | fixed | `d6744f1` | The test seeds a genuinely Shipped order and advances it out of Shipped mid-poll through a separate scope, so the guarded update runs and returns 0; removing the predicate reddens it. |
| D8 | fixed | `ef8d323` | The guard compared attempt against schedule length with the wrong operator; the delay is now a pure tested function, exhausted only past the last entry. |
| D9 | fixed | `1a240b7` | One sliding-window limiter per handler, surfaced by the pipeline builder and disposed. The queue limit stays unbounded — see Decisions. |
| D10 | fixed | `010c6dc` | The admin transition overwrites the AWB number and tracking link only when the field is non-empty; the test seeds a machine-created number and asserts it survives an omitted field. |
| D11 | fixed | `010c6dc` | The admin path to Paid stamps the paid timestamp and calls the notifier, which is a no-op while jobs are off. It still sends no confirmation email — see Decisions. |
| D12 | fixed | `edd49f7` | The write guard tests Status != Cancelled rather than == Paid, per the approach-check: an order that moved Paid to Printing mid-call keeps its label instead of being orphaned. |
| D13 | fixed | `e8d4b53` | Courier recipient name, phone, street, number, city, county and postal code get non-empty and maximum-length rules plus a phone format check; tests cover blank, overlong and malformed. |
| D14 | fixed | `d6744f1` | The unreachable case in the tracking poll now logs a warning with the order id before returning. |
| D15 | fixed | `edd49f7` | The billable AWB number is logged before the database write; a persist failure returns a transient retry and is tested by dropping the table under it. |
| D16 | fixed | `edd49f7` | The creator tests moved to SQLite and the happy-path read-back goes through a fresh context, so a missing persist reddens the test. |
| D17 | fixed | `010c6dc` | Tests assert the shipped timestamp on the Shipped transition and the delivered timestamp on the Delivered one. |
| D18 | fixed | `835e932` | Clearing the city search no longer tears down the stream: the inner fetch is wrapped so its error cannot reach the outer subscription. |
| D19 | fixed | `835e932` | The init prime is now an immediate but cancellable first value inside the same stream, replacing the rival subscription that could overwrite a filtered result. |
| D32 | fixed | `edd49f7` | Folded into D4 — the mapper rejects a blank recipient name or phone with an argument exception, which becomes a give-up. |
| D34 | fixed | `1a240b7` | Folded into D9 — a finite-limit test exercises the production limiter branch every earlier test skipped. |
| D20 | deferred | — | Ledger backlog (Low) — not in this round. |
| D21 | deferred | — | Ledger backlog (Low) — not in this round. |
| D22 | deferred | — | Ledger backlog (Low, dual-database parity) — not in this round. |
| D23 | deferred | — | Ledger backlog (Low, dual-database parity) — not in this round. |
| D24 | deferred | — | Ledger backlog (Low) — not in this round. |
| D25 | deferred | — | Ledger backlog (Low) — not in this round. |
| D26 | deferred | — | Ledger backlog (Low) — not in this round. |
| D27 | deferred | — | Ledger backlog (Low, one leg refuted) — not in this round. |
| D28 | deferred | — | Ledger backlog (Low) — not in this round. |
| D29 | deferred | — | Ledger backlog (Low) — not in this round. |
| D30 | deferred | — | Ledger backlog (Low) — not in this round. |
| D31 | deferred | — | Ledger backlog (Low) — not in this round. |
| D33 | deferred | — | Ledger backlog (Low) — the tracking-job reload disappeared as a side effect of the D3 fix, but the finding itself stays at backlog. |
| D35 | deferred | — | Ledger backlog (Low) — not in this round. |
| D36 | deferred | — | Ledger backlog (Cleanup) — not in this round. |
| D37 | deferred | — | Ledger backlog (Cleanup) — not in this round. |
| D38 | deferred | — | Ledger backlog (Cleanup) — not in this round. |
| D39 | deferred | — | Ledger backlog (Cleanup) — not in this round. |
| D40 | deferred | — | Ledger backlog (Cleanup) — not in this round. |
| D41 | deferred | — | Ledger backlog (Cleanup) — not in this round. |
| D42 | false-positive | — | Refuted at the review: the JSON content re-serializes on every attempt, so there is no exhausted stream. The residual test gap folded into D34. |

## Scope

| Cluster | Findings | Files | Approach-check |
|---|---|---|---|
| A — AWB idempotency, guarded write, recipient and service mapping (`edd49f7`) | D1, D2, D4, D5, D12, D15, D16, D32 | `Services/Sameday/SamedayClient.cs`, `AwbCreator.cs`, `OrderToAwbRequestMapper.cs`, `AwbCreationRequest.cs` | run before implementation — sound-with-changes, all changes folded in |
| B — Tracking poll scope, swallowed-outage log, real CAS test (`d6744f1`) | D3, D7, D14 | `BackgroundJobs/ShipmentTrackingJob.cs`, `Tests/…/ShipmentTrackingJobTests.cs` | run before implementation — sound-with-changes, all changes folded in |
| C — Rate limiter built once and disposed (`1a240b7`) | D9, D34 | `Services/Sameday/SamedayPolicies.cs` | covered by cluster B's check |
| D — Dispatcher backoff boundary (`ef8d323`) | D8 | `BackgroundJobs/AwbDispatcher.cs` | not needed (an off-by-one closed by extracting a pure function) |
| E — Admin Paid enqueue, preserved AWB, timestamp tests (`010c6dc`) | D10, D11, D17 | `Services/AdminOrderService.cs` | not needed (no new mechanism) |
| F — Server-side recipient rules and the webhook enqueue test (`e8d4b53`) | D6, D13 | `Validators/Payments/CreateOrderRequestValidator.cs`, `Tests/…/PaymentControllerIntegrationTests.cs` | not needed (validation rules and one test double) |
| G — Easybox contact capture and resilient locker search (`835e932`) | D18, D19 | `UI/…/delivery-step.ts` | not needed (no new mechanism) |
| H — Left at backlog, plus the one refuted row | D20–D31, D33, D35–D42 | — | not needed (no code changed) |

## Decisions

### Service ids became per-merchant configuration (D5)

The Sameday service codes are vendor contract data, not universal constants, so they are now two
settings with a documented default of 7. The code differentiates by delivery type and sends the
locker's own vendor id, which was the concretely fixable defect; the real numbers are not in the
repository. The owner must set the courier and locker service ids from the Sameday contract before
enabling the jobs. Recorded as a pre-enable task rather than a code residual.

### The limiter's queue limit stays unbounded (D9)

The jobs are already capped by the semaphore, using the same number as the permit limit, so queue
depth is naturally bounded. The request path delegates locker and cost lookups to the static
fallback and does not route through this limiter today. If a future request-path caller does, a
finite queue limit that rejects fast should be reconsidered, so a checkout call cannot queue until
the client timeout.

### Admin-paid orders still send no confirmation email (D11)

Spotted during the fix-diff micro-review, outside every finding. The webhook path to Paid fires the
order-confirmation email and enqueues photo promotion. The admin path now stamps the paid timestamp
and enqueues the label but does neither of those two. Photo promotion self-heals through the
recovery sweep; the confirmation email has no such backstop, so an order reconciled to Paid by an
admin never sends one. Left as a conscious decision for the re-reviewer and the owner: it was
outside this finding's scope, and an admin may prefer to send that message by hand. If wanted, it
is a one-line call in the admin service's Paid branch.
