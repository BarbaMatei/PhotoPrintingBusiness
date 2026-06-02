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
        using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PhotoPrintDbContext>();
        var sameday = scope.ServiceProvider.GetRequiredService<ISamedayClient>();
        var emails = scope.ServiceProvider.GetRequiredService<IOrderEmailService>();

        var now = _clock.GetUtcNow();
        var earliestShipped = now - TimeSpan.FromDays(_settings.TrackingMaxAgeDays);
        var minSinceLastSync = now - TimeSpan.FromMinutes(_settings.TrackingIntervalMinutes);

        // Inside the polling window — fetch full entities so we can pass them
        // to FireOrderDeliveredEmail (the email service requires nav properties).
        var inWindow = await db.Orders
            .Include(o => o.User)
            .Include(o => o.EasyboxLocker)
            .Where(o => o.Status == OrderStatus.Shipped
                        && o.AwbNumber != null
                        && o.ShippedAt != null
                        && o.ShippedAt > earliestShipped
                        && (o.LastTrackingSyncAt == null
                            || o.LastTrackingSyncAt < minSinceLastSync))
            .ToListAsync(ct);

        var pollTasks = inWindow.Select(order => PollOneAsync(db, sameday, emails, order, ct));
        await Task.WhenAll(pollTasks);

        // Outside the polling window — one-shot warning per order id.
        var outOfWindow = await db.Orders
            .AsNoTracking()
            .Where(o => o.Status == OrderStatus.Shipped
                        && o.ShippedAt != null
                        && o.ShippedAt <= earliestShipped
                        && o.ShippedAt > earliestShipped - TimeSpan.FromDays(60))
            .Select(o => new { o.Id, o.ShippedAt })
            .ToListAsync(ct);

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

    private async Task PollOneAsync(
        PhotoPrintDbContext db,
        ISamedayClient sameday,
        IOrderEmailService emails,
        Order order,
        CancellationToken ct)
    {
        try { await _gate.WaitAsync(ct); }
        catch (OperationCanceledException) { return; }

        try
        {
            TrackingSnapshot snapshot;
            try
            {
                snapshot = await sameday.GetTrackingAsync(order.AwbNumber!, ct);
            }
            catch (SamedayUnreachableException)
            {
                return; // wait for next tick
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

                // Re-load with nav properties so the email service has what it needs.
                var freshOrder = await db.Orders
                    .Include(o => o.User)
                    .Include(o => o.EasyboxLocker)
                    .AsNoTracking()
                    .FirstAsync(o => o.Id == order.Id, ct);
                emails.FireOrderDeliveredEmail(freshOrder);
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
