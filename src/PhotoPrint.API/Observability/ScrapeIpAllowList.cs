using System.Collections.Frozen;
using System.Net;
using System.Net.Sockets;

namespace PhotoPrint.API.Observability;

public sealed class ScrapeIpAllowList
{
    private readonly FrozenSet<IPAddress> _addresses;
    private readonly IPNetwork[] _networks;

    private ScrapeIpAllowList(FrozenSet<IPAddress> addresses, IPNetwork[] networks)
    {
        _addresses = addresses;
        _networks  = networks;
    }

    public static ScrapeIpAllowList Parse(IReadOnlyList<string>? entries, out IReadOnlyList<string> errors)
    {
        var addresses = new HashSet<IPAddress>();
        var networks  = new List<IPNetwork>();
        var failures  = new List<string>();

        foreach (var raw in entries ?? [])
        {
            var entry = (raw ?? string.Empty).Trim();

            if (entry.Length == 0)
            {
                failures.Add("an empty entry is not an IP address or CIDR range.");
            }
            else if (entry.Contains('/'))
            {
                if (!IPNetwork.TryParse(entry, out var network) || !RoundTrips(entry, network.ToString()))
                    failures.Add(CidrFailure(entry));
                else if (network.BaseAddress.IsIPv4MappedToIPv6)
                    failures.Add($"'{entry}' is an IPv4-mapped IPv6 range; peers are compared in "
                                 + "their IPv4 form, so it would match nothing — write it as an IPv4 range.");
                else
                    networks.Add(network);
            }
            else if (IPAddress.TryParse(entry, out var address) && RoundTrips(entry, address.ToString()))
            {
                addresses.Add(Canonicalize(address));
            }
            else
            {
                failures.Add($"'{entry}' is not an IP address or CIDR range.");
            }
        }

        errors = failures;
        return new ScrapeIpAllowList(addresses.ToFrozenSet(), [.. networks]);
    }

    public IReadOnlyCollection<IPAddress> Addresses => _addresses;

    public IReadOnlyList<IPNetwork> Networks => Array.AsReadOnly(_networks);

    public bool Contains(IPAddress? address)
    {
        if (address is null)
            return false;

        var peer = Canonicalize(address);
        if (_addresses.Contains(peer))
            return true;

        foreach (var network in _networks)
        {
            if (network.BaseAddress.AddressFamily == peer.AddressFamily && network.Contains(peer))
                return true;
        }

        return false;
    }

    public static IPAddress Canonicalize(IPAddress address)
    {
        if (address.AddressFamily != AddressFamily.InterNetworkV6)
            return address;

        // A dual-mode socket delivers IPv4 peers as ::ffff:a.b.c.d, and a link-local peer carries
        // a scope id (fe80::1%3) that IPAddress.Equals compares. Neither matches a plain entry.
        if (address.IsIPv4MappedToIPv6)
            return address.MapToIPv4();

        return address.ScopeId != 0 ? new IPAddress(address.GetAddressBytes()) : address;
    }

    // .NET still accepts inet_aton forms: "10" parses as 0.0.0.10 and "010.0.0.1" as octal
    // 8.0.0.1, so a typo would otherwise become a valid entry that silently matches nothing.
    private static bool RoundTrips(string entry, string parsed) =>
        entry.Contains(':') || string.Equals(entry, parsed, StringComparison.Ordinal);

    private static string CidrFailure(string entry)
    {
        var masked = MaskedForm(entry);
        return masked is null
            ? $"'{entry}' is not an IP address or CIDR range."
            : $"'{entry}' is not a valid CIDR range — the base address must have all host bits "
              + $"zero, so write '{masked}'.";
    }

    private static string? MaskedForm(string entry)
    {
        var slash = entry.IndexOf('/');
        if (slash < 0
            || !IPAddress.TryParse(entry[..slash], out var address)
            // Without this the octal reading of "010.0.0.0" would suggest "8.0.0.0/16" — a real,
            // public network the operator never meant.
            || !RoundTrips(entry[..slash], address.ToString())
            || !int.TryParse(entry[(slash + 1)..], out var prefix))
        {
            return null;
        }

        var bytes = address.GetAddressBytes();
        if (prefix < 0 || prefix > bytes.Length * 8)
            return null;

        for (var i = 0; i < bytes.Length; i++)
        {
            var bitsInByte = Math.Clamp(prefix - (i * 8), 0, 8);
            bytes[i] &= (byte)(0xFF << (8 - bitsInByte));
        }

        return $"{new IPAddress(bytes)}/{prefix}";
    }
}
