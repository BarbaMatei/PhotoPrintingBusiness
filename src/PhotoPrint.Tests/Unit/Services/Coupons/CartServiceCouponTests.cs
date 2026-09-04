using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PhotoPrint.API.Data;
using PhotoPrint.API.DTOs.Cart;
using PhotoPrint.API.Exceptions;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services;
using PhotoPrint.API.Services.Coupons;
using PhotoPrint.Tests.Helpers;

namespace PhotoPrint.Tests.Unit.Services.Coupons;

public class CartServiceCouponTests
{
    private readonly PhotoPrintDbContext _db;
    private readonly ICartService _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public CartServiceCouponTests()
    {
        var options = new DbContextOptionsBuilder<PhotoPrintDbContext>()
            .UseInMemoryDatabase($"CartSvcCoupon_{Guid.NewGuid():N}")
            .Options;
        _db = new PhotoPrintDbContext(options);
        _sut = new CartService(
            _db,
            TestCoupons.ServiceFor(_db),
            Microsoft.Extensions.Options.Options.Create(
                new PhotoPrint.API.Configuration.VatSettings()));
    }

    private async Task<Product> SeedProductAsync(decimal unitPrice)
    {
        var product = new Product { Name = "Test Product", IsActive = true };
        var size = new ProductSize
        {
            ProductId = product.Id,
            Label = "10x15",
            WidthMm = 100,
            HeightMm = 150,
            IsActive = true,
        };
        size.PricingTiers =
        [
            new PricingTier
            {
                ProductSizeId = size.Id,
                MinQuantity = 1,
                MaxQuantity = null,
                UnitPrice = unitPrice,
            },
        ];
        product.Sizes = [size];
        product.Finishes = [new ProductFinish { ProductId = product.Id, Name = "Lucios" }];

        _db.Products.Add(product);
        await _db.SaveChangesAsync();
        return product;
    }

    private async Task<Upload> SeedUploadAsync()
    {
        var upload = new Upload
        {
            UserId = _userId,
            FilePath = $"test/{Guid.NewGuid():N}.jpg",
            OriginalFileName = "test.jpg",
            ContentType = "image/jpeg",
            WidthPx = 800,
            HeightPx = 600,
            FileSizeBytes = 1024,
        };
        _db.Uploads.Add(upload);
        await _db.SaveChangesAsync();
        return upload;
    }

    private async Task<CartItem> SeedCartItemAsync(Product product, int quantity)
    {
        var upload = await SeedUploadAsync();
        var item = new CartItem
        {
            UserId = _userId,
            UploadId = upload.Id,
            ProductId = product.Id,
            SizeId = product.Sizes.First().Id,
            Quantity = quantity,
        };
        _db.CartItems.Add(item);
        await _db.SaveChangesAsync();
        return item;
    }

    private async Task<Coupon> SeedAndApplyAsync(Coupon coupon)
    {
        _db.Coupons.Add(coupon);
        await _db.SaveChangesAsync();
        await _sut.ApplyCouponAsync(_userId, null, coupon.Code);
        return coupon;
    }

    private bool StoredCouponSurvives()
        => _db.CartCoupons.AsNoTracking().Any(cc => cc.UserId == _userId);

    [Fact]
    public async Task GetCart_SubtotalBelowMinimum_ReportsStale_AndWritesNothing()
    {
        var product = await SeedProductAsync(unitPrice: 10m);
        await SeedCartItemAsync(product, quantity: 10);
        var droppedItem = await SeedCartItemAsync(product, quantity: 15);
        await SeedAndApplyAsync(TestCoupons.Make(
            code: "BIGONLY", type: CouponType.Percent, value: 10m, minSubtotalRon: 200m));

        _db.CartItems.Remove(droppedItem);
        await _db.SaveChangesAsync();

        var cart = await _sut.GetCartAsync(_userId, null);

        cart.Subtotal.Should().Be(100m);
        cart.CouponCode.Should().Be("BIGONLY");
        cart.CouponStatus.Should().Be(CouponCartStatus.Stale);
        cart.CouponReason.Should().Be(CouponErrorCodes.MinSubtotalNotMet);
        cart.DiscountRon.Should().Be(0m);
        cart.TotalRon.Should().Be(100m);
        StoredCouponSurvives().Should().BeTrue();
    }

    [Fact]
    public async Task GetCart_BackAboveTheMinimum_ReportsValidAgain()
    {
        var product = await SeedProductAsync(unitPrice: 10m);
        await SeedCartItemAsync(product, quantity: 10);
        var droppedItem = await SeedCartItemAsync(product, quantity: 15);
        await SeedAndApplyAsync(TestCoupons.Make(
            code: "BIGONLY", type: CouponType.Percent, value: 10m, minSubtotalRon: 200m));

        _db.CartItems.Remove(droppedItem);
        await _db.SaveChangesAsync();

        (await _sut.GetCartAsync(_userId, null)).CouponStatus
            .Should().Be(CouponCartStatus.Stale);

        await SeedCartItemAsync(product, quantity: 15);

        var cart = await _sut.GetCartAsync(_userId, null);

        cart.Subtotal.Should().Be(250m);
        cart.CouponStatus.Should().Be(CouponCartStatus.Valid);
        cart.CouponReason.Should().BeNull();
        cart.DiscountRon.Should().Be(25m);
        cart.TotalRon.Should().Be(225m);
    }

    [Fact]
    public async Task GetCart_CouponExpiredAfterApply_ReportsStaleInvalid()
    {
        var product = await SeedProductAsync(unitPrice: 10m);
        await SeedCartItemAsync(product, quantity: 10);
        var coupon = await SeedAndApplyAsync(TestCoupons.Make(code: "VARA25", value: 15m));

        coupon.ValidUntil = DateTimeOffset.UtcNow.AddMinutes(-1);
        await _db.SaveChangesAsync();

        var cart = await _sut.GetCartAsync(_userId, null);

        cart.CouponCode.Should().Be("VARA25");
        cart.CouponStatus.Should().Be(CouponCartStatus.Stale);
        cart.CouponReason.Should().Be(CouponErrorCodes.InvalidCoupon);
        cart.DiscountRon.Should().Be(0m);
        cart.TotalRon.Should().Be(100m);
        StoredCouponSurvives().Should().BeTrue();
    }

    [Fact]
    public async Task GetCart_CouponExhaustedAfterApply_ReportsStaleExhausted()
    {
        var product = await SeedProductAsync(unitPrice: 10m);
        await SeedCartItemAsync(product, quantity: 10);
        var coupon = await SeedAndApplyAsync(
            TestCoupons.Make(code: "LIMIT1", value: 15m, maxRedemptions: 1));

        coupon.RedemptionsCount = 1;
        await _db.SaveChangesAsync();

        var cart = await _sut.GetCartAsync(_userId, null);

        cart.CouponStatus.Should().Be(CouponCartStatus.Stale);
        cart.CouponReason.Should().Be(CouponErrorCodes.CouponExhausted);
        cart.DiscountRon.Should().Be(0m);
        StoredCouponSurvives().Should().BeTrue();
    }

    [Fact]
    public async Task GetCart_CouponDeactivatedByAdmin_ReportsStaleInvalid_AndWritesNothing()
    {
        var product = await SeedProductAsync(unitPrice: 10m);
        await SeedCartItemAsync(product, quantity: 10);
        var coupon = await SeedAndApplyAsync(TestCoupons.Make(code: "VARA25", value: 15m));

        coupon.IsActive = false;
        await _db.SaveChangesAsync();

        var cart = await _sut.GetCartAsync(_userId, null);

        cart.CouponStatus.Should().Be(CouponCartStatus.Stale);
        cart.CouponReason.Should().Be(CouponErrorCodes.InvalidCoupon);
        cart.DiscountRon.Should().Be(0m);
        StoredCouponSurvives().Should().BeTrue();
    }

    [Fact]
    public async Task SetCart_WithStaleCoupon_ReportsStale_AndDeletesNothing()
    {
        var product = await SeedProductAsync(unitPrice: 10m);
        await SeedCartItemAsync(product, quantity: 10);
        var coupon = await SeedAndApplyAsync(TestCoupons.Make(code: "VARA25", value: 15m));

        coupon.IsActive = false;
        await _db.SaveChangesAsync();

        var replacement = await SeedUploadAsync();
        var cart = await _sut.SetCartAsync(_userId, null, new CartRequest(
            product.Id, product.Sizes.First().Id, FinishName: null,
            [new CartItemRequest(replacement.Id, 10)]));

        cart.CouponCode.Should().Be("VARA25");
        cart.CouponStatus.Should().Be(CouponCartStatus.Stale);
        cart.CouponReason.Should().Be(CouponErrorCodes.InvalidCoupon);
        cart.DiscountRon.Should().Be(0m);
        StoredCouponSurvives().Should().BeTrue();
    }
}
