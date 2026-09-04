using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PhotoPrint.API.Data;
using PhotoPrint.API.Exceptions;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services.Coupons;
using PhotoPrint.Tests.Helpers;
using Xunit;

namespace PhotoPrint.Tests.Unit.Services.Coupons;

public class CouponServiceTests : IDisposable
{
    private readonly PhotoPrintDbContext _db;
    private readonly CouponService _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public CouponServiceTests()
    {
        _db = new PhotoPrintDbContext(
            new DbContextOptionsBuilder<PhotoPrintDbContext>()
                .UseInMemoryDatabase($"CouponSvc_{Guid.NewGuid():N}")
                .Options);
        _sut = TestCoupons.ServiceFor(_db);
    }

    public void Dispose() => _db.Dispose();

    private async Task<Coupon> SeedAsync(Coupon coupon)
    {
        _db.Coupons.Add(coupon);
        await _db.SaveChangesAsync();
        return coupon;
    }

    [Fact]
    public async Task ApplyToCart_ValidCode_StoresItAgainstTheOwner()
    {
        var coupon = await SeedAsync(TestCoupons.Make(code: "VARA25", value: 15m));

        var result = await _sut.ApplyToCartAsync(_userId, null, "vara25", 100m);

        result.Code.Should().Be("VARA25");
        result.DiscountRon.Should().Be(15.00m);
        _db.CartCoupons.Single(cc => cc.UserId == _userId).CouponId.Should().Be(coupon.Id);
    }

    [Fact]
    public async Task ApplyToCart_LowerCaseAndPaddedInput_MatchesTheStoredCode()
    {
        await SeedAsync(TestCoupons.Make(code: "VARA25"));

        var result = await _sut.ApplyToCartAsync(_userId, null, "  vara25 ", 100m);

        result.Code.Should().Be("VARA25");
    }

    [Fact]
    public async Task ApplyToCart_UnknownCode_Returns422InvalidCoupon()
    {
        var act = () => _sut.ApplyToCartAsync(_userId, null, "NOSUCH", 100m);

        (await act.Should().ThrowAsync<CouponRejectedException>())
            .Which.ErrorCode.Should().Be(CouponErrorCodes.InvalidCoupon);
    }

    [Fact]
    public async Task ApplyToCart_InactiveCode_IsIndistinguishableFromUnknown()
    {
        await SeedAsync(TestCoupons.Make(code: "OFFCODE", isActive: false));

        var act = () => _sut.ApplyToCartAsync(_userId, null, "OFFCODE", 100m);

        (await act.Should().ThrowAsync<CouponRejectedException>())
            .Which.ErrorCode.Should().Be(CouponErrorCodes.InvalidCoupon);
    }

    [Fact]
    public async Task ApplyToCart_ExpiredCode_IsIndistinguishableFromUnknown()
    {
        await SeedAsync(TestCoupons.Make(
            code: "OLDCODE",
            validFrom: DateTimeOffset.UtcNow.AddDays(-10),
            validUntil: DateTimeOffset.UtcNow.AddDays(-1)));

        var act = () => _sut.ApplyToCartAsync(_userId, null, "OLDCODE", 100m);

        (await act.Should().ThrowAsync<CouponRejectedException>())
            .Which.ErrorCode.Should().Be(CouponErrorCodes.InvalidCoupon);
    }

    [Fact]
    public async Task ApplyToCart_NotYetValidCode_IsIndistinguishableFromUnknown()
    {
        await SeedAsync(TestCoupons.Make(
            code: "SOONCODE",
            validFrom: DateTimeOffset.UtcNow.AddDays(1),
            validUntil: DateTimeOffset.UtcNow.AddDays(10)));

        var act = () => _sut.ApplyToCartAsync(_userId, null, "SOONCODE", 100m);

        (await act.Should().ThrowAsync<CouponRejectedException>())
            .Which.ErrorCode.Should().Be(CouponErrorCodes.InvalidCoupon);
    }

    [Fact]
    public async Task ApplyToCart_BelowMinimumSubtotal_Returns422MinSubtotalNotMet()
    {
        await SeedAsync(TestCoupons.Make(code: "BIGONLY", minSubtotalRon: 200m));

        var act = () => _sut.ApplyToCartAsync(_userId, null, "BIGONLY", 100m);

        var thrown = (await act.Should().ThrowAsync<CouponRejectedException>()).Which;
        thrown.ErrorCode.Should().Be(CouponErrorCodes.MinSubtotalNotMet);
        thrown.Message.Should().Contain("200,00");
    }

    [Fact]
    public async Task ApplyToCart_EmptyCart_Returns422EmptyCart()
    {
        await SeedAsync(TestCoupons.Make(code: "VARA25"));

        var act = () => _sut.ApplyToCartAsync(_userId, null, "VARA25", 0m);

        (await act.Should().ThrowAsync<CouponRejectedException>())
            .Which.ErrorCode.Should().Be(CouponErrorCodes.EmptyCart);
    }

    [Fact]
    public async Task ApplyToCart_ExhaustedCode_Returns422CouponExhausted()
    {
        await SeedAsync(TestCoupons.Make(code: "SOLDOUT", maxRedemptions: 5, redemptionsCount: 5));

        var act = () => _sut.ApplyToCartAsync(_userId, null, "SOLDOUT", 100m);

        (await act.Should().ThrowAsync<CouponRejectedException>())
            .Which.ErrorCode.Should().Be(CouponErrorCodes.CouponExhausted);
    }

    [Fact]
    public async Task ApplyToCart_Twice_ReplacesTheFirstCodeWithoutStacking()
    {
        await SeedAsync(TestCoupons.Make(code: "FIRST", value: 5m));
        var second = await SeedAsync(TestCoupons.Make(code: "SECOND", value: 9m));

        await _sut.ApplyToCartAsync(_userId, null, "FIRST", 100m);
        await _sut.ApplyToCartAsync(_userId, null, "SECOND", 100m);

        var stored = _db.CartCoupons.Where(cc => cc.UserId == _userId).ToList();
        stored.Should().ContainSingle();
        stored[0].CouponId.Should().Be(second.Id);
    }

    [Fact]
    public async Task ClearCartCoupon_RemovesTheStoredRow_AndIsANoOpWhenNoneApplied()
    {
        await SeedAsync(TestCoupons.Make(code: "VARA25"));
        await _sut.ApplyToCartAsync(_userId, null, "VARA25", 100m);

        await _sut.ClearCartCouponAsync(_userId, null);
        await _sut.ClearCartCouponAsync(_userId, null);

        _db.CartCoupons.Any(cc => cc.UserId == _userId).Should().BeFalse();
    }

    [Fact]
    public async Task ResolveForCart_WhenCouponStopsQualifying_ReportsStale_AndWritesNothing()
    {
        await SeedAsync(TestCoupons.Make(code: "BIGONLY", minSubtotalRon: 200m));
        _db.CartCoupons.Add(new CartCoupon
        {
            UserId = _userId,
            CouponId = _db.Coupons.Single(c => c.Code == "BIGONLY").Id,
        });
        await _db.SaveChangesAsync();

        var resolved = await _sut.ResolveForCartAsync(_userId, null, 100m);

        resolved.Should().NotBeNull();
        resolved!.IsStale.Should().BeTrue();
        resolved.ReasonCode.Should().Be(CouponErrorCodes.MinSubtotalNotMet);
        resolved.Code.Should().Be("BIGONLY");
        resolved.DiscountRon.Should().Be(0m);
        _db.CartCoupons.Any(cc => cc.UserId == _userId).Should().BeTrue();
    }

    [Fact]
    public async Task ResolveForCart_BackAboveTheMinimum_ReportsValidAgain()
    {
        await SeedAsync(TestCoupons.Make(
            code: "BIGONLY", type: CouponType.Percent, value: 10m, minSubtotalRon: 200m));
        _db.CartCoupons.Add(new CartCoupon
        {
            UserId = _userId,
            CouponId = _db.Coupons.Single(c => c.Code == "BIGONLY").Id,
        });
        await _db.SaveChangesAsync();

        (await _sut.ResolveForCartAsync(_userId, null, 100m))!.IsStale.Should().BeTrue();

        var recovered = await _sut.ResolveForCartAsync(_userId, null, 250m);

        recovered.Should().NotBeNull();
        recovered!.IsStale.Should().BeFalse();
        recovered.ReasonCode.Should().BeNull();
        recovered.DiscountRon.Should().Be(25m);
    }

    [Fact]
    public async Task ResolveForOrder_FreeShippingWithNoShippingCost_ResolvesToNoCoupon()
    {
        await SeedAsync(TestCoupons.Make(code: "FREESHIP", type: CouponType.FreeShipping, value: 1m));
        await _sut.ApplyToCartAsync(_userId, null, "FREESHIP", 50m);

        var resolved = await _sut.ResolveForOrderAsync(_userId, null, 50m, 0m);

        resolved.Should().BeNull();
    }

    [Fact]
    public async Task ResolveForOrder_CouponDeactivatedAfterApply_Throws409InvalidCoupon()
    {
        var coupon = await SeedAsync(TestCoupons.Make(code: "VARA25"));
        await _sut.ApplyToCartAsync(_userId, null, "VARA25", 100m);

        coupon.IsActive = false;
        await _db.SaveChangesAsync();

        var act = () => _sut.ResolveForOrderAsync(_userId, null, 100m, 20m);

        (await act.Should().ThrowAsync<CouponConflictException>())
            .Which.ErrorCode.Should().Be(CouponErrorCodes.InvalidCoupon);
    }

    [Fact]
    public async Task Consume_AtTheCap_RefusesWithExhausted_AndLeavesTheCountAlone()
    {
        var coupon = await SeedAsync(TestCoupons.Make(maxRedemptions: 2, redemptionsCount: 2));

        var act = () => _sut.ConsumeOrThrowAsync(coupon.Id);

        (await act.Should().ThrowAsync<CouponConflictException>())
            .Which.ErrorCode.Should().Be(CouponErrorCodes.CouponExhausted);
        _db.Coupons.Single(c => c.Id == coupon.Id).RedemptionsCount.Should().Be(2);
    }

    [Fact]
    public async Task Consume_DeactivatedCoupon_ReportsInvalidRatherThanExhausted()
    {
        var coupon = await SeedAsync(TestCoupons.Make(isActive: false));

        var act = () => _sut.ConsumeOrThrowAsync(coupon.Id);

        (await act.Should().ThrowAsync<CouponConflictException>())
            .Which.ErrorCode.Should().Be(CouponErrorCodes.InvalidCoupon);
    }

    [Fact]
    public async Task Consume_UnlimitedCoupon_IncrementsTheCount()
    {
        var coupon = await SeedAsync(TestCoupons.Make(maxRedemptions: null));

        await _sut.ConsumeOrThrowAsync(coupon.Id);

        _db.Coupons.Single(c => c.Id == coupon.Id).RedemptionsCount.Should().Be(1);
    }

    [Fact]
    public async Task ReleaseForOrder_RemovesTheRedemption_AndDecrementsOnce()
    {
        var coupon = await SeedAsync(TestCoupons.Make(maxRedemptions: 5, redemptionsCount: 1));
        var orderId = Guid.NewGuid();
        _db.CouponRedemptions.Add(new CouponRedemption
        {
            CouponId = coupon.Id,
            OrderId = orderId,
            DiscountRon = 10m,
        });
        await _db.SaveChangesAsync();

        await _sut.ReleaseForOrderAsync(orderId);
        await _sut.ReleaseForOrderAsync(orderId);

        _db.CouponRedemptions.Any(r => r.OrderId == orderId).Should().BeFalse();
        _db.Coupons.Single(c => c.Id == coupon.Id).RedemptionsCount.Should().Be(0);
    }

    [Fact]
    public async Task ReleaseForOrder_NeverDrivesTheCountBelowZero()
    {
        var coupon = await SeedAsync(TestCoupons.Make(redemptionsCount: 0));
        var orderId = Guid.NewGuid();
        _db.CouponRedemptions.Add(new CouponRedemption
        {
            CouponId = coupon.Id,
            OrderId = orderId,
            DiscountRon = 10m,
        });
        await _db.SaveChangesAsync();

        await _sut.ReleaseForOrderAsync(orderId);

        _db.Coupons.Single(c => c.Id == coupon.Id).RedemptionsCount.Should().Be(0);
    }

    [Fact]
    public async Task TransferGuestCoupon_MovesTheCodeOntoTheUsersCart()
    {
        var guestSessionId = Guid.NewGuid();
        var coupon = await SeedAsync(TestCoupons.Make(code: "VARA25"));
        _db.CartCoupons.Add(new CartCoupon { GuestSessionId = guestSessionId, CouponId = coupon.Id });
        await _db.SaveChangesAsync();

        await _sut.TransferGuestCouponAsync(_userId, guestSessionId);

        var moved = _db.CartCoupons.Single();
        moved.UserId.Should().Be(_userId);
        moved.GuestSessionId.Should().BeNull();
    }

    [Fact]
    public async Task TransferGuestCoupon_WhenTheUserAlreadyHasOne_KeepsTheUsersAndDropsTheGuests()
    {
        var guestSessionId = Guid.NewGuid();
        var guestCoupon = await SeedAsync(TestCoupons.Make(code: "GUESTONE"));
        var userCoupon = await SeedAsync(TestCoupons.Make(code: "USERONE"));
        _db.CartCoupons.Add(new CartCoupon { GuestSessionId = guestSessionId, CouponId = guestCoupon.Id });
        _db.CartCoupons.Add(new CartCoupon { UserId = _userId, CouponId = userCoupon.Id });
        await _db.SaveChangesAsync();

        await _sut.TransferGuestCouponAsync(_userId, guestSessionId);

        var remaining = _db.CartCoupons.Single();
        remaining.UserId.Should().Be(_userId);
        remaining.CouponId.Should().Be(userCoupon.Id);
    }
}
