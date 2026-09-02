using Microsoft.Extensions.Caching.Memory;

namespace PhotoPrint.API.Services;

/// <summary>
/// Shared "log this once" dedup backed by <see cref="IMemoryCache"/>. Resets across process
/// restarts (a once-per-restart line is acceptable noise and obviates a durable state column).
/// Subclasses own their key namespace and entry lifetime.
/// </summary>
public abstract class MemoryCacheOnceRegistry
{
    private readonly IMemoryCache _cache;

    protected MemoryCacheOnceRegistry(IMemoryCache cache)
    {
        _cache = cache;
    }

    /// <summary>Marks <paramref name="key"/> and returns <c>true</c> only on the FIRST call while
    /// the entry lives.</summary>
    protected bool MarkOnce(string key, MemoryCacheEntryOptions options)
    {
        if (_cache.TryGetValue(key, out _))
            return false;
        _cache.Set(key, true, options);
        return true;
    }
}
