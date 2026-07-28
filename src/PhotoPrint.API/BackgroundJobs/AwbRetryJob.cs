using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Data;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services.Sameday;

namespace PhotoPrint.API.BackgroundJobs;

/// <summary>
/// Periodic safety net for AWB creation. Re-enqueues orders that the
/// dispatcher missed (crash, replica restart, give-up reset, ...). For
/// orders older than <c>AwbGiveUpHours</c> from <c>PaidAt</c>, emits a
/// one-shot give-up Error log per order id and stops re-enqueueing.
/// </summary>
public sealed class AwbRetryJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IAwbJobQueue _queue;
    private readonly AwbGiveUpRegistry _giveUp;
    private readonly SamedayJobsSettings _settings;
    private readonly TimeProvider _clock;
    private readonly ILogger<AwbRetryJob> _logger;

    public AwbRetryJob(
        IServiceScopeFactory scopeFactory,
        IAwbJobQueue queue,
        AwbGiveUpRegistry giveUp,
        IOptions<SamedaySettings> samedaySettings,
        TimeProvider clock,
        ILogger<AwbRetryJob> logger)
    {
        _scopeFactory = scopeFactory;
        _queue = queue;
        _giveUp = giveUp;
        _settings = samedaySettings.Value.Jobs;
        _clock = clock;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "AwbRetryJob started (intervalMinutes={Interval} giveUpHours={GiveUp})",
            _settings.AwbRetryIntervalMinutes, _settings.AwbGiveUpHours);

        using var timer = new PeriodicTimer(
            TimeSpan.FromMinutes(_settings.AwbRetryIntervalMinutes));

        // Run once at startup so the recovery sweep happens without waiting
        // for the first tick.
        try { await RunOneTickAsync(stoppingToken); }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AwbRetryJob startup tick failed");
        }

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try { await RunOneTickAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AwbRetryJob tick failed");
            }
        }
    }

    private async Task RunOneTickAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PhotoPrintDbContext>();

        var now = _clock.GetUtcNow();
        var giveUpThreshold = now - TimeSpan.FromHours(_settings.AwbGiveUpHours);
        // Floor the outside-window scan at the dedup lifetime so an order can never re-fire its
        // give-up log after the registry entry ages out (the two must stay coupled).
        var queryFloor = giveUpThreshold - AwbGiveUpRegistry.EntryLifetime;
        var claimFloor = now - TimeSpan.FromMinutes(Math.Max(1, _settings.AwbClaimTtlMinutes));

        // Inside the give-up window → re-enqueue, but skip orders a worker is actively
        // creating (a fresh AwbClaimedAt) so the sweep doesn't churn against live attempts.
        var insideWindow = await db.Orders
            .AsNoTracking()
            .Where(o => o.Status == OrderStatus.Paid
                        && o.AwbNumber == null
                        && o.PaidAt != null
                        && o.PaidAt > giveUpThreshold
                        && (o.AwbClaimedAt == null || o.AwbClaimedAt < claimFloor))
            .Select(o => o.Id)
            .ToListAsync(ct);

        foreach (var id in insideWindow)
        {
            await _queue.EnqueueAsync(new AwbJob(id, Attempt: 1, EnqueuedAt: now), ct);
            _logger.LogInformation("sameday.awb.retry-enqueue order_id={OrderId}", id);
        }

        if (insideWindow.Count > 0)
        {
            _logger.LogInformation(
                "sameday.awb.retry-sweep enqueued={Count}", insideWindow.Count);
        }

        // Outside the give-up window → one-shot Error log per order id.
        var outsideWindow = await db.Orders
            .AsNoTracking()
            .Where(o => o.Status == OrderStatus.Paid
                        && o.AwbNumber == null
                        && o.PaidAt != null
                        && o.PaidAt <= giveUpThreshold
                        && o.PaidAt > queryFloor)
            .Select(o => new { o.Id, o.PaidAt })
            .ToListAsync(ct);

        foreach (var row in outsideWindow)
        {
            if (_giveUp.MarkOnce(row.Id))
            {
                _logger.LogError(
                    "sameday.awb.give-up order_id={OrderId} paid_at={PaidAt:o}",
                    row.Id, row.PaidAt);
            }
        }
    }
}
