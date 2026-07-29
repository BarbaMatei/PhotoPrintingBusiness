using FluentAssertions;
using PhotoPrint.API.Services.Sameday;

namespace PhotoPrint.Tests.Unit.Services.Sameday;

public class SamedayTokenTests
{
    [Fact]
    public void IsValid_returns_true_when_now_plus_safety_window_is_before_expiry()
    {
        var expiry = DateTimeOffset.UtcNow.AddHours(1);
        var token = new SamedayToken("abc", expiry);

        token.IsValid(DateTimeOffset.UtcNow).Should().BeTrue();
    }

    [Fact]
    public void IsValid_returns_false_when_inside_safety_window()
    {
        var now = new DateTimeOffset(2026, 6, 2, 12, 0, 0, TimeSpan.Zero);
        var expiry = now.AddSeconds(30); // 30s ahead, default window is 60s
        var token = new SamedayToken("abc", expiry);

        token.IsValid(now).Should().BeFalse();
    }

    [Fact]
    public void IsValid_with_custom_window_honours_it()
    {
        var now = new DateTimeOffset(2026, 6, 2, 12, 0, 0, TimeSpan.Zero);
        var expiry = now.AddSeconds(30);
        var token = new SamedayToken("abc", expiry);

        token.IsValid(now, safetyWindow: TimeSpan.FromSeconds(5)).Should().BeTrue();
        token.IsValid(now, safetyWindow: TimeSpan.FromMinutes(5)).Should().BeFalse();
    }

    [Fact]
    public void IsValid_returns_false_when_already_expired()
    {
        var now = DateTimeOffset.UtcNow;
        var token = new SamedayToken("abc", now.AddMinutes(-1));

        token.IsValid(now).Should().BeFalse();
    }

    [Fact]
    public void ToString_does_not_expose_token_value()
    {
        var token = new SamedayToken("super-secret-bearer-value", DateTimeOffset.UtcNow.AddHours(1));
        token.ToString().Should().NotContain("super-secret-bearer-value");
    }

    [Fact]
    public void ToString_exposes_expires_at()
    {
        var expiry = new DateTimeOffset(2026, 6, 2, 12, 0, 0, TimeSpan.Zero);
        var token = new SamedayToken("abc", expiry);
        token.ToString().Should().Contain("2026-06-02");
    }
}
