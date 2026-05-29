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
/// Unit tests for <see cref="OriginalPurgeRecoveryScanner"/>. Verifies the startup
/// query (status floor + non-null FilePath) and the refusal posture.
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

    [Fact]
    public async Task StartAsync_ArchiveDisabled_DoesNothing()
    {
        using var db = CreateDb();
        var purger = new Mock<IOriginalPurger>();
        var sut = new OriginalPurgeRecoveryScanner(
            BuildScopes(db, purger.Object), Router(true).Object, Settings(enabled: false),
            Mock.Of<ILogger<OriginalPurgeRecoveryScanner>>());

        await sut.StartAsync(CancellationToken.None);

        purger.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task StartAsync_CloudTierOff_DoesNothing()
    {
        using var db = CreateDb();
        var purger = new Mock<IOriginalPurger>();
        var sut = new OriginalPurgeRecoveryScanner(
            BuildScopes(db, purger.Object), Router(cloudEnabled: false).Object, Settings(),
            Mock.Of<ILogger<OriginalPurgeRecoveryScanner>>());

        await sut.StartAsync(CancellationToken.None);

        purger.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task StartAsync_NoStuckOrders_EnqueuesNothing()
    {
        using var db = CreateDb();
        // Shipped order with a properly-purged upload (FilePath null) — not stuck.
        var u = SeedUpload(db, filePath: null);
        SeedOrder(db, OrderStatus.Shipped, u);

        var purger = new Mock<IOriginalPurger>();
        var sut = new OriginalPurgeRecoveryScanner(
            BuildScopes(db, purger.Object), Router(true).Object, Settings(),
            Mock.Of<ILogger<OriginalPurgeRecoveryScanner>>());

        await sut.StartAsync(CancellationToken.None);

        purger.Verify(p => p.PurgeOrderOriginalsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(OrderStatus.Shipped)]
    [InlineData(OrderStatus.Delivered)]
    public async Task StartAsync_StuckOrderAtOrPastShipped_FiresPurger(OrderStatus status)
    {
        using var db = CreateDb();
        var u = SeedUpload(db);  // FilePath still set, StorageLocation Cloud → stuck
        var order = SeedOrder(db, status, u);

        var purger = new Mock<IOriginalPurger>();
        purger.Setup(p => p.PurgeOrderOriginalsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(PurgeOutcome.Empty);
        var sut = new OriginalPurgeRecoveryScanner(
            BuildScopes(db, purger.Object), Router(true).Object, Settings("Shipped"),
            Mock.Of<ILogger<OriginalPurgeRecoveryScanner>>());

        await sut.StartAsync(CancellationToken.None);

        purger.Verify(p => p.PurgeOrderOriginalsAsync(order.Id, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(OrderStatus.AwaitingPayment)]
    [InlineData(OrderStatus.Paid)]
    [InlineData(OrderStatus.Printing)]
    [InlineData(OrderStatus.Cancelled)]
    [InlineData(OrderStatus.PaymentFailed)]
    public async Task StartAsync_PrePurgeStatuses_NotFired(OrderStatus status)
    {
        using var db = CreateDb();
        var u = SeedUpload(db);
        SeedOrder(db, status, u);

        var purger = new Mock<IOriginalPurger>();
        var sut = new OriginalPurgeRecoveryScanner(
            BuildScopes(db, purger.Object), Router(true).Object, Settings("Shipped"),
            Mock.Of<ILogger<OriginalPurgeRecoveryScanner>>());

        await sut.StartAsync(CancellationToken.None);

        purger.Verify(p => p.PurgeOrderOriginalsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task StartAsync_ConfiguredDelivered_ShippedOrderNotFired()
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

        await sut.StartAsync(CancellationToken.None);

        purger.Verify(p => p.PurgeOrderOriginalsAsync(deliveredOrder.Id, It.IsAny<CancellationToken>()),
            Times.Once);
        purger.Verify(p => p.PurgeOrderOriginalsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Once);  // not twice
    }
}
