using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using PhotoPrint.API.Configuration;
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
    private readonly Mock<IOriginalPurger> _purger = new();
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

        // Default: cloud tier off so existing tests don't accidentally fire the purger.
        // Tests that need to verify purge wiring set the option explicitly.
        _purger.Setup(p => p.PurgeOrderOriginalsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(PurgeOutcome.Empty);

        _sut = new AdminOrderService(
            _db,
            _emailSvc.Object,
            _euPlatesc.Object,
            _stripeClient.Object,
            _storage.Object,
            _purger.Object,
            Options.Create(new ArchiveSettings()),
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

    // ── Bolt 052: original-purge hook on production-complete transition ──────

    /// <summary>
    /// Builds an SUT with a custom ArchiveSettings — default test setup uses defaults
    /// (PurgeOriginalAtStatus = Shipped). This override lets a single test pretend
    /// PurgeOriginalAtStatus = Delivered without disturbing the shared _sut.
    /// </summary>
    private AdminOrderService BuildSutWithArchive(ArchiveSettings archive)
        => new(_db, _emailSvc.Object, _euPlatesc.Object, _stripeClient.Object,
            _storage.Object, _purger.Object, Options.Create(archive),
            _hub.Object, NullLogger<AdminOrderService>.Instance);

    [Fact]
    public async Task UpdateStatusAsync_PrintingToShipped_TriggersOriginalPurge()
    {
        var order = await SeedOrderAsync(OrderStatus.Printing);

        await _sut.UpdateStatusAsync(order.Id, "Shipped", "AWB", null);

        _purger.Verify(
            p => p.PurgeOrderOriginalsAsync(order.Id, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_PaidToPrinting_DoesNotTriggerPurge()
    {
        var order = await SeedOrderAsync(OrderStatus.Paid);

        await _sut.UpdateStatusAsync(order.Id, "Printing", null, null);

        _purger.Verify(
            p => p.PurgeOrderOriginalsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateStatusAsync_ShippedToDelivered_DoesNotTriggerPurge_WithDefaultConfig()
    {
        // Default PurgeOriginalAtStatus = Shipped → the purge already fired on Shipped.
        // The Shipped → Delivered transition must NOT re-fire it.
        var order = await SeedOrderAsync(OrderStatus.Shipped);

        await _sut.UpdateStatusAsync(order.Id, "Delivered", null, null);

        _purger.Verify(
            p => p.PurgeOrderOriginalsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateStatusAsync_ConfigSetToDelivered_OnlyDeliveredTriggersPurge()
    {
        var sut = BuildSutWithArchive(new ArchiveSettings
        {
            PurgeOriginalAtStatus = "Delivered",
        });

        // Printing → Shipped MUST NOT trigger (Shipped is no longer the production-complete status).
        var order1 = await SeedOrderAsync(OrderStatus.Printing);
        await sut.UpdateStatusAsync(order1.Id, "Shipped", "AWB", null);

        _purger.Verify(
            p => p.PurgeOrderOriginalsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);

        // Shipped → Delivered SHOULD trigger.
        var order2 = await SeedOrderAsync(OrderStatus.Shipped);
        await sut.UpdateStatusAsync(order2.Id, "Delivered", null, null);

        _purger.Verify(
            p => p.PurgeOrderOriginalsAsync(order2.Id, It.IsAny<CancellationToken>()),
            Times.Once);
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
