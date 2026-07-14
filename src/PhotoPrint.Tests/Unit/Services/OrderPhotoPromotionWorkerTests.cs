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
/// Shutdown-drain behaviour of <see cref="OrderPhotoPromotionWorker"/> (F6, review 043-v1).
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
        // finally { Release() } then threw ObjectDisposedException (unobserved) and abandoned
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
