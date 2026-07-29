using Microsoft.Extensions.Caching.Memory;

namespace PhotoPrint.API.Services.Sameday;

/// <summary>
/// One-shot dedup for the "AWB creation given up after 24 h" log line.
/// </summary>
public sealed class AwbGiveUpRegistry : MemoryCacheOnceRegistry
{
    /// <summary>Dedup entry lifetime. The retry sweep's outside-window query floor is derived from
    /// this so a give-up log can never re-fire for an order still within the query window.</summary>
    public static readonly TimeSpan EntryLifetime = TimeSpan.FromDays(32);

    public AwbGiveUpRegistry(IMemoryCache cache) : base(cache)
    {
    }

    /// <summary>Marks the order id as "give-up logged" and returns <c>true</c>
    /// only on the FIRST call per process. Subsequent calls return <c>false</c>
    /// so the retry job can avoid duplicate log lines.</summary>
    public bool MarkOnce(Guid orderId) =>
        MarkOnce($"sameday.awb.give-up::{orderId:N}",
            new MemoryCacheEntryOptions { SlidingExpiration = EntryLifetime });
}
