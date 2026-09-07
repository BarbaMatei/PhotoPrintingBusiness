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
using PhotoPrint.Tests.Helpers;
using Xunit;

namespace PhotoPrint.Tests.Integration;

public class CouponRedemptionConcurrencyRelationalTests : IClassFixture<PostgresTestDatabase>
{
    private const int Shoppers = 100;
    private const int Cap = 5;
    private const decimal UnitPrice = 20.00m;
    private const int Quantity = 5;
    private const decimal ShippingCost = 20.00m;

    private const int MaxPoolSize = 20;

    private readonly PostgresTestDatabase _database;
    private readonly DbContextOptions<PhotoPrintDbContext> _shopperOptions;

    public CouponRedemptionConcurrencyRelationalTests(PostgresTestDatabase database)
    {
        _database = database;
        database.ResetForTest();

        _shopperOptions = new DbContextOptionsBuilder<PhotoPrintDbContext>()
            .UseNpgsql($"{database.ConnectionString};Maximum Pool Size={MaxPoolSize}")
            .Options;
    }

    [Fact]
    public async Task ParallelCheckouts_ForCappedCoupon_RedeemExactlyTheCap_AndCreateNoLosingOrders()
    {
        var (couponId, shoppers) = await SeedAsync();

        var outcomes = await Task.WhenAll(shoppers.Select(CheckoutAsync));

        var succeeded = outcomes.Count(o => o.Succeeded);
        var exhausted = outcomes.Count(
            o => o.ErrorCode == CouponErrorCodes.CouponExhausted);

        succeeded.Should().Be(Cap);
        exhausted.Should().Be(Shoppers - Cap);

        using var db = _database.NewContext();
        db.Coupons.Single(c => c.Id == couponId).RedemptionsCount.Should().Be(Cap);
        db.CouponRedemptions.Count(r => r.CouponId == couponId).Should().Be(Cap);
        db.Orders.Count().Should().Be(Cap);
        db.Orders.Count(o => o.CouponCode == "VARA30").Should().Be(Cap);
        db.Orders.Select(o => o.DiscountRon).Should().AllBeEquivalentTo(30.00m);
    }

    private sealed record Outcome(bool Succeeded, string? ErrorCode);

    private async Task<Outcome> CheckoutAsync(Guid userId)
    {
        using var db = new PhotoPrintDbContext(_shopperOptions);
        var service = BuildService(db);

        try
        {
            await service.CreateFromCartAsync(userId, null, Request(), null);
            return new Outcome(true, null);
        }
        catch (CouponConflictException ex)
        {
            return new Outcome(false, ex.ErrorCode);
        }
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

    private async Task<(Guid CouponId, IReadOnlyList<Guid> Shoppers)> SeedAsync()
    {
        using var db = _database.NewContext();

        var coupon = TestCoupons.Make(
            code: "VARA30", type: CouponType.Fixed, value: 30m, maxRedemptions: Cap);
        db.Coupons.Add(coupon);

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
        db.AddRange(product, size, tier, finish);

        var shoppers = new List<Guid>(Shoppers);
        for (var i = 0; i < Shoppers; i++)
        {
            var user = new User
            {
                Email = $"shopper{i}@example.com",
                NormalizedEmail = $"SHOPPER{i}@EXAMPLE.COM",
                FirstName = "Shopper",
                LastName = i.ToString(),
                IsEmailConfirmed = true,
            };
            var upload = new Upload
            {
                UserId = user.Id,
                OriginalFileName = "photo.jpg",
                FilePath = $"/uploads/{i}.jpg",
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

            db.AddRange(user, upload, cartItem, applied);
            shoppers.Add(user.Id);
        }

        await db.SaveChangesAsync();
        return (coupon.Id, shoppers);
    }
}
