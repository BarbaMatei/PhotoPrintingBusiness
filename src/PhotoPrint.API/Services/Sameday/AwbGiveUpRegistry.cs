using Microsoft.Extensions.Caching.Memory;

namespace PhotoPrint.API.Services.Sameday;

/// <summary>
/// One-shot dedup for the "AWB creation given up after 24 h" log line. Backed
/// by <see cref="IMemoryCache"/>; resets across process restarts (a
/// once-per-restart Error log is acceptable noise and obviates a durable
/// state-tracking column).
/// </summary>
public sealed class AwbGiveUpRegistry
{
    private static readonly TimeSpan EntryLifetime = TimeSpan.FromDays(32);

    private readonly IMemoryCache _cache;

    public AwbGiveUpRegistry(IMemoryCache cache)
    {
        _cache = cache;
    }

    /// <summary>Marks the order id as "give-up logged" and returns <c>true</c>
    /// only on the FIRST call per process. Subsequent calls return <c>false</c>
    /// so the retry job can avoid duplicate log lines.</summary>
    public bool MarkOnce(Guid orderId)
    {
        var key = $"sameday.awb.give-up::{orderId:N}";
        if (_cache.TryGetValue(key, out _))
            return false;
        _cache.Set(key, true, new MemoryCacheEntryOptions
        {
            SlidingExpiration = EntryLifetime,
        });
        return true;
    }
}
