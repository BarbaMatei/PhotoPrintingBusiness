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
    public void ProductionSerilogConfig_ShipsConsoleAndFileSinks()
    {
        var sinks = MergedSinkNames("appsettings.Production.json");

        sinks.Should().Contain(
            "Console",
            "the runbook verifies this bolt with `docker compose logs api | grep …`, which reads stdout");
        sinks.Should().Contain("File", "production keeps the rolling audit trail on disk");
    }

    [Theory]
    [InlineData("appsettings.Development.json")]
    [InlineData("appsettings.Testing.json")]
    public void NonProductionSerilogConfigs_WriteNoLogFiles(string overlay)
    {
        MergedSinkNames(overlay).Should().NotContain(
            "File",
            "configuration arrays merge by index and cannot be truncated, so a File sink left in "
            + "the base opens logs/*.json in every dev run and every test host");
    }

    [Fact]
    public void EnvExampleScrapeIpsSitInsideComposeDynamicPool()
    {
        var pool     = IPNetwork.Parse(ComposeIpamValue("ip_range"));
        var caddy    = IPAddress.Parse(CaddyStaticAddress());
        var examples = EnvExampleValues("Observability__Metrics__AllowedScrapeIps__", includeCommented: true)
            .Where(value => value.Contains('/'))
            .ToList();

        ComposeIpamValue("subnet").Should().Be(PinnedSubnet);
        examples.Should().NotBeEmpty("§14.5 offers a range example for the Compose network");
        examples.Should().NotContain(StaleBridgeSubnet);
        examples.Should().NotContain(
            PinnedSubnet,
            "the whole subnet spans the reverse proxy's pinned address, which the same file forbids allow-listing");

        foreach (var example in examples)
        {
            var range = IPNetwork.Parse(example);
            pool.Contains(range.BaseAddress).Should().BeTrue(
                "{0} must sit inside the pool Compose allocates from, or the allow-list can never match",
                example);
            range.PrefixLength.Should().BeGreaterThanOrEqualTo(pool.PrefixLength);
            range.Contains(caddy).Should().BeFalse(
                "{0} covers the reverse proxy, whose address every proxied request carries",
                example);
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

    [Fact]
    public void DirectoryPackagesProps_PromotesAuditWarningsToErrors()
    {
        var props = RepoFiles.ReadAllText("Directory.Packages.props");

        props.Should().Contain("<NuGetAuditMode>all</NuGetAuditMode>",
            "the SDK audits only direct packages by default, so transitive pins go unscanned");
        props.Should().Contain("<NuGetAuditLevel>low</NuGetAuditLevel>");

        var promotion = props
            .Split('\n')
            .Single(line => line.Contains("<WarningsAsErrors") && line.Contains("FailOnAudit"));

        foreach (var code in new[] { "NU1901", "NU1902", "NU1903", "NU1904", "NU1905" })
        {
            promotion.Should().Contain(code);
        }
    }

    [Fact]
    public void CiRestore_HardFailsOnAuditWarnings()
    {
        var ci = RepoFiles.ReadAllText(".github", "workflows", "ci.yml");

        ci.Should().Contain("dotnet restore PhotoPrint.sln -p:FailOnAudit=true",
            "the props promote audit warnings only when that switch is passed, so without it "
            + "on the CI restore a new advisory reaches main as a warning nobody reads");
        ci.Should().NotContain("dotnet list package --vulnerable",
            "that command exits 0 on findings, so it cannot be the gate");
    }

    private static List<string?> MergedSinkNames(string overlay)
    {
        var shipped  = SinkNames("appsettings.json");
        var overlaid = SinkNames(overlay);

        return shipped
            .Select((name, index) => index < overlaid.Count ? overlaid[index] : name)
            .Concat(overlaid.Skip(shipped.Count))
            .ToList();
    }

    private static List<string?> SinkNames(string file)
    {
        using var document = JsonDocument.Parse(
            RepoFiles.ReadAllText("src", "PhotoPrint.API", file));

        return document.RootElement.TryGetProperty("Serilog", out var serilog)
            && serilog.TryGetProperty("WriteTo", out var sinks)
                ? sinks.EnumerateArray().Select(sink => sink.GetProperty("Name").GetString()).ToList()
                : [];
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
