using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Data;
using PhotoPrint.API.Exceptions;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services;
using PhotoPrint.API.Services.Sameday;

namespace PhotoPrint.API.BackgroundJobs;

/// <summary>
/// Periodic poller for <c>Shipped</c> orders. On Sameday <c>Delivered</c>,
/// performs the ADR-016 CAS transition (<c>WHERE Status = Shipped</c>) and
/// enqueues the customer delivery email. Skips orders older than
/// <c>TrackingMaxAgeDays</c> from <c>ShippedAt</c> after a one-shot warning.
/// </summary>
public sealed class ShipmentTrackingJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TrackingStopRegistry _stop;
    private readonly SamedayJobsSettings _settings;
    private readonly TimeProvider _clock;
    private readonly ILogger<ShipmentTrackingJob> _logger;
    private readonly SemaphoreSlim _gate;

    public ShipmentTrackingJob(
        IServiceScopeFactory scopeFactory,
        TrackingStopRegistry stop,
        IOptions<SamedaySettings> samedaySettings,
        TimeProvider clock,
        ILogger<ShipmentTrackingJob> logger)
    {
        _scopeFactory = scopeFactory;
        _stop = stop;
        _settings = samedaySettings.Value.Jobs;
        _clock = clock;
        _logger = logger;
        _gate = new SemaphoreSlim(_settings.MaxConcurrentSamedayCalls,
                                  _settings.MaxConcurrentSamedayCalls);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "ShipmentTrackingJob started (intervalMinutes={Interval} maxAgeDays={MaxAge})",
            _settings.TrackingIntervalMinutes, _settings.TrackingMaxAgeDays);

        using var timer = new PeriodicTimer(
            TimeSpan.FromMinutes(_settings.TrackingIntervalMinutes));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            // Only a real shutdown exits the loop; a per-poll timeout (a non-shutdown
            // OperationCanceledException) must not kill delivery detection.
            try { await RunOneTickAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ShipmentTrackingJob tick failed");
            }
        }
    }

    private async Task RunOneTickAsync(CancellationToken ct)
    {
        var now = _clock.GetUtcNow();
        var earliestShipped = now - TimeSpan.FromDays(_settings.TrackingMaxAgeDays);
        // Buffer just below a full interval: an order polled last tick is stamped with THIS tick's
        // start clock (passed into PollOneAsync), so without the buffer next tick's threshold would
        // equal the stamp and skip it to every other tick. The buffer stays well under the interval,
        // so it barely widens the cross-replica re-poll band that LastTrackingSyncAt guards.
        var pollFloor = TimeSpan.FromMinutes(_settings.TrackingIntervalMinutes) - TimeSpan.FromSeconds(30);
        if (pollFloor < TimeSpan.FromSeconds(1)) pollFloor = TimeSpan.FromSeconds(1);
        var minSinceLastSync = now - pollFloor;

        List<Guid> inWindowIds;
        List<OutOfWindowRow> outOfWindow;

        // One short-lived scope for the read queries; each order is then polled on
        // its OWN scoped DbContext (a shared context can't service parallel tasks).
        using (var scope = _scopeFactory.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PhotoPrintDbContext>();

            inWindowIds = await db.Orders
                .AsNoTracking()
                .Where(o => o.Status == OrderStatus.Shipped
                            && o.AwbNumber != null
                            && o.ShippedAt != null
                            && o.ShippedAt > earliestShipped
                            && (o.LastTrackingSyncAt == null
                                || o.LastTrackingSyncAt < minSinceLastSync))
                .Select(o => o.Id)
                .ToListAsync(ct);

            outOfWindow = await db.Orders
                .AsNoTracking()
                .Where(o => o.Status == OrderStatus.Shipped
                            && o.ShippedAt != null
                            && o.ShippedAt <= earliestShipped
                            && o.ShippedAt > earliestShipped - TimeSpan.FromDays(60))
                .Select(o => new OutOfWindowRow(o.Id, o.ShippedAt))
                .ToListAsync(ct);
        }

        await Task.WhenAll(inWindowIds.Select(id => PollOneAsync(id, now, ct)));

        // Outside the polling window — one-shot warning per order id.
        foreach (var row in outOfWindow)
        {
            if (_stop.MarkOnce(row.Id))
            {
                _logger.LogWarning(
                    "sameday.tracking.polling-stopped order_id={OrderId} shipped_at={ShippedAt:o}",
                    row.Id, row.ShippedAt);
            }
        }
    }

    private sealed record OutOfWindowRow(Guid Id, DateTimeOffset? ShippedAt);

    private async Task PollOneAsync(Guid orderId, DateTimeOffset now, CancellationToken ct)
    {
        try { await _gate.WaitAsync(ct); }
        catch (OperationCanceledException) { return; }

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<PhotoPrintDbContext>();
            var sameday = scope.ServiceProvider.GetRequiredService<ISamedayClient>();
            var emails = scope.ServiceProvider.GetRequiredService<IOrderEmailService>();

            var order = await db.Orders
                .AsNoTracking()
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == orderId, ct);
            if (order?.AwbNumber is null)
                return;

            TrackingSnapshot snapshot;
            try
            {
                snapshot = await sameday.GetTrackingAsync(order.AwbNumber, ct);
            }
            catch (SamedayUnreachableException ex)
            {
                _logger.LogWarning(ex,
                    "sameday.tracking.unreachable order_id={OrderId} — will retry next tick", order.Id);
                return;
            }
            catch (SamedayAuthException ex)
            {
                // Systemic (rotated credentials): every order fails identically each tick. Escalate
                // to Error but once per outage window so the alert isn't buried under per-order noise.
                if (_stop.MarkOutageOnce("auth", TimeSpan.FromMinutes(30)))
                    _logger.LogError(ex,
                        "sameday.tracking.auth-outage — Sameday credentials rejected; delivery detection stalled");
                else
                    _logger.LogDebug(ex, "sameday.tracking.auth-outage order_id={OrderId}", order.Id);
                return;
            }
            catch (SamedayProtocolException ex)
            {
                if (_stop.MarkOutageOnce($"protocol::{ex.Endpoint}", TimeSpan.FromMinutes(30)))
                    _logger.LogError(ex,
                        "sameday.tracking.protocol-outage endpoint={Endpoint} — vendor contract drift", ex.Endpoint);
                else
                    _logger.LogDebug(ex, "sameday.tracking.protocol-outage order_id={OrderId}", order.Id);
                return;
            }
            catch (SamedayException ex)
            {
                // Per-order failures (e.g. a 4xx on one AWB) stay Warning.
                _logger.LogWarning(ex,
                    "sameday.tracking.failed order_id={OrderId}", order.Id);
                return;
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                // HttpClient timeout, not shutdown — skip this order, keep polling.
                _logger.LogWarning(
                    "sameday.tracking.timeout order_id={OrderId} — will retry next tick", order.Id);
                return;
            }

            // LastTrackingSyncAt is a poll-throttle timestamp on OUR clock (the tick start, so
            // eligibility and stamp share a basis), NOT the vendor observed time — so a later
            // Delivered scan carrying an earlier vendor timestamp is never dropped as "out of order".

            if (snapshot.State == TrackingState.Delivered)
            {
                var affected = await db.Orders
                    .Where(o => o.Id == order.Id && o.Status == OrderStatus.Shipped)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(o => o.Status,             OrderStatus.Delivered)
                        .SetProperty(o => o.DeliveredAt,        snapshot.ObservedAt ?? now)
                        .SetProperty(o => o.LastTrackingSyncAt, (DateTimeOffset?)now)
                        .SetProperty(o => o.UpdatedAt,          (DateTimeOffset?)now),
                        ct);

                if (affected == 0)
                {
                    _logger.LogInformation(
                        "sameday.tracking.race-lost order_id={OrderId} — status already advanced",
                        order.Id);
                    return;
                }

                _logger.LogInformation(
                    "sameday.shipment.delivered order_id={OrderId} awb={Awb}",
                    order.Id, order.AwbNumber);

                // The delivered email reads only User / GuestEmail / ShippingAddress
                // (owned, already loaded) / OrderNumber — none of which the CAS changed.
                emails.FireOrderDeliveredEmail(order);
                return;
            }

            // Any other state: advance the poll-throttle timestamp (our clock). Guarded so a slow
            // replica can't move the stamp backward, and so it never touches a row another replica
            // just advanced to Delivered.
            await db.Orders
                .Where(o => o.Id == order.Id
                            && o.Status == OrderStatus.Shipped
                            && (o.LastTrackingSyncAt == null || o.LastTrackingSyncAt < now))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(o => o.LastTrackingSyncAt, (DateTimeOffset?)now),
                    ct);
        }
        finally
        {
            _gate.Release();
        }
    }

    public override void Dispose()
    {
        _gate.Dispose();
        base.Dispose();
    }
}
