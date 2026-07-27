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
            try { await RunOneTickAsync(stoppingToken); }
            catch (OperationCanceledException) { return; }
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
        var minSinceLastSync = now - TimeSpan.FromMinutes(_settings.TrackingIntervalMinutes);

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

        await Task.WhenAll(inWindowIds.Select(id => PollOneAsync(id, ct)));

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

    private async Task PollOneAsync(Guid orderId, CancellationToken ct)
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
            catch (SamedayException ex)
            {
                _logger.LogWarning(ex,
                    "sameday.tracking.failed order_id={OrderId}", order.Id);
                return;
            }

            // Monotonic invariant — refuse to move LastTrackingSyncAt backwards.
            if (order.LastTrackingSyncAt is { } prev && snapshot.ObservedAt < prev)
            {
                _logger.LogWarning(
                    "sameday.tracking.observed-out-of-order order_id={OrderId} observed={Obs:o} previous={Prev:o} — skipping write",
                    order.Id, snapshot.ObservedAt, prev);
                return;
            }

            if (snapshot.State == TrackingState.Delivered)
            {
                var affected = await db.Orders
                    .Where(o => o.Id == order.Id && o.Status == OrderStatus.Shipped)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(o => o.Status,             OrderStatus.Delivered)
                        .SetProperty(o => o.DeliveredAt,        (DateTimeOffset?)snapshot.ObservedAt)
                        .SetProperty(o => o.LastTrackingSyncAt, (DateTimeOffset?)snapshot.ObservedAt)
                        .SetProperty(o => o.UpdatedAt,          (DateTimeOffset?)_clock.GetUtcNow()),
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

            // Any other state: just touch LastTrackingSyncAt.
            await db.Orders
                .Where(o => o.Id == order.Id)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(o => o.LastTrackingSyncAt, (DateTimeOffset?)snapshot.ObservedAt),
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
