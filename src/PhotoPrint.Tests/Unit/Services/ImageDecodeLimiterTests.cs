using FluentAssertions;
using PhotoPrint.API.Services;
using Xunit;

namespace PhotoPrint.Tests.Unit.Services;

public class ImageDecodeLimiterTests
{
    [Fact]
    public async Task AcquireAsync_BeyondLimit_BlocksUntilAHolderReleases()
    {
        using var limiter = new ImageDecodeLimiter(maxConcurrentDecodes: 1);

        var first = await limiter.AcquireAsync();

        // The single slot is taken, so a second acquisition cannot complete yet.
        var second = limiter.AcquireAsync();
        second.IsCompleted.Should().BeFalse();

        first.Dispose();

        // Releasing the first hands the slot to the waiter.
        (await second).Dispose();
        limiter.AvailableSlots.Should().Be(1);
    }

    [Fact]
    public async Task AcquireAsync_WithinLimit_AllowsConcurrentHolders()
    {
        using var limiter = new ImageDecodeLimiter(maxConcurrentDecodes: 2);

        var a = await limiter.AcquireAsync();
        var b = await limiter.AcquireAsync();   // second slot — no block

        limiter.AvailableSlots.Should().Be(0);
        a.Dispose();
        b.Dispose();
        limiter.AvailableSlots.Should().Be(2);
    }

    [Fact]
    public void Ctor_NonPositiveLimit_Throws()
    {
        var act = () => new ImageDecodeLimiter(0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // F1 (review 042-v6): the default slot count must bound by memory, not just cores. On a
    // high-core / low-RAM host, ProcessorCount slots × ~400-512 MB per decode overruns RAM and
    // re-opens the M3 OOM DoS. The default is now min(cores, availableRAM / perDecodeBudget).

    [Fact]
    public void RecommendedMaxConcurrentDecodes_LowRamHighCore_BoundedByMemoryNotCores()
    {
        // 8-core box with only 2 GB available RAM: at 512 MB per decode, memory permits 4 —
        // fewer than the 8 cores, so memory must win and the sum stays within RAM.
        var slots = ImageDecodeLimiter.RecommendedMaxConcurrentDecodes(
            availableMemoryBytes: 2L * 1024 * 1024 * 1024,
            processorCount: 8);

        slots.Should().Be(4);
        ((long)slots * ImageDecodeLimiter.PerDecodeMemoryBudgetBytes)
            .Should().BeLessThanOrEqualTo(2L * 1024 * 1024 * 1024,
                "summed worst-case decode memory must not exceed available RAM");
    }

    [Fact]
    public void RecommendedMaxConcurrentDecodes_AmpleRam_BoundedByCores()
    {
        // 4-core box with 64 GB RAM: memory permits far more than 4, so cores cap it.
        var slots = ImageDecodeLimiter.RecommendedMaxConcurrentDecodes(
            availableMemoryBytes: 64L * 1024 * 1024 * 1024,
            processorCount: 4);

        slots.Should().Be(4);
    }

    [Fact]
    public void RecommendedMaxConcurrentDecodes_TinyRam_NeverBelowOne()
    {
        // Under one decode's budget of RAM, still allow a single decode rather than deadlocking.
        var slots = ImageDecodeLimiter.RecommendedMaxConcurrentDecodes(
            availableMemoryBytes: 128L * 1024 * 1024,
            processorCount: 8);

        slots.Should().Be(1);
    }
}
