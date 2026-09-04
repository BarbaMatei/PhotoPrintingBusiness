using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PhotoPrint.API.Extensions;

namespace PhotoPrint.Tests.Unit.Configuration;

public class ForwardedHeadersOptionsTests
{
    [Fact]
    public void The_framework_loopback_defaults_are_cleared()
    {
        var options = BuildOptions("172.28.0.2");

        options.KnownProxies.Should().ContainSingle()
            .Which.Should().Be(IPAddress.Parse("172.28.0.2"));
        options.KnownNetworks.Should().BeEmpty();
    }

    [Fact]
    public void Nothing_is_trusted_when_no_proxy_is_configured()
    {
        var options = BuildOptions();

        options.KnownProxies.Should().BeEmpty();
        options.KnownNetworks.Should().BeEmpty();
    }

    [Fact]
    public void Only_one_hop_is_read_from_the_forwarded_chain()
    {
        var options = BuildOptions("172.28.0.2");

        options.ForwardLimit.Should().Be(1);
    }

    [Fact]
    public void Only_the_for_and_proto_headers_are_honoured()
    {
        var options = BuildOptions("172.28.0.2");

        options.ForwardedHeaders.Should()
            .Be(ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);
    }

    [Fact]
    public void A_cidr_entry_becomes_a_known_network_that_matches_its_members()
    {
        var options = BuildOptions("172.28.0.2/31");

        options.KnownNetworks.Should().ContainSingle();
        options.KnownNetworks[0].Contains(IPAddress.Parse("172.28.0.3")).Should().BeTrue();
        options.KnownNetworks[0].Contains(IPAddress.Parse("172.28.0.7")).Should().BeFalse();
    }

    private static ForwardedHeadersOptions BuildOptions(params string[] trustedProxies)
    {
        var values = new Dictionary<string, string?>();
        for (var i = 0; i < trustedProxies.Length; i++)
            values[$"ForwardedHeaders:TrustedProxies:{i}"] = trustedProxies[i];

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();

        return new ServiceCollection()
            .AddTrustedProxyForwardedHeaders(configuration)
            .BuildServiceProvider()
            .GetRequiredService<IOptions<ForwardedHeadersOptions>>()
            .Value;
    }
}
