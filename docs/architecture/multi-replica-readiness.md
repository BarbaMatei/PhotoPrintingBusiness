# Multi-Replica Readiness

**Where we are**: one API instance. Every decision below was taken deliberately with that
topology in mind, and each one is cheap and correct at one replica.

**What this document is for**: so that "can we run two instances?" is answerable without
reading five ADRs and grepping the codebase. It states, per concern, what the code does today
and what would change if the distributed-state work (bolt 046) were ever built.

**What this document is not**: a plan to build Redis. Bolt 046 is **deprioritized** and stays
that way until the application is actually deployed and there is real scaling pressure. Nothing
here is a commitment. If you are reading this because you want to scale, read "What a second
replica would actually take" at the bottom first — the five decided concerns are not the thing
standing in your way.

---

## Before the five concerns: three things stop a second replica outright

The first two are absent from every ADR, because both predate the question. The third has an
ADR that decided the opposite trade-off on purpose.

### The schema is migrated at process boot

`Program.cs` calls `db.Database.Migrate()` unconditionally during startup. EF Core takes no
lock around the migration chain, so two instances starting together both try to apply it. One
wins; the other fails on objects that already exist and crash-loops.

- **Today**: nothing coordinates this. A rolling deploy that starts one instance at a time is
  the only shape that is safe, and even then a fresh migration is applied by whichever instance
  boots first while the other is still serving the old schema.
- **If bolt 046 lands**: a startup lock (advisory lock or leader election) around the migration
  call, or migrations moved out of boot into a deploy step. The second is the better answer and
  does not need Redis.

### Fourteen hosted services, and only two of them claim their work

Fourteen services run in every instance — thirteen registered with `AddHostedService`, plus
`ScrapeListenerGuard` registered as an `IHostedService` singleton directly. Running everywhere
is correct for a queue consumer and wrong for a sweeper. Only two take a durable per-row claim
*before* acting; one more guards only its write; the rest select rows and act on them.

| Service | Trigger | Claims its rows? | What two instances do |
|---|---|---|---|
| `AwbCreator` (via `AwbDispatcher`) | channel | **yes** — `Orders.AwbClaimedAt` lease + TTL, taken before the vendor call | safe apart from the crash window; see concern 3 |
| `InvoiceUploadJob` | 30-min poll | **partly** — `Invoices.ClaimedAt` on the upload path only | see concern 5 |
| `ShipmentTrackingJob` | timer | no claim — compare-and-swap at the write only | both poll the courier for the same parcel, then exactly one transition lands; duplicated vendor calls, no duplicated side effect. See concern 4 |
| `EmailRetryJob` | 10-s poll | no | both select the same `Pending` row and both send it — **the customer gets the email twice** |
| `AccountDeletionJob` | daily | no | both select the same due users and both `RemoveRange`; the loser's `SaveChanges` deletes zero rows and throws, rolling back its whole batch — a GDPR erasure run that reports failure |
| `UploadCleanupJob` | hourly | no | both delete the same expired uploads; the second one's blob delete finds nothing and the row delete conflicts |
| `GuestSessionCleanupJob` | hourly | no | same shape, lower stakes |
| `ArchiveRetentionJob` | every 6 h | no | both delete the same past-retention previews from cloud storage |
| `OriginalPurgeRecoveryScanner` | boot + every 6 h | no | both purge the same originals |
| `PromotionRecoveryScanner` | boot + every 6 h | no | both re-enqueue the same orders into their own local queues, every sweep; see concern 1 |
| `OrderPhotoPromotionWorker` | channel | n/a — consumes its own instance's queue | promotes the same order twice; per-upload `StorageLocation == Cloud` checks absorb it |
| `AwbRetryJob` | every 60 min | no, but the creator it feeds does | safe by the claim downstream |
| `S3BucketVerifier` | boot only | n/a — read-only probe | harmless |
| `ScrapeListenerGuard` | boot only | n/a | harmless |

- **Today**: correct at one instance. The two rows worth remembering are `EmailRetryJob`
  (a customer-visible duplicate) and `AccountDeletionJob` (a failing erasure run).
- **If bolt 046 lands**: leader election so sweepers run in one place, or the same per-row
  claim the AWB and ANAF paths already use. The claim pattern is the cheaper answer and needs
  no new infrastructure — it is a column and a guarded `ExecuteUpdate`.

### Local disk is storage tier 1

**ADR-008/011** ([two-tier storage](../../memory-bank/bolts/043-cloud-storage-provider/adr-008-two-tier-storage-with-storage-location.md))

Every new upload starts at `StorageLocation = Local`, on the disk of the instance that received
it, and only moves to cloud storage after payment. `IStorageRouter` routes every read, write and
delete by that column — so a `Local` upload can only be served by the instance holding the
bytes.

- **Today**: an upload written to instance A cannot be read by instance B. Uploading, previewing
  and editing a basket all break under a load balancer that does not pin a visitor to one
  instance, and the whole pre-payment phase is the part of the product with the most traffic.
  ADR-008 took this trade deliberately: it buys GDPR data minimisation and a simpler
  promote-on-payment lifecycle, and it says in as many words that it trades multi-replica
  scaling for the pre-payment phase.
- **If bolt 046 lands**: nothing changes here. Redis does not move bytes. The answer is either
  shared storage mounted at tier 1, or putting every tier behind the cloud adapter and losing
  the local-first lifecycle. **This is the largest single blocker and the one no queue work
  addresses.**

---

## The five decided concerns

### 1. Promotion queue — in-process channel

**ADR-010** ([in-process `Channel<T>` + startup recovery scan](../../memory-bank/bolts/051-order-photo-promotion/adr-010-in-process-promotion-queue.md))

- **Today**: `PromotionQueue` wraps an unbounded `Channel<PromotionJob>` with
  `SingleReader = true`, written by the payment webhook and read by
  `OrderPhotoPromotionWorker`. Durability is not in the channel: `PromotionRecoveryScanner`
  rebuilds the work from `Upload.StorageLocation == Local` on paid orders — once at boot and
  then every 6 hours, because boot-only proved insufficient on an always-on server. Per-upload
  promotion is idempotent, so a repeat is absorbed rather than duplicated.
- **On a second instance**: the queue itself is not the hazard — the recovery scanner is. Both
  instances sweep the same rows and enqueue them into their own channels, and because the sweep
  repeats every 6 hours this is a standing duplication, not a once-per-boot one. The idempotency
  check makes it wasteful rather than wrong. Two ceilings also stop meaning what they say:
  `MaxConcurrentOrders` and the parked-retry cap of 100 are per process, so the real limits are
  N times the configured ones.
- **If bolt 046 lands**: a shared queue (Redis Streams behind the same `IPromotionQueue`
  interface) makes the recovery scan a single-owner operation and restores the ceilings. ADR-010
  names this as the revisit point.

### 2. Vendor token caches — in-process, one per instance

**ADR-013** ([in-process singleton token cache](../../memory-bank/bolts/036-sameday-api-client/adr-013-in-process-sameday-token-cache.md))

- **Today**: `SamedayTokenProvider` is a singleton holding one token in a field, with a
  `SemaphoreSlim(1,1)` serialising first fetches and a 60-second safety window before expiry.
  There is a **second cache of the same shape that ADR-013 does not cover**:
  `AnafTokenProvider`, also a singleton, also a gated in-process token.
- **On a second instance**: each instance authenticates independently, and both providers use a
  plain client-credentials grant with no rotating refresh token, so the cost is exactly N logins
  per token cycle rather than one. For Sameday that sits comfortably inside the 5 req/s ceiling,
  which is the cost ADR-013 accepted; for ANAF the same arithmetic applies against whatever the
  SPV quota turns out to be. Credential rotation becomes a fleet-wide operation: both providers
  expose an `Invalidate()` method, but nothing calls it from outside, so in practice evicting a
  cached token means restarting every instance holding one.
- **If bolt 046 lands**: a shared cache makes it one authentication per deployment and turns
  rotation into a single eviction. ADR-013 expects to be superseded at that point;
  `AnafTokenProvider` needs a decision record of its own either way, since it is a second
  instance of a pattern only one ADR covers.

### 3. AWB duplicate-create — a durable per-order lease

**ADR-015** ([accept duplicate `CreateAwb`](../../memory-bank/bolts/037-awb-and-tracking-jobs/adr-015-accept-duplicate-awb-create-on-multi-replica.md)) — **read the amendment at the top, not the original decision.**

- **Today**: three defences, in order. The vendor idempotency key is
  `clientInternalReference = Order.OrderNumber`. Before calling Sameday, `AwbCreator` claims the
  order atomically by setting `Orders.AwbClaimedAt` under a TTL predicate, so a concurrent
  creator — a retry re-enqueue, a second instance, a duplicate webhook — finds a fresh claim and
  skips **before** a second label is billed. The persist is a guarded `ExecuteUpdate`
  (`AwbNumber IS NULL AND Status != Cancelled`), and a zero-affected result is read back to tell
  benign vendor convergence from a genuine orphan.
- **On a second instance**: the concurrent double-call is closed by the lease. The **crash
  window is not**: an instance that bills a label and dies before persisting has its claim
  reclaimed after the TTL, and whether the re-creation mints a second billable label rests
  entirely on Sameday deduplicating on `clientInternalReference` — which is **unverified**.
  Verify that with the vendor before enabling the jobs in production.
- **If bolt 046 lands**: a distributed lock shrinks the window but does not close the crash
  case either. The real closure is a vendor "AWB by reference" lookup, which the client does not
  implement.

### 4. Order status transitions — database compare-and-swap

**ADR-016** ([CAS via `ExecuteUpdateAsync`](../../memory-bank/bolts/037-awb-and-tracking-jobs/adr-016-cas-execute-update-for-multi-replica-status-transitions.md))

- **Today**: any background worker moving `Order.Status` issues a single
  `UPDATE … WHERE Id = … AND Status = <expected source>` and treats the affected-row count as
  the outcome. `affected == 0` means someone else — another instance, or an admin cancelling —
  already moved the row, and the caller must then skip the side effects bound to that
  transition (no email, no event). No `RowVersion` column exists anywhere.
- **On a second instance**: this one is genuinely safe by construction. Both instances observe
  delivery, both attempt the transition, exactly one succeeds, the loser logs a race-lost line
  at Info and sends no second email. Race-lost lines are healthy, not a warning sign.
- **If bolt 046 lands**: CAS stays valid and is not replaced. A distributed lock only becomes
  preferable where ordering or fairness matters, which status transitions do not need.

### 5. ANAF dispatch — database polling with a partial lease

**ADR-023** ([DB polling, not an in-process channel](../../memory-bank/bolts/039-efactura-anaf/adr-023-worker-dispatch-db-polling-not-in-process-channel.md))

- **Today**: `InvoiceUploadJob` polls `Invoices` on a `PeriodicTimer` (default 30 minutes,
  validator floor 1 minute) and acts on what it finds. There is no in-process queue and no
  recovery scanner — the query at each tick *is* the recovery. Multi-replica safety comes from
  a per-row claim: `Invoices.ClaimedAt` with a TTL, taken before the upload and released after.
  **Note that ADR-023's own summary credits compare-and-swap for this; the claim column is what
  actually does the work**, and it covers the `Pending` upload path only. The status-poll slice
  (`Submitted`) and the retry slice (`Rejected`) are selected by the batch queries without a
  claim and rely on the handling being idempotent instead.
- **On a second instance**: the upload path is protected. Two instances polling the same
  `Submitted` invoice will both poll ANAF for its status — extra vendor calls, and the
  attempt-count reasoning that derives a retry budget from timestamps sees the row touched more
  often than one worker would touch it.
- **If bolt 046 lands**: leader election would let one instance own the timer, and would make
  an in-process channel viable again. Polling survives without it; the claim is why.

---

## Instance-local state with no decision record

Everything below is per-process by construction. None of it has an ADR, and none of it is wrong
at one instance. The local-disk storage tier belongs to this category too, but it is severe
enough to sit with the blockers above.

| What | Where | Symptom on a second instance |
|---|---|---|
| SignalR has no backplane | `AddSignalR()` with no Redis backplane; `IHubContext<AdminOrderHub>` | an admin connected to instance B never sees an order that arrived on instance A. Silent — no error anywhere |
| Inbound rate limits are per process | `SecurityExtensions` global partitioned limiter; `AuthExtensions` fixed-window policies | the effective public budget is N × the configured one. "100 requests a minute" becomes 200 at two instances |
| Outbound vendor rate limits are per process | `SamedayPolicies` sliding-window limiter; `AwbDispatcher` and `ShipmentTrackingJob` each hold their own `SemaphoreSlim(MaxConcurrentSamedayCalls)` | N instances send N × the configured rate at the courier's quota, and get 429s the configuration says are impossible |
| Image decode concurrency cap | `ImageDecodeLimiter`, a singleton `SemaphoreSlim` sized from CPU and RAM | the cap multiplies by instance count. Two instances co-located on one host can exhaust the host's memory while each honours its own limit — this is an availability guard, not a convenience |
| Admin statistics cache | `AdminStatsService` over `IMemoryCache` | two admins see different numbers for the cache's lifetime depending on which instance answered |
| Log-once registries | `AnafOutageRegistry`, `AwbGiveUpRegistry`, `TrackingStopRegistry` over `MemoryCacheOnceRegistry` | N log lines instead of one. **Log volume only** — no decision or state is lost, so this is noise, not a defect |
| Metrics-endpoint denial dedup | `MetricsEndpointIpAllowListMiddleware` — a `ConcurrentDictionary` of seen denials with a logging cap | a single probe of `/metrics` produces up to N × the cap in log lines, so any alert threshold on it depends on instance count |
| AWB dispatch queue | `AwbJobQueue`, a second in-process `Channel<AwbJob>` | jobs do not shard; the claim in concern 3 is what keeps this safe |

## What is *not* a problem

Recorded so nobody re-investigates:

- **No ASP.NET Data Protection or antiforgery usage anywhere**, so there is no shared key ring
  to provision. JWTs are signed with an RS256 key supplied by configuration, which every
  instance reads identically.
- **No optimistic-concurrency tokens** by design — uniqueness constraints plus
  violation-detection are the mechanism, and those live in the database, so they work the same
  at any instance count.
- **Payment idempotency** is a globally-unique index on `Orders.IdempotencyKey`, not in-process
  state.
- **The log-once registries** lose nothing but log lines.

## What a second replica would actually take

In dependency order, and note that the first three need no Redis at all:

1. Get migrations out of process boot, or lock them.
2. Give the sweeping background services either leader election or the per-row claim the AWB
   and ANAF paths already demonstrate.
3. Share the upload storage — either put every tier behind the cloud adapter, or mount shared
   storage for tier 1.
4. Add a SignalR backplane, or accept that admin notifications are best-effort.
5. Move the rate limiters, both inbound and outbound, to shared counters — or divide the
   configured budgets by the instance count and document why.
6. Then, and only then, the five decided concerns above become worth revisiting.

Steps 1–3 are the work. Bolt 046 is about step 4 onward, and it remains deprioritized.
