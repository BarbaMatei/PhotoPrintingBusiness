using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PhotoPrint.API.Data;
using PhotoPrint.API.Exceptions;
using PhotoPrint.API.Hubs;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services;
using Stripe;

namespace PhotoPrint.Tests.Unit.Services;

public class AdminOrderServiceTests
{
    private readonly PhotoPrintDbContext _db;
    private readonly Mock<IOrderEmailService> _emailSvc = new();
    private readonly Mock<IEuPlatescService> _euPlatesc = new();
    private readonly Mock<IStripeClient> _stripeClient = new();
    private readonly Mock<IStorageService> _storage = new();
    private readonly Mock<IHubContext<AdminOrderHub>> _hub = new();
    private readonly Mock<IHubClients> _hubClients = new();
    private readonly Mock<IClientProxy> _clientProxy = new();

    private readonly AdminOrderService _sut;

    public AdminOrderServiceTests()
    {
        var options = new DbContextOptionsBuilder<PhotoPrintDbContext>()
            .UseInMemoryDatabase($"AdminOrderSvc_{Guid.NewGuid():N}")
            .Options;
        _db = new PhotoPrintDbContext(options);

        _hub.Setup(h => h.Clients).Returns(_hubClients.Object);
        _hubClients.Setup(c => c.All).Returns(_clientProxy.Object);
        _clientProxy
            .Setup(c => c.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _sut = new AdminOrderService(
            _db,
            _emailSvc.Object,
            _euPlatesc.Object,
            _stripeClient.Object,
            _storage.Object,
            _hub.Object,
            NullLogger<AdminOrderService>.Instance);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static ShippingAddressSnapshot DefaultAddress() => new()
    {
        RecipientName = "Test User",
        Street = "Str. Test",
        Number = "1",
        City = "București",
        County = "Ilfov",
        PostalCode = "010000",
        Phone = "0700000000",
    };

    private async Task<Order> SeedOrderAsync(
        OrderStatus status = OrderStatus.Paid,
        PaymentProcessor processor = PaymentProcessor.Stripe,
        string? paymentIntentId = "pi_test_123",
        string? euTxId = null)
    {
        var order = new Order
        {
            OrderNumber = "FT-TEST-001",
            Status = status,
            PaymentProcessor = processor,
            PaymentIntentId = paymentIntentId,
            EuPlatescTransactionId = euTxId,
            DeliveryType = DeliveryType.Courier,
            ShippingAddress = DefaultAddress(),
            SubtotalRon = 30m,
            ShippingCostRon = 15m,
            TotalRon = 45m,
            Items = new List<OrderItem>
            {
                new()
                {
                    UploadId = Guid.NewGuid(),
                    ProductSnapshot = new ProductSnapshot
                    {
                        ProductName = "Foto 10x15",
                        Size = "10x15",
                        Finish = "Lucios",
                    },
                    Quantity = 10,
                    UnitPriceRon = 3m,
                    LineTotalRon = 30m,
                }
            }
        };
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();
        return order;
    }

    // ── GetOrderDetailAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetOrderDetailAsync_ExistingOrder_ReturnsDto()
    {
        var order = await SeedOrderAsync();

        var result = await _sut.GetOrderDetailAsync(order.Id);

        result.Should().NotBeNull();
        result.OrderNumber.Should().Be("FT-TEST-001");
        result.Status.Should().Be("Paid");
        result.TotalRon.Should().Be(45m);
    }

    [Fact]
    public async Task GetOrderDetailAsync_UnknownId_ThrowsNotFoundException()
    {
        var act = () => _sut.GetOrderDetailAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ── GetOrdersAsync (pagination) ───────────────────────────────────────────

    [Fact]
    public async Task GetOrdersAsync_TiedCreatedAt_PagesDeterministicallyKeepingItemsPerOrder()
    {
        // F2 (review 042-v8): the admin list is OrderByDescending(CreatedAt) + Skip/Take +
        // Include(Items) under the global SplitQuery default. With no unique tiebreaker, a page
        // boundary splitting orders that share a CreatedAt can page the parent and the Items child
        // inconsistently on Postgres -> an order returns with missing items. ThenBy(Id) makes the
        // order total so paging is stable and complete.
        // NOTE: InMemory can't split queries, so this pins the deterministic-ordering + per-order
        // item contract; the split-query symptom itself is a Postgres/3-env concern (see resolution).
        var sharedTime = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var seeded = new List<Order>();
        for (var i = 0; i < 5; i++)
        {
            seeded.Add(new Order
            {
                OrderNumber = $"FT-TIE-{i:D2}",
                Status = OrderStatus.Paid,
                PaymentProcessor = PaymentProcessor.Stripe,
                DeliveryType = DeliveryType.Courier,
                ShippingAddress = DefaultAddress(),
                CreatedAt = sharedTime,                       // all tied — ThenBy(Id) is the sole discriminator
                SubtotalRon = 10m,
                ShippingCostRon = 5m,
                TotalRon = 15m,
                Items = Enumerable.Range(0, i + 1).Select(_ => new OrderItem
                {
                    UploadId = Guid.NewGuid(),
                    ProductSnapshot = new ProductSnapshot { ProductName = "P", Size = "S", Finish = "F" },
                    Quantity = 1,
                    UnitPriceRon = 1m,
                    LineTotalRon = 1m,
                }).ToList(),                                  // order i carries i+1 items (ItemCount = i+1)
            });
        }
        // Insert in reverse-Id order so a stable sort WITHOUT the tiebreaker would not match Id order.
        foreach (var o in seeded.OrderByDescending(o => o.Id)) _db.Orders.Add(o);
        await _db.SaveChangesAsync();

        // Page through in size-2 pages (2 + 2 + 1), which splits the tied group across boundaries.
        var paged = new List<(Guid Id, string OrderNumber, int ItemCount)>();
        for (var page = 1; page <= 3; page++)
        {
            var (items, total) = await _sut.GetOrdersAsync(page, pageSize: 2, status: null, search: null);
            total.Should().Be(5);
            foreach (var dto in items)
                paged.Add((dto.Id, dto.OrderNumber, dto.ItemCount));
        }

        // Completeness: every order exactly once — none dropped or duplicated across page boundaries.
        paged.Should().HaveCount(5);
        paged.Select(p => p.Id).Should().OnlyHaveUniqueItems();

        // Per-order items survive paging (the "missing items" symptom): ItemCount == i+1.
        foreach (var p in paged)
            p.ItemCount.Should().Be(int.Parse(p.OrderNumber["FT-TIE-".Length..]) + 1);

        // Deterministic total order: tied CreatedAt -> ThenBy(Id) ascending decides the sequence.
        paged.Select(p => p.Id).Should().Equal(seeded.OrderBy(o => o.Id).Select(o => o.Id));
    }

    // ── UpdateStatusAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateStatusAsync_PaidToPrinting_SavesAndBroadcasts()
    {
        var order = await SeedOrderAsync(OrderStatus.Paid);

        var result = await _sut.UpdateStatusAsync(order.Id, "Printing", null, null);

        result.Status.Should().Be("Printing");

        var dbOrder = await _db.Orders.FindAsync(order.Id);
        dbOrder!.Status.Should().Be(OrderStatus.Printing);

        _clientProxy.Verify(c => c.SendCoreAsync(
            "OrderStatusChanged", It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_PrintingToShipped_FiresShippedEmailAndSetsAwb()
    {
        var order = await SeedOrderAsync(OrderStatus.Printing);

        var result = await _sut.UpdateStatusAsync(order.Id, "Shipped", "AWB12345", "https://track.ro/AWB12345");

        result.Status.Should().Be("Shipped");
        result.AwbNumber.Should().Be("AWB12345");
        result.TrackingUrl.Should().Be("https://track.ro/AWB12345");

        _emailSvc.Verify(e => e.FireOrderShippedEmail(It.IsAny<Order>()), Times.Once);
        _emailSvc.Verify(e => e.FireOrderDeliveredEmail(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task UpdateStatusAsync_ShippedToDelivered_FiresDeliveredEmail()
    {
        var order = await SeedOrderAsync(OrderStatus.Shipped);

        var result = await _sut.UpdateStatusAsync(order.Id, "Delivered", null, null);

        result.Status.Should().Be("Delivered");

        _emailSvc.Verify(e => e.FireOrderDeliveredEmail(It.IsAny<Order>()), Times.Once);
        _emailSvc.Verify(e => e.FireOrderShippedEmail(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task UpdateStatusAsync_UnknownStatus_ThrowsBadRequestException()
    {
        var order = await SeedOrderAsync(OrderStatus.Paid);

        var act = () => _sut.UpdateStatusAsync(order.Id, "Bogus", null, null);

        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task UpdateStatusAsync_InvalidTransition_ThrowsInvalidOrderTransitionException()
    {
        var order = await SeedOrderAsync(OrderStatus.Delivered);

        var act = () => _sut.UpdateStatusAsync(order.Id, "Printing", null, null);

        await act.Should().ThrowAsync<InvalidOrderTransitionException>();
    }

    // ── CancelOrderAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task CancelOrderAsync_StripeOrder_AttemptsStripeRefund()
    {
        var order = await SeedOrderAsync(
            OrderStatus.Paid,
            PaymentProcessor.Stripe,
            "pi_real_123");

        _stripeClient
            .Setup(c => c.RequestAsync<Refund>(
                It.IsAny<HttpMethod>(),
                It.IsAny<string>(),
                It.IsAny<BaseOptions>(),
                It.IsAny<RequestOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Refund { Id = "re_test_ok" });

        var result = await _sut.CancelOrderAsync(order.Id, null);

        result.Status.Should().Be("Cancelled");

        var dbOrder = await _db.Orders.FindAsync(order.Id);
        dbOrder!.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public async Task CancelOrderAsync_EuPlatescOrder_AttemptsEuPlatescRefund()
    {
        var order = await SeedOrderAsync(
            OrderStatus.Paid,
            PaymentProcessor.EuPlatesc,
            null,
            "EP-TX-999");

        _euPlatesc
            .Setup(e => e.RefundAsync("EP-TX-999", 45m, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.CancelOrderAsync(order.Id, null);

        result.Status.Should().Be("Cancelled");
        _euPlatesc.Verify(e => e.RefundAsync("EP-TX-999", 45m, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelOrderAsync_RefundThrows_OrderStillCancelledAndExceptionSwallowed()
    {
        var order = await SeedOrderAsync(
            OrderStatus.Paid,
            PaymentProcessor.EuPlatesc,
            null,
            "EP-TX-FAIL");

        _euPlatesc
            .Setup(e => e.RefundAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("EuPlatesc gateway timeout"));

        // Should NOT throw — refund errors are swallowed
        var result = await _sut.CancelOrderAsync(order.Id, null);

        result.Status.Should().Be("Cancelled");

        var dbOrder = await _db.Orders.FindAsync(order.Id);
        dbOrder!.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public async Task CancelOrderAsync_BroadcastsSignalR()
    {
        var order = await SeedOrderAsync(OrderStatus.Paid);

        await _sut.CancelOrderAsync(order.Id, null);

        _clientProxy.Verify(c => c.SendCoreAsync(
            "OrderStatusChanged", It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── UpdateNotesAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateNotesAsync_SetsInternalNotes()
    {
        var order = await SeedOrderAsync();

        var result = await _sut.UpdateNotesAsync(order.Id, "Handle with care");

        result.InternalNotes.Should().Be("Handle with care");

        var dbOrder = await _db.Orders.FindAsync(order.Id);
        dbOrder!.InternalNotes.Should().Be("Handle with care");
    }

    [Fact]
    public async Task UpdateNotesAsync_NullNotes_ClearsInternalNotes()
    {
        var order = await SeedOrderAsync();
        order.InternalNotes = "Old note";
        await _db.SaveChangesAsync();

        var result = await _sut.UpdateNotesAsync(order.Id, null);

        result.InternalNotes.Should().BeNull();
    }

    // ── GetOrdersAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetOrdersAsync_ReturnsAllOrders_Paged()
    {
        await SeedOrderAsync(OrderStatus.Paid);
        await SeedOrderAsync(OrderStatus.Printing);

        var (items, total) = await _sut.GetOrdersAsync(1, 10, null, null);

        total.Should().Be(2);
        items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetOrdersAsync_FilterByStatus_ReturnsMatchingOnly()
    {
        await SeedOrderAsync(OrderStatus.Paid);
        await SeedOrderAsync(OrderStatus.Printing);

        var (items, total) = await _sut.GetOrdersAsync(1, 10, "Paid", null);

        total.Should().Be(1);
        items.Single().Status.Should().Be("Paid");
    }
}
