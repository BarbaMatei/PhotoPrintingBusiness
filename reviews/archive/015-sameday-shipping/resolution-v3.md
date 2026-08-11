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

| ID | Status | Commit | Note |
|---|---|---|---|
| PPW-282 | fixed | `aada94b` | The gate reads a mirrored form-validity signal, so typing the contact re-enables Continue. The same fix was applied to the courier form's latent version of the bug. |
| PPW-283 | fixed | `18e7815` | The tracking loop's catch is gated on the stopping token, with a per-poll cancellation catch; the retry job was gated too as a class sweep (5fc330b). A slow vendor response no longer kills delivery detection. |
| PPW-284 | fixed | `2c434ad` | A durable per-order claim is taken before the vendor call, approach-checked. This closes the concurrent double call. The crash-window residual is accepted and alerted — see Decisions and ADR-015. |
| PPW-285 | fixed | `aada94b` | The Easybox branch of the completeness check now requires the recipient contact, so the stepper cannot skip to a payment 400. |
| PPW-286 | fixed | `2c434ad` | A vendor-call timeout becomes a transient retry in the creator; the dispatcher and the retry job shutdown catches were gated (5fc330b). |
| PPW-287 | fixed | `18e7815` | The sync timestamp is the poll clock, which always moves forward, and the monotonic guard is gone, so a later delivered snapshot with an earlier vendor timestamp is no longer dropped. |
| PPW-288 | fixed | `19cd0b8` | The EuPlatesc webhook's label enqueue is now asserted, mirroring the Stripe test. |
| PPW-289 | deferred | — | The dispatcher's outcome routing and re-enqueue got no runtime test: a faithful one needs a background-service harness with an injected delay. The backoff decision is already a tested pure function. Open coverage gap. |
| PPW-290 | fixed | `2c434ad` | A test pins the persist guard, so an order that moves Paid to Printing mid-call keeps its label. |
| PPW-291 | fixed | `6606c25` | A rate-limit response surviving the retries becomes the transient unreachable exception, not a permanent give-up, which matches the exception's own documentation. |
| PPW-293 | fixed | `2c434ad` | The genuine-orphan branch is raised to Error so operators can alert on it; the client has no vendor void endpoint. |
| PPW-270 | fixed | `f3d2508` | Re-opened and raised to Medium by this pass. The shipping endpoint now reports automatic creation instead of telling the admin to create a label by hand, which risked a double booking. |
| PPW-292 | fixed | `ce4941a` | ADR-015 amended: it names the field the code actually sends, and records the claim-and-guard mechanism plus the honest crash-window residual. |
| PPW-260 | fixed | `6606c25` | Backlog row fixed while in the same file — the vendor rejection body no longer rides on the exception message, so it stays out of Error logs. |
| PPW-261 | fixed | `2c434ad` | Backlog row fixed while in the same file — the label-link migration ships a 500-character column on Postgres, not unbounded text. |
| PPW-263 | fixed | `18e7815` | Backlog row fixed as part of the PPW-287 poll-clock change. |
| PPW-265 | fixed | `18e7815` | Backlog row fixed as part of PPW-287 — the monotonic guard was removed. |
| PPW-267 | fixed | `6606c25` | Backlog row fixed while here — the enqueue log moved from Debug to Information, above the configured floor. |
| PPW-275 | fixed | `f3d2508` | Backlog row fixed — the dead outcome union deleted. |
| PPW-280 | fixed | `f3d2508` | Backlog row fixed — the per-print gram literal named. |
| PPW-262 | deferred | — | Backlog re-raise: the Postgres migrate-and-update container test is still not added. Dual-database parity belongs to the owner's three-environment stage. |

## Scope

| Cluster | Findings | Files | Approach-check |
|---|---|---|---|
| A — Easybox Continue signal and stepper gate (`aada94b`) | PPW-282, PPW-285 | `UI/…/delivery-step.ts`, `UI/…/checkout-state.service.ts` | not needed (a reactivity fix, no new mechanism) |
| B — Tracking timeout survival, poll clock, monotonic guard dropped (`18e7815`) | PPW-283, PPW-287, PPW-263, PPW-265 | `BackgroundJobs/ShipmentTrackingJob.cs` | not needed (removes a guard, adds no mechanism) |
| C — Durable per-order AWB claim (`2c434ad`) | PPW-284, PPW-286, PPW-290, PPW-293, PPW-261 | `Services/Sameday/AwbCreator.cs`, `Migrations/` | run before implementation — sound-with-changes, all six required changes folded in |
| D — Transient 429, vendor body off the logs, enqueue log level (`6606c25`) | PPW-291, PPW-260, PPW-267 | `Services/Sameday/SamedayClient.cs`, `AwbCreationNotifier.cs` | not needed (classification and log levels) |
| E — Manual-label endpoint and cleanups (`f3d2508`) | PPW-270, PPW-275, PPW-280 | `Services/SamedayShippingService.cs`, `Services/Sameday/ParcelWeight.cs` | not needed (no new mechanism) |
| F — EuPlatesc enqueue test (`19cd0b8`) | PPW-288 | `Tests/…/PaymentControllerIntegrationTests.cs` | not needed (test only) |
| G — ADR-015 amendment (`ce4941a`) | PPW-292 | `memory-bank/…/adr-015-*.md` | not needed (document only) |
| H — Retry-job cancellation gate, the micro-review's class sweep (`5fc330b`) | PPW-283, PPW-286 | `BackgroundJobs/AwbRetryJob.cs` | covered by cluster C's check |
| I — Left deferred | PPW-289, PPW-262 | — | not needed (no code changed) |

## Decisions

### The crash-window residual on the durable claim is accepted and alerted

The claim closes the concurrent double vendor call. A worker that bills a label and then dies before
persisting is reclaimed after the claim lifetime and re-creates; whether that mints a second billable
label still rests on Sameday deduplicating on the reference we send, which nobody has confirmed.
There is no client-side close without a lookup-by-reference call, which the client does not have. So
the residual is accepted, alerted with an Error log on the orphan, and documented in ADR-015.
Confirm Sameday's create-idempotency before turning the jobs on.

### The dispatcher runtime test was not written

The dispatcher starts its re-enqueue as fire-and-forget work, so a faithful test needs a
background-service harness with an injected delay. The parts that carry the decision are already
covered: the backoff choice is a pure tested function and the outcome handler is a plain switch.
Recorded as an open coverage gap for the verifier rather than a fake test that would pass whatever
the code did.

### Which backlog rows rode along, and which did not (PPW-259, PPW-262, PPW-264, PPW-266, PPW-268, PPW-269, PPW-272, PPW-274, PPW-276–PPW-279)

Eight backlog rows were folded in because they sat in files this round was already changing: PPW-260,
PPW-261, PPW-263, PPW-265, PPW-267, PPW-270, PPW-275, PPW-280. The rest stay deferred, either low value or off the round's
clusters. PPW-269, PPW-274 and PPW-278 were considered and dropped to bound the round; PPW-274 in particular carries
a regression risk against the PPW-258 fix, which shares the same stream.
