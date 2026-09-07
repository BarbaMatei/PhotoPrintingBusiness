using System.Net;
using Microsoft.Extensions.Options;
using PhotoPrint.API.Observability;

namespace PhotoPrint.API.Configuration;

public sealed class TrustedProxyList
{
    private readonly ScrapeIpAllowList _entries;

    public TrustedProxyList(IOptions<ForwardedHeadersSettings> settings) =>
        _entries = ScrapeIpAllowList.Parse(settings.Value.TrustedProxies, out _);

    public IReadOnlyCollection<IPAddress> Addresses => _entries.Addresses;

    public IReadOnlyList<IPNetwork> Networks => _entries.Networks;

    public bool Trusts(IPAddress? peer) => _entries.Contains(peer);
}
