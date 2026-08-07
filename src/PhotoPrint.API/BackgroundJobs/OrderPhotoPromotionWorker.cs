using Microsoft.Extensions.Options;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Services;

namespace PhotoPrint.API.BackgroundJobs;

/// <summary>
/// Single consumer of <see cref="IPromotionQueue"/>. Reads <see cref="PromotionJob"/>s from
/// the channel, fans out to <see cref="OrderPhotoArchiveSettings.MaxConcurrentOrders"/>
/// parallel slots (via a <see cref="SemaphoreSlim"/>), and re-enqueues with backoff on
/// failure up to <see cref="OrderPhotoArchiveSettings.MaxAttempts"/>.
/// </summary>
public class OrderPhotoPromotionWorker : BackgroundService
{
    private readonly IPromotionQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly OrderPhotoArchiveSettings _settings;
    private readonly ILogger<OrderPhotoPromotionWorker> _logger;

    public OrderPhotoPromotionWorker(
        IPromotionQueue queue,
        IServiceScopeFactory scopeFactory,
        IOptions<OrderPhotoArchiveSettings> settings,
        ILogger<OrderPhotoPromotionWorker> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("promotion.worker.disabled (OrderPhotoArchive:Enabled=false)");
            return;
        }

        _logger.LogInformation(
            "promotion.worker.started max_concurrent={Max} max_attempts={Attempts}",
            _settings.MaxConcurrentOrders, _settings.MaxAttempts);

        using var concurrency = new SemaphoreSlim(_settings.MaxConcurrentOrders);

        // Tracked out here (not inside the try) so the finally can drain it before the
        // semaphore is disposed.
        var inFlight = new List<Task>();

        try
        {
            await foreach (var job in _queue.Reader.ReadAllAsync(stoppingToken))
            {
                await concurrency.WaitAsync(stoppingToken);

                // Fire-and-forget so the reader stays hot. Each slot releases the semaphore
                // and handles its own exception surface (we don't want one bad job to crash
                // the worker loop). Prune completed tasks so the list can't grow unbounded
                // over a long-running process.
                inFlight.Add(ProcessAsync(job, concurrency, stoppingToken));
                inFlight.RemoveAll(t => t.IsCompleted);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown — the in-flight promotions are drained in the finally below.
        }
        finally
        {
            // Drain in-flight promotions BEFORE `concurrency` is disposed. Otherwise a task
            // still mid-PromoteOrderAsync reaches its finally { Release } on a disposed
            // semaphore → ObjectDisposedException (unobserved), abandoning the promotion
            // mid-write — the exact defect this drain closes. This is bounded by the host
            // shutdown timeout; PromoteOrderAsync honours stoppingToken so in-flight work
            // winds down promptly. ProcessAsync swallows its own exceptions so WhenAll should
            // not fault; log defensively if it ever does.
            try
            {
                await Task.WhenAll(inFlight);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "promotion.worker.drain-error");
            }
        }

        _logger.LogInformation("promotion.worker.stopped");
    }

    private async Task ProcessAsync(
        PromotionJob job, SemaphoreSlim concurrency, CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var promoter = scope.ServiceProvider.GetRequiredService<IOrderPhotoPromoter>();

            try
            {
                var outcome = await promoter.PromoteOrderAsync(job.OrderId, stoppingToken);

                // Any per-upload Failed > 0 → re-enqueue with the next attempt unless we're
                // at the ceiling. Promoted/Skipped failures elsewhere (DB row update, S3) are
                // also reflected in Failed via the promoter; we don't distinguish here.
                if (outcome.Failed > 0 && job.Attempt < _settings.MaxAttempts)
                {
                    ScheduleRetryDetached(job, stoppingToken);
                }
                else if (outcome.Failed > 0)
                {
                    _logger.LogError(
                        "promotion.failed.terminal order_id={OrderId} attempt={Attempt} failed_uploads={Count}",
                        job.OrderId, job.Attempt, outcome.Failed);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Shutdown mid-work — leave the row Local; recovery scan picks it up next boot.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "promotion.unhandled order_id={OrderId} attempt={Attempt}",
                    job.OrderId, job.Attempt);

                if (job.Attempt < _settings.MaxAttempts)
                    ScheduleRetryDetached(job, stoppingToken);
            }
        }
        finally
        {
            concurrency.Release();
        }
    }

    // Backoff must not hold a concurrency slot: awaiting the delay inside ProcessAsync parked
    // every slot in Task.Delay during a cloud blip, starving fresh promotions until the backoff
    // elapsed. The detached retry never touches the semaphore (safe past
    // the shutdown drain; a post-shutdown enqueue lands in the dead channel and is dropped —
    // the recovery sweep re-enqueues stuck orders). Parked retries are bounded: past the cap
    // the retry is dropped to the sweep instead of accumulating unbounded tasks under a
    // poison-order storm.
    private const int MaxParkedRetries = 100;
    private int _parkedRetries;

    private void ScheduleRetryDetached(PromotionJob job, CancellationToken stoppingToken)
    {
        if (Interlocked.Increment(ref _parkedRetries) > MaxParkedRetries)
        {
            Interlocked.Decrement(ref _parkedRetries);
            _logger.LogWarning(
                "promotion.retry.dropped order_id={OrderId} attempt={Attempt} reason=retry-backlog-full cap={Cap} — recovery sweep will re-enqueue",
                job.OrderId, job.Attempt, MaxParkedRetries);
            return;
        }

        _ = RunAsync();

        async Task RunAsync()
        {
            try
            {
                await ScheduleRetryAsync(job, stoppingToken);
            }
            catch (Exception ex)
            {
                // Fire-and-forget: never let a retry fault go unobserved.
                _logger.LogWarning(ex, "promotion.retry.error order_id={OrderId}", job.OrderId);
            }
            finally
            {
                Interlocked.Decrement(ref _parkedRetries);
            }
        }
    }

    private async Task ScheduleRetryAsync(PromotionJob job, CancellationToken stoppingToken)
    {
        var delaySeconds = LookupBackoffSeconds(job.Attempt);

        _logger.LogWarning(
            "promotion.retry order_id={OrderId} attempt={Attempt} backoff_seconds={Delay}",
            job.OrderId, job.Attempt, delaySeconds);

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken);
            await _queue.EnqueueAsync(new PromotionJob(job.OrderId, job.Attempt + 1), stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Shutdown — drop the retry; recovery scan handles it next boot.
        }
    }

    private int LookupBackoffSeconds(int attempt)
    {
        // attempt is 1-based; backoff[0] = delay after attempt 1's failure (before attempt 2).
        var idx = Math.Min(attempt - 1, _settings.BackoffSeconds.Length - 1);
        return _settings.BackoffSeconds[Math.Max(idx, 0)];
    }
}
