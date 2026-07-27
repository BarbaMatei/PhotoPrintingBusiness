using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using PhotoPrint.API.BackgroundJobs;
using PhotoPrint.API.Services.Sameday;

namespace PhotoPrint.Tests.Unit.Services.Sameday;

/// <summary>
/// Tests for the small infrastructure pieces: queue, registries, notifier.
/// Grouped in one file because each surface is small enough that a separate
/// file per class would be more noise than signal.
/// </summary>
public class AwbInfrastructureTests
{
    // ── AwbJobQueue ──────────────────────────────────────────────────────────

    [Fact]
    public async Task AwbJobQueue_enqueue_then_dequeue_round_trips()
    {
        var queue = new AwbJobQueue();
        var job = new AwbJob(Guid.NewGuid(), Attempt: 1, EnqueuedAt: DateTimeOffset.UtcNow);

        await queue.EnqueueAsync(job);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        await foreach (var dequeued in queue.DequeueAllAsync(cts.Token))
        {
            dequeued.Should().Be(job);
            return;
        }
    }

    // ── AwbGiveUpRegistry ────────────────────────────────────────────────────

    [Fact]
    public void AwbGiveUpRegistry_MarkOnce_returns_true_then_false_for_same_id()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new AwbGiveUpRegistry(cache);
        var id = Guid.NewGuid();

        sut.MarkOnce(id).Should().BeTrue();
        sut.MarkOnce(id).Should().BeFalse();
        sut.MarkOnce(id).Should().BeFalse();
    }

    [Fact]
    public void AwbGiveUpRegistry_MarkOnce_dedupes_per_id_independently()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new AwbGiveUpRegistry(cache);
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();

        sut.MarkOnce(a).Should().BeTrue();
        sut.MarkOnce(b).Should().BeTrue();   // different id — not deduped
        sut.MarkOnce(a).Should().BeFalse();
    }

    // ── TrackingStopRegistry ─────────────────────────────────────────────────

    [Fact]
    public void TrackingStopRegistry_MarkOnce_returns_true_then_false_for_same_id()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new TrackingStopRegistry(cache);
        var id = Guid.NewGuid();

        sut.MarkOnce(id).Should().BeTrue();
        sut.MarkOnce(id).Should().BeFalse();
    }

    // ── NullAwbCreationNotifier ──────────────────────────────────────────────

    [Fact]
    public async Task NullAwbCreationNotifier_is_a_no_op()
    {
        var sut = new NullAwbCreationNotifier();
        await sut.NotifyPaidAsync(Guid.NewGuid(), CancellationToken.None);
        // No assertion needed beyond not throwing — the no-op contract is the test.
    }

    // ── AwbCreationNotifier ─────────────────────────────────────────────────

    [Fact]
    public async Task AwbCreationNotifier_enqueues_AwbJob_with_attempt_1()
    {
        var queue = new AwbJobQueue();
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 6, 2, 12, 0, 0, TimeSpan.Zero));
        var sut = new AwbCreationNotifier(
            queue, clock, new LoggerFactory().CreateLogger<AwbCreationNotifier>());

        var orderId = Guid.NewGuid();
        await sut.NotifyPaidAsync(orderId);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        await foreach (var job in queue.DequeueAllAsync(cts.Token))
        {
            job.OrderId.Should().Be(orderId);
            job.Attempt.Should().Be(1);
            job.EnqueuedAt.Should().Be(clock.GetUtcNow());
            return;
        }
    }

    // ── AwbDispatcher.NextDispatchDelay (in-process backoff schedule) ─────────

    [Theory]
    [InlineData(1, 30)]
    [InlineData(2, 120)]
    [InlineData(3, 300)]
    [InlineData(4, 900)]
    [InlineData(5, 3600)]
    public void NextDispatchDelay_covers_every_configured_attempt_including_the_last(int attempt, int expectedSeconds)
    {
        var backoffs = new[] { 30, 120, 300, 900, 3600 };
        AwbDispatcher.NextDispatchDelay(attempt, backoffs)
            .Should().Be(TimeSpan.FromSeconds(expectedSeconds));
    }

    [Fact]
    public void NextDispatchDelay_is_exhausted_only_past_the_last_attempt()
    {
        var backoffs = new[] { 30, 120, 300, 900, 3600 };
        // The off-by-one exhausted at attempt == Length (5), skipping the final 3600s entry.
        AwbDispatcher.NextDispatchDelay(5, backoffs).Should().Be(TimeSpan.FromSeconds(3600));
        AwbDispatcher.NextDispatchDelay(6, backoffs).Should().BeNull();
        AwbDispatcher.NextDispatchDelay(0, backoffs).Should().BeNull();
    }
}
