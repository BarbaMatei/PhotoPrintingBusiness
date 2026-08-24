using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using PhotoPrint.API.Cli;
using PhotoPrint.API.Data;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services;
using Xunit;

namespace PhotoPrint.Tests.Unit.Cli;

/// <summary>
/// Unit tests for <see cref="BackfillCommand"/>. The exit codes
/// (0 ok / 1 any failure / 2 cloud-off) drive ops automation and the order-selection filter is
/// a hand-copy of <see cref="PhotoPrint.API.BackgroundJobs.PromotionRecoveryScanner"/>, so filter
/// drift must not ship untested.
/// </summary>
public class BackfillCommandTests
{
    private static ServiceProvider BuildProvider(
        string dbName, bool cloudEnabled, IOrderPhotoPromoter promoter)
    {
        var router = new Mock<IStorageRouter>();
        router.SetupGet(r => r.CloudEnabled).Returns(cloudEnabled);

        var services = new ServiceCollection();
        services.AddDbContext<PhotoPrintDbContext>(o => o.UseInMemoryDatabase(dbName));
        services.AddSingleton(router.Object);
        services.AddSingleton(promoter);
        services.AddLogging();
        return services.BuildServiceProvider();
    }

    private static async Task SeedLocalUploadOrderAsync(ServiceProvider sp, OrderStatus status)
    {
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PhotoPrintDbContext>();

        var upload = new Upload
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            FilePath = $"uploads/2026/05/{Guid.NewGuid():N}.jpg",
            StorageLocation = StorageLocation.Local,
            OriginalFileName = "x.jpg", ContentType = "image/jpeg",
            WidthPx = 1, HeightPx = 1, FileSizeBytes = 1,
            UploadedAt = DateTimeOffset.UtcNow,
        };
        db.Uploads.Add(upload);

        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = "FT-" + Random.Shared.Next(100_000, 999_999),
            Status = status,
            DeliveryType = DeliveryType.Easybox,
            ShippingAddress = new ShippingAddressSnapshot
            {
                RecipientName = "x", Phone = "x", Street = "x", Number = "1",
                City = "x", County = "x", PostalCode = "x",
            },
            PaidAt = DateTimeOffset.UtcNow.AddDays(-1),
        };
        db.Orders.Add(order);
        db.OrderItems.Add(new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id, UploadId = upload.Id,
            ProductId = Guid.NewGuid(),
            Quantity = 1, UnitPriceRon = 1, LineTotalRon = 1,
            ProductSnapshot = new ProductSnapshot { ProductName = "x", Size = "x", Finish = "x" },
        });
        await db.SaveChangesAsync();
    }

    private static Mock<IOrderPhotoPromoter> Promoter(PromotionOutcome outcome)
    {
        var m = new Mock<IOrderPhotoPromoter>();
        m.Setup(p => p.PromoteOrderAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
         .ReturnsAsync(outcome);
        return m;
    }

    [Fact]
    public async Task RunAsync_CloudTierOff_ReturnsExitCode2()
    {
        using var sp = BuildProvider(Guid.NewGuid().ToString(), cloudEnabled: false,
            Promoter(PromotionOutcome.Empty).Object);

        var code = await BackfillCommand.RunAsync(sp, [], CancellationToken.None);

        code.Should().Be(2);
    }

    [Fact]
    public async Task RunAsync_NoLocalUploads_ReturnsZero()
    {
        using var sp = BuildProvider(Guid.NewGuid().ToString(), cloudEnabled: true,
            Promoter(PromotionOutcome.Empty).Object);

        var code = await BackfillCommand.RunAsync(sp, [], CancellationToken.None);

        code.Should().Be(0);
    }

    [Fact]
    public async Task RunAsync_DryRun_DoesNotPromote_ReturnsZero()
    {
        var promoter = Promoter(new PromotionOutcome(1, 0, 0, 100));
        using var sp = BuildProvider(Guid.NewGuid().ToString(), cloudEnabled: true, promoter.Object);
        await SeedLocalUploadOrderAsync(sp, OrderStatus.Paid);

        var code = await BackfillCommand.RunAsync(sp, ["--dry-run"], CancellationToken.None);

        code.Should().Be(0);
        promoter.Verify(p => p.PromoteOrderAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RunAsync_Live_AllSucceed_ReturnsZero_AndPromotes()
    {
        var promoter = Promoter(new PromotionOutcome(1, 0, 0, 100));
        using var sp = BuildProvider(Guid.NewGuid().ToString(), cloudEnabled: true, promoter.Object);
        await SeedLocalUploadOrderAsync(sp, OrderStatus.Paid);

        var code = await BackfillCommand.RunAsync(sp, [], CancellationToken.None);

        code.Should().Be(0);
        promoter.Verify(p => p.PromoteOrderAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RunAsync_Live_AnyPerOrderFailure_ReturnsOne()
    {
        var promoter = Promoter(new PromotionOutcome(0, 0, 1, 0)); // Failed > 0
        using var sp = BuildProvider(Guid.NewGuid().ToString(), cloudEnabled: true, promoter.Object);
        await SeedLocalUploadOrderAsync(sp, OrderStatus.Printing);

        var code = await BackfillCommand.RunAsync(sp, [], CancellationToken.None);

        code.Should().Be(1);
    }

    // ── Filter-parity boundary ───────────────────────────
    // The prior tests seeded only INCLUDED statuses (Paid/Printing), so filter drift that started
    // promoting excluded statuses shipped green. Pin both sides of the boundary.

    [Theory]
    [InlineData(OrderStatus.Cancelled)]
    [InlineData(OrderStatus.PaymentFailed)]
    [InlineData(OrderStatus.AwaitingPayment)]
    public async Task RunAsync_ExcludedStatus_IsNotPromoted(OrderStatus status)
    {
        // e.g. a drift adding `|| o.Status == Cancelled` would re-promote a refunded order's
        // purged photos — this asserts excluded statuses are never selected.
        var promoter = Promoter(new PromotionOutcome(1, 0, 0, 100));
        using var sp = BuildProvider(Guid.NewGuid().ToString(), cloudEnabled: true, promoter.Object);
        await SeedLocalUploadOrderAsync(sp, status);

        var code = await BackfillCommand.RunAsync(sp, [], CancellationToken.None);

        code.Should().Be(0); // nothing selected
        promoter.Verify(p => p.PromoteOrderAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(OrderStatus.Shipped)]
    [InlineData(OrderStatus.Delivered)]
    public async Task RunAsync_IncludedPostPaidStatus_IsPromoted(OrderStatus status)
    {
        var promoter = Promoter(new PromotionOutcome(1, 0, 0, 100));
        using var sp = BuildProvider(Guid.NewGuid().ToString(), cloudEnabled: true, promoter.Object);
        await SeedLocalUploadOrderAsync(sp, status);

        var code = await BackfillCommand.RunAsync(sp, [], CancellationToken.None);

        code.Should().Be(0);
        promoter.Verify(p => p.PromoteOrderAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
