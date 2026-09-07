using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.RateLimiting;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Controllers;
using PhotoPrint.API.Extensions;
using Xunit;

namespace PhotoPrint.Tests.Integration;

public class CouponRateLimitIntegrationTests
{
    [Fact]
    public void ApplyCoupon_CarriesThePerIdentityRateLimitPolicy()
    {
        var attribute = typeof(CartController)
            .GetMethod(nameof(CartController.ApplyCouponAsync), BindingFlags.Public | BindingFlags.Instance)!
            .GetCustomAttribute<EnableRateLimitingAttribute>();

        attribute.Should().NotBeNull();
        attribute!.PolicyName.Should().Be(SecurityExtensions.CouponRateLimitPolicy);
    }

    [Fact]
    public void CouponRateLimit_DefaultsToFifteenAttemptsPerWindow()
    {
        var settings = new RateLimitSettings();

        settings.Coupon.PermitLimit.Should().Be(15);
        settings.WindowSeconds.Should().Be(60);
    }
}
