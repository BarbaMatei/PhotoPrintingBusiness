using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Data;
using PhotoPrint.API.DTOs.Payments;
using PhotoPrint.API.DTOs.Shipping;
using PhotoPrint.API.Exceptions;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services;
using PhotoPrint.API.Services.Coupons;
using PhotoPrint.Tests.Helpers;
using Xunit;

namespace PhotoPrint.Tests.Unit.Services.Coupons;

public class OrderServiceCouponTests : IDisposable
{
    private const decimal ShippingCost = 20.00m;
    private const decimal VatRate = 0.19m;

    private readonly PhotoPrintDbContext _db;
    private readonly CouponService _coupons;
    private readonly OrderService _sut;

    public OrderServiceCouponTests()
    {
        _db = new PhotoPrintDbContext(
            new DbContextOptionsBuilder<PhotoPrintDbContext>()
                .UseInMemoryDatabase($"OrderCoupon_{Guid.NewGuid():N}")
                .Options);

        var numbers = new Mock<IOrderNumberService>();
        var counter = 0;
        numbers.Setup(s => s.GenerateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => $"FT-{Interlocked.Increment(ref counter):D8}");

        var shipping = new Mock<IShippingService>();
        shipping.Setup(s => s.GetShippingCostAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShippingCostDto(ShippingCost));

        var router = new Mock<IStorageRouter>();
        router.SetupGet(r => r.CloudEnabled).Returns(false);

        _coupons = TestCoupons.ServiceFor(_db);
        _sut = new OrderService(
            _db, numbers.Object, shipping.Object, router.Object, _coupons,
            Options.Create(new StorageSettings()),
            Options.Create(new VatSettings { Rate = VatRate }),
            Options.Create(new StripeSettings { MinimumChargeRon = 2.00m }));
    }

    public void Dispose() => _db.Dispose();

    private static CreateOrderRequest MakeRequest()
        => new(DeliveryType.Courier, null, new ShippingAddressSnapshot
        {
            RecipientName = "Test", Street = "Str. Test", Number = "1",
            City = "București", County = "Ilfov", PostalCode = "010101", Phone = "0700000000",
        });

    private async Task<Guid> SeedCartAsync(decimal unitPrice = 20.00m, int quantity = 5)
    {
        var userId = Guid.NewGuid();
        var graph = TestCartSeed.Build(userId: userId, unitPrice: unitPrice, quantity: quantity);
        graph.AddTo(_db);
        await _db.SaveChangesAsync();
        return userId;
    }

    private async Task<Coupon> SeedCouponAsync(Coupon coupon)
    {
        _db.Coupons.Add(coupon);
        await _db.SaveChangesAsync();
        return coupon;
    }

    [Fact]
    public async Task Checkout_WithoutCoupon_LeavesTheOrderUndiscounted()
    {
        var userId = await SeedCartAsync();

        var order = (await _sut.CreateFromCartAsync(userId, null, MakeRequest())).Order;

        order.CouponCode.Should().BeNull();
        order.DiscountRon.Should().Be(0m);
        order.TotalRon.Should().Be(order.SubtotalRon + order.ShippingCostRon);
        _db.CouponRedemptions.Should().BeEmpty();
    }

    [Fact]
    public async Task Checkout_WithDiscount_ExtractsVatAfterDiscount_NotBefore()
    {
        var userId = await SeedCartAsync(unitPrice: 20.00m, quantity: 5);
        await SeedCouponAsync(TestCoupons.Make(code: "VARA30", type: CouponType.Fixed, value: 30m));
        await _coupons.ApplyToCartAsync(userId, null, "VARA30", 100m);

        var order = (await _sut.CreateFromCartAsync(userId, null, MakeRequest())).Order;

        order.SubtotalRon.Should().Be(100.00m);
        order.ShippingCostRon.Should().Be(20.00m);
        order.DiscountRon.Should().Be(30.00m);
        order.TotalRon.Should().Be(90.00m);
        order.VatRon.Should().Be(14.37m);
        order.NetTotalRon.Should().Be(75.63m);
        order.VatRon.Should().NotBe(19.16m);
        (order.NetTotalRon + order.VatRon).Should().Be(order.TotalRon);
    }

    [Fact]
    public async Task Checkout_WithDiscount_KeepsTheMoneyInvariant()
    {
        var userId = await SeedCartAsync(unitPrice: 20.00m, quantity: 5);
        await SeedCouponAsync(TestCoupons.Make(code: "PROC10", type: CouponType.Percent, value: 10m));
        await _coupons.ApplyToCartAsync(userId, null, "PROC10", 100m);

        var order = (await _sut.CreateFromCartAsync(userId, null, MakeRequest())).Order;

        order.TotalRon.Should().Be(
            order.SubtotalRon + order.ShippingCostRon - order.DiscountRon);
    }

    [Fact]
    public async Task Checkout_WithCoupon_WritesTheRedemptionAndIncrementsTheCount()
    {
        var userId = await SeedCartAsync();
        var coupon = await SeedCouponAsync(
            TestCoupons.Make(code: "VARA30", value: 30m, maxRedemptions: 10));
        await _coupons.ApplyToCartAsync(userId, null, "VARA30", 100m);

        var order = (await _sut.CreateFromCartAsync(userId, null, MakeRequest())).Order;

        var redemption = _db.CouponRedemptions.Single();
        redemption.OrderId.Should().Be(order.Id);
        redemption.CouponId.Should().Be(coupon.Id);
        redemption.UserId.Should().Be(userId);
        redemption.DiscountRon.Should().Be(order.DiscountRon);
        _db.Coupons.Single(c => c.Id == coupon.Id).RedemptionsCount.Should().Be(1);
    }

    [Fact]
    public async Task Checkout_ExhaustedCoupon_Returns409_AndCreatesNoOrder()
    {
        var userId = await SeedCartAsync();
        var coupon = await SeedCouponAsync(
            TestCoupons.Make(code: "SOLDOUT", value: 30m, maxRedemptions: 1));
        _db.CartCoupons.Add(new CartCoupon { UserId = userId, CouponId = coupon.Id });
        await _db.SaveChangesAsync();

        coupon.RedemptionsCount = 1;
        await _db.SaveChangesAsync();

        var act = () => _sut.CreateFromCartAsync(userId, null, MakeRequest());

        (await act.Should().ThrowAsync<CouponConflictException>())
            .Which.ErrorCode.Should().Be(CouponErrorCodes.CouponExhausted);
        _db.Orders.Should().BeEmpty();
        _db.CouponRedemptions.Should().BeEmpty();
    }

    [Fact]
    public async Task Checkout_CouponDeactivatedAfterApply_Returns409InvalidCoupon_AndCreatesNoOrder()
    {
        var userId = await SeedCartAsync();
        var coupon = await SeedCouponAsync(TestCoupons.Make(code: "VARA30", value: 30m));
        await _coupons.ApplyToCartAsync(userId, null, "VARA30", 100m);

        coupon.IsActive = false;
        await _db.SaveChangesAsync();

        var act = () => _sut.CreateFromCartAsync(userId, null, MakeRequest());

        (await act.Should().ThrowAsync<CouponConflictException>())
            .Which.ErrorCode.Should().Be(CouponErrorCodes.InvalidCoupon);
        _db.Orders.Should().BeEmpty();
    }

    [Fact]
    public async Task Checkout_DiscountLeavesTotalBelowStripeMinimum_Returns409_AndWritesNothing()
    {
        var userId = await SeedCartAsync(unitPrice: 1.00m, quantity: 1);
        var coupon = await SeedCouponAsync(
            TestCoupons.Make(code: "FREESHIP", type: CouponType.FreeShipping, value: 1m));
        _db.CartCoupons.Add(new CartCoupon { UserId = userId, CouponId = coupon.Id });
        await _db.SaveChangesAsync();

        var act = () => _sut.CreateFromCartAsync(userId, null, MakeRequest());

        (await act.Should().ThrowAsync<CouponConflictException>())
            .Which.ErrorCode.Should().Be(CouponErrorCodes.OrderTotalBelowMinimum);
        _db.Orders.Should().BeEmpty();
        _db.CouponRedemptions.Should().BeEmpty();
        _db.Coupons.Single(c => c.Id == coupon.Id).RedemptionsCount.Should().Be(0);
    }

    [Fact]
    public async Task Checkout_FreeShippingWithZeroShippingCost_RedeemsNothing()
    {
        var db = new PhotoPrintDbContext(
            new DbContextOptionsBuilder<PhotoPrintDbContext>()
                .UseInMemoryDatabase($"OrderCouponFreeShip_{Guid.NewGuid():N}")
                .Options);
        using var _ = db;

        var numbers = new Mock<IOrderNumberService>();
        numbers.Setup(s => s.GenerateAsync(It.IsAny<CancellationToken>())).ReturnsAsync("FT-00000001");
        var shipping = new Mock<IShippingService>();
        shipping.Setup(s => s.GetShippingCostAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShippingCostDto(0m));
        var router = new Mock<IStorageRouter>();
        router.SetupGet(r => r.CloudEnabled).Returns(false);

        var coupons = TestCoupons.ServiceFor(db);
        var sut = new OrderService(
            db, numbers.Object, shipping.Object, router.Object, coupons,
            Options.Create(new StorageSettings()),
            Options.Create(new VatSettings { Rate = VatRate }),
            Options.Create(new StripeSettings { MinimumChargeRon = 2.00m }));

        var userId = Guid.NewGuid();
        TestCartSeed.Build(userId: userId, unitPrice: 20.00m, quantity: 5).AddTo(db);
        db.Coupons.Add(TestCoupons.Make(code: "FREESHIP", type: CouponType.FreeShipping, value: 1m));
        await db.SaveChangesAsync();
        await coupons.ApplyToCartAsync(userId, null, "FREESHIP", 100m);

        var order = (await sut.CreateFromCartAsync(userId, null, MakeRequest())).Order;

        order.CouponCode.Should().BeNull();
        order.DiscountRon.Should().Be(0m);
        db.CouponRedemptions.Should().BeEmpty();
        db.Coupons.Single().RedemptionsCount.Should().Be(0);
    }

    [Fact]
    public async Task Replay_OfDiscountedOrder_IsNotTreatedAsDivergent()
    {
        var userId = await SeedCartAsync();
        await SeedCouponAsync(TestCoupons.Make(code: "VARA30", value: 30m, maxRedemptions: 10));
        await _coupons.ApplyToCartAsync(userId, null, "VARA30", 100m);
        const string key = "idem-key-discounted";

        var first = await _sut.CreateFromCartAsync(userId, null, MakeRequest(), key);
        var second = await _sut.CreateFromCartAsync(userId, null, MakeRequest(), key);

        second.WasIdempotentReplay.Should().BeTrue();
        second.Order.Id.Should().Be(first.Order.Id);
    }

    [Fact]
    public async Task Replay_OfDiscountedOrder_DoesNotRedeemTwice()
    {
        var userId = await SeedCartAsync();
        var coupon = await SeedCouponAsync(
            TestCoupons.Make(code: "VARA30", value: 30m, maxRedemptions: 10));
        await _coupons.ApplyToCartAsync(userId, null, "VARA30", 100m);
        const string key = "idem-key-no-double-redeem";

        await _sut.CreateFromCartAsync(userId, null, MakeRequest(), key);
        await _sut.CreateFromCartAsync(userId, null, MakeRequest(), key);

        _db.CouponRedemptions.Should().ContainSingle();
        _db.Coupons.Single(c => c.Id == coupon.Id).RedemptionsCount.Should().Be(1);
    }

    [Fact]
    public async Task DivergenceCheck_ForCouponFreeOrder_IsUnchangedByTheDiscountAwareComparison()
    {
        var userId = await SeedCartAsync();
        const string key = "idem-key-divergent";

        await _sut.CreateFromCartAsync(userId, null, MakeRequest(), key);

        var divergent = new CreateOrderRequest(
            DeliveryType.Easybox, Guid.NewGuid(), null);
        var act = () => _sut.CreateFromCartAsync(userId, null, divergent, key);

        (await act.Should().ThrowAsync<IdempotencyConflictException>())
            .Which.DivergentFields.Should().Contain("deliveryType");
    }

    [Fact]
    public async Task PaymentFailedRetry_ReleasesTheAbandonedRedemption_SoOnePurchaseSpendsOneSlot()
    {
        var userId = await SeedCartAsync();
        var coupon = await SeedCouponAsync(
            TestCoupons.Make(code: "ONEONLY", value: 30m, maxRedemptions: 1));
        await _coupons.ApplyToCartAsync(userId, null, "ONEONLY", 100m);
        const string key = "idem-key-declined-card";

        var first = await _sut.CreateFromCartAsync(userId, null, MakeRequest(), key);
        first.Order.Status = OrderStatus.PaymentFailed;
        await _db.SaveChangesAsync();

        var retry = await _sut.CreateFromCartAsync(userId, null, MakeRequest(), key);

        retry.Order.Id.Should().NotBe(first.Order.Id);
        retry.Order.DiscountRon.Should().Be(30.00m);
        _db.CouponRedemptions.Should().ContainSingle();
        _db.CouponRedemptions.Single().OrderId.Should().Be(retry.Order.Id);
        _db.Coupons.Single(c => c.Id == coupon.Id).RedemptionsCount.Should().Be(1);
    }
}
