using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Data;
using PhotoPrint.API.DTOs.Payments;
using PhotoPrint.API.DTOs.Shipping;
using PhotoPrint.API.Exceptions;
using PhotoPrint.API.Models;
using PhotoPrint.API.Observability;
using PhotoPrint.API.Services;
using PhotoPrint.Tests.Helpers;

namespace PhotoPrint.Tests.Unit.Services;

public class OrderServiceTests : IDisposable
{
    private readonly PhotoPrintDbContext _db;
    private readonly Mock<IOrderNumberService> _orderNumberServiceMock;
    private readonly Mock<IShippingService> _shippingMock;
    private readonly Mock<IStorageRouter> _storageRouterMock;
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

        // OrderService now depends on storage for the photos endpoint. Default
        // to cloud-tier-off so existing tests don't accidentally exercise the new path;
        // Photos tests construct their own SUT with cloud enabled.
        _storageRouterMock = new Mock<IStorageRouter>();
        _storageRouterMock.SetupGet(r => r.CloudEnabled).Returns(false);

        _service = new OrderService(
            _db,
            _orderNumberServiceMock.Object,
            _shippingMock.Object,
            _storageRouterMock.Object,
            TestCoupons.ServiceFor(_db),
            Options.Create(new StorageSettings()),
            Options.Create(new VatSettings()));
    }

    public void Dispose() => _db.Dispose();

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<(Guid userId, Guid productId, Guid uploadId)> SeedCartAsync(
        decimal unitPrice = 2.00m,
        int quantity = 3)
    {
        var userId = Guid.NewGuid();

        // Shared canonical cart graph — see TestCartSeed.
        var graph = TestCartSeed.Build(userId: userId, unitPrice: unitPrice, quantity: quantity);
        graph.AddTo(_db);
        await _db.SaveChangesAsync();

        return (userId, graph.Product.Id, graph.Upload.Id);
    }

    private static CreateOrderRequest MakeRequest()
        => new CreateOrderRequest(
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

    // ── VAT breakdown (bolt 038) ──────────────────────────────────────────────

    [Fact]
    public async Task CreateFromCartAsync_ValidCart_PopulatesVatBreakdown()
    {
        // Cart subtotal 6 + shipping 20 = gross 26.00. At 19% (default):
        //   VatRon = round(26.00 * 0.19 / 1.19, 2) = round(4.1512..., 2) = 4.15
        //   NetTotalRon = 26.00 - 4.15 = 21.85
        var (userId, _, _) = await SeedCartAsync(unitPrice: 2.00m, quantity: 3);

        var order = (await _service.CreateFromCartAsync(userId, null, MakeRequest())).Order;

        Assert.Equal(21.85m, order.NetTotalRon);
        Assert.Equal(4.15m,  order.VatRon);
        Assert.Equal(0.19m,  order.VatRate);
        // Reconciles to the gross within rounding tolerance.
        Assert.True(Math.Abs((order.NetTotalRon + order.VatRon) - order.TotalRon) <= 0.01m);
    }

    [Fact]
    public async Task CreateFromCartAsync_FreeOrder_HasZeroVat()
    {
        // Future intent 022 (coupons) will produce zero-gross orders. Guard
        // against div-by-zero / negative-VAT regressions today.
        var (userId, _, _) = await SeedCartAsync(unitPrice: 0m, quantity: 1);
        _shippingMock.Setup(s => s.GetShippingCostAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShippingCostDto(0m));

        var order = (await _service.CreateFromCartAsync(userId, null, MakeRequest())).Order;

        Assert.Equal(0m, order.NetTotalRon);
        Assert.Equal(0m, order.VatRon);
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

    // ── Idempotency ──────────────────────────────────────────────────

    [Fact]
    public async Task CreateFromCart_SameKey_SameRequest_ReplaysOriginalOrder()
    {
        var (userId, _, _) = await SeedCartAsync(unitPrice: 2.00m, quantity: 3);
        const string key = "idem-key-001";

        // Reuse the SAME request instance — MakeRequest randomizes EasyboxLockerId
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
    public async Task CreateFromCart_SameKey_DivergentDeliveryType_ThrowsConflictNamingField()
    {
        var (userId, _, _) = await SeedCartAsync();
        const string key = "idem-key-002";

        // Same base request; vary ONLY the delivery type so the divergence is unambiguous.
        var request = MakeRequest();
        await _service.CreateFromCartAsync(userId, null, request, key);

        var divergent = request with { DeliveryType = DeliveryType.Courier };
        var ex = await Assert.ThrowsAsync<IdempotencyConflictException>(
            () => _service.CreateFromCartAsync(userId, null, divergent, key));

        Assert.Contains("deliveryType", ex.DivergentFields);
        Assert.DoesNotContain("easyboxLockerId", ex.DivergentFields);
        // Still only one order — the conflicting second request created nothing.
        Assert.Equal(1, await _db.Orders.CountAsync());
    }

    [Fact]
    public async Task CreateFromCart_SameKey_SameTotalDifferentItems_ThrowsConflictNamingItems()
    {
        // With uniform per-unit pricing, a different photo at the same qty has
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

    // ── Idempotency resolution scoped to the caller (tenant isolation) ────
    // GetByIdempotencyKeyAsync was removed as dead production code,
    // so these behaviors are now asserted through the only real entry point,
    // CreateFromCartAsync. The stale-window and cross-tenant cases are covered by
    // CreateFromCart_StaleKey_* and CreateFromCart_OtherTenantsKey_* below (previously
    // duplicated by GetByIdempotencyKey_* tests); the both-null guard is retargeted here.

    [Fact]
    public async Task CreateFromCart_BothIdentitiesNull_Throws_DoesNotResolveArbitraryOrder()
    {
        // An idempotency resolution with neither a user nor a guest
        // identity must be rejected by FindKeyHolderAsync's guard rather than silently
        // collapsing to "any guestless order" (which would disclose an arbitrary user's
        // order/secret). The cart's items are guest-null, so a both-null call reaches the
        // idempotency block and the guard fires.
        await SeedCartAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateFromCartAsync(null, null, MakeRequest(), "idem-key-both-null"));
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
            UserId = userId,                 // the caller's OWN stale row (only own keys are freed)
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

    [Fact]
    public async Task CreateFromCart_SameKey_AfterOrderPaid_DoesNotReplayThePaidOrder()
    {
        var (userId, _, _) = await SeedCartAsync(unitPrice: 2.00m, quantity: 3);
        const string key = "idem-key-settled";

        var request = MakeRequest();
        var first = await _service.CreateFromCartAsync(userId, null, request, key);
        first.Order.Status = OrderStatus.Paid;
        first.Order.StripeClientSecret = "pi_secret_paid";
        first.Order.PaidAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<IdempotencyKeyConsumedException>(
            () => _service.CreateFromCartAsync(userId, null, request, key));

        Assert.Equal(first.Order.Id, ex.OrderId);
        Assert.Equal(1, await _db.Orders.CountAsync());
    }

    [Fact]
    public async Task CreateFromCart_SameKey_AfterPaymentFailed_FreesTheKeyAndCreatesANewOrder()
    {
        var (userId, _, _) = await SeedCartAsync();
        const string key = "idem-key-failed-attempt";

        var request = MakeRequest();
        var first = await _service.CreateFromCartAsync(userId, null, request, key);
        first.Order.Status = OrderStatus.PaymentFailed;
        await _db.SaveChangesAsync();

        var second = await _service.CreateFromCartAsync(userId, null, request, key);

        Assert.False(second.WasIdempotentReplay);
        Assert.NotEqual(first.Order.Id, second.Order.Id);
        Assert.Equal(key, second.Order.IdempotencyKey);
        var freed = await _db.Orders.FindAsync(first.Order.Id);
        Assert.Null(freed!.IdempotencyKey);
    }

    // Handing the key on leaves the failed order's intent chargeable, so one basket would hold
    // two confirmable intents — the double charge the idempotency key exists to prevent.
    [Fact]
    public async Task CreateFromCart_SameKey_AfterPaymentFailed_AbandonsTheOldPaymentIntent()
    {
        var (userId, _, _) = await SeedCartAsync();
        const string key = "idem-key-failed-intent";
        var gateway = new RecordingGateway();
        var sut = new OrderService(
            _db, _orderNumberServiceMock.Object, _shippingMock.Object, _storageRouterMock.Object,
            TestCoupons.ServiceFor(_db),
            Options.Create(new StorageSettings()), Options.Create(new VatSettings()),
            paymentGateway: gateway);

        var request = MakeRequest();
        var first = await sut.CreateFromCartAsync(userId, null, request, key);
        first.Order.Status = OrderStatus.PaymentFailed;
        first.Order.PaymentIntentId = "pi_declined";
        first.Order.StripeClientSecret = "secret_declined";
        await _db.SaveChangesAsync();

        await sut.CreateFromCartAsync(userId, null, request, key);

        Assert.Contains("pi_declined", gateway.Cancelled);
        var abandoned = await _db.Orders.FindAsync(first.Order.Id);
        Assert.Null(abandoned!.StripeClientSecret);
    }

    private sealed class RecordingGateway : IStripePaymentGateway
    {
        public List<string> Cancelled { get; } = [];

        public Task<(string ClientSecret, string PaymentIntentId)> CreatePaymentIntentAsync(
            long amountBani, string currency, string orderIdMetadata,
            string? idempotencyKey = null, CancellationToken ct = default) =>
            Task.FromResult(("secret", "pi_new"));

        public Task<bool> CancelPaymentIntentAsync(string paymentIntentId, CancellationToken ct = default)
        {
            Cancelled.Add(paymentIntentId);
            return Task.FromResult(true);
        }
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

    // ── Bolt 053: GetOrderPhotosAsync ────────────────────────────────────────

    /// <summary>
    /// Builds a separate OrderService SUT with the cloud tier ENABLED — the default
    /// _service field in this fixture has cloud off (so legacy tests don't touch the new
    /// path). The mocked Cloud adapter returns a deterministic URL per (key, ttl).
    /// </summary>
    private (OrderService Sut, Mock<IStorageService> CloudMock) CreateSutWithCloud(int presignTtlMinutes = 60)
    {
        var cloud = new Mock<IStorageService>(MockBehavior.Strict);
        cloud.Setup(s => s.GetPresignedUrlAsync(
                It.IsAny<string>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
             .ReturnsAsync((string key, TimeSpan ttl, CancellationToken _) =>
                 $"https://cdn.test/{key}?sig=test&ttl={(int)ttl.TotalMinutes}");

        var router = new Mock<IStorageRouter>();
        router.SetupGet(r => r.CloudEnabled).Returns(true);
        router.SetupGet(r => r.Cloud).Returns(cloud.Object);

        var settings = Options.Create(new StorageSettings
        {
            PresignTtlMinutes = presignTtlMinutes,
        });

        var sut = new OrderService(
            _db, _orderNumberServiceMock.Object, _shippingMock.Object, router.Object,
            TestCoupons.ServiceFor(_db),
            settings, Options.Create(new VatSettings()));
        return (sut, cloud);
    }

    private async Task<(Order Order, Upload Upload)> SeedPaidOrderWithPromotedUploadAsync(
        StorageLocation loc = StorageLocation.Cloud,
        string? largePreviewPath = "previews/abc.jpg",
        string? thumbnailPath = "thumbs/abc.jpg",
        string fileName = "photo.jpg",
        Guid? userId = null)
    {
        var owner = userId ?? Guid.NewGuid();
        var upload = new Upload
        {
            UserId = owner,
            OriginalFileName = fileName,
            FilePath = "uploads/2026/01/abc.jpg",
            LargePreviewPath = largePreviewPath,
            ThumbnailPath = thumbnailPath,
            StorageLocation = loc,
            ContentType = "image/jpeg",
            WidthPx = 4000, HeightPx = 3000, FileSizeBytes = 5_000_000,
        };
        var order = new Order
        {
            OrderNumber = "FT-" + Random.Shared.Next(100_000, 999_999),
            UserId = owner,
            Status = OrderStatus.Paid,
            ShippingAddress = new ShippingAddressSnapshot
            {
                RecipientName = "x", Phone = "x",
                Street = "x", Number = "1",
                City = "x", County = "x", PostalCode = "0",
            },
            DeliveryType = DeliveryType.Easybox,
            PaidAt = DateTimeOffset.UtcNow.AddDays(-3),
            Items = new List<OrderItem>
            {
                new()
                {
                    UploadId = upload.Id, Upload = upload,
                    ProductId = Guid.NewGuid(),
                    Quantity = 1, UnitPriceRon = 1, LineTotalRon = 1,
                    ProductSnapshot = new ProductSnapshot
                    {
                        ProductName = "x", Size = "x", Finish = "x",
                    },
                },
            },
        };
        _db.Uploads.Add(upload);
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();
        return (order, upload);
    }

    [Fact]
    public async Task GetOrderPhotosAsync_OrderNotFound_ThrowsNotFoundException()
    {
        var (sut, _) = CreateSutWithCloud();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            sut.GetOrderPhotosAsync(Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public async Task GetOrderPhotosAsync_NonOwner_ThrowsForbiddenException()
    {
        var (sut, _) = CreateSutWithCloud();
        var (order, _) = await SeedPaidOrderWithPromotedUploadAsync();

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            sut.GetOrderPhotosAsync(order.Id, Guid.NewGuid())); // different user
    }

    [Fact]
    public async Task GetOrderPhotosAsync_CloudTierOff_ReturnsEmptyPhotos()
    {
        // Use the fixture's _service (cloud off) — should NOT throw, just return empty.
        var (order, _) = await SeedPaidOrderWithPromotedUploadAsync();

        var result = await _service.GetOrderPhotosAsync(order.Id, order.UserId!.Value);

        Assert.Empty(result.Photos);
    }

    [Fact]
    public async Task GetOrderPhotosAsync_HappyPath_ReturnsPresignedUrlsForEachPhoto()
    {
        var (sut, cloud) = CreateSutWithCloud();
        var (order, upload) = await SeedPaidOrderWithPromotedUploadAsync(fileName: "vacation.jpg");

        var result = await sut.GetOrderPhotosAsync(order.Id, order.UserId!.Value);

        Assert.Single(result.Photos);
        var photo = result.Photos[0];
        Assert.Equal(upload.Id, photo.UploadId);
        Assert.Equal("vacation.jpg", photo.FileName);
        Assert.Equal($"https://cdn.test/thumbs/abc.jpg?sig=test&ttl=60", photo.ThumbnailUrl);
        Assert.Equal($"https://cdn.test/previews/abc.jpg?sig=test&ttl=60", photo.LargeUrl);

        cloud.Verify(c => c.GetPresignedUrlAsync(
            "thumbs/abc.jpg", It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once);
        cloud.Verify(c => c.GetPresignedUrlAsync(
            "previews/abc.jpg", It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetOrderPhotosAsync_LocalUpload_ExcludedFromResults()
    {
        var (sut, cloud) = CreateSutWithCloud();
        var (order, _) = await SeedPaidOrderWithPromotedUploadAsync(loc: StorageLocation.Local);

        var result = await sut.GetOrderPhotosAsync(order.Id, order.UserId!.Value);

        Assert.Empty(result.Photos);
        cloud.VerifyNoOtherCalls(); // no presigning attempted for Local
    }

    [Fact]
    public async Task GetOrderPhotosAsync_LargePreviewPathNull_ExcludedFromResults()
    {
        // Mid-retention half-expired row: thumb still cached, preview gone.
        var (sut, _) = CreateSutWithCloud();
        var (order, _) = await SeedPaidOrderWithPromotedUploadAsync(largePreviewPath: null);

        var result = await sut.GetOrderPhotosAsync(order.Id, order.UserId!.Value);

        Assert.Empty(result.Photos);
    }

    [Fact]
    public async Task GetOrderPhotosAsync_SoftDeletedUpload_ExcludedFromResults()
    {
        // UploadCleanupJob soft-deletes the row and deletes its cloud
        // blobs but leaves the path fields set; without a DeletedAt filter this endpoint
        // presigned URLs for the deleted blobs — broken thumbnails/lightbox that the
        // one-shot refresh cannot recover.
        var (sut, cloud) = CreateSutWithCloud();
        var (order, upload) = await SeedPaidOrderWithPromotedUploadAsync();
        upload.DeletedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();

        var result = await sut.GetOrderPhotosAsync(order.Id, order.UserId!.Value);

        Assert.Empty(result.Photos);
        cloud.VerifyNoOtherCalls(); // no presigning of dead blobs
    }

    [Fact]
    public async Task GetOrderPhotosAsync_ThumbnailPathNull_ExcludedFromResults()
    {
        // Mirror of the above — preview kept, thumb gone. Either-null filters the row out.
        var (sut, _) = CreateSutWithCloud();
        var (order, _) = await SeedPaidOrderWithPromotedUploadAsync(thumbnailPath: null);

        var result = await sut.GetOrderPhotosAsync(order.Id, order.UserId!.Value);

        Assert.Empty(result.Photos);
    }

    [Fact]
    public async Task GetOrderPhotosAsync_PresignTtl_MatchesConfiguredMinutes()
    {
        var (sut, cloud) = CreateSutWithCloud(presignTtlMinutes: 90);
        var (order, _) = await SeedPaidOrderWithPromotedUploadAsync();

        var result = await sut.GetOrderPhotosAsync(order.Id, order.UserId!.Value);

        Assert.Single(result.Photos);
        // The deterministic mock URL embeds the requested TTL — proves the value flowed
        // from StorageSettings.PresignTtlMinutes through to the SDK call.
        Assert.Contains("ttl=90", result.Photos[0].ThumbnailUrl);
        Assert.Contains("ttl=90", result.Photos[0].LargeUrl);

        cloud.Verify(c => c.GetPresignedUrlAsync(
            It.IsAny<string>(),
            It.Is<TimeSpan>(t => t == TimeSpan.FromMinutes(90)),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    // ── orders_created_total emission ─────────────────────────────────────────

    [Fact]
    public async Task CreateFromCartAsync_RecordsOrdersCreatedWithTheStripeProcessorLabel()
    {
        var (userId, _, _) = await SeedCartAsync();
        using var metrics = new MetricCapture(MetricNames.Instruments.OrdersCreatedTotal);

        await _service.CreateFromCartAsync(userId, null, MakeRequest());

        var recorded = metrics.For(
            MetricNames.Instruments.OrdersCreatedTotal,
            (MetricNames.Labels.Processor, MetricNames.ProcessorValues.Stripe),
            (MetricNames.Labels.Status, MetricNames.OrderStatusValues.Created));
        Assert.Single(recorded);
        Assert.Equal(1, recorded[0].Value);
        Assert.Equal(
            new[] { MetricNames.Labels.Processor, MetricNames.Labels.Status },
            recorded[0].Tags.Keys.OrderBy(k => k, StringComparer.Ordinal));
        Assert.Empty(metrics.ContractViolations());
    }

    [Fact]
    public async Task CreateFromCartAsync_IdempotentReplay_DoesNotDoubleCountOrdersCreated()
    {
        var (userId, _, _) = await SeedCartAsync();
        var request = MakeRequest();
        var key = Guid.NewGuid().ToString();
        using var metrics = new MetricCapture(MetricNames.Instruments.OrdersCreatedTotal);

        await _service.CreateFromCartAsync(userId, null, request, key);
        await _service.CreateFromCartAsync(userId, null, request, key);

        Assert.Single(metrics.Measurements);
    }

    [Fact]
    public async Task CreateFromCartAsync_EmptyCart_RecordsNoOrdersCreated()
    {
        var userId = Guid.NewGuid();
        using var metrics = new MetricCapture(MetricNames.Instruments.OrdersCreatedTotal);

        await Assert.ThrowsAsync<BadRequestException>(
            () => _service.CreateFromCartAsync(userId, null, MakeRequest(), default));

        Assert.Empty(metrics.Measurements);
    }
}
