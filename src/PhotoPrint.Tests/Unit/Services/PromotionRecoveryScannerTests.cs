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
/// Unit tests for <see cref="PromotionRecoveryScanner"/> (periodic since F1, review 043-v3 — the
/// class sibling of F4's purge-sweep fix). The selection logic is driven through <c>RunSweepAsync</c>
/// via reflection (matching <see cref="OriginalPurgeRecoveryScanner"/>'s pattern); a separate
/// <c>ExecuteAsync</c> boot-sweep test proves the periodic wiring actually invokes the sweep, and the
/// two refusal-guard tests cover archive-disabled / cloud-off.
/// </summary>
public class PromotionRecoveryScannerTests
{
    private static PhotoPrintDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<PhotoPrintDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static IServiceScopeFactory BuildScopes(PhotoPrintDbContext db)
    {
        var services = new ServiceCollection();
        services.AddSingleton(db);
        services.AddLogging();
        return services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
    }

    private static Mock<IStorageRouter> Router(bool cloudEnabled)
    {
        var r = new Mock<IStorageRouter>();
        r.SetupGet(x => x.CloudEnabled).Returns(cloudEnabled);
        return r;
    }

    private static IOptions<OrderPhotoArchiveSettings> Settings(bool enabled = true) =>
        Options.Create(new OrderPhotoArchiveSettings { Enabled = enabled });

    private static async Task<int> RunSweepAsync(PromotionRecoveryScanner sut, CancellationToken ct)
    {
        var method = typeof(PromotionRecoveryScanner).GetMethod(
            "RunSweepAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        return await (Task<int>)method!.Invoke(sut, new object[] { ct })!;
    }

    private static Upload SeedUpload(PhotoPrintDbContext db, StorageLocation loc)
    {
        var u = new Upload
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            FilePath = $"uploads/2026/05/{Guid.NewGuid():N}.jpg",
            StorageLocation = loc,
            OriginalFileName = "x.jpg", ContentType = "image/jpeg",
            WidthPx = 100, HeightPx = 100, FileSizeBytes = 1,
            UploadedAt = DateTimeOffset.UtcNow,
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
            PaidAt = status >= OrderStatus.Paid ? DateTimeOffset.UtcNow : null,
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
        var queue = new Mock<IPromotionQueue>();
        var sut = new PromotionRecoveryScanner(
            BuildScopes(db), queue.Object, Router(true).Object, Settings(enabled: false),
            Mock.Of<ILogger<PromotionRecoveryScanner>>());

        await sut.StartAsync(CancellationToken.None);
        await sut.StopAsync(CancellationToken.None);

        queue.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExecuteAsync_CloudTierOff_DoesNothing()
    {
        using var db = CreateDb();
        var queue = new Mock<IPromotionQueue>();
        var sut = new PromotionRecoveryScanner(
            BuildScopes(db), queue.Object, Router(cloudEnabled: false).Object, Settings(),
            Mock.Of<ILogger<PromotionRecoveryScanner>>());

        await sut.StartAsync(CancellationToken.None);
        await sut.StopAsync(CancellationToken.None);

        queue.VerifyNoOtherCalls();
    }

    // ── ExecuteAsync happy path — proves the boot sweep is actually wired (F1/F3 class) ──

    [Fact]
    public async Task ExecuteAsync_StuckOrder_BootSweepEnqueues()
    {
        // Guards the ExecuteAsync → boot-sweep → RunSweepAsync wiring: delete the boot sweep and
        // this test times out (the enqueue never fires). Mirrors the F3 fix for the purge sibling.
        using var db = CreateDb();
        var u = SeedUpload(db, StorageLocation.Local);
        var order = SeedOrder(db, OrderStatus.Paid, u);

        var enqueued = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var queue = new Mock<IPromotionQueue>();
        queue.Setup(q => q.EnqueueAsync(
                It.Is<PromotionJob>(j => j.OrderId == order.Id), It.IsAny<CancellationToken>()))
             .Callback(() => enqueued.TrySetResult())
             .Returns(ValueTask.CompletedTask);
        var sut = new PromotionRecoveryScanner(
            BuildScopes(db), queue.Object, Router(true).Object, Settings(),
            Mock.Of<ILogger<PromotionRecoveryScanner>>());

        await sut.StartAsync(CancellationToken.None);
        await enqueued.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await sut.StopAsync(CancellationToken.None);

        queue.Verify(q => q.EnqueueAsync(
            It.Is<PromotionJob>(j => j.OrderId == order.Id),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── RunSweepAsync selection ───────────────────────────────────────────────

    [Fact]
    public async Task RunSweep_NoStuckOrders_EnqueuesNothing()
    {
        using var db = CreateDb();
        // One Paid order whose uploads are already Cloud — not stuck.
        var u = SeedUpload(db, StorageLocation.Cloud);
        SeedOrder(db, OrderStatus.Paid, u);

        var queue = new Mock<IPromotionQueue>();
        var sut = new PromotionRecoveryScanner(
            BuildScopes(db), queue.Object, Router(true).Object, Settings(),
            Mock.Of<ILogger<PromotionRecoveryScanner>>());

        await RunSweepAsync(sut, CancellationToken.None);

        queue.Verify(q => q.EnqueueAsync(It.IsAny<PromotionJob>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RunSweep_PaidOrderWithLocalUploads_Enqueued()
    {
        using var db = CreateDb();
        var u = SeedUpload(db, StorageLocation.Local);
        var order = SeedOrder(db, OrderStatus.Paid, u);

        var queue = new Mock<IPromotionQueue>();
        var sut = new PromotionRecoveryScanner(
            BuildScopes(db), queue.Object, Router(true).Object, Settings(),
            Mock.Of<ILogger<PromotionRecoveryScanner>>());

        await RunSweepAsync(sut, CancellationToken.None);

        queue.Verify(q => q.EnqueueAsync(
            It.Is<PromotionJob>(j => j.OrderId == order.Id),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(OrderStatus.Printing)]
    [InlineData(OrderStatus.Shipped)]
    [InlineData(OrderStatus.Delivered)]
    public async Task RunSweep_PostPaidStatusesAlsoCovered(OrderStatus status)
    {
        using var db = CreateDb();
        var u = SeedUpload(db, StorageLocation.Local);
        var order = SeedOrder(db, status, u);

        var queue = new Mock<IPromotionQueue>();
        var sut = new PromotionRecoveryScanner(
            BuildScopes(db), queue.Object, Router(true).Object, Settings(),
            Mock.Of<ILogger<PromotionRecoveryScanner>>());

        await RunSweepAsync(sut, CancellationToken.None);

        queue.Verify(q => q.EnqueueAsync(
            It.Is<PromotionJob>(j => j.OrderId == order.Id),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(OrderStatus.AwaitingPayment)]
    [InlineData(OrderStatus.PaymentFailed)]
    [InlineData(OrderStatus.Cancelled)]
    public async Task RunSweep_NonPaidStatuses_NotEnqueued(OrderStatus status)
    {
        using var db = CreateDb();
        var u = SeedUpload(db, StorageLocation.Local);
        SeedOrder(db, status, u);

        var queue = new Mock<IPromotionQueue>();
        var sut = new PromotionRecoveryScanner(
            BuildScopes(db), queue.Object, Router(true).Object, Settings(),
            Mock.Of<ILogger<PromotionRecoveryScanner>>());

        await RunSweepAsync(sut, CancellationToken.None);

        queue.Verify(q => q.EnqueueAsync(It.IsAny<PromotionJob>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
