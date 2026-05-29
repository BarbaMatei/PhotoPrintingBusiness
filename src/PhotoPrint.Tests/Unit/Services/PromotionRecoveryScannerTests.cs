using FluentAssertions;
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
/// Unit tests for <see cref="PromotionRecoveryScanner"/>. The scanner runs once on
/// host start; these tests cover the four states that matter:
/// archive disabled, cloud tier off, no work, work found.
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

    [Fact]
    public async Task StartAsync_ArchiveDisabled_DoesNothing()
    {
        using var db = CreateDb();
        var queue = new Mock<IPromotionQueue>();
        var sut = new PromotionRecoveryScanner(
            BuildScopes(db), queue.Object, Router(true).Object, Settings(enabled: false),
            Mock.Of<ILogger<PromotionRecoveryScanner>>());

        await sut.StartAsync(CancellationToken.None);

        queue.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task StartAsync_CloudTierOff_DoesNothing()
    {
        using var db = CreateDb();
        var queue = new Mock<IPromotionQueue>();
        var sut = new PromotionRecoveryScanner(
            BuildScopes(db), queue.Object, Router(cloudEnabled: false).Object, Settings(),
            Mock.Of<ILogger<PromotionRecoveryScanner>>());

        await sut.StartAsync(CancellationToken.None);

        queue.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task StartAsync_NoStuckOrders_EnqueuesNothing()
    {
        using var db = CreateDb();
        // One Paid order whose uploads are already Cloud — not stuck.
        var u = SeedUpload(db, StorageLocation.Cloud);
        SeedOrder(db, OrderStatus.Paid, u);

        var queue = new Mock<IPromotionQueue>();
        var sut = new PromotionRecoveryScanner(
            BuildScopes(db), queue.Object, Router(true).Object, Settings(),
            Mock.Of<ILogger<PromotionRecoveryScanner>>());

        await sut.StartAsync(CancellationToken.None);

        queue.Verify(q => q.EnqueueAsync(It.IsAny<PromotionJob>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task StartAsync_PaidOrderWithLocalUploads_Enqueued()
    {
        using var db = CreateDb();
        var u = SeedUpload(db, StorageLocation.Local);
        var order = SeedOrder(db, OrderStatus.Paid, u);

        var queue = new Mock<IPromotionQueue>();
        var sut = new PromotionRecoveryScanner(
            BuildScopes(db), queue.Object, Router(true).Object, Settings(),
            Mock.Of<ILogger<PromotionRecoveryScanner>>());

        await sut.StartAsync(CancellationToken.None);

        queue.Verify(q => q.EnqueueAsync(
            It.Is<PromotionJob>(j => j.OrderId == order.Id),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(OrderStatus.Printing)]
    [InlineData(OrderStatus.Shipped)]
    [InlineData(OrderStatus.Delivered)]
    public async Task StartAsync_PostPaidStatusesAlsoCovered(OrderStatus status)
    {
        using var db = CreateDb();
        var u = SeedUpload(db, StorageLocation.Local);
        var order = SeedOrder(db, status, u);

        var queue = new Mock<IPromotionQueue>();
        var sut = new PromotionRecoveryScanner(
            BuildScopes(db), queue.Object, Router(true).Object, Settings(),
            Mock.Of<ILogger<PromotionRecoveryScanner>>());

        await sut.StartAsync(CancellationToken.None);

        queue.Verify(q => q.EnqueueAsync(
            It.Is<PromotionJob>(j => j.OrderId == order.Id),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(OrderStatus.AwaitingPayment)]
    [InlineData(OrderStatus.PaymentFailed)]
    [InlineData(OrderStatus.Cancelled)]
    public async Task StartAsync_NonPaidStatuses_NotEnqueued(OrderStatus status)
    {
        using var db = CreateDb();
        var u = SeedUpload(db, StorageLocation.Local);
        SeedOrder(db, status, u);

        var queue = new Mock<IPromotionQueue>();
        var sut = new PromotionRecoveryScanner(
            BuildScopes(db), queue.Object, Router(true).Object, Settings(),
            Mock.Of<ILogger<PromotionRecoveryScanner>>());

        await sut.StartAsync(CancellationToken.None);

        queue.Verify(q => q.EnqueueAsync(It.IsAny<PromotionJob>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
