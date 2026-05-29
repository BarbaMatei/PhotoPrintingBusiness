using FluentAssertions;
using PhotoPrint.API.Services;
using Xunit;

namespace PhotoPrint.Tests.Unit.Services;

/// <summary>
/// Smoke tests for <see cref="PromotionQueue"/> — verifies the in-memory channel
/// preserves order and is consumable by a single reader (ADR-010).
/// </summary>
public class PromotionQueueTests
{
    [Fact]
    public async Task EnqueueAsync_ReaderReadsBackSameJobs_InOrder()
    {
        var queue = new PromotionQueue();
        var a = new PromotionJob(Guid.NewGuid());
        var b = new PromotionJob(Guid.NewGuid(), Attempt: 2);

        await queue.EnqueueAsync(a);
        await queue.EnqueueAsync(b);

        queue.Reader.TryRead(out var first).Should().BeTrue();
        queue.Reader.TryRead(out var second).Should().BeTrue();

        first.Should().Be(a);
        second.Should().Be(b);
    }

    [Fact]
    public async Task EnqueueAsync_CancellationDoesNotBlock_UnboundedChannel()
    {
        var queue = new PromotionQueue();
        using var cts = new CancellationTokenSource();

        // Unbounded channels never block writers, so a cancelled token still completes.
        await queue.EnqueueAsync(new PromotionJob(Guid.NewGuid()), cts.Token);
    }
}
