using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using PhotoPrint.Tests.Helpers;

namespace PhotoPrint.Tests.Unit.Configuration;

public class DeploymentDefaultsTests
{
    private const string PinnedProxyAddress = "172.28.0.2";
    private const string PinnedSubnet       = "172.28.0.0/24";
    private const string StaleBridgeSubnet  = "172.20.0.0/16";

    [Fact]
    public void EnvExample_ShipsTrustedProxyMatchingCompose()
    {
        var shipped = EnvExampleValues("ForwardedHeaders__TrustedProxies__");

        shipped.Should().ContainSingle(
            "a deploy following the runbook must boot with the proxy trusted, not with an empty list");
        shipped[0].Should().Be(PinnedProxyAddress);
        shipped[0].Should().Be(CaddyStaticAddress());
    }

    [Fact]
    public void EnvExample_SizesThePublicRateLimitBudgetForAPageLoad()
    {
        var shipped = EnvExampleValues("RateLimit__Public__PermitLimit");

        shipped.Should().ContainSingle(
            "trusting the proxy makes this budget per-client for the first time, so the "
            + "shipped default must fit a page load's asset requests");
        int.Parse(shipped[0]).Should().BeGreaterThanOrEqualTo(600);
    }

    [Fact]
    public void ProductionSerilogConfig_HasConsoleSink()
    {
        using var document = JsonDocument.Parse(
            RepoFiles.ReadAllText("src", "PhotoPrint.API", "appsettings.json"));

        var sinks = document.RootElement
            .GetProperty("Serilog").GetProperty("WriteTo").EnumerateArray()
            .Select(sink => sink.GetProperty("Name").GetString())
            .ToList();

        sinks.Should().Contain(
            "Console",
            "the runbook verifies this bolt with `docker compose logs api | grep …`, which reads stdout");
    }

    [Fact]
    public void EnvExampleScrapeIpsMatchComposeSubnet()
    {
        var subnet   = ComposeIpamValue("subnet");
        var declared = IPNetwork.Parse(subnet);
        var examples = EnvExampleValues("Observability__Metrics__AllowedScrapeIps__", includeCommented: true)
            .Where(value => value.Contains('/'))
            .ToList();

        subnet.Should().Be(PinnedSubnet);
        examples.Should().NotBeEmpty("§14.5 offers a range example for the Compose network");
        examples.Should().NotContain(StaleBridgeSubnet);

        foreach (var example in examples)
        {
            var range = IPNetwork.Parse(example);
            declared.Contains(range.BaseAddress).Should().BeTrue(
                "{0} must sit inside the pinned Compose subnet or the allow-list can never match",
                example);
            range.PrefixLength.Should().BeGreaterThanOrEqualTo(declared.PrefixLength);
        }
    }

    [Fact]
    public void ProdComposeStaticProxyAddressIsOutsideDynamicPool()
    {
        var pool  = ComposeIpamValue("ip_range");
        var caddy = IPAddress.Parse(CaddyStaticAddress());

        pool.Should().NotBeNullOrWhiteSpace(
            "without a reserved pool Docker hands caddy's pinned address to whichever "
            + "container starts first, and caddy then fails to bind");
        IPNetwork.Parse(pool).Contains(caddy).Should().BeFalse();
        IPNetwork.Parse(PinnedSubnet).Contains(caddy).Should().BeTrue();
    }

    private static string CaddyStaticAddress()
    {
        var block = ComposeServiceBlock("caddy");
        var match = Regex.Match(block, @"^\s*ipv4_address:\s*(\S+)", RegexOptions.Multiline);

        match.Success.Should().BeTrue("caddy's address is what TrustedProxies names");
        return match.Groups[1].Value;
    }

    private static string ComposeIpamValue(string key)
    {
        var text  = Compose();
        var ipam  = text[text.IndexOf("ipam:", StringComparison.Ordinal)..];
        var match = Regex.Match(ipam, $@"^\s*-?\s*{Regex.Escape(key)}:\s*(\S+)", RegexOptions.Multiline);

        return match.Success ? match.Groups[1].Value : string.Empty;
    }

    private static string ComposeServiceBlock(string service)
    {
        var text  = Compose();
        var start = text.IndexOf($"\n  {service}:", StringComparison.Ordinal);
        start.Should().BeGreaterThan(-1, "service '{0}' is declared", service);

        var next = Regex.Match(text[(start + 1)..], @"^  [a-z][a-z0-9_-]*:", RegexOptions.Multiline);
        var body = text[(start + 1)..];
        return next.Success && next.Index > 0 ? body[..next.Index] : body;
    }

    private static string Compose() => RepoFiles.ReadAllText("docker-compose.prod.yml");

    private static List<string> EnvExampleValues(string keyPrefix, bool includeCommented = false)
    {
        var pattern = includeCommented
            ? $@"^\s*#?\s*{Regex.Escape(keyPrefix)}\S*=([^\s#]*)"
            : $@"^{Regex.Escape(keyPrefix)}\S*=([^\s#]*)";

        return Regex.Matches(RepoFiles.ReadAllText(".env.example"), pattern, RegexOptions.Multiline)
            .Select(m => m.Groups[1].Value.Trim())
            .Where(value => value.Length > 0)
            .ToList();
    }
}
