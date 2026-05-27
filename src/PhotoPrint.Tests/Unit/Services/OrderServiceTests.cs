using Microsoft.EntityFrameworkCore;
using Moq;
using PhotoPrint.API.Data;
using PhotoPrint.API.DTOs.Payments;
using PhotoPrint.API.DTOs.Shipping;
using PhotoPrint.API.Exceptions;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services;

namespace PhotoPrint.Tests.Unit.Services;

public class OrderServiceTests : IDisposable
{
    private readonly PhotoPrintDbContext _db;
    private readonly Mock<IOrderNumberService> _orderNumberServiceMock;
    private readonly Mock<IShippingService> _shippingMock;
    private readonly OrderService _service;

    public OrderServiceTests()
    {
        var opts = new DbContextOptionsBuilder<PhotoPrintDbContext>()
            .UseInMemoryDatabase($"OrderServiceTests_{Guid.NewGuid():N}")
            .Options;
        _db = new PhotoPrintDbContext(opts);

        _orderNumberServiceMock = new Mock<IOrderNumberService>();
        _orderNumberServiceMock
            .Setup(s => s.GenerateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("FT-20260001");

        _shippingMock = new Mock<IShippingService>();
        _shippingMock
            .Setup(s => s.GetShippingCostAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShippingCostDto(20.00m));

        _service = new OrderService(_db, _orderNumberServiceMock.Object, _shippingMock.Object);
    }

    public void Dispose() => _db.Dispose();

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<(Guid userId, Guid productId, Guid uploadId)> SeedCartAsync(
        decimal unitPrice = 2.00m,
        int quantity = 3)
    {
        var userId = Guid.NewGuid();

        var product = new Product { Name = "Foto 10x15", IsActive = true };
        var size = new ProductSize
        {
            ProductId = product.Id,
            Label = "10x15",
            WidthMm = 100,
            HeightMm = 150,
            IsActive = true,
        };
        var tier = new PricingTier
        {
            ProductSizeId = size.Id,
            MinQuantity = 1,
            MaxQuantity = null,
            UnitPrice = unitPrice,
        };
        var finish = new ProductFinish { ProductId = product.Id, Name = "Lucios" };

        var upload = new Upload
        {
            UserId = userId,
            OriginalFileName = "photo.jpg",
            FilePath = "/uploads/photo.jpg",
            ContentType = "image/jpeg",
            WidthPx = 1800,
            HeightPx = 1200,
        };

        var cartItem = new CartItem
        {
            UserId = userId,
            UploadId = upload.Id,
            ProductId = product.Id,
            Quantity = quantity,
        };

        _db.Products.Add(product);
        _db.ProductSizes.Add(size);
        _db.PricingTiers.Add(tier);
        _db.ProductFinishes.Add(finish);
        _db.Uploads.Add(upload);
        _db.CartItems.Add(cartItem);
        await _db.SaveChangesAsync();

        return (userId, product.Id, upload.Id);
    }

    private static CreateOrderRequest MakeRequest(
        PaymentProcessor processor = PaymentProcessor.Stripe)
        => new CreateOrderRequest(
            PaymentProcessor: processor,
            DeliveryType: DeliveryType.Easybox,
            EasyboxLockerId: Guid.NewGuid(),
            ShippingAddress: null);

    // ── CreateFromCartAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task CreateFromCartAsync_ValidCart_CreatesOrderWithCorrectItems()
    {
        var (userId, productId, uploadId) = await SeedCartAsync(unitPrice: 2.00m, quantity: 3);

        var order = await _service.CreateFromCartAsync(userId, null, MakeRequest(), default);

        Assert.NotNull(order);
        Assert.Single(order.Items);
        var item = order.Items.First();
        Assert.Equal(productId, item.ProductId);
        Assert.Equal(uploadId, item.UploadId);
        Assert.Equal(3, item.Quantity);
        Assert.Equal(2.00m, item.UnitPriceRon);
        Assert.Equal(6.00m, item.LineTotalRon);
    }

    [Fact]
    public async Task CreateFromCartAsync_ValidCart_SetsSubtotalAndTotal()
    {
        var (userId, _, _) = await SeedCartAsync(unitPrice: 2.00m, quantity: 3);

        var order = await _service.CreateFromCartAsync(userId, null, MakeRequest(), default);

        Assert.Equal(6.00m, order.SubtotalRon);
        Assert.Equal(26.00m, order.TotalRon);
        // Server-resolved shipping cost from IShippingService mock (20.00 RON).
        Assert.Equal(20.00m, order.ShippingCostRon);
    }

    [Fact]
    public async Task CreateFromCartAsync_ValidCart_SetsOrderNumber()
    {
        var (userId, _, _) = await SeedCartAsync();

        var order = await _service.CreateFromCartAsync(userId, null, MakeRequest(), default);

        Assert.Equal("FT-20260001", order.OrderNumber);
    }

    [Fact]
    public async Task CreateFromCartAsync_ValidCart_SetsStatusAwaitingPayment()
    {
        var (userId, _, _) = await SeedCartAsync();

        var order = await _service.CreateFromCartAsync(userId, null, MakeRequest(), default);

        Assert.Equal(OrderStatus.AwaitingPayment, order.Status);
    }

    [Fact]
    public async Task CreateFromCartAsync_ValidCart_StoresProductSnapshot()
    {
        var (userId, _, _) = await SeedCartAsync();

        var order = await _service.CreateFromCartAsync(userId, null, MakeRequest(), default);

        var snapshot = order.Items.First().ProductSnapshot;
        Assert.Equal("Foto 10x15", snapshot.ProductName);
        Assert.Equal("10x15", snapshot.Size);
        Assert.Equal("Lucios", snapshot.Finish);
    }

    [Fact]
    public async Task CreateFromCartAsync_EmptyCart_ThrowsBadRequest()
    {
        var userId = Guid.NewGuid(); // no cart items for this user

        await Assert.ThrowsAsync<BadRequestException>(
            () => _service.CreateFromCartAsync(userId, null, MakeRequest(), default));
    }

    [Fact]
    public async Task CreateFromCartAsync_GuestCart_CreatesOrder()
    {
        var guestId = Guid.NewGuid();
        var product = new Product { Name = "Foto 10x15", IsActive = true };
        var size = new ProductSize { ProductId = product.Id, Label = "10x15", WidthMm = 100, HeightMm = 150, IsActive = true };
        var tier = new PricingTier { ProductSizeId = size.Id, MinQuantity = 1, MaxQuantity = null, UnitPrice = 1.50m };
        var upload = new Upload { GuestSessionId = guestId, OriginalFileName = "g.jpg", FilePath = "/g.jpg", ContentType = "image/jpeg", WidthPx = 800, HeightPx = 600 };
        var cartItem = new CartItem { GuestSessionId = guestId, UploadId = upload.Id, ProductId = product.Id, Quantity = 2 };

        _db.Products.Add(product);
        _db.ProductSizes.Add(size);
        _db.PricingTiers.Add(tier);
        _db.Uploads.Add(upload);
        _db.CartItems.Add(cartItem);
        await _db.SaveChangesAsync();

        var order = await _service.CreateFromCartAsync(null, guestId, MakeRequest(), default);

        Assert.Null(order.UserId);
        Assert.Equal(guestId, order.GuestSessionId);
        Assert.Equal(3.00m, order.SubtotalRon);
    }

    [Fact]
    public async Task CreateFromCartAsync_GuestCart_SetsGuestEmailFromSession()
    {
        var guestSession = new GuestSession
        {
            Email = "guest@test.com",
            FirstName = "Mircea",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(1),
        };
        var product = new Product { Name = "Foto 10x15", IsActive = true };
        var size = new ProductSize { ProductId = product.Id, Label = "10x15", WidthMm = 100, HeightMm = 150, IsActive = true };
        var tier = new PricingTier { ProductSizeId = size.Id, MinQuantity = 1, MaxQuantity = null, UnitPrice = 1.50m };
        var upload = new Upload { GuestSessionId = guestSession.Id, OriginalFileName = "g.jpg", FilePath = "/g.jpg", ContentType = "image/jpeg", WidthPx = 800, HeightPx = 600 };
        var cartItem = new CartItem { GuestSessionId = guestSession.Id, UploadId = upload.Id, ProductId = product.Id, Quantity = 2 };

        _db.GuestSessions.Add(guestSession);
        _db.Products.Add(product);
        _db.ProductSizes.Add(size);
        _db.PricingTiers.Add(tier);
        _db.Uploads.Add(upload);
        _db.CartItems.Add(cartItem);
        await _db.SaveChangesAsync();

        var order = await _service.CreateFromCartAsync(null, guestSession.Id, MakeRequest(), default);

        Assert.Equal("guest@test.com", order.GuestEmail);
    }

    // ── Queries ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByPaymentIntentIdAsync_ExistingId_ReturnsOrder()
    {
        var order = new Order
        {
            OrderNumber = "FT-20260001",
            PaymentIntentId = "pi_test_123",
            ShippingAddress = new PhotoPrint.API.Models.ShippingAddressSnapshot
            {
                Street = "Str. Test", Number = "1", City = "București",
                County = "Ilfov", PostalCode = "010101", RecipientName = "Test", Phone = "0700000000",
            },
        };
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        var result = await _service.GetByPaymentIntentIdAsync("pi_test_123");

        Assert.NotNull(result);
        Assert.Equal("FT-20260001", result!.OrderNumber);
    }

    [Fact]
    public async Task GetByPaymentIntentIdAsync_UnknownId_ReturnsNull()
    {
        var result = await _service.GetByPaymentIntentIdAsync("pi_does_not_exist");
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsOrder()
    {
        var order = new Order
        {
            OrderNumber = "FT-20260002",
            ShippingAddress = new PhotoPrint.API.Models.ShippingAddressSnapshot
            {
                Street = "Str. Test", Number = "2", City = "Cluj",
                County = "Cluj", PostalCode = "400000", RecipientName = "User", Phone = "0711111111",
            },
        };
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        var result = await _service.GetByIdAsync(order.Id);

        Assert.NotNull(result);
        Assert.Equal("FT-20260002", result!.OrderNumber);
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        var result = await _service.GetByIdAsync(Guid.NewGuid());
        Assert.Null(result);
    }
}
