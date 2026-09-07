using System.Net;
using FluentAssertions;
using Xunit;

namespace PhotoPrint.Tests.Integration;

/// <summary>
/// Rate limit tests use their own factory instance (separate from SecurityHeadersIntegrationTests)
/// so that the per-IP request counters are isolated and cannot bleed between test classes.
/// </summary>
public class RateLimitIntegrationTests
{
    // Low permit limit so 429 is triggered quickly without hundreds of requests
    private readonly HttpClient _client = new SecurityBaselineFactory { PublicPermitLimit = 3 }
        .CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false,
        });

    [Fact]
    public async Task RateLimit_ExceedsPublicLimit_Returns429()
    {
        // Arrange — permit limit is 3; send 4 requests
        HttpResponseMessage? lastResponse = null;

        for (var i = 0; i < 4; i++)
        {
            lastResponse = await _client.GetAsync("/health");
        }

        // Assert — the 4th request should be rejected
        ((int)lastResponse!.StatusCode).Should().Be(429);
    }

    [Fact]
    public async Task RateLimit_ExceedsPublicLimit_ResponseIncludesRetryAfterHeader()
    {
        // Exhaust the limit (permit limit = 3)
        for (var i = 0; i < 3; i++)
            await _client.GetAsync("/health");

        var rejected = await _client.GetAsync("/health");

        // Retry-After must be present and a positive integer
        rejected.Headers.TryGetValues("Retry-After", out var values).Should().BeTrue();
        int.TryParse(values!.First(), out var retryAfterSeconds).Should().BeTrue();
        retryAfterSeconds.Should().BePositive();
    }
}

public class ForwardedClientRateLimitTests
{
    private const string ProxyAddress = "172.28.0.2";

    [Fact]
    public async Task RateLimit_PartitionsPerForwardedClient()
    {
        using var factory = new ForwardedClientRateLimitFactory();

        for (var i = 0; i < 3; i++)
            (await Send(factory, "203.0.113.1")).Should().Be(HttpStatusCode.OK);

        var fourthFromTheSameClient = await Send(factory, "203.0.113.1");
        var firstFromAnotherClient  = await Send(factory, "198.51.100.7");

        fourthFromTheSameClient.Should().Be(HttpStatusCode.TooManyRequests);
        firstFromAnotherClient.Should().Be(HttpStatusCode.OK);
    }

    private static Task<HttpStatusCode> Send(ForwardedClientRateLimitFactory factory, string client) =>
        factory.SendForwardedAsync(peer: ProxyAddress, forwardedFor: client, path: "/health");
}

internal sealed class ForwardedClientRateLimitFactory : TrustedProxyFactory
{
    public ForwardedClientRateLimitFactory() : base("172.28.0.2") { }

    protected override Dictionary<string, string?> ExtraConfig()
    {
        var config = base.ExtraConfig();
        config["RateLimit:Public:PermitLimit"] = "3";
        return config;
    }
}
