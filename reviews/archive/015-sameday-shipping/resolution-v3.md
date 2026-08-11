---
type: resolution
target: 015-sameday-shipping
version: 3
answers: review-v3.md
status: resolved
fixed_commit: 5fc330b
closed: 2026-07-27
---

# Resolution v3 — 015-sameday-shipping

## Findings

| D# | Status | Commit | Note |
|---|---|---|---|
| D43 | fixed | `aada94b` | The gate reads a mirrored form-validity signal, so typing the contact re-enables Continue. The same fix was applied to the courier form's latent version of the bug. |
| D44 | fixed | `18e7815` | The tracking loop's catch is gated on the stopping token, with a per-poll cancellation catch; the retry job was gated too as a class sweep (5fc330b). A slow vendor response no longer kills delivery detection. |
| D45 | fixed | `2c434ad` | A durable per-order claim is taken before the vendor call, approach-checked. This closes the concurrent double call. The crash-window residual is accepted and alerted — see Decisions and ADR-015. |
| D46 | fixed | `aada94b` | The Easybox branch of the completeness check now requires the recipient contact, so the stepper cannot skip to a payment 400. |
| D47 | fixed | `2c434ad` | A vendor-call timeout becomes a transient retry in the creator; the dispatcher and the retry job shutdown catches were gated (5fc330b). |
| D48 | fixed | `18e7815` | The sync timestamp is the poll clock, which always moves forward, and the monotonic guard is gone, so a later delivered snapshot with an earlier vendor timestamp is no longer dropped. |
| D49 | fixed | `19cd0b8` | The EuPlatesc webhook's label enqueue is now asserted, mirroring the Stripe test. |
| D50 | deferred | — | The dispatcher's outcome routing and re-enqueue got no runtime test: a faithful one needs a background-service harness with an injected delay. The backoff decision is already a tested pure function. Open coverage gap. |
| D51 | fixed | `2c434ad` | A test pins the persist guard, so an order that moves Paid to Printing mid-call keeps its label. |
| D52 | fixed | `6606c25` | A rate-limit response surviving the retries becomes the transient unreachable exception, not a permanent give-up, which matches the exception's own documentation. |
| D54 | fixed | `2c434ad` | The genuine-orphan branch is raised to Error so operators can alert on it; the client has no vendor void endpoint. |
| D31 | fixed | `f3d2508` | Re-opened and raised to Medium by this pass. The shipping endpoint now reports automatic creation instead of telling the admin to create a label by hand, which risked a double booking. |
| D53 | fixed | `ce4941a` | ADR-015 amended: it names the field the code actually sends, and records the claim-and-guard mechanism plus the honest crash-window residual. |
| D21 | fixed | `6606c25` | Backlog row fixed while in the same file — the vendor rejection body no longer rides on the exception message, so it stays out of Error logs. |
| D22 | fixed | `2c434ad` | Backlog row fixed while in the same file — the label-link migration ships a 500-character column on Postgres, not unbounded text. |
| D24 | fixed | `18e7815` | Backlog row fixed as part of the D48 poll-clock change. |
| D26 | fixed | `18e7815` | Backlog row fixed as part of D48 — the monotonic guard was removed. |
| D28 | fixed | `6606c25` | Backlog row fixed while here — the enqueue log moved from Debug to Information, above the configured floor. |
| D36 | fixed | `f3d2508` | Backlog row fixed — the dead outcome union deleted. |
| D41 | fixed | `f3d2508` | Backlog row fixed — the per-print gram literal named. |
| D23 | deferred | — | Backlog re-raise: the Postgres migrate-and-update container test is still not added. Dual-database parity belongs to the owner's three-environment stage. |

## Scope

| Cluster | Findings | Files | Approach-check |
|---|---|---|---|
| A — Easybox Continue signal and stepper gate (`aada94b`) | D43, D46 | `UI/…/delivery-step.ts`, `UI/…/checkout-state.service.ts` | not needed (a reactivity fix, no new mechanism) |
| B — Tracking timeout survival, poll clock, monotonic guard dropped (`18e7815`) | D44, D48, D24, D26 | `BackgroundJobs/ShipmentTrackingJob.cs` | not needed (removes a guard, adds no mechanism) |
| C — Durable per-order AWB claim (`2c434ad`) | D45, D47, D51, D54, D22 | `Services/Sameday/AwbCreator.cs`, `Migrations/` | run before implementation — sound-with-changes, all six required changes folded in |
| D — Transient 429, vendor body off the logs, enqueue log level (`6606c25`) | D52, D21, D28 | `Services/Sameday/SamedayClient.cs`, `AwbCreationNotifier.cs` | not needed (classification and log levels) |
| E — Manual-label endpoint and cleanups (`f3d2508`) | D31, D36, D41 | `Services/SamedayShippingService.cs`, `Services/Sameday/ParcelWeight.cs` | not needed (no new mechanism) |
| F — EuPlatesc enqueue test (`19cd0b8`) | D49 | `Tests/…/PaymentControllerIntegrationTests.cs` | not needed (test only) |
| G — ADR-015 amendment (`ce4941a`) | D53 | `memory-bank/…/adr-015-*.md` | not needed (document only) |
| H — Retry-job cancellation gate, the micro-review's class sweep (`5fc330b`) | D44, D47 | `BackgroundJobs/AwbRetryJob.cs` | covered by cluster C's check |
| I — Left deferred | D50, D23 | — | not needed (no code changed) |

## Decisions

### The crash-window residual on the durable claim is accepted and alerted (D45)

The claim closes the concurrent double vendor call. A worker that bills a label and then dies before
persisting is reclaimed after the claim lifetime and re-creates; whether that mints a second billable
label still rests on Sameday deduplicating on the reference we send, which nobody has confirmed.
There is no client-side close without a lookup-by-reference call, which the client does not have. So
the residual is accepted, alerted with an Error log on the orphan, and documented in ADR-015.
Confirm Sameday's create-idempotency before turning the jobs on.

### The dispatcher runtime test was not written (D50)

The dispatcher starts its re-enqueue as fire-and-forget work, so a faithful test needs a
background-service harness with an injected delay. The parts that carry the decision are already
covered: the backoff choice is a pure tested function and the outcome handler is a plain switch.
Recorded as an open coverage gap for the verifier rather than a fake test that would pass whatever
the code did.

### Which backlog rows rode along, and which did not (D20, D23, D25, D27, D29, D30, D33, D35, D37–D40)

Eight backlog rows were folded in because they sat in files this round was already changing: D21,
D22, D24, D26, D28, D31, D36, D41. The rest stay deferred, either low value or off the round's
clusters. D30, D35 and D39 were considered and dropped to bound the round; D35 in particular carries
a regression risk against the D19 fix, which shares the same stream.
