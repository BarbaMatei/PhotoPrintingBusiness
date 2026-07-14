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
}
