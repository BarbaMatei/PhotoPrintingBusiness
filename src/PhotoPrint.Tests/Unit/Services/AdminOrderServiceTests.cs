using System.IO.Compression;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
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
using PhotoPrint.API.Services.Sameday;
using Stripe;

namespace PhotoPrint.Tests.Unit.Services;

public class AdminOrderServiceTests
{
    private readonly PhotoPrintDbContext _db;
    private readonly Mock<IOrderEmailService> _emailSvc = new();
    private readonly Mock<IEuPlatescService> _euPlatesc = new();
    private readonly Mock<IStripeClient> _stripeClient = new();
    private readonly Mock<IStorageRouter> _router = new();
    private readonly Mock<IStorageService> _localStore = new();
    private readonly Mock<IStorageService> _cloudStore = new();
    private readonly Mock<IOriginalPurger> _purger = new();
    private readonly Mock<IHubContext<AdminOrderHub>> _hub = new();
    private readonly Mock<IHubClients> _hubClients = new();
    private readonly Mock<IClientProxy> _clientProxy = new();
    private readonly Mock<IAwbCreationNotifier> _awbNotifier = new();

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

        // The purger is a no-op stub by default; individual tests assert whether it was called.
        _purger.Setup(p => p.PurgeOrderOriginalsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(PurgeOutcome.Empty);

        // Router resolves each tier to its own fake store; the ZIP read routes by
        // Upload.StorageLocation (F1, review 043-v1). Cloud tier on so purge-on-cancel (F17)
        // runs — the purger mock returns Empty.
        _router.SetupGet(r => r.CloudEnabled).Returns(true);
        _router.SetupGet(r => r.Local).Returns(_localStore.Object);
        _router.SetupGet(r => r.Cloud).Returns(_cloudStore.Object);
        _router.Setup(r => r.For(StorageLocation.Local)).Returns(_localStore.Object);
        _router.Setup(r => r.For(StorageLocation.Cloud)).Returns(_cloudStore.Object);

        _awbNotifier.Setup(n => n.NotifyPaidAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

        _sut = new AdminOrderService(
            _db,
            _emailSvc.Object,
            _euPlatesc.Object,
            _stripeClient.Object,
            _router.Object,
            _purger.Object,
            Options.Create(new ArchiveSettings()),
            _hub.Object,
            _awbNotifier.Object,
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

    [Fact]
    public async Task UpdateStatusAsync_PrintingToShipped_StampsShippedAt()
    {
        // The tracking job only polls orders with ShippedAt != null.
        var order = await SeedOrderAsync(OrderStatus.Printing);

        await _sut.UpdateStatusAsync(order.Id, "Shipped", "AWB", null);

        (await _db.Orders.FindAsync(order.Id))!.ShippedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateStatusAsync_ShippedToDelivered_StampsDeliveredAt()
    {
        var order = await SeedOrderAsync(OrderStatus.Shipped);

        await _sut.UpdateStatusAsync(order.Id, "Delivered", null, null);

        (await _db.Orders.FindAsync(order.Id))!.DeliveredAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateStatusAsync_ShippedWithoutAwb_PreservesMachineCreatedAwb()
    {
        var order = await SeedOrderAsync(OrderStatus.Printing);
        order.AwbNumber = "RO-AUTO-999"; // machine-created by the AWB job while Paid
        await _db.SaveChangesAsync();

        await _sut.UpdateStatusAsync(order.Id, "Shipped", awbNumber: null, trackingUrl: null);

        (await _db.Orders.FindAsync(order.Id))!.AwbNumber.Should().Be("RO-AUTO-999");
    }

    [Fact]
    public async Task UpdateStatusAsync_AwaitingPaymentToPaid_StampsPaidAtAndEnqueuesAwb()
    {
        // Offline / manual reconciliation: must mirror the webhook Paid path.
        var order = await SeedOrderAsync(OrderStatus.AwaitingPayment);

        await _sut.UpdateStatusAsync(order.Id, "Paid", null, null);

        var dbOrder = await _db.Orders.FindAsync(order.Id);
        dbOrder!.PaidAt.Should().NotBeNull();
        _awbNotifier.Verify(
            n => n.NotifyPaidAsync(order.Id, It.IsAny<CancellationToken>()), Times.Once);
        _emailSvc.Verify(e => e.FireOrderConfirmedEmail(It.IsAny<Order>()), Times.Once);
    }

    // ── Bolt 052: original-purge hook on production-complete transition ──────

    /// <summary>
    /// Builds an SUT with a custom ArchiveSettings — default test setup uses defaults
    /// (PurgeOriginalAtStatus = Shipped). This override lets a single test pretend
    /// PurgeOriginalAtStatus = Delivered without disturbing the shared _sut.
    /// </summary>
    private AdminOrderService BuildSutWithArchive(ArchiveSettings archive)
        => new(_db, _emailSvc.Object, _euPlatesc.Object, _stripeClient.Object,
            _router.Object, _purger.Object, Options.Create(archive),
            _hub.Object, _awbNotifier.Object, NullLogger<AdminOrderService>.Instance);

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
    public async Task UpdateStatusAsync_ShipWithCloudTierOff_DoesNotInvokePurger()
    {
        // D57 (review 043-v7): with the supported Provider=local config the ship path called
        // the purger ungated and its self-refusal logged an Error on EVERY ship — chronic
        // false-alarm noise. The ship path now gates on CloudEnabled like the cancel path;
        // the archive-on-but-cloud-off mismatch is surfaced by the recovery scanners instead.
        var cloudOffRouter = new Mock<IStorageRouter>();
        cloudOffRouter.SetupGet(r => r.CloudEnabled).Returns(false);
        cloudOffRouter.SetupGet(r => r.Local).Returns(_localStore.Object);
        cloudOffRouter.Setup(r => r.For(StorageLocation.Local)).Returns(_localStore.Object);
        var sut = new AdminOrderService(
            _db, _emailSvc.Object, _euPlatesc.Object, _stripeClient.Object,
            cloudOffRouter.Object, _purger.Object, Options.Create(new ArchiveSettings()),
            _hub.Object, _awbNotifier.Object, NullLogger<AdminOrderService>.Instance);
        var order = await SeedOrderAsync(OrderStatus.Printing);

        await sut.UpdateStatusAsync(order.Id, "Shipped", "AWB", null);

        _purger.Verify(
            p => p.PurgeOrderOriginalsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
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

    [Fact]
    public async Task UpdateStatusAsync_ProductionCompletePurgeThrows_TransitionStillCommittedAndNotified()
    {
        // F4 (review 043-v3): the production-complete purge is best-effort, like its cancel sibling
        // (F17). A purge throw must NOT 500 the PATCH after Shipped is already committed + emailed +
        // broadcast — the recovery sweep backstops it. Removing the try/catch reddens this.
        var order = await SeedOrderAsync(OrderStatus.Printing);
        _purger.Setup(p => p.PurgeOrderOriginalsAsync(order.Id, It.IsAny<CancellationToken>()))
               .ThrowsAsync(new InvalidOperationException("purge backend unavailable"));

        var result = await _sut.UpdateStatusAsync(order.Id, "Shipped", "AWB", null); // must NOT throw

        result.Status.Should().Be("Shipped");
        (await _db.Orders.FindAsync(order.Id))!.Status.Should().Be(OrderStatus.Shipped);
        _emailSvc.Verify(e => e.FireOrderShippedEmail(It.IsAny<Order>()), Times.Once);
        _clientProxy.Verify(c => c.SendCoreAsync(
            "OrderStatusChanged", It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Once);
        _purger.Verify(
            p => p.PurgeOrderOriginalsAsync(order.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── StreamZipAsync (F1, review 043-v1) ────────────────────────────────────

    [Fact]
    public async Task StreamZipAsync_PromotedCloudOrder_ReadsOriginalsFromCloudTier()
    {
        // A paid order promoted to cloud: StorageLocation=Cloud, FilePath still set (same key,
        // new tier), local copy best-effort-deleted. The admin fulfilment ZIP must read the
        // original from the cloud tier. The pre-fix code read the local-only default store →
        // FileNotFoundException mid-ZIP → corrupt download, admin can't print.
        var upload = new Upload
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            FilePath = "uploads/2026/05/original.jpg",
            StorageLocation = StorageLocation.Cloud,
            OriginalFileName = "photo.jpg",
            ContentType = "image/jpeg",
            WidthPx = 800, HeightPx = 600, FileSizeBytes = 4,
            UploadedAt = DateTimeOffset.UtcNow,
        };
        _db.Uploads.Add(upload);
        var order = new Order
        {
            OrderNumber = "FT-ZIP-001",
            Status = OrderStatus.Printing,
            PaymentProcessor = PaymentProcessor.Stripe,
            DeliveryType = DeliveryType.Courier,
            ShippingAddress = DefaultAddress(),
            SubtotalRon = 10m, ShippingCostRon = 5m, TotalRon = 15m,
            Items = new List<OrderItem>
            {
                new()
                {
                    UploadId = upload.Id, Upload = upload,
                    ProductSnapshot = new ProductSnapshot
                    {
                        ProductName = "Foto", Size = "10x15", Finish = "Lucios",
                    },
                    Quantity = 1, UnitPriceRon = 10m, LineTotalRon = 10m,
                }
            },
        };
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        var cloudBytes = new byte[] { 1, 2, 3, 4 };
        _cloudStore
            .Setup(s => s.GetStreamAsync(upload.FilePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new MemoryStream(cloudBytes));
        // The local tier no longer holds the bytes — this is where the pre-fix path read.
        _localStore
            .Setup(s => s.GetStreamAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FileNotFoundException("Stored upload not found."));

        var httpContext = new DefaultHttpContext();
        using var body = new MemoryStream();
        httpContext.Response.Body = body;

        await _sut.StreamZipAsync(order.Id, httpContext.Response);

        body.Position = 0;
        using var archive = new ZipArchive(body, ZipArchiveMode.Read);
        var entry = archive.Entries.Should().ContainSingle().Subject;
        entry.Name.Should().EndWith(".jpg");

        await using var entryStream = entry.Open();
        using var read = new MemoryStream();
        await entryStream.CopyToAsync(read);
        read.ToArray().Should().Equal(cloudBytes);

        _cloudStore.Verify(
            s => s.GetStreamAsync(upload.FilePath, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StreamZipAsync_CloudOriginalWithCloudDisabled_FailsBeforeWritingAnyBody()
    {
        // F9 (review 043-v3): cloud was reverted to local while a Cloud-located original is
        // un-purged. For(Cloud) is unroutable. The pre-fix code threw mid-stream — after the ZIP
        // headers + earlier entries were committed to Response.Body — handing the admin a truncated
        // ZIP with no clean error. The fix fails BEFORE writing any response byte.
        var upload = new Upload
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            FilePath = "uploads/2026/05/original.jpg",
            StorageLocation = StorageLocation.Cloud,
            OriginalFileName = "photo.jpg",
            ContentType = "image/jpeg",
            WidthPx = 800, HeightPx = 600, FileSizeBytes = 4,
            UploadedAt = DateTimeOffset.UtcNow,
        };
        _db.Uploads.Add(upload);
        var order = new Order
        {
            OrderNumber = "FT-ZIP-OFF",
            Status = OrderStatus.Printing,
            PaymentProcessor = PaymentProcessor.Stripe,
            DeliveryType = DeliveryType.Courier,
            ShippingAddress = DefaultAddress(),
            SubtotalRon = 10m, ShippingCostRon = 5m, TotalRon = 15m,
            Items = new List<OrderItem>
            {
                new()
                {
                    UploadId = upload.Id, Upload = upload,
                    ProductSnapshot = new ProductSnapshot
                    {
                        ProductName = "Foto", Size = "10x15", Finish = "Lucios",
                    },
                    Quantity = 1, UnitPriceRon = 10m, LineTotalRon = 10m,
                }
            },
        };
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        var router = new Mock<IStorageRouter>();
        router.SetupGet(r => r.CloudEnabled).Returns(false);
        router.SetupGet(r => r.Local).Returns(_localStore.Object);
        router.Setup(r => r.For(StorageLocation.Local)).Returns(_localStore.Object);
        router.Setup(r => r.For(StorageLocation.Cloud))
              .Throws(new InvalidOperationException("Cloud storage is not enabled."));
        var sut = new AdminOrderService(
            _db, _emailSvc.Object, _euPlatesc.Object, _stripeClient.Object,
            router.Object, _purger.Object, Options.Create(new ArchiveSettings()),
            _hub.Object, _awbNotifier.Object, NullLogger<AdminOrderService>.Instance);

        var httpContext = new DefaultHttpContext();
        using var body = new MemoryStream();
        httpContext.Response.Body = body;

        var act = () => sut.StreamZipAsync(order.Id, httpContext.Response);

        await act.Should().ThrowAsync<InvalidOperationException>();
        body.Length.Should().Be(0);                       // nothing written — no truncated ZIP
        httpContext.Response.ContentType.Should().BeNull(); // headers never set
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
    public async Task CancelOrderAsync_TriggersOriginalPurge()
    {
        // F17 (review 043-v1): a cancelled/refunded order's cloud original must be purged
        // (owner decision). The purger self-refuses when cloud/archive is off, so cancel
        // always fires it and the purger decides.
        var order = await SeedOrderAsync(OrderStatus.Paid);

        await _sut.CancelOrderAsync(order.Id, null);

        _purger.Verify(
            p => p.PurgeOrderOriginalsAsync(order.Id, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CancelOrderAsync_CloudTierOff_DoesNotTriggerPurge()
    {
        // Gate's false branch (F17 hardening, review 043-v1): on a local-only deployment there is
        // nothing to purge and the purger's cloud-off refusal logs at Error, so cancel must skip
        // the call entirely rather than false-alarm.
        var router = new Mock<IStorageRouter>();
        router.SetupGet(r => r.CloudEnabled).Returns(false);
        var sut = new AdminOrderService(
            _db, _emailSvc.Object, _euPlatesc.Object, _stripeClient.Object,
            router.Object, _purger.Object, Options.Create(new ArchiveSettings()),
            _hub.Object, _awbNotifier.Object, NullLogger<AdminOrderService>.Instance);

        var order = await SeedOrderAsync(OrderStatus.Paid);

        await sut.CancelOrderAsync(order.Id, null);

        _purger.Verify(
            p => p.PurgeOrderOriginalsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CancelOrderAsync_PurgeThrows_OrderStillCancelledAndExceptionSwallowed()
    {
        // F5 (review 043-v3): the purge-on-cancel try/catch (F17) must keep a purge failure from
        // failing the already-committed cancel + refund. Removing the try/catch reddens this.
        var order = await SeedOrderAsync(OrderStatus.Paid);
        _purger.Setup(p => p.PurgeOrderOriginalsAsync(order.Id, It.IsAny<CancellationToken>()))
               .ThrowsAsync(new InvalidOperationException("purge backend unavailable"));

        var result = await _sut.CancelOrderAsync(order.Id, null); // must NOT throw

        result.Status.Should().Be("Cancelled");
        (await _db.Orders.FindAsync(order.Id))!.Status.Should().Be(OrderStatus.Cancelled);
        _purger.Verify(
            p => p.PurgeOrderOriginalsAsync(order.Id, It.IsAny<CancellationToken>()), Times.Once);
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
