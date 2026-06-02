using Microsoft.Extensions.Caching.Memory;

namespace PhotoPrint.API.Services.Sameday;

/// <summary>
/// One-shot dedup for the "tracking polling stopped after 30 d" warning.
/// Mirror of <see cref="AwbGiveUpRegistry"/>; same trade-offs.
/// </summary>
public sealed class TrackingStopRegistry
{
    private static readonly TimeSpan EntryLifetime = TimeSpan.FromDays(60);

    private readonly IMemoryCache _cache;

    public TrackingStopRegistry(IMemoryCache cache)
    {
        _cache = cache;
    }

    /// <summary>Marks the order id as "polling-stopped logged" and returns
    /// <c>true</c> only on the FIRST call per process.</summary>
    public bool MarkOnce(Guid orderId)
    {
        var key = $"sameday.tracking.stopped::{orderId:N}";
        if (_cache.TryGetValue(key, out _))
            return false;
        _cache.Set(key, true, new MemoryCacheEntryOptions
        {
            SlidingExpiration = EntryLifetime,
        });
        return true;
    }
}
