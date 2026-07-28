using Microsoft.Extensions.Caching.Memory;

namespace PhotoPrint.API.Services.Sameday;

/// <summary>
/// One-shot dedup for tracking-side log lines: the per-order "polling stopped after 30 d" warning
/// and the per-outage systemic-failure Error (credentials rejected / vendor contract drift).
/// </summary>
public sealed class TrackingStopRegistry : MemoryCacheOnceRegistry
{
    private static readonly TimeSpan EntryLifetime = TimeSpan.FromDays(60);

    public TrackingStopRegistry(IMemoryCache cache) : base(cache)
    {
    }

    /// <summary>Marks the order id as "polling-stopped logged" and returns
    /// <c>true</c> only on the FIRST call per process.</summary>
    public bool MarkOnce(Guid orderId) =>
        MarkOnce($"sameday.tracking.stopped::{orderId:N}",
            new MemoryCacheEntryOptions { SlidingExpiration = EntryLifetime });

    /// <summary>Marks an outage-class key with an ABSOLUTE TTL and returns <c>true</c> only on the
    /// first hit in the window — so a systemic failure logs one Error per window instead of one per
    /// order per tick, and re-alerts on a heartbeat once the window expires.</summary>
    public bool MarkOutageOnce(string outageKey, TimeSpan window) =>
        MarkOnce($"sameday.tracking.outage::{outageKey}",
            new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = window });
}
