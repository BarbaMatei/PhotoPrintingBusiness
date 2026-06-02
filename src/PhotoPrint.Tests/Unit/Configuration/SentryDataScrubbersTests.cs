using FluentAssertions;
using PhotoPrint.API.Configuration;
using Sentry;

namespace PhotoPrint.Tests.Unit.Configuration;

/// <summary>
/// Pins the scrubber contract. Every event leaving the SDK passes through
/// <see cref="SentryDataScrubbers.Scrub"/> — if a sensitive value ever reaches
/// Sentry, the list of sensitive keys or this test is wrong.
/// </summary>
public class SentryDataScrubbersTests
{
    [Fact]
    public void Scrub_replaces_request_body_with_marker()
    {
        var e = new SentryEvent { Request = new SentryRequest { Data = "raw body with PII" } };
        var result = SentryDataScrubbers.Scrub(e);

        result.Should().NotBeNull();
        result!.Request!.Data!.ToString().Should().Be(SentryDataScrubbers.ScrubbedBodyMarker);
    }

    [Theory]
    [InlineData("Authorization")]
    [InlineData("Cookie")]
    [InlineData("Set-Cookie")]
    [InlineData("X-Guest-Token")]
    public void Scrub_strips_sensitive_headers(string header)
    {
        var e = new SentryEvent { Request = new SentryRequest() };
        e.Request.Headers[header] = "secret-value-xyz";

        SentryDataScrubbers.Scrub(e);

        e.Request.Headers[header].Should().Be(SentryDataScrubbers.ScrubbedMarker);
    }

    [Fact]
    public void Scrub_keeps_non_sensitive_headers()
    {
        var e = new SentryEvent { Request = new SentryRequest() };
        e.Request.Headers["User-Agent"] = "Mozilla/5.0";
        e.Request.Headers["X-Correlation-Id"] = "abc-123";

        SentryDataScrubbers.Scrub(e);

        e.Request.Headers["User-Agent"].Should().Be("Mozilla/5.0");
        e.Request.Headers["X-Correlation-Id"].Should().Be("abc-123");
    }

    [Theory]
    [InlineData("email")]
    [InlineData("Email")]
    [InlineData("userEmail")]
    [InlineData("phone")]
    [InlineData("password")]
    [InlineData("confirmPassword")]
    [InlineData("CurrentPassword")]
    public void Scrub_strips_extras_with_sensitive_keys(string key)
    {
        var e = new SentryEvent();
        e.SetExtra(key, "sensitive-value");
        e.SetExtra("safe", "ok");

        SentryDataScrubbers.Scrub(e);

        e.Extra[key].Should().Be(SentryDataScrubbers.ScrubbedMarker);
        e.Extra["safe"].Should().Be("ok");
    }

    [Fact]
    public void IsSensitiveKey_returns_true_for_substring_match()
    {
        SentryDataScrubbers.IsSensitiveKey("user.email.normalized").Should().BeTrue();
        SentryDataScrubbers.IsSensitiveKey("PHONE_NUMBER").Should().BeTrue();
        SentryDataScrubbers.IsSensitiveKey("newPassword").Should().BeTrue();
    }

    [Fact]
    public void IsSensitiveKey_returns_false_for_unrelated_keys()
    {
        SentryDataScrubbers.IsSensitiveKey("orderId").Should().BeFalse();
        SentryDataScrubbers.IsSensitiveKey("environment").Should().BeFalse();
        SentryDataScrubbers.IsSensitiveKey("correlation_id").Should().BeFalse();
    }

    [Fact]
    public void Scrub_handles_event_without_request_object()
    {
        var e = new SentryEvent();
        e.SetExtra("password", "secret");

        var result = SentryDataScrubbers.Scrub(e);

        result.Should().NotBeNull();
        result!.Extra["password"].Should().Be(SentryDataScrubbers.ScrubbedMarker);
    }
}
