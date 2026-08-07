using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PhotoPrint.API.BackgroundJobs;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Services;
using Xunit;

namespace PhotoPrint.Tests.Unit.Services;

/// <summary>
/// Shutdown-drain behaviour of <see cref="OrderPhotoPromotionWorker"/>.
/// </summary>
public class OrderPhotoPromotionWorkerTests
{
    private static IServiceScopeFactory Scopes(IOrderPhotoPromoter promoter)
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => promoter);
        services.AddLogging();
        return services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
    }

    [Fact]
    public async Task Shutdown_WithInFlightPromotion_DrainsBeforeDisposingSemaphore()
    {
        // The worker launches ProcessAsync fire-and-forget; on shutdown it must await the
        // in-flight tasks BEFORE the `using` disposes the SemaphoreSlim. Pre-fix, ExecuteAsync
        // returned on cancellation and disposed the semaphore under an in-flight task, whose
        // finally { Release } then threw ObjectDisposedException (unobserved) and abandoned
        // the promotion mid-write.
        var queue = new PromotionQueue();
        var promoter = new GatedPromoter();
        var settings = Options.Create(new OrderPhotoArchiveSettings
        {
            Enabled = true,
            MaxConcurrentOrders = 1,
            MaxAttempts = 1,
        });

        var worker = new OrderPhotoPromotionWorker(
            queue, Scopes(promoter), settings, NullLogger<OrderPhotoPromotionWorker>.Instance);

        await worker.StartAsync(CancellationToken.None);
        await queue.EnqueueAsync(new PromotionJob(Guid.NewGuid()));

        // Wait until the promotion is genuinely in-flight.
        (await Task.WhenAny(promoter.Started.Task, Task.Delay(TimeSpan.FromSeconds(5))))
            .Should().Be(promoter.Started.Task, "the promotion should have started");

        // Begin shutdown. StopAsync cancels the stopping token and awaits ExecuteAsync; the
        // worker must DRAIN the in-flight promotion, so StopAsync cannot complete while the
        // promotion is still gated.
        var stopTask = worker.StopAsync(CancellationToken.None);

        var settled = await Task.WhenAny(stopTask, Task.Delay(TimeSpan.FromMilliseconds(300)));
        settled.Should().NotBe(stopTask,
            "the worker must wait for the in-flight promotion, not dispose the semaphore under it");
        promoter.Completed.Should().Be(0);

        // Open the gate: the drain completes and shutdown finishes cleanly, with the promotion
        // fully processed rather than abandoned.
        promoter.Release.TrySetResult();
        await stopTask.WaitAsync(TimeSpan.FromSeconds(5));
        promoter.Completed.Should().Be(1);
    }

    [Fact]
    public async Task FailedJobInBackoff_DoesNotHoldTheConcurrencySlot()
    {
        // The retry backoff was awaited INSIDE the semaphore-guarded
        // region, so all MaxConcurrentOrders slots could park in Task.Delay (up to 1h) during
        // a cloud blip — fresh promotions starved until the backoff elapsed. With a 1h backoff
        // and a single slot, a healthy job behind a failed one must still process promptly.
        var queue = new PromotionQueue();
        var failedOrder = Guid.NewGuid();
        var healthyOrder = Guid.NewGuid();
        var promoter = new PerOrderPromoter(orderId =>
            orderId == failedOrder ? new PromotionOutcome(0, 0, 1, 0) : PromotionOutcome.Empty);
        var settings = Options.Create(new OrderPhotoArchiveSettings
        {
            Enabled = true,
            MaxConcurrentOrders = 1,
            MaxAttempts = 2,
            BackoffSeconds = [3600], // parked far beyond the test window
        });

        var worker = new OrderPhotoPromotionWorker(
            queue, Scopes(promoter), settings, NullLogger<OrderPhotoPromotionWorker>.Instance);

        await worker.StartAsync(CancellationToken.None);
        await queue.EnqueueAsync(new PromotionJob(failedOrder));
        await queue.EnqueueAsync(new PromotionJob(healthyOrder));

        var healthyCompleted = promoter.Completed(healthyOrder);
        var winner = await Task.WhenAny(healthyCompleted, Task.Delay(TimeSpan.FromSeconds(5)));
        winner.Should().Be(healthyCompleted,
            "the healthy promotion must not wait behind a failed job's 1h backoff");

        await worker.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task FailedJob_IsReenqueuedAfterBackoff_AndSucceedsOnRetry()
    {
        // The retry/backoff/re-enqueue path had no test — deleting the
        // re-enqueue shipped green. With a zero backoff, a failed job must come back through
        // the channel and complete on its second attempt.
        var queue = new PromotionQueue();
        var order = Guid.NewGuid();
        var calls = 0;
        var promoter = new PerOrderPromoter(_ =>
            Interlocked.Increment(ref calls) == 1
                ? new PromotionOutcome(0, 0, 1, 0)
                : PromotionOutcome.Empty);
        var settings = Options.Create(new OrderPhotoArchiveSettings
        {
            Enabled = true,
            MaxConcurrentOrders = 1,
            MaxAttempts = 2,
            BackoffSeconds = [0],
        });

        var worker = new OrderPhotoPromotionWorker(
            queue, Scopes(promoter), settings, NullLogger<OrderPhotoPromotionWorker>.Instance);

        await worker.StartAsync(CancellationToken.None);
        await queue.EnqueueAsync(new PromotionJob(order));

        var secondAttempt = promoter.CompletedTimes(order, 2);
        var winner = await Task.WhenAny(secondAttempt, Task.Delay(TimeSpan.FromSeconds(5)));
        winner.Should().Be(secondAttempt,
            "the failed job must be re-enqueued and processed a second time");

        await worker.StopAsync(CancellationToken.None);
    }

    /// <summary>Configurable promoter: outcome per order id, with completion signals.</summary>
    private sealed class PerOrderPromoter(Func<Guid, PromotionOutcome> outcome) : IOrderPhotoPromoter
    {
        private readonly Dictionary<Guid, int> _completions = new();
        private readonly List<(Guid OrderId, int Count, TaskCompletionSource Tcs)> _waiters = new();
        private readonly object _lock = new();

        public Task Completed(Guid orderId) => CompletedTimes(orderId, 1);

        public Task CompletedTimes(Guid orderId, int times)
        {
            lock (_lock)
            {
                if (_completions.GetValueOrDefault(orderId) >= times)
                    return Task.CompletedTask;
                var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                _waiters.Add((orderId, times, tcs));
                return tcs.Task;
            }
        }

        public Task<PromotionOutcome> PromoteOrderAsync(Guid orderId, CancellationToken ct)
        {
            var result = outcome(orderId);
            lock (_lock)
            {
                var n = _completions.GetValueOrDefault(orderId) + 1;
                _completions[orderId] = n;
                foreach (var w in _waiters.Where(w => w.OrderId == orderId && n >= w.Count))
                    w.Tcs.TrySetResult();
            }
            return Task.FromResult(result);
        }

        public ValueTask EnqueueAsync(Guid orderId, CancellationToken ct = default)
            => ValueTask.CompletedTask;
    }

    private sealed class GatedPromoter : IOrderPhotoPromoter
    {
        public readonly TaskCompletionSource Started =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource Release =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        private int _completed;
        public int Completed => Volatile.Read(ref _completed);

        public async Task<PromotionOutcome> PromoteOrderAsync(Guid orderId, CancellationToken ct)
        {
            Started.TrySetResult();
            await Release.Task; // stay in-flight until the test opens the gate
            Interlocked.Increment(ref _completed);
            return PromotionOutcome.Empty;
        }

        public ValueTask EnqueueAsync(Guid orderId, CancellationToken ct = default)
            => ValueTask.CompletedTask;
    }
}
