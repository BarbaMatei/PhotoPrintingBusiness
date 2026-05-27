using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace PhotoPrint.Tests.Integration;

public class SecurityHeadersIntegrationTests : IClassFixture<SecurityBaselineFactory>
{
    private readonly HttpClient _client;

    public SecurityHeadersIntegrationTests(SecurityBaselineFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false,
        });
    }

    // ── Security Headers ──────────────────────────────────────────────────────

    [Fact]
    public async Task SecurityHeaders_XContentTypeOptions_PresentOnEveryResponse()
    {
        var response = await _client.GetAsync("/health");
        response.Headers.TryGetValues("X-Content-Type-Options", out var values).Should().BeTrue();
        values!.First().Should().Be("nosniff");
    }

    [Fact]
    public async Task SecurityHeaders_XFrameOptions_PresentOnEveryResponse()
    {
        var response = await _client.GetAsync("/health");
        response.Headers.TryGetValues("X-Frame-Options", out var values).Should().BeTrue();
        values!.First().Should().Be("DENY");
    }

    [Fact]
    public async Task SecurityHeaders_ReferrerPolicy_PresentOnEveryResponse()
    {
        var response = await _client.GetAsync("/health");
        response.Headers.TryGetValues("Referrer-Policy", out var values).Should().BeTrue();
        values!.First().Should().Be("strict-origin-when-cross-origin");
    }

    [Fact]
    public async Task SecurityHeaders_ContentSecurityPolicy_PresentOnEveryResponse()
    {
        var response = await _client.GetAsync("/health");
        response.Headers.TryGetValues("Content-Security-Policy", out var values).Should().BeTrue();
        values!.First().Should().NotBeEmpty();
    }

    // ── CORS ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Cors_AllowedOrigin_ReturnsAccessControlAllowOriginHeader()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("Origin", "https://test.example.com");

        var response = await _client.SendAsync(request);

        response.Headers.TryGetValues("Access-Control-Allow-Origin", out var values).Should().BeTrue();
        values!.First().Should().Be("https://test.example.com");
    }

    [Fact]
    public async Task Cors_DisallowedOrigin_NoAccessControlAllowOriginHeader()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("Origin", "https://evil.attacker.com");

        var response = await _client.SendAsync(request);

        response.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse();
    }

    [Fact]
    public async Task Cors_Preflight_AllowedOrigin_ReturnsCorsHeaders()
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/health");
        request.Headers.Add("Origin", "https://test.example.com");
        request.Headers.Add("Access-Control-Request-Method", "GET");
        request.Headers.Add("Access-Control-Request-Headers", "Content-Type");

        var response = await _client.SendAsync(request);

        response.Headers.TryGetValues("Access-Control-Allow-Origin", out var origin).Should().BeTrue();
        origin!.First().Should().Be("https://test.example.com");

        // AllowCredentials must be true (required for HttpOnly refresh-token cookie)
        response.Headers.TryGetValues("Access-Control-Allow-Credentials", out var credentials).Should().BeTrue();
        credentials!.First().Should().Be("true");
    }
}
