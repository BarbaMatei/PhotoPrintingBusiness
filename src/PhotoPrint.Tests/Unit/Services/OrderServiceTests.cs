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

        var order = (await _service.CreateFromCartAsync(userId, null, MakeRequest())).Order;

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

        var order = (await _service.CreateFromCartAsync(userId, null, MakeRequest())).Order;

        Assert.Equal(6.00m, order.SubtotalRon);
        Assert.Equal(26.00m, order.TotalRon);
        // Server-resolved shipping cost from IShippingService mock (20.00 RON).
        Assert.Equal(20.00m, order.ShippingCostRon);
    }

    [Fact]
    public async Task CreateFromCartAsync_ValidCart_SetsOrderNumber()
    {
        var (userId, _, _) = await SeedCartAsync();

        var order = (await _service.CreateFromCartAsync(userId, null, MakeRequest())).Order;

        Assert.Equal("FT-20260001", order.OrderNumber);
    }

    [Fact]
    public async Task CreateFromCartAsync_ValidCart_SetsStatusAwaitingPayment()
    {
        var (userId, _, _) = await SeedCartAsync();

        var order = (await _service.CreateFromCartAsync(userId, null, MakeRequest())).Order;

        Assert.Equal(OrderStatus.AwaitingPayment, order.Status);
    }

    [Fact]
    public async Task CreateFromCartAsync_ValidCart_StoresProductSnapshot()
    {
        var (userId, _, _) = await SeedCartAsync();

        var order = (await _service.CreateFromCartAsync(userId, null, MakeRequest())).Order;

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

        var order = (await _service.CreateFromCartAsync(null, guestId, MakeRequest())).Order;

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

        var order = (await _service.CreateFromCartAsync(null, guestSession.Id, MakeRequest())).Order;

        Assert.Equal("guest@test.com", order.GuestEmail);
    }

    // ── Idempotency (bolt 035) ──────────────────────────────────────────────────

    [Fact]
    public async Task CreateFromCart_SameKey_SameRequest_ReplaysOriginalOrder()
    {
        var (userId, _, _) = await SeedCartAsync(unitPrice: 2.00m, quantity: 3);
        const string key = "idem-key-001";

        // Reuse the SAME request instance — MakeRequest() randomizes EasyboxLockerId
        // per call, which would otherwise register as a divergent field.
        var request = MakeRequest();
        var first = await _service.CreateFromCartAsync(userId, null, request, key);
        var second = await _service.CreateFromCartAsync(userId, null, request, key);

        Assert.False(first.WasIdempotentReplay);
        Assert.True(second.WasIdempotentReplay);
        Assert.Equal(first.Order.Id, second.Order.Id);

        // Exactly one order row persisted.
        Assert.Equal(1, await _db.Orders.CountAsync());
    }

    [Fact]
    public async Task CreateFromCart_SameKey_DivergentProcessor_ThrowsConflictNamingField()
    {
        var (userId, _, _) = await SeedCartAsync();
        const string key = "idem-key-002";

        // Same base request; vary ONLY the processor so the divergence is unambiguous.
        var request = MakeRequest(PaymentProcessor.Stripe);
        await _service.CreateFromCartAsync(userId, null, request, key);

        var divergent = request with { PaymentProcessor = PaymentProcessor.EuPlatesc };
        var ex = await Assert.ThrowsAsync<IdempotencyConflictException>(
            () => _service.CreateFromCartAsync(userId, null, divergent, key));

        Assert.Contains("paymentProcessor", ex.DivergentFields);
        Assert.DoesNotContain("easyboxLockerId", ex.DivergentFields);
        // Still only one order — the conflicting second request created nothing.
        Assert.Equal(1, await _db.Orders.CountAsync());
    }

    [Fact]
    public async Task CreateFromCart_SameKey_SameTotalDifferentItems_ThrowsConflictNamingItems()
    {
        // BUG-3: with uniform per-unit pricing, a different photo at the same qty has
        // an identical total. Total parity alone must NOT authorize a replay, or the
        // reused key silently ships the wrong order's images.
        var (userId, productId, _) = await SeedCartAsync(unitPrice: 2.00m, quantity: 3);
        const string key = "idem-items-001";

        // Reuse the SAME request instance so ONLY the items differ between calls.
        var request = MakeRequest();
        var first = await _service.CreateFromCartAsync(userId, null, request, key);
        Assert.False(first.WasIdempotentReplay);

        // Swap the cart to a different photo at the same price/quantity → same total.
        var oldItem = await _db.CartItems.FirstAsync(ci => ci.UserId == userId);
        _db.CartItems.Remove(oldItem);
        var upload2 = new Upload
        {
            UserId = userId,
            OriginalFileName = "other.jpg",
            FilePath = "/uploads/other.jpg",
            ContentType = "image/jpeg",
            WidthPx = 1800,
            HeightPx = 1200,
        };
        _db.Uploads.Add(upload2);
        _db.CartItems.Add(new CartItem
        {
            UserId = userId, UploadId = upload2.Id, ProductId = productId, Quantity = 3,
        });
        await _db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<IdempotencyConflictException>(
            () => _service.CreateFromCartAsync(userId, null, request, key));

        Assert.Contains("items", ex.DivergentFields);
        Assert.DoesNotContain("totalRon", ex.DivergentFields); // totals are identical
        Assert.Equal(1, await _db.Orders.CountAsync());          // no second order created
    }

    [Fact]
    public async Task CreateFromCart_NoKey_DoesNotReplay_CreatesDistinctOrders()
    {
        var (userId, _, _) = await SeedCartAsync();

        var first = await _service.CreateFromCartAsync(userId, null, MakeRequest());
        var second = await _service.CreateFromCartAsync(userId, null, MakeRequest());

        Assert.False(first.WasIdempotentReplay);
        Assert.False(second.WasIdempotentReplay);
        Assert.NotEqual(first.Order.Id, second.Order.Id);
        Assert.Equal(2, await _db.Orders.CountAsync());
    }

    [Fact]
    public async Task GetByIdempotencyKey_StaleOrder_ReturnsNull()
    {
        var staleOrder = new Order
        {
            OrderNumber = "FT-STALE-1",
            IdempotencyKey = "idem-key-stale",
            CreatedAt = DateTimeOffset.UtcNow.AddHours(-25), // outside the 24h window
            ShippingAddress = new ShippingAddressSnapshot
            {
                Street = "S", Number = "1", City = "C", County = "J",
                PostalCode = "010101", RecipientName = "R", Phone = "0700000000",
            },
        };
        _db.Orders.Add(staleOrder);
        await _db.SaveChangesAsync();

        var found = await _service.GetByIdempotencyKeyAsync("idem-key-stale", null, null);

        Assert.Null(found);
    }

    // ── SEC-1: idempotency lookup scoped to the caller (tenant isolation) ────────

    [Fact]
    public async Task GetByIdempotencyKey_KeyOwnedByAnotherUser_ReturnsNull()
    {
        var ownerId = Guid.NewGuid();
        var attackerId = Guid.NewGuid();
        const string key = "victim-key";

        var ownersOrder = new Order
        {
            OrderNumber = "FT-OWNER-1",
            UserId = ownerId,
            IdempotencyKey = key,
            StripeClientSecret = "pi_secret_victim", // must never leak to another caller
            CreatedAt = DateTimeOffset.UtcNow,
            ShippingAddress = new ShippingAddressSnapshot
            {
                Street = "S", Number = "1", City = "C", County = "J",
                PostalCode = "010101", RecipientName = "R", Phone = "0700000000",
            },
        };
        _db.Orders.Add(ownersOrder);
        await _db.SaveChangesAsync();

        // Same key, different caller → must not resolve the owner's order.
        var leakedToUser = await _service.GetByIdempotencyKeyAsync(key, attackerId, null);
        var leakedToGuest = await _service.GetByIdempotencyKeyAsync(key, null, Guid.NewGuid());

        Assert.Null(leakedToUser);
        Assert.Null(leakedToGuest);

        // The owner still resolves their own order.
        var ownerView = await _service.GetByIdempotencyKeyAsync(key, ownerId, null);
        Assert.NotNull(ownerView);
        Assert.Equal(ownersOrder.Id, ownerView!.Id);
    }

    [Fact]
    public async Task CreateFromCart_OtherTenantsKey_DoesNotReplayOrLeakOrder()
    {
        // Tenant A owns an order under `key`.
        var (attackerId, _, _) = await SeedCartAsync();
        var ownerId = Guid.NewGuid();
        const string key = "shared-key";

        var ownersOrder = new Order
        {
            OrderNumber = "FT-OWNER-2",
            UserId = ownerId,
            IdempotencyKey = key,
            StripeClientSecret = "pi_secret_owner",
            CreatedAt = DateTimeOffset.UtcNow,
            PaymentProcessor = PaymentProcessor.Stripe,
            DeliveryType = DeliveryType.Easybox,
            TotalRon = 26.00m,
            ShippingAddress = new ShippingAddressSnapshot
            {
                Street = "S", Number = "1", City = "C", County = "J",
                PostalCode = "010101", RecipientName = "R", Phone = "0700000000",
            },
        };
        _db.Orders.Add(ownersOrder);
        await _db.SaveChangesAsync();

        // Tenant A (the caller seeded above) presents the owner's key.
        var result = await _service.CreateFromCartAsync(attackerId, null, MakeRequest(), key);

        // Must NOT be a replay of the owner's order, and must not surface its secret.
        Assert.False(result.WasIdempotentReplay);
        Assert.NotEqual(ownersOrder.Id, result.Order.Id);
        Assert.Equal(attackerId, result.Order.UserId);
        Assert.Null(result.Order.StripeClientSecret);
    }

    [Fact]
    public async Task CreateFromCart_StaleKey_CreatesNewOrderAndFreesOldKey()
    {
        var (userId, _, _) = await SeedCartAsync();
        const string key = "idem-key-reuse";

        var staleOrder = new Order
        {
            OrderNumber = "FT-STALE-2",
            UserId = userId,                 // the caller's OWN stale row (SEC-1: only own keys are freed)
            IdempotencyKey = key,
            CreatedAt = DateTimeOffset.UtcNow.AddHours(-25),
            ShippingAddress = new ShippingAddressSnapshot
            {
                Street = "S", Number = "1", City = "C", County = "J",
                PostalCode = "010101", RecipientName = "R", Phone = "0700000000",
            },
        };
        _db.Orders.Add(staleOrder);
        await _db.SaveChangesAsync();

        var result = await _service.CreateFromCartAsync(userId, null, MakeRequest(), key);

        Assert.False(result.WasIdempotentReplay);          // not a replay — stale key
        Assert.NotEqual(staleOrder.Id, result.Order.Id);    // a brand-new order
        Assert.Equal(key, result.Order.IdempotencyKey);     // new order owns the key

        var freed = await _db.Orders.FindAsync(staleOrder.Id);
        Assert.Null(freed!.IdempotencyKey);                 // old row's key was nulled
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
