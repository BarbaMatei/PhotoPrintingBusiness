using Microsoft.Extensions.Caching.Memory;

namespace PhotoPrint.API.Services.Invoicing.Anaf;

public sealed class AnafOutageRegistry : MemoryCacheOnceRegistry
{
    public AnafOutageRegistry(IMemoryCache cache) : base(cache)
    {
    }

    public bool MarkOutageOnce(string outageKey, TimeSpan window) =>
        MarkOnce($"anaf.upload-job.outage::{outageKey}",
            new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = window });
}
