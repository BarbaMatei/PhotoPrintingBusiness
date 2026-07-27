using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PhotoPrint.API.BackgroundJobs;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Data;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services;
using Xunit;

namespace PhotoPrint.Tests.Unit.Services;

/// <summary>
/// Unit tests for <see cref="OriginalPurgeRecoveryScanner"/> (bolt 052 backstop). Targets the
/// <c>RunSweepAsync</c> tick directly via reflection (matching <see cref="ArchiveRetentionJob"/>'s
/// pattern) plus the <c>ExecuteAsync</c> refusal guards. The scanner is periodic since F4
/// (review 043-v1) — the sweep catches promotions that complete after the Shipped transition.
/// </summary>
public class OriginalPurgeRecoveryScannerTests
{
    private static PhotoPrintDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<PhotoPrintDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static IServiceScopeFactory BuildScopes(PhotoPrintDbContext db, IOriginalPurger purger)
    {
        var services = new ServiceCollection();
        services.AddSingleton(db);
        services.AddSingleton(purger);
        services.AddLogging();
        return services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
    }

    private static Mock<IStorageRouter> Router(bool cloudEnabled)
    {
        var r = new Mock<IStorageRouter>();
        r.SetupGet(x => x.CloudEnabled).Returns(cloudEnabled);
        return r;
    }

    private static IOptions<ArchiveSettings> Settings(
        string purgeStatus = "Shipped", bool enabled = true)
        => Options.Create(new ArchiveSettings
        {
            Enabled = enabled,
            PurgeOriginalAtStatus = purgeStatus,
        });

    private static async Task<int> RunSweepAsync(OriginalPurgeRecoveryScanner sut, CancellationToken ct)
    {
        var method = typeof(OriginalPurgeRecoveryScanner).GetMethod(
            "RunSweepAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        return await (Task<int>)method!.Invoke(sut, new object[] { ct })!;
    }

    private static Upload SeedUpload(
        PhotoPrintDbContext db,
        string? filePath = "uploads/2025/01/abc.jpg",
        StorageLocation loc = StorageLocation.Cloud)
    {
        var u = new Upload
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            FilePath = filePath,
            StorageLocation = loc,
            OriginalFileName = "x.jpg", ContentType = "image/jpeg",
            WidthPx = 100, HeightPx = 100, FileSizeBytes = 1,
            UploadedAt = DateTimeOffset.UtcNow.AddDays(-30),
        };
        db.Uploads.Add(u);
        return u;
    }

    private static Order SeedOrder(PhotoPrintDbContext db, OrderStatus status, Upload upload)
    {
        var o = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = "FT-" + Random.Shared.Next(100_000, 999_999),
            Status = status,
            ShippingAddress = new ShippingAddressSnapshot
            {
                RecipientName = "x", Phone = "x",
                Street = "x", Number = "1",
                City = "x", County = "x", PostalCode = "x",
            },
            DeliveryType = DeliveryType.Easybox,
            PaidAt = status >= OrderStatus.Paid ? DateTimeOffset.UtcNow.AddDays(-5) : null,
        };
        db.Orders.Add(o);
        db.OrderItems.Add(new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = o.Id, Order = o,
            UploadId = upload.Id, Upload = upload,
            ProductId = Guid.NewGuid(),
            Quantity = 1, UnitPriceRon = 1, LineTotalRon = 1,
            ProductSnapshot = new ProductSnapshot
            {
                ProductName = "x", Size = "x", Finish = "x",
            },
        });
        db.SaveChanges();
        return o;
    }

    // ── ExecuteAsync refusal guards ───────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_ArchiveDisabled_DoesNothing()
    {
        using var db = CreateDb();
        var purger = new Mock<IOriginalPurger>();
        var sut = new OriginalPurgeRecoveryScanner(
            BuildScopes(db, purger.Object), Router(true).Object, Settings(enabled: false),
            Mock.Of<ILogger<OriginalPurgeRecoveryScanner>>());

        await sut.StartAsync(CancellationToken.None);
        await sut.StopAsync(CancellationToken.None);

        purger.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExecuteAsync_CloudTierOff_DoesNothing()
    {
        using var db = CreateDb();
        var purger = new Mock<IOriginalPurger>();
        var sut = new OriginalPurgeRecoveryScanner(
            BuildScopes(db, purger.Object), Router(cloudEnabled: false).Object, Settings(),
            Mock.Of<ILogger<OriginalPurgeRecoveryScanner>>());

        await sut.StartAsync(CancellationToken.None);
        await sut.StopAsync(CancellationToken.None);

        purger.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExecuteAsync_EnabledCloudOn_BootSweepFiresPurger()
    {
        // F3 (review 043-v3): the RunSweep tests all reach the sweep via reflection, and the two
        // guard tests short-circuit before it — so nothing drove ExecuteAsync's boot sweep. Deleting
        // the boot-sweep line (or breaking the periodic loop) left the suite green. This test drives
        // the real ExecuteAsync path: a stuck order must be purged by the boot sweep, or it times out.
        using var db = CreateDb();
        var u = SeedUpload(db);  // Cloud + FilePath set → stuck
        var order = SeedOrder(db, OrderStatus.Shipped, u);

        var fired = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var purger = new Mock<IOriginalPurger>();
        purger.Setup(p => p.PurgeOrderOriginalsAsync(order.Id, It.IsAny<CancellationToken>()))
              .Callback(() => fired.TrySetResult())
              .ReturnsAsync(PurgeOutcome.Empty);
        var sut = new OriginalPurgeRecoveryScanner(
            BuildScopes(db, purger.Object), Router(true).Object, Settings("Shipped"),
            Mock.Of<ILogger<OriginalPurgeRecoveryScanner>>());

        await sut.StartAsync(CancellationToken.None);
        await fired.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await sut.StopAsync(CancellationToken.None);

        purger.Verify(p => p.PurgeOrderOriginalsAsync(order.Id, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── RunSweepAsync selection ───────────────────────────────────────────────

    [Fact]
    public async Task RunSweep_NoStuckOrders_FiresNothing()
    {
        using var db = CreateDb();
        // Shipped order with a properly-purged upload (FilePath null) — not stuck.
        var u = SeedUpload(db, filePath: null);
        SeedOrder(db, OrderStatus.Shipped, u);

        var purger = new Mock<IOriginalPurger>();
        var sut = new OriginalPurgeRecoveryScanner(
            BuildScopes(db, purger.Object), Router(true).Object, Settings(),
            Mock.Of<ILogger<OriginalPurgeRecoveryScanner>>());

        await RunSweepAsync(sut, CancellationToken.None);

        purger.Verify(p => p.PurgeOrderOriginalsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(OrderStatus.Shipped)]
    [InlineData(OrderStatus.Delivered)]
    public async Task RunSweep_StuckOrderAtOrPastShipped_FiresPurger(OrderStatus status)
    {
        // F4 (review 043-v1): a promotion that completed after Shipped leaves the upload Cloud
        // with FilePath still set. The periodic sweep must catch it — the one-shot Shipped purge
        // skipped it while it was still Local.
        using var db = CreateDb();
        var u = SeedUpload(db);  // FilePath still set, StorageLocation Cloud → stuck
        var order = SeedOrder(db, status, u);

        var purger = new Mock<IOriginalPurger>();
        purger.Setup(p => p.PurgeOrderOriginalsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(PurgeOutcome.Empty);
        var sut = new OriginalPurgeRecoveryScanner(
            BuildScopes(db, purger.Object), Router(true).Object, Settings("Shipped"),
            Mock.Of<ILogger<OriginalPurgeRecoveryScanner>>());

        await RunSweepAsync(sut, CancellationToken.None);

        purger.Verify(p => p.PurgeOrderOriginalsAsync(order.Id, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(OrderStatus.AwaitingPayment)]
    [InlineData(OrderStatus.Paid)]
    [InlineData(OrderStatus.Printing)]
    [InlineData(OrderStatus.PaymentFailed)]
    public async Task RunSweep_PrePurgeStatuses_NotFired(OrderStatus status)
    {
        using var db = CreateDb();
        var u = SeedUpload(db);
        SeedOrder(db, status, u);

        var purger = new Mock<IOriginalPurger>();
        var sut = new OriginalPurgeRecoveryScanner(
            BuildScopes(db, purger.Object), Router(true).Object, Settings("Shipped"),
            Mock.Of<ILogger<OriginalPurgeRecoveryScanner>>());

        await RunSweepAsync(sut, CancellationToken.None);

        purger.Verify(p => p.PurgeOrderOriginalsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RunSweep_CancelledStuckOrder_FiresPurger()
    {
        // F17 (review 043-v1): a paid-then-cancelled order whose promotion completed leaves a
        // Cloud original with FilePath set. Cancel fires purge synchronously, but a promotion
        // still in flight at cancel time is skipped there — the sweep must backstop it.
        using var db = CreateDb();
        var u = SeedUpload(db);  // Cloud + FilePath still set → stuck
        var order = SeedOrder(db, OrderStatus.Cancelled, u);

        var purger = new Mock<IOriginalPurger>();
        purger.Setup(p => p.PurgeOrderOriginalsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(PurgeOutcome.Empty);
        var sut = new OriginalPurgeRecoveryScanner(
            BuildScopes(db, purger.Object), Router(true).Object, Settings("Shipped"),
            Mock.Of<ILogger<OriginalPurgeRecoveryScanner>>());

        await RunSweepAsync(sut, CancellationToken.None);

        purger.Verify(p => p.PurgeOrderOriginalsAsync(order.Id, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RunSweep_ConfiguredDelivered_ShippedOrderNotFired()
    {
        // When PurgeOriginalAtStatus = Delivered, only Delivered orders should be in scope.
        using var db = CreateDb();
        var u1 = SeedUpload(db);
        SeedOrder(db, OrderStatus.Shipped, u1);
        var u2 = SeedUpload(db);
        var deliveredOrder = SeedOrder(db, OrderStatus.Delivered, u2);

        var purger = new Mock<IOriginalPurger>();
        purger.Setup(p => p.PurgeOrderOriginalsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(PurgeOutcome.Empty);
        var sut = new OriginalPurgeRecoveryScanner(
            BuildScopes(db, purger.Object), Router(true).Object, Settings("Delivered"),
            Mock.Of<ILogger<OriginalPurgeRecoveryScanner>>());

        await RunSweepAsync(sut, CancellationToken.None);

        purger.Verify(p => p.PurgeOrderOriginalsAsync(deliveredOrder.Id, It.IsAny<CancellationToken>()),
            Times.Once);
        purger.Verify(p => p.PurgeOrderOriginalsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Once);  // not twice
    }
}
