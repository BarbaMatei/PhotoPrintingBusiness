---
stage: design
bolt: 037-awb-and-tracking-jobs
created: 2026-06-02T16:35:00Z
---

# Stage 2 — Technical Design: AWB & Tracking Jobs

## Architecture Pattern

**Pattern**: In-process `Channel<AwbJob>` event-driven dispatcher + two
`PeriodicTimer`-based `BackgroundService` jobs. Identical shape to bolt
051's photo-promotion lifecycle. The order-table is the durable
source-of-truth; the channel is a latency optimization, the retry job
is its crash-safety net.

```text
┌─────────────────────────────────────────────────────────────────┐
│  PAYMENT WEBHOOK REQUEST (one replica)                          │
│   └→ OrderStatusMachine.AfterTransitionAsync(Paid)              │
│        └→ IAwbJobQueue.EnqueueAsync(AwbJob(orderId, 1, now))    │
│             [returns immediately; controller responds 200]      │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼  (Channel<AwbJob>, unbounded)
┌─────────────────────────────────────────────────────────────────┐
│  AwbDispatcher : BackgroundService (1 per host)                 │
│   └→ DequeueAllAsync foreach job                                │
│        └→ SemaphoreSlim.WaitAsync (cap=5 concurrent)            │
│             └→ IAwbCreator.CreateForOrderAsync(orderId)         │
│                  └→ outcome handling (re-enqueue or done)       │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│  IAwbCreator (Scoped)                                           │
│   ├→ DB: SELECT order WHERE Id=…  (status + AwbNumber check)    │
│   ├→ Map: OrderToAwbRequestMapper.ToRequest(order, settings)    │
│   ├→ HTTP: ISamedayClient.CreateAwbAsync(request, ct)           │
│   └→ DB: UPDATE Orders SET AwbNumber=…, AwbLabelUrl=…           │
└─────────────────────────────────────────────────────────────────┘

╔═════════════════════════════════════════════════════════════════╗
║  Safety net + tracking — independent of the channel             ║
╚═════════════════════════════════════════════════════════════════╝

┌─────────────────────────────────────────────────────────────────┐
│  AwbRetryJob : BackgroundService                                │
│   PeriodicTimer(60 min) ▶                                       │
│   SELECT Orders WHERE Status=Paid AND AwbNumber IS NULL AND     │
│                       PaidAt > now - 24h                        │
│   → IAwbJobQueue.EnqueueAsync(…)  (re-uses the same dispatcher) │
│                                                                 │
│   PaidAt < now - 24h → log AwbCreationGivenUp once, skip.       │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│  ShipmentTrackingJob : BackgroundService                        │
│   PeriodicTimer(15 min) ▶                                       │
│   SELECT Orders WHERE Status=Shipped AND ShippedAt > now - 30d  │
│                       AND AwbNumber IS NOT NULL                 │
│                       AND (LastTrackingSyncAt IS NULL OR        │
│                            LastTrackingSyncAt < now - 15min)    │
│   foreach (SemaphoreSlim cap=5):                                │
│     ISamedayClient.GetTrackingAsync(awb)                        │
│     → CAS UPDATE if state=Delivered                             │
│     → otherwise UPDATE LastTrackingSyncAt only                  │
└─────────────────────────────────────────────────────────────────┘
```

**Rationale** (same trade-offs as bolt 051):

- Channel chosen over a `JobQueue` DB table for the same reason
  ADR-010 chose it for promotion: every state we need is already
  on `Order`; a parallel table would duplicate the source of truth.
- The retry job IS the recovery scanner. No separate
  `AwbCreationRecoveryScanner` because the work-to-do query already
  selects every dispatch-needing order.
- Dispatcher in-process retry budget (5 attempts, exponential 30 s /
  120 s / 300 s / 900 s / 3600 s) keeps transient blips invisible
  to the customer; the retry job's 60-min sweep is for crashes and
  the channel's "this replica restarted, lost its pending channel
  items" case.
- Two distinct cadences (60 min for AWB retry, 15 min for tracking):
  AWB creation latency targets `< 60 s p95 from Paid` (FR-3); the
  channel handles the happy path; the retry job is a safety floor,
  not the primary path. Tracking polling is bounded by Sameday's
  rate budget (5 req/s × 15 min ≈ 4500 polls per tick — well above
  any realistic active-shipment cohort).

---

## Layer Structure

```text
┌────────────────────────────────────────────────────────────────┐
│  Presentation                                                   │
│   (no new controllers — admin endpoints land in a later intent) │
├────────────────────────────────────────────────────────────────┤
│  Application                                                    │
│   AwbDispatcher : BackgroundService                             │
│   AwbRetryJob : BackgroundService                               │
│   ShipmentTrackingJob : BackgroundService                       │
│   IAwbCreator / AwbCreator                                      │
│   OrderToAwbRequestMapper (static)                              │
│   AwbGiveUpRegistry (one-shot log dedupe)                       │
├────────────────────────────────────────────────────────────────┤
│  Domain                                                         │
│   ParcelWeight (value object)                                   │
│   AwbJob (channel payload)                                      │
│   AwbCreationOutcome (discriminated union, sealed record base)  │
│   TrackingPollOutcome (discriminated union)                     │
│   Order entity (extended: DeliveredAt)                          │
├────────────────────────────────────────────────────────────────┤
│  Infrastructure                                                 │
│   IAwbJobQueue / AwbJobQueue (Channel<AwbJob>, singleton)       │
│   PhotoPrintDbContext + Order entity (extended — 1 new column)  │
│   EF migration: 20260602_AddOrderDeliveredAt                    │
│   ISamedayClient (bolt 036 — now fully implemented for AWB +    │
│                   label + tracking endpoints)                   │
└────────────────────────────────────────────────────────────────┘
```

**Responsibility split**:

- **Application layer** owns the *workflow*: when to fire, what to do
  with success / failure outcomes, how to dedupe one-shot give-up
  events.
- **Domain layer** holds the small value objects and the discriminated
  unions that pass between the application services. Pure data;
  reusable from tests without DI.
- **Infrastructure** owns the channel singleton and the DbContext
  extension. The Sameday HTTP transport is already in bolt 036's
  infrastructure layer; this bolt only fills in the three
  `NotImplementedException`-throwing stubs.

---

## Component Design

### `IAwbCreator` / `AwbCreator` (Scoped)

```text
+-------------------------------------------------------------+
|  IAwbCreator                                                 |
|  + Task<AwbCreationOutcome> CreateForOrderAsync(             |
|       Guid orderId, int attempt, CancellationToken ct)       |
+-------------------------------------------------------------+
                            ▲
                            │ implements
                            │
+-------------------------------------------------------------+
|  AwbCreator                                                  |
|  - PhotoPrintDbContext _db                                   |
|  - ISamedayClient _sameday                                   |
|  - IOptions<SamedaySettings> _samedaySettings                |
|  - ILogger<AwbCreator> _logger                               |
|  - TimeProvider _clock                                       |
|                                                              |
|  + CreateForOrderAsync(orderId, attempt, ct):                |
|      order = await _db.Orders.Include(o=>o.Items)            |
|                    .Include(o=>o.EasyboxLocker)              |
|                    .FirstOrDefaultAsync(o=>o.Id==orderId)    |
|      if order is null                  return Skipped("…")   |
|      if order.Status != Paid           return Skipped("…")   |
|      if order.AwbNumber is not null    return Skipped("…")   |
|                                                              |
|      try { req = OrderToAwbRequestMapper.ToRequest(order,    |
|                                          _samedaySettings) }|
|      catch (ArgumentException ex)                            |
|          return GiveUp("invalid request: " + ex.Message)     |
|                                                              |
|      try { res = await _sameday.CreateAwbAsync(req, ct) }    |
|      catch (SamedayUnreachableException) → RetryLater(t:T)   |
|      catch (SamedayAuthException)        → RetryLater(t:F)   |
|      catch (SamedayProtocolException)    → RetryLater(t:F)   |
|      catch (SamedayValidationException)  → GiveUp(…)         |
|                                                              |
|      order.AwbNumber   = res.AwbNumber                       |
|      order.AwbLabelUrl = res.LabelUrl                        |
|      order.UpdatedAt   = _clock.GetUtcNow()                  |
|      await _db.SaveChangesAsync(ct)                          |
|      _logger.LogInformation("sameday.awb.created …")         |
|      return Created(res.AwbNumber, res.LabelUrl)             |
+-------------------------------------------------------------+
```

**Scoped lifetime**: `AwbCreator` holds a `PhotoPrintDbContext`. The
dispatcher creates a scope per job to avoid sharing one DbContext
across the lifetime of the host.

### `IAwbJobQueue` / `AwbJobQueue` (Singleton)

```csharp
public interface IAwbJobQueue
{
    ValueTask EnqueueAsync(AwbJob job, CancellationToken ct = default);
    IAsyncEnumerable<AwbJob> DequeueAllAsync(CancellationToken ct);
}

public sealed class AwbJobQueue : IAwbJobQueue
{
    private readonly Channel<AwbJob> _channel =
        Channel.CreateUnbounded<AwbJob>(new UnboundedChannelOptions
        {
            SingleReader = true,   // AwbDispatcher is the only reader
            SingleWriter = false,  // hook + retry job both enqueue
        });

    public ValueTask EnqueueAsync(AwbJob job, CancellationToken ct)
        => _channel.Writer.WriteAsync(job, ct);

    public IAsyncEnumerable<AwbJob> DequeueAllAsync(CancellationToken ct)
        => _channel.Reader.ReadAllAsync(ct);
}
```

Identical shape to `PromotionQueue` from bolt 051. Singleton.

### `AwbDispatcher : BackgroundService` (Hosted Singleton)

```text
ExecuteAsync(stoppingToken):
  await foreach (job in _queue.DequeueAllAsync(stoppingToken)):
      _ = ProcessAsync(job, stoppingToken)
         (fire-and-forget — the SemaphoreSlim caps real concurrency)

ProcessAsync(job, ct):
  await _gate.WaitAsync(ct)        // SemaphoreSlim(5, 5)
  try:
      using var scope = _scopeFactory.CreateAsyncScope()
      var creator = scope.ServiceProvider.GetRequiredService<IAwbCreator>()
      outcome = await creator.CreateForOrderAsync(job.OrderId, job.Attempt, ct)
      await HandleOutcomeAsync(outcome, job, ct)
  catch (OperationCanceledException):
      throw  // shutdown
  catch (Exception ex):
      _logger.LogError(ex, "AwbDispatcher: unexpected error processing job {OrderId}", job.OrderId)
      // do not crash the host; retry job will pick it up
  finally:
      _gate.Release()

HandleOutcomeAsync(outcome, job, ct):
  switch outcome:
    Created       → done (log already happened inside creator)
    Skipped(r)    → log Info "skipped" + done
    RetryLater(transient=true)  → schedule re-enqueue with backoff
    RetryLater(transient=false) → log Warn + done (retry job picks up)
    GiveUp(r)     → log Error "permanent fail" + done

ScheduleReEnqueue(job, ct):
  if job.Attempt >= 5:
      log Info "dispatcher backoff exhausted, leaving to AwbRetryJob"
      return
  delay = BackoffSeconds[Math.Min(job.Attempt - 1, 4)]
  _ = Task.Run(async () =>
      {
          await Task.Delay(TimeSpan.FromSeconds(delay), ct)
          await _queue.EnqueueAsync(job with { Attempt = job.Attempt + 1 }, ct)
      }, ct)
```

**Concurrency cap**: `SemaphoreSlim(5, 5)`. Five concurrent in-flight
Sameday calls per host. Matches story 002's technical note.

**Dispatcher backoff schedule** (mirrors bolt 051's
`OrderPhotoArchive:BackoffSeconds`):
`[30, 120, 300, 900, 3600]` seconds, indexed by `attempt-1`, clamped
at the last value.

### `AwbRetryJob : BackgroundService` (Hosted Singleton)

```text
ExecuteAsync(stoppingToken):
  using var timer = new PeriodicTimer(TimeSpan.FromMinutes(
                                        _settings.AwbRetryIntervalMinutes))
  while (await timer.WaitForNextTickAsync(stoppingToken)):
      try { await RunOneTickAsync(stoppingToken) }
      catch (OperationCanceledException) { throw }
      catch (Exception ex) {
          _logger.LogError(ex, "AwbRetryJob tick failed")
      }

RunOneTickAsync(ct):
  using var scope = _scopeFactory.CreateAsyncScope()
  var db = scope.ServiceProvider.GetRequiredService<PhotoPrintDbContext>()
  var giveUp = scope.ServiceProvider.GetRequiredService<AwbGiveUpRegistry>()

  var now    = _clock.GetUtcNow()
  var window = now - TimeSpan.FromHours(_settings.AwbGiveUpHours)  // default 24h

  // Two passes: re-enqueue inside window, give-up outside window.
  var insideWindow = await db.Orders
      .Where(o => o.Status == OrderStatus.Paid
                  && o.AwbNumber == null
                  && o.PaidAt > window)
      .Select(o => o.Id)
      .ToListAsync(ct)

  foreach (var id in insideWindow):
      await _queue.EnqueueAsync(new AwbJob(id, 1, now), ct)

  var outsideWindow = await db.Orders
      .Where(o => o.Status == OrderStatus.Paid
                  && o.AwbNumber == null
                  && o.PaidAt <= window
                  && o.PaidAt > window - TimeSpan.FromDays(30))   // bound query
      .Select(o => new { o.Id, o.PaidAt })
      .ToListAsync(ct)

  foreach (var o in outsideWindow):
      if (giveUp.MarkOnce(o.Id))
          _logger.LogError(
              "sameday.awb.give-up order_id={Id} paid_at={Paid:o}", o.Id, o.PaidAt)
```

**`AwbGiveUpRegistry`** is a thin wrapper around an
`IMemoryCache` instance (already registered project-wide). It calls
`Set` with a 32-day sliding expiration and returns `true` from
`MarkOnce` only the first time per process; subsequent ticks within
the same process get `false` (no duplicate log line). Across process
restarts the dedup resets — acceptable; a once-per-restart log is
still well below "noise."

**Bounded query**: the `> window - 30 days` floor stops the query
from selecting stuck-in-Paid orders from arbitrary history (e.g. if
we ever roll out a migration that misses some). The exact bound is
arbitrary; 30 days is two cycles of the tracking-window and easy
to remember.

### `ShipmentTrackingJob : BackgroundService` (Hosted Singleton)

```text
ExecuteAsync(stoppingToken):
  using var timer = new PeriodicTimer(TimeSpan.FromMinutes(
                                        _settings.TrackingIntervalMinutes))
  while (await timer.WaitForNextTickAsync(stoppingToken)):
      try { await RunOneTickAsync(stoppingToken) }
      catch (OperationCanceledException) { throw }
      catch (Exception ex) {
          _logger.LogError(ex, "ShipmentTrackingJob tick failed")
      }

RunOneTickAsync(ct):
  using var scope = _scopeFactory.CreateAsyncScope()
  var db      = scope.ServiceProvider.GetRequiredService<PhotoPrintDbContext>()
  var sameday = scope.ServiceProvider.GetRequiredService<ISamedayClient>()
  var emails  = scope.ServiceProvider.GetRequiredService<IOrderEmailService>()
  var stop    = scope.ServiceProvider.GetRequiredService<TrackingStopRegistry>()

  var now            = _clock.GetUtcNow()
  var minSinceLastSync = now - TimeSpan.FromMinutes(_settings.TrackingIntervalMinutes)
  var earliestShipped = now - TimeSpan.FromDays(_settings.TrackingMaxAgeDays)

  // In-window: poll
  var inWindow = await db.Orders
      .Where(o => o.Status == OrderStatus.Shipped
                  && o.AwbNumber != null
                  && o.ShippedAt > earliestShipped
                  && (o.LastTrackingSyncAt == null
                       || o.LastTrackingSyncAt < minSinceLastSync))
      .ToListAsync(ct)

  foreach order in inWindow:
      await _gate.WaitAsync(ct)
      try:
          await PollOneAsync(scope, order, ct)
      finally:
          _gate.Release()

  // Out-of-window: one-shot give-up log
  var outOfWindow = await db.Orders
      .Where(o => o.Status == OrderStatus.Shipped
                  && o.ShippedAt <= earliestShipped
                  && o.ShippedAt > earliestShipped - TimeSpan.FromDays(60))
      .Select(o => new { o.Id, o.ShippedAt })
      .ToListAsync(ct)

  foreach o in outOfWindow:
      if (stop.MarkOnce(o.Id))
          _logger.LogWarning(
              "sameday.tracking.polling-stopped order_id={Id} shipped_at={ts:o}",
              o.Id, o.ShippedAt)

PollOneAsync(scope, order, ct):
  TrackingSnapshot snapshot;
  try { snapshot = await sameday.GetTrackingAsync(order.AwbNumber!, ct) }
  catch (SamedayUnreachableException)        { return }   // wait next tick
  catch (SamedayException ex)                { _logger.LogWarning(ex, "tracking failed"); return }

  // Monotonic invariant — refuse to move LastTrackingSyncAt backwards.
  if (order.LastTrackingSyncAt is { } prev && snapshot.ObservedAt < prev):
      _logger.LogWarning("Sameday tracking ObservedAt {Obs:o} < stored {Prev:o} — skipping write",
                         snapshot.ObservedAt, prev)
      return

  if (snapshot.State == TrackingState.Delivered):
      // CAS transition. EF's optimistic concurrency on Status is the cheapest path.
      var affected = await scope.Db.Orders
          .Where(o => o.Id == order.Id && o.Status == OrderStatus.Shipped)
          .ExecuteUpdateAsync(setters => setters
              .SetProperty(o => o.Status,             OrderStatus.Delivered)
              .SetProperty(o => o.DeliveredAt,        snapshot.ObservedAt)
              .SetProperty(o => o.LastTrackingSyncAt, snapshot.ObservedAt)
              .SetProperty(o => o.UpdatedAt,          _clock.GetUtcNow()),
              ct)
      if (affected == 0):
          _logger.LogInformation(
              "sameday.tracking.race-lost order_id={Id} — status already advanced",
              order.Id)
          return
      _logger.LogInformation(
          "sameday.shipment.delivered order_id={Id} awb={Awb}",
          order.Id, order.AwbNumber)
      await emails.FireOrderDeliveredEmailAsync(order.Id, ct)
      return

  // Any other state: just touch LastTrackingSyncAt.
  await scope.Db.Orders
      .Where(o => o.Id == order.Id)
      .ExecuteUpdateAsync(setters => setters
          .SetProperty(o => o.LastTrackingSyncAt, snapshot.ObservedAt),
          ct)
```

**`ExecuteUpdateAsync` on EF 8**: lets us do a single round-trip,
guarded UPDATE without loading the entity into the change tracker.
Returns the affected row count which is exactly the CAS signal we
need.

**`TrackingStopRegistry`**: same shape as `AwbGiveUpRegistry` —
`IMemoryCache`-backed once-per-process dedup.

### `OrderStatusMachine.AfterTransitionAsync` extension

Single new handler on the existing extension point:

```csharp
public async Task AfterTransitionAsync(Order order, OrderStatus from, OrderStatus to, CancellationToken ct)
{
    // ... existing handlers ...

    // Bolt 037 hook: kick off AWB creation when the order goes Paid.
    if (to == OrderStatus.Paid && _samedaySettings.Enabled && _jobsSettings.Enabled)
    {
        await _awbQueue.EnqueueAsync(
            new AwbJob(order.Id, Attempt: 1, EnqueuedAt: _clock.GetUtcNow()),
            ct);
    }
}
```

The two flags (`Sameday:Enabled` + `Sameday:Jobs:Enabled`) are both
checked. With either off, the hook is a no-op. The enqueue is fast
and never throws (the channel is unbounded); even if it did throw,
the retry job would cover the miss.

### `SamedayClient.CreateAwbAsync / GetLabelPdfAsync / GetTrackingAsync`

The three `NotImplementedException` stubs from bolt 036 get real
implementations now:

**`CreateAwbAsync`**:
- POST `/api/awb` with body shape per Sameday docs:
  ```json
  {
    "pickupPoint": "<settings.PickupPointId>",
    "awbPayment": 1,
    "thirdPartyPickup": 0,
    "service": 7,
    "packageType": 1,
    "packageNumber": 1,
    "packageWeight": <kg>,
    "client": { "name": "FotoTipar", "phoneNumber": "..." },
    "awbRecipient": {
      "name": "...", "phoneNumber": "...",
      "address": "...", "city": "...",
      "county": "...", "postalCode": "..."
    },
    "cashOnDelivery": 0,
    "observation": "Order #FT-...",
    "thirdParty": null,
    "parcels": [ { "weight": "...", "length": "20", "width": "15", "height": "2", "type": 0 } ],
    "lockerFirstMile": 0,
    "lockerLastMile": <lockerId or 0>
  }
  ```
- Maps the response to `AwbCreationResult(AwbNumber, LabelUrl,
  CalculatedPrice)`.
- Same error-mapping as `AuthenticateAsync` from bolt 036: 401 →
  caught by SamedayAuthHandler (retry-once), 5xx → Polly retry,
  4xx → `SamedayValidationException`, malformed body →
  `SamedayProtocolException`.

**`GetLabelPdfAsync`** (not exercised by jobs in this bolt but
demanded by the interface contract):
- GET `/api/awb/{awbNumber}/label`
- Returns the raw PDF stream. Future admin-download endpoint
  consumes this.

**`GetTrackingAsync`**:
- GET `/api/awb/{awbNumber}/tracking` (or whatever path the vendor
  docs specify — exact path pinned in the fixture during Stage 5).
- Vendor returns a status code (string) + history array. Maps to
  `TrackingSnapshot` via a private static `MapVendorStatus(string)
  → TrackingState` function. The mapping is the anti-corruption
  boundary — every place that needed to know Sameday's specific
  status codes ends here. (See the inline mapping table in the
  ubiquitous-language section below.)

### Vendor status → `TrackingState` mapping

| Sameday code (raw) | `TrackingState` |
|---|---|
| `awb-issued`, `pickup-pending` | `Pending` |
| `picked-up`, `in-transit`, `arrived-at-sortation`, `out-for-pickup` | `InTransit` |
| `out-for-delivery`, `at-locker` | `OutForDelivery` |
| `delivered`, `delivered-to-locker-with-pickup` | `Delivered` |
| `failed-delivery`, `returned-to-sender`, `lost` | `Failed` |
| `cancelled` | `Cancelled` |
| *anything else* | `Unknown` |

This table is the *initial* mapping derived from public docs; the
test fixture (Stage 5) pins the strings, and future drift gets
caught the moment the fixture refresh fails.

---

## API Design

### Outbound (we → Sameday)

| Endpoint | Method | Implemented in 037? |
|---|---|---|
| `/api/authenticate` | POST | ✅ (already in 036) |
| `/api/awb` | POST | ✅ |
| `/api/awb/{number}/label` | GET | ✅ (interface contract — unused by jobs) |
| `/api/awb/{number}/tracking` | GET | ✅ |

### Inbound (clients → us)

**No new HTTP endpoints in this bolt.** Side effects are durable
columns on `Order` (`AwbNumber`, `AwbLabelUrl`, `DeliveredAt`,
`LastTrackingSyncAt`) plus structured logs plus rows in
`EmailQueue`. The existing `GET /api/orders/{id}` will surface the
new `DeliveredAt` field automatically if its DTO is widened (out of
scope here — admin / customer UI work).

`/health` does NOT add a Sameday-jobs check. The jobs run forever
on a timer; a failed tick logs Error and the next tick retries, so
there's no liveness signal that maps to a healthy/unhealthy
boolean.

---

## Data Model

### Schema additions

One column on `Orders`:

| Column | Type (Postgres) | Type (PostgreSQL — dev) | Nullable | Notes |
|---|---|---|---|---|
| `DeliveredAt` | `timestamp with time zone` | `INTEGER` (via Postgres-only Unix-ms converter) | yes | UTC. Set exactly once by the tracking job when transitioning Shipped → Delivered. |

EF Core configuration (added to existing `Order` entity block in
`PhotoPrintDbContext.OnModelCreating`):

```csharp
entity.Property(o => o.DeliveredAt).IsRequired(false);
```

### Migration

`Migrations/20260603_AddOrderDeliveredAt.cs`:

```csharp
migrationBuilder.AddColumn<DateTimeOffset>(
    name: "DeliveredAt",
    table: "Orders",
    type: "timestamp with time zone",
    nullable: true);
```

`Down` is a `DropColumn`. The migration runs at boot on PostgreSQL.


### `Order` entity (extension)

```csharp
public class Order
{
    // ... existing properties ...
    public DateTimeOffset? DeliveredAt { get; set; }
}
```

Setter is public (matches existing entity style). The "writes only
via the tracking job" rule is preserved by convention + the CAS
UPDATE.

---

## Configuration

Extension to `SamedaySettings` with one new nested section
`Jobs`:

```csharp
public sealed class SamedaySettings
{
    public const string SectionName = "Sameday";

    public bool   Enabled               { get; set; }
    public string BaseUrl               { get; set; } = "https://api.sameday.ro";
    public string Username              { get; set; } = string.Empty;
    public string Password              { get; set; } = string.Empty;
    public string PickupPointId         { get; set; } = string.Empty;
    public int    RequestTimeoutSeconds { get; set; } = 10;

    // New in bolt 037 — nested for clarity, but flattened binding works fine
    public SamedayJobsSettings Jobs    { get; set; } = new();
}

public sealed class SamedayJobsSettings
{
    public bool   Enabled                       { get; set; } = false;
    public int    AwbRetryIntervalMinutes       { get; set; } = 60;
    public int    AwbGiveUpHours                { get; set; } = 24;
    public int    TrackingIntervalMinutes       { get; set; } = 15;
    public int    TrackingMaxAgeDays            { get; set; } = 30;
    public int    MaxConcurrentSamedayCalls     { get; set; } = 5;
    public int[]  DispatchBackoffSeconds        { get; set; } = [30, 120, 300, 900, 3600];
}
```

Validator extension (`SamedaySettingsValidator`):

```csharp
if (options.Enabled && options.Jobs.Enabled)
{
    if (options.Jobs.AwbRetryIntervalMinutes < 1)        failures.Add(…);
    if (options.Jobs.AwbGiveUpHours < 1)                 failures.Add(…);
    if (options.Jobs.TrackingIntervalMinutes < 1)        failures.Add(…);
    if (options.Jobs.TrackingMaxAgeDays < 1)             failures.Add(…);
    if (options.Jobs.MaxConcurrentSamedayCalls is < 1 or > 50) failures.Add(…);
    if (options.Jobs.DispatchBackoffSeconds.Length == 0) failures.Add(…);
    if (options.Jobs.DispatchBackoffSeconds.Any(s => s < 1)) failures.Add(…);
}
```

### `appsettings.json` defaults (default OFF)

```json
{
  "Sameday": {
    "Enabled": false,
    "BaseUrl": "https://api.sameday.ro",
    "Username": "",
    "Password": "",
    "PickupPointId": "",
    "RequestTimeoutSeconds": 10,
    "Jobs": {
      "Enabled": false,
      "AwbRetryIntervalMinutes": 60,
      "AwbGiveUpHours": 24,
      "TrackingIntervalMinutes": 15,
      "TrackingMaxAgeDays": 30,
      "MaxConcurrentSamedayCalls": 5,
      "DispatchBackoffSeconds": [30, 120, 300, 900, 3600]
    }
  }
}
```

---

## Polly Rate-Limit (deferred from bolt 036, now lands here)

`SamedayPolicies.BuildRetryPipeline()` gains a rate-limit step *in
front of* the retry strategy. Requires the `Polly.RateLimiting`
NuGet (small, pure-managed package).

```csharp
public static ResiliencePipeline<HttpResponseMessage> BuildPipeline(int maxPerSecond)
{
    return new ResiliencePipelineBuilder<HttpResponseMessage>()
        .AddRateLimiter(new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
        {
            PermitLimit = maxPerSecond,
            Window      = TimeSpan.FromSeconds(1),
            SegmentsPerWindow = 4,
            QueueLimit  = int.MaxValue,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
        }))
        .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
        {
            // ... unchanged from bolt 036 ...
        })
        .Build();
}
```

The previous `BuildRetryPipeline()` (no rate limit) is renamed to
`BuildPipeline(5)`, called from the same `SamedayResilienceHandler`.
Default 5 req/s, configurable via
`Sameday:Jobs:MaxConcurrentSamedayCalls`.

**Order of operations** (outer → inner):

1. `SamedayAuthHandler` — bearer attach + 401 retry-once.
2. *(new layer:)* `Polly.RateLimiter` — block until a permit is
   available; never reject (the dispatcher's `SemaphoreSlim` provides
   the upstream queue depth bound).
3. `Polly.Retry` — 5xx / 408 / 429 backoff.
4. Primary handler.

This is the *only* design change to bolt 036's HTTP pipeline.

---

## Security Design

Three concerns; all enforced in code.

1. **No credential leakage in logs.** Same as bolt 036 — `LogRedactor`
   already redacts `Authorization`; `SamedayCredentials` /
   `SamedayToken` override `ToString`. Nothing new to add here.

2. **Sensitive customer data on the wire.** AWB-creation requests
   contain recipient names, addresses, phone numbers — PII. The
   structured log inside `AwbCreator` MUST emit only the order ID
   and AWB number on success; the request body never goes into a
   logger call. Defence: a code-review check + a unit test that
   greps the captured log output for the recipient name and
   asserts it's NOT present.

3. **CAS UPDATE is the only path to write Status.** EF's
   `ExecuteUpdateAsync(o => o.Id == id && o.Status == Shipped, …)`
   produces a single SQL UPDATE with a `WHERE` clause; no entity
   tracking, no SELECT-then-UPDATE race window. This is
   functionally equivalent to a CAS instruction and is the basis
   of the multi-replica safety story.

---

## Non-Functional Design

| NFR (from `requirements.md`) | Design choice |
|---|---|
| **AWB creation p95 < 60 s from Paid** | Channel + dispatcher path: enqueue is sub-ms; consumer processes immediately. Sameday round-trip is the dominant latency (~1–3 s). Well under 60 s p95. |
| **AWB success rate ≥ 98%** | Dispatcher in-process retry (5 attempts, exp backoff) + hourly retry-job sweep for crashes. The retry job has 24 h to succeed before give-up. |
| **Tracking poll throughput: 200 shipments / tick < 30 s wall** | Rate-limit budget: 5 req/s × 30 s = 150 successful polls / 30 s sustained. With 200 active shipments and a 15-min tick, average tick takes 200/5 = 40 s — slightly over the target. **Mitigation**: the tick continues past 30 s; nothing breaks. If real volume requires faster polls, bump the rate limit (5 → 10 is still under Sameday's 10 req/s ceiling) and re-tune. |
| **No credential plaintext in logs** | Inherited from bolt 036. |
| **Polly rate-limit ≤ 5 req/s** | `SlidingWindowRateLimiter` with `PermitLimit=5`, `Window=1s`, `Segments=4` for smoothness. Configurable. |
| **Tests hermetic** | Reuse bolt 036's `ScriptedHttpMessageHandler` + `FakeTimeProvider` + in-memory `PhotoPrintDbContext` from bolt 052/053's patterns. |

---

## Project Structure

```text
src/PhotoPrint.API/
  Configuration/
    SamedaySettings.cs                    ← extend with Jobs nested
  Validators/
    SamedaySettingsValidator.cs           ← add Jobs rules
  Services/
    Sameday/
      ISamedayClient.cs                   ← unchanged
      SamedayClient.cs                    ← implement 3 stubs
      SamedayPolicies.cs                  ← add rate limiter
      SamedayWireDtos.cs                  ← add AWB + Tracking wire shapes
      AwbCreationOutcome.cs               ← new
      TrackingPollOutcome.cs              ← new
      ParcelWeight.cs                     ← new
    Sameday/Lifecycle/
      AwbJob.cs                           ← channel payload
      IAwbJobQueue.cs                     ← new
      AwbJobQueue.cs                      ← new
      IAwbCreator.cs                      ← new
      AwbCreator.cs                       ← new
      OrderToAwbRequestMapper.cs          ← new
      AwbGiveUpRegistry.cs                ← new
      TrackingStopRegistry.cs             ← new
  BackgroundJobs/
    AwbDispatcher.cs                      ← new
    AwbRetryJob.cs                        ← new
    ShipmentTrackingJob.cs                ← new
  Services/
    OrderStatusMachine.cs                 ← extend AfterTransitionAsync
  Models/
    Order.cs                              ← + DeliveredAt
  Data/
    PhotoPrintDbContext.cs                ← + DeliveredAt EF mapping
  Migrations/
    20260603_AddOrderDeliveredAt.cs       ← new
  Program.cs                              ← conditional DI for the 3 jobs

src/PhotoPrint.Tests/
  Unit/Services/Sameday/Lifecycle/
    AwbCreatorTests.cs                    ← outcome matrix
    OrderToAwbRequestMapperTests.cs       ← recipient resolution + weight
    AwbDispatcherTests.cs                 ← backoff, semaphore, scope mgmt
    AwbRetryJobTests.cs                   ← inside / outside 24h window
    ShipmentTrackingJobTests.cs           ← CAS transition + race-lost
    ParcelWeightTests.cs                  ← heuristic floor
  Unit/Services/Sameday/
    SamedayClientAwbTests.cs              ← new endpoints, error matrix
    SamedayClientTrackingTests.cs         ← state mapping table
    SamedayPoliciesTests.cs               ← + rate-limit test
```

---

## DI Wiring

In `Program.cs`, extend the `if (samedayEnabled)` block:

```csharp
if (samedayEnabled)
{
    // ── existing bolt 036 wiring ─────────────────────────────
    services.AddSingleton<ISamedayTokenProvider, SamedayTokenProvider>();
    services.AddTransient<SamedayAuthHandler>();
    services.AddTransient<SamedayResilienceHandler>();
    services.AddHttpClient<ISamedayClient, SamedayClient>(...)
            .AddHttpMessageHandler<SamedayAuthHandler>()
            .AddHttpMessageHandler<SamedayResilienceHandler>();
    services.AddScoped<IShippingService, SamedayShippingService>();

    // ── NEW: bolt 037 lifecycle wiring ──────────────────────
    var jobsEnabled = builder.Configuration
        .GetSection(SamedaySettings.SectionName + ":Jobs")
        .GetValue<bool>("Enabled");
    if (jobsEnabled)
    {
        services.AddSingleton<IAwbJobQueue, AwbJobQueue>();
        services.AddSingleton<AwbGiveUpRegistry>();
        services.AddSingleton<TrackingStopRegistry>();
        services.AddScoped<IAwbCreator, AwbCreator>();

        services.AddHostedService<AwbDispatcher>();
        services.AddHostedService<AwbRetryJob>();
        services.AddHostedService<ShipmentTrackingJob>();
    }
}
```

Same posture as bolt 036's `if (samedayEnabled)` gate: with the flag
off, no new services register, the OrderStatusMachine hook no-ops,
and runtime is byte-identical to today.

---

## Integration Points

- **`OrderStatusMachine.AfterTransitionAsync`** — extend with the AWB
  enqueue. Existing extension point.
- **`IOrderEmailService.FireOrderDeliveredEmailAsync(orderId)`** —
  reused for the delivery email. Lives in the existing email
  infrastructure; this bolt only calls it.
- **`PhotoPrintDbContext.Orders`** — read/write path for every
  persistence operation. `ExecuteUpdateAsync` (EF 8) for the CAS
  transition.
- **`ISamedayClient` (bolt 036)** — three new method implementations
  (CreateAwb / GetLabelPdf / GetTracking) replace the
  `NotImplementedException` stubs.
- **`IMemoryCache` (existing)** — backs the two one-shot dedup
  registries. Already registered in `Program.cs`.

---

## Completion Criteria

- [x] Architecture pattern: channel + dispatcher + two periodic jobs,
      isomorphic to bolt 051.
- [x] All layers designed with responsibilities.
- [x] Outbound contracts pinned: `/api/awb`, `/api/awb/{n}/label`,
      `/api/awb/{n}/tracking`.
- [x] Schema additions designed: one nullable column on `Orders`
      with cross-provider notes.
- [x] NFRs addressed (latency, success rate, rate-limit,
      multi-replica safety via CAS).
- [x] Security patterns applied (no PII in success logs, CAS as the
      only Status writer).
- [x] Polly rate-limit landing wired into the existing
      `SamedayResilienceHandler` with one new NuGet
      (`Polly.RateLimiting`).

---

## ⛔ Human Checkpoint

Stage 2 (Technical Design) is drafted. Please review and approve
before I move to Stage 3 (ADR Analysis).

**Ready to proceed?**

- **1** — Approve and continue to Stage 3.
- **2** — Need changes (specify which section).
