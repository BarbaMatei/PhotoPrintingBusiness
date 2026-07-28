using Microsoft.Extensions.Options;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Services.Sameday;

namespace PhotoPrint.API.BackgroundJobs;

/// <summary>
/// Drains <see cref="IAwbJobQueue"/> and runs each <see cref="AwbJob"/>
/// through <see cref="IAwbCreator"/>. In-process retry is bounded by
/// <c>Sameday:Jobs:DispatchBackoffSeconds</c>; beyond that, the
/// <c>AwbRetryJob</c> safety net takes over.
/// </summary>
public sealed class AwbDispatcher : BackgroundService
{
    private readonly IAwbJobQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SamedayJobsSettings _settings;
    private readonly TimeProvider _clock;
    private readonly ILogger<AwbDispatcher> _logger;
    private readonly SemaphoreSlim _gate;

    public AwbDispatcher(
        IAwbJobQueue queue,
        IServiceScopeFactory scopeFactory,
        IOptions<SamedaySettings> samedaySettings,
        TimeProvider clock,
        ILogger<AwbDispatcher> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _settings = samedaySettings.Value.Jobs;
        _clock = clock;
        _logger = logger;
        _gate = new SemaphoreSlim(_settings.MaxConcurrentSamedayCalls,
                                  _settings.MaxConcurrentSamedayCalls);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "AwbDispatcher started (maxConcurrent={Max})",
            _settings.MaxConcurrentSamedayCalls);

        await foreach (var job in _queue.DequeueAllAsync(stoppingToken))
        {
            // Fire-and-forget per job — the SemaphoreSlim caps real concurrency.
            _ = ProcessAsync(job, stoppingToken);
        }
    }

    private async Task ProcessAsync(AwbJob job, CancellationToken ct)
    {
        try
        {
            await _gate.WaitAsync(ct);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateAsyncScope();
            var creator = scope.ServiceProvider.GetRequiredService<IAwbCreator>();
            var outcome = await creator.CreateForOrderAsync(job.OrderId, job.Attempt, ct);
            await HandleOutcomeAsync(outcome, job, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { /* shutdown */ }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "AwbDispatcher: unexpected error processing job order_id={OrderId} attempt={Attempt}",
                job.OrderId, job.Attempt);
            // Don't crash the host. Retry job will re-discover.
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task HandleOutcomeAsync(AwbCreationOutcome outcome, AwbJob job, CancellationToken ct)
    {
        switch (outcome)
        {
            case AwbCreationOutcome.Created:
                // Success log already happened inside the creator.
                return;

            case AwbCreationOutcome.Skipped skipped:
                _logger.LogInformation(
                    "sameday.awb.skipped order_id={OrderId} reason={Reason}",
                    job.OrderId, skipped.Reason);
                return;

            case AwbCreationOutcome.RetryLater { IsTransient: true } transient:
                await ScheduleReEnqueueAsync(job, transient.Reason, ct);
                return;

            case AwbCreationOutcome.RetryLater { IsTransient: false } nonTransient:
                _logger.LogWarning(
                    "sameday.awb.non-transient-retry-later order_id={OrderId} attempt={Attempt} reason={Reason}",
                    job.OrderId, job.Attempt, nonTransient.Reason);
                return;

            case AwbCreationOutcome.GiveUp giveUp:
                _logger.LogError(
                    "sameday.awb.permanent-fail order_id={OrderId} attempt={Attempt} reason={Reason}",
                    job.OrderId, job.Attempt, giveUp.Reason);
                return;
        }
    }

    // Attempt is 1-based; a schedule of N entries covers attempts 1..N (index attempt-1),
    // exhausted only past the last entry.
    public static TimeSpan? NextDispatchDelay(int attempt, IReadOnlyList<int> backoffSeconds)
    {
        if (attempt < 1 || attempt > backoffSeconds.Count) return null;
        return TimeSpan.FromSeconds(backoffSeconds[attempt - 1]);
    }

    private async Task ScheduleReEnqueueAsync(AwbJob job, string reason, CancellationToken ct)
    {
        var delay = NextDispatchDelay(job.Attempt, _settings.DispatchBackoffSeconds);
        if (delay is null)
        {
            _logger.LogInformation(
                "sameday.awb.dispatcher-backoff-exhausted order_id={OrderId} attempt={Attempt} — handing off to AwbRetryJob",
                job.OrderId, job.Attempt);
            return;
        }

        _logger.LogInformation(
            "sameday.awb.retry-scheduled order_id={OrderId} attempt={Attempt} delay={Delay}s reason={Reason}",
            job.OrderId, job.Attempt, (int)delay.Value.TotalSeconds, reason);

        // Fire-and-forget delayed re-enqueue.
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(delay.Value, ct);
                await _queue.EnqueueAsync(
                    new AwbJob(job.OrderId, job.Attempt + 1, _clock.GetUtcNow()),
                    ct);
            }
            catch (OperationCanceledException) { /* shutdown */ }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "AwbDispatcher: failed to re-enqueue order_id={OrderId}",
                    job.OrderId);
            }
        }, ct);
    }

    public override void Dispose()
    {
        _gate.Dispose();
        base.Dispose();
    }
}
