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

namespace PhotoPrint.Tests.Integration;

public class CouponRedemptionRelationalTests : IClassFixture<PostgresTestDatabase>
{
    private const decimal UnitPrice = 20.00m;
    private const int Quantity = 5;
    private const decimal ShippingCost = 20.00m;

    private readonly PostgresTestDatabase _database;

    public CouponRedemptionRelationalTests(PostgresTestDatabase database)
    {
        _database = database;
        database.ResetForTest();
    }

    [Fact]
    public async Task Checkout_WithCoupon_CommitsTheOrderAndItsRedemptionTogether()
    {
        var (userId, couponId) = await SeedAsync();

        using var db = _database.NewContext();
        var order = (await BuildService(db).CreateFromCartAsync(userId, null, Request())).Order;

        using var verify = _database.NewContext();
        verify.Orders.Single(o => o.Id == order.Id).DiscountRon.Should().Be(30.00m);
        verify.CouponRedemptions.Single(r => r.OrderId == order.Id).CouponId.Should().Be(couponId);
        verify.Coupons.Single(c => c.Id == couponId).RedemptionsCount.Should().Be(1);
    }

    [Fact]
    public async Task Checkout_OrderInsertFails_LeavesNoRedemptionAndNoCountChange()
    {
        var (userId, couponId) = await SeedAsync();

        using var db = _database.NewContext();
        var unknownLocker = new CreateOrderRequest(DeliveryType.Easybox, Guid.NewGuid(), null);

        var act = () => BuildService(db).CreateFromCartAsync(userId, null, unknownLocker);

        await act.Should().ThrowAsync<DbUpdateException>();

        using var verify = _database.NewContext();
        verify.Orders.Should().BeEmpty();
        verify.CouponRedemptions.Should().BeEmpty();
        verify.Coupons.Single(c => c.Id == couponId).RedemptionsCount.Should().Be(0);
    }

    [Fact]
    public async Task Checkout_IdempotentReplay_DoesNotRedeemTwice()
    {
        var (userId, couponId) = await SeedAsync();
        const string key = "relational-replay-key";

        using var first = _database.NewContext();
        var original = (await BuildService(first)
            .CreateFromCartAsync(userId, null, Request(), key)).Order;

        using var second = _database.NewContext();
        var replay = await BuildService(second).CreateFromCartAsync(userId, null, Request(), key);

        replay.WasIdempotentReplay.Should().BeTrue();
        replay.Order.Id.Should().Be(original.Id);

        using var verify = _database.NewContext();
        verify.CouponRedemptions.Count(r => r.CouponId == couponId).Should().Be(1);
        verify.Coupons.Single(c => c.Id == couponId).RedemptionsCount.Should().Be(1);
    }

    [Fact]
    public async Task PaymentFailedRetry_ReleasesTheAbandonedRedemption()
    {
        var (userId, couponId) = await SeedAsync(maxRedemptions: 1);
        const string key = "relational-declined-card";

        using var first = _database.NewContext();
        var failed = (await BuildService(first)
            .CreateFromCartAsync(userId, null, Request(), key)).Order;

        using var fail = _database.NewContext();
        var stored = fail.Orders.Single(o => o.Id == failed.Id);
        stored.Status = OrderStatus.PaymentFailed;
        await fail.SaveChangesAsync();

        using var retry = _database.NewContext();
        var replacement = (await BuildService(retry)
            .CreateFromCartAsync(userId, null, Request(), key)).Order;

        replacement.Id.Should().NotBe(failed.Id);

        using var verify = _database.NewContext();
        verify.CouponRedemptions.Single().OrderId.Should().Be(replacement.Id);
        verify.Coupons.Single(c => c.Id == couponId).RedemptionsCount.Should().Be(1);
    }

    [Fact]
    public async Task StaleKeyReclamation_ReleasesTheAbandonedRedemption()
    {
        var (userId, couponId) = await SeedAsync(maxRedemptions: 1);
        const string key = "relational-stale-key";

        using var first = _database.NewContext();
        var stale = (await BuildService(first)
            .CreateFromCartAsync(userId, null, Request(), key)).Order;

        using var age = _database.NewContext();
        var stored = age.Orders.Single(o => o.Id == stale.Id);
        stored.CreatedAt = DateTimeOffset.UtcNow.AddHours(-25);
        await age.SaveChangesAsync();

        using var reuse = _database.NewContext();
        var replacement = (await BuildService(reuse)
            .CreateFromCartAsync(userId, null, Request(), key)).Order;

        replacement.Id.Should().NotBe(stale.Id);

        using var verify = _database.NewContext();
        verify.CouponRedemptions.Single().OrderId.Should().Be(replacement.Id);
        verify.Coupons.Single(c => c.Id == couponId).RedemptionsCount.Should().Be(1);
    }

    [Fact]
    public async Task Consume_CouponDeactivatedAfterValidation_RefusesWithInvalidCoupon()
    {
        var (_, couponId) = await SeedAsync();

        using var deactivate = _database.NewContext();
        deactivate.Coupons.Single(c => c.Id == couponId).IsActive = false;
        await deactivate.SaveChangesAsync();

        using var db = _database.NewContext();
        var act = () => TestCoupons.ServiceFor(db).ConsumeOrThrowAsync(couponId);

        (await act.Should().ThrowAsync<CouponConflictException>())
            .Which.ErrorCode.Should().Be(CouponErrorCodes.InvalidCoupon);

        using var verify = _database.NewContext();
        verify.Coupons.Single(c => c.Id == couponId).RedemptionsCount.Should().Be(0);
    }

    [Fact]
    public async Task Consume_CouponExpiredAfterValidation_RefusesWithInvalidCoupon()
    {
        var (_, couponId) = await SeedAsync();

        using var expire = _database.NewContext();
        expire.Coupons.Single(c => c.Id == couponId).ValidUntil = DateTimeOffset.UtcNow.AddMinutes(-1);
        await expire.SaveChangesAsync();

        using var db = _database.NewContext();
        var act = () => TestCoupons.ServiceFor(db).ConsumeOrThrowAsync(couponId);

        (await act.Should().ThrowAsync<CouponConflictException>())
            .Which.ErrorCode.Should().Be(CouponErrorCodes.InvalidCoupon);

        using var verify = _database.NewContext();
        verify.Coupons.Single(c => c.Id == couponId).RedemptionsCount.Should().Be(0);
    }

    [Fact]
    public async Task Release_RemovesTheRedemptionAndDecrementsInOneUnit()
    {
        var (userId, couponId) = await SeedAsync();

        using var db = _database.NewContext();
        var order = (await BuildService(db).CreateFromCartAsync(userId, null, Request())).Order;

        using var release = _database.NewContext();
        await TestCoupons.ServiceFor(release).ReleaseForOrderAsync(order.Id);

        using var verify = _database.NewContext();
        verify.CouponRedemptions.Should().BeEmpty();
        verify.Coupons.Single(c => c.Id == couponId).RedemptionsCount.Should().Be(0);
    }

    private static OrderService BuildService(PhotoPrintDbContext db)
    {
        var shipping = new Mock<IShippingService>();
        shipping.Setup(s => s.GetShippingCostAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShippingCostDto(ShippingCost));

        return new OrderService(
            db,
            new OrderNumberService(db),
            shipping.Object,
            Mock.Of<IStorageRouter>(),
            TestCoupons.ServiceFor(db),
            Options.Create(new StorageSettings()),
            Options.Create(new VatSettings { Rate = 0.19m }),
            Options.Create(new StripeSettings { MinimumChargeRon = 2.00m }));
    }

    private static CreateOrderRequest Request()
        => new(DeliveryType.Courier, null, new ShippingAddressSnapshot
        {
            RecipientName = "Test", Street = "Str. Test", Number = "1",
            City = "București", County = "Ilfov", PostalCode = "010101", Phone = "0700000000",
        });

    private async Task<(Guid UserId, Guid CouponId)> SeedAsync(int? maxRedemptions = 10)
    {
        using var db = _database.NewContext();

        var coupon = TestCoupons.Make(
            code: "VARA30", type: CouponType.Fixed, value: 30m, maxRedemptions: maxRedemptions);
        db.Coupons.Add(coupon);

        var user = new User
        {
            Email = "shopper@example.com",
            NormalizedEmail = "SHOPPER@EXAMPLE.COM",
            FirstName = "Shopper",
            LastName = "One",
            IsEmailConfirmed = true,
        };
        var product = new Product { Name = "Foto 10x15", IsActive = true };
        var size = new ProductSize
        {
            ProductId = product.Id, Label = "10x15", WidthMm = 100, HeightMm = 150, IsActive = true,
        };
        var tier = new PricingTier
        {
            ProductSizeId = size.Id, MinQuantity = 1, MaxQuantity = null, UnitPrice = UnitPrice,
        };
        var finish = new ProductFinish { ProductId = product.Id, Name = "Lucios" };
        var upload = new Upload
        {
            UserId = user.Id,
            OriginalFileName = "photo.jpg",
            FilePath = "/uploads/photo.jpg",
            ContentType = "image/jpeg",
            WidthPx = 1800,
            HeightPx = 1200,
        };
        var cartItem = new CartItem
        {
            UserId = user.Id,
            UploadId = upload.Id,
            ProductId = product.Id,
            SizeId = size.Id,
            Quantity = Quantity,
        };
        var applied = new CartCoupon { UserId = user.Id, CouponId = coupon.Id };

        db.AddRange(user, product, size, tier, finish, upload, cartItem, applied);
        await db.SaveChangesAsync();

        return (user.Id, coupon.Id);
    }
}
