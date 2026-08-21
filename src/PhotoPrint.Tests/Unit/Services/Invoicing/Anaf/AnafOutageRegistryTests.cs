using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using PhotoPrint.API.Services.Invoicing.Anaf;
using Xunit;

namespace PhotoPrint.Tests.Unit.Services.Invoicing.Anaf;

public class AnafOutageRegistryTests
{
    private static readonly TimeSpan Window = TimeSpan.FromHours(2);

    [Fact]
    public void MarkOutageOnce_IsTrueOncePerWindowAndTrueAgainAfterItExpires()
    {
        var clock = new FakeSystemClock(new DateTimeOffset(2026, 6, 3, 12, 0, 0, TimeSpan.Zero));
        using var cache = new MemoryCache(new MemoryCacheOptions { Clock = clock });
        var sut = new AnafOutageRegistry(cache);

        sut.MarkOutageOnce("auth", Window).Should().BeTrue();
        sut.MarkOutageOnce("auth", Window).Should().BeFalse();

        clock.UtcNow = clock.UtcNow.Add(Window).AddMinutes(1);

        sut.MarkOutageOnce("auth", Window).Should().BeTrue("the window is a heartbeat, not a mute");
    }

    [Fact]
    public void MarkOutageOnce_DoesNotLetOneOutageClassShadowAnother()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new AnafOutageRegistry(cache);

        sut.MarkOutageOnce("auth", Window).Should().BeTrue();
        sut.MarkOutageOnce("unreachable", Window).Should().BeTrue();
    }

    [Fact]
    public void MarkOutageOnce_DoesNotCollideWithTheSamedayRegistryKeyspace()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var anaf = new AnafOutageRegistry(cache);
        var sameday = new PhotoPrint.API.Services.Sameday.TrackingStopRegistry(cache);

        anaf.MarkOutageOnce("auth", Window).Should().BeTrue();
        sameday.MarkOutageOnce("auth", Window).Should().BeTrue();
    }
}

internal sealed class FakeSystemClock : Microsoft.Extensions.Internal.ISystemClock
{
    public FakeSystemClock(DateTimeOffset now) => UtcNow = now;

    public DateTimeOffset UtcNow { get; set; }
}
