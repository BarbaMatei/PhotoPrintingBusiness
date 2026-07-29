using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PhotoPrint.API.BackgroundJobs;
using PhotoPrint.API.Configuration;
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

    // ── AwbDispatcher re-enqueue orchestration ───────────────────────────────

    private static readonly DateTimeOffset T0 = new(2026, 6, 2, 12, 0, 0, TimeSpan.Zero);

    private static AwbDispatcher BuildDispatcher(IAwbJobQueue queue, TimeProvider clock, int claimTtlMinutes = 5)
    {
        var settings = Options.Create(new SamedaySettings
        {
            Jobs = new SamedayJobsSettings
            {
                DispatchBackoffSeconds = new[] { 30, 120, 300, 900, 3600 },
                AwbClaimTtlMinutes = claimTtlMinutes,
                MaxConcurrentSamedayCalls = 2,
            },
        });
        var scopeFactory = new ServiceCollection().BuildServiceProvider()
            .GetRequiredService<IServiceScopeFactory>();
        return new AwbDispatcher(queue, scopeFactory, settings, clock,
            new LoggerFactory().CreateLogger<AwbDispatcher>());
    }

    [Fact]
    public void ComputeReEnqueueDelay_uses_the_backoff_schedule_and_exhausts_past_the_last()
    {
        var d = BuildDispatcher(new AwbJobQueue(), new FakeTimeProvider(T0));
        d.ComputeReEnqueueDelay(1, preserveClaim: false).Should().Be(TimeSpan.FromSeconds(30));
        d.ComputeReEnqueueDelay(3, preserveClaim: false).Should().Be(TimeSpan.FromSeconds(300));
        d.ComputeReEnqueueDelay(6, preserveClaim: false).Should().BeNull();
    }

    [Fact]
    public void ComputeReEnqueueDelay_floors_a_preserve_claim_outcome_past_the_claim_TTL()
    {
        var d = BuildDispatcher(new AwbJobQueue(), new FakeTimeProvider(T0), claimTtlMinutes: 5);
        // attempt-1 backoff (30s) is inside the 5-min claim window → floored so the re-attempt
        // re-claims instead of hitting the fresh-claim skip.
        d.ComputeReEnqueueDelay(1, preserveClaim: true)
            .Should().Be(TimeSpan.FromMinutes(5) + TimeSpan.FromSeconds(30));
        // a backoff already past the TTL is left unchanged.
        d.ComputeReEnqueueDelay(5, preserveClaim: true).Should().Be(TimeSpan.FromSeconds(3600));
    }

    [Fact]
    public async Task DelayedReEnqueueAsync_re_enqueues_the_next_attempt_after_the_delay()
    {
        var queue = new AwbJobQueue();
        var clock = new FakeTimeProvider(T0);
        var dispatcher = BuildDispatcher(queue, clock);
        var job = new AwbJob(Guid.NewGuid(), Attempt: 1, EnqueuedAt: T0);

        var method = typeof(AwbDispatcher).GetMethod("DelayedReEnqueueAsync",
            BindingFlags.NonPublic | BindingFlags.Instance);
        var task = (Task)method!.Invoke(dispatcher,
            new object[] { job, TimeSpan.FromSeconds(30), CancellationToken.None })!;

        clock.Advance(TimeSpan.FromSeconds(30)); // fire the delay deterministically
        await task;

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        await foreach (var requeued in queue.DequeueAllAsync(cts.Token))
        {
            requeued.OrderId.Should().Be(job.OrderId);
            requeued.Attempt.Should().Be(2);
            return;
        }

        throw new Xunit.Sdk.XunitException("expected a re-enqueued job");
    }
}
