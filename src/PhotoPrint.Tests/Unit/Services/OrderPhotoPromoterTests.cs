using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Data;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services;
using Xunit;

namespace PhotoPrint.Tests.Unit.Services;

/// <summary>
/// Unit tests for <see cref="OrderPhotoPromoter"/> — the intent-024 orchestrator.
/// Exercises the per-upload atomicity + Confirmed-Write-Then-Delete contract (ADR-011),
/// the idempotency rule (already-Cloud uploads are skipped), and the cloud-off safety
/// (refuse + log Error).
/// </summary>
public class OrderPhotoPromoterTests
{
    // ── Fixtures ──────────────────────────────────────────────────────────────

    private static PhotoPrintDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<PhotoPrintDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private sealed record SutBundle(
        OrderPhotoPromoter Sut,
        Mock<IStorageRouter> Router,
        Mock<IStorageService> Local,
        Mock<IStorageService> Cloud,
        Mock<IImageProcessor> ImageProcessor,
        Mock<IPromotionQueue> Queue);

    private static SutBundle CreateSut(PhotoPrintDbContext db, bool cloudEnabled = true, bool enabled = true)
    {
        var local = new Mock<IStorageService>(MockBehavior.Strict);
        var cloud = new Mock<IStorageService>(MockBehavior.Strict);
        var router = new Mock<IStorageRouter>();
        router.SetupGet(r => r.Local).Returns(local.Object);
        router.SetupGet(r => r.Cloud).Returns(cloud.Object);
        router.SetupGet(r => r.CloudEnabled).Returns(cloudEnabled);

        var img = new Mock<IImageProcessor>();
        // Default: thumb + preview return tiny streams so happy-path tests don't need to wire each.
        img.Setup(p => p.GenerateThumbnailAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(() => new MemoryStream([0xFF, 0xD8]));
        img.Setup(p => p.GenerateLargePreviewAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(() => new MemoryStream([0xFF, 0xE1]));

        var queue = new Mock<IPromotionQueue>();

        var settings = Options.Create(new OrderPhotoArchiveSettings { Enabled = enabled });

        var sut = new OrderPhotoPromoter(
            queue.Object, router.Object, img.Object, db, settings,
            Mock.Of<ILogger<OrderPhotoPromoter>>());

        return new SutBundle(sut, router, local, cloud, img, queue);
    }

    private static Upload SeedUpload(
        PhotoPrintDbContext db, StorageLocation loc, string? thumbnailPath = null)
    {
        var id = Guid.NewGuid();
        var upload = new Upload
        {
            Id = id,
            UserId = Guid.NewGuid(),
            FilePath = $"uploads/2026/05/{id:N}.jpg",
            ThumbnailPath = thumbnailPath,
            StorageLocation = loc,
            OriginalFileName = "photo.jpg",
            ContentType = "image/jpeg",
            WidthPx = 1200, HeightPx = 800, FileSizeBytes = 4096,
            UploadedAt = new DateTimeOffset(2026, 5, 28, 12, 0, 0, TimeSpan.Zero),
        };
        db.Uploads.Add(upload);
        return upload;
    }

    private static Order SeedOrder(PhotoPrintDbContext db, OrderStatus status, params Upload[] uploads)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = "FT-" + Random.Shared.Next(100_000, 999_999),
            Status = status,
            ShippingAddress = new ShippingAddressSnapshot
            {
                RecipientName = "Test User", Phone = "0",
                Street = "x", Number = "1",
                City = "x", County = "x", PostalCode = "0",
            },
            DeliveryType = DeliveryType.Easybox,
            ShippingCostRon = 0, SubtotalRon = 0, TotalRon = 0,
            PaidAt = status >= OrderStatus.Paid ? DateTimeOffset.UtcNow : null,
        };
        db.Orders.Add(order);
        foreach (var u in uploads)
        {
            db.OrderItems.Add(new OrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id, Order = order,
                UploadId = u.Id, Upload = u,
                ProductId = Guid.NewGuid(),
                Quantity = 1, UnitPriceRon = 1, LineTotalRon = 1,
                ProductSnapshot = new ProductSnapshot
                {
                    ProductName = "x", Size = "x", Finish = "x",
                },
            });
        }
        db.SaveChanges();
        return order;
    }

    private static void SetupLocalSource(Mock<IStorageService> local, string key, byte[] bytes)
    {
        local.Setup(s => s.GetStreamAsync(key, It.IsAny<CancellationToken>()))
             .ReturnsAsync(() => new MemoryStream(bytes));
    }

    // ── Pre-flight refusals ───────────────────────────────────────────────────

    [Fact]
    public async Task PromoteOrderAsync_OrderMissing_ReturnsEmpty()
    {
        using var db = CreateDb();
        var (sut, _, _, _, _, _) = CreateSut(db);

        var outcome = await sut.PromoteOrderAsync(Guid.NewGuid());

        outcome.Should().Be(PromotionOutcome.Empty);
    }

    [Fact]
    public async Task PromoteOrderAsync_OrderNotPaid_ReturnsEmpty()
    {
        using var db = CreateDb();
        var upload = SeedUpload(db, StorageLocation.Local);
        var order = SeedOrder(db, OrderStatus.AwaitingPayment, upload);
        var bundle = CreateSut(db);

        var outcome = await bundle.Sut.PromoteOrderAsync(order.Id);

        outcome.Should().Be(PromotionOutcome.Empty);
        bundle.Cloud.VerifyNoOtherCalls(); // no cloud activity for not-paid orders
    }

    [Fact]
    public async Task PromoteOrderAsync_CloudTierOff_ReturnsEmpty_NoCloudCalls()
    {
        using var db = CreateDb();
        var upload = SeedUpload(db, StorageLocation.Local);
        var order = SeedOrder(db, OrderStatus.Paid, upload);
        var bundle = CreateSut(db, cloudEnabled: false);

        var outcome = await bundle.Sut.PromoteOrderAsync(order.Id);

        outcome.Should().Be(PromotionOutcome.Empty);
        bundle.Cloud.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PromoteOrderAsync_ArchiveDisabled_ReturnsEmpty()
    {
        using var db = CreateDb();
        var upload = SeedUpload(db, StorageLocation.Local);
        var order = SeedOrder(db, OrderStatus.Paid, upload);
        var bundle = CreateSut(db, enabled: false);

        var outcome = await bundle.Sut.PromoteOrderAsync(order.Id);

        outcome.Should().Be(PromotionOutcome.Empty);
    }

    // ── Idempotency ───────────────────────────────────────────────────────────

    [Fact]
    public async Task PromoteOrderAsync_AlreadyCloud_Skips()
    {
        using var db = CreateDb();
        var upload = SeedUpload(db, StorageLocation.Cloud);
        var order = SeedOrder(db, OrderStatus.Paid, upload);
        var bundle = CreateSut(db);

        var outcome = await bundle.Sut.PromoteOrderAsync(order.Id);

        outcome.Promoted.Should().Be(0);
        outcome.Skipped.Should().Be(1);
        outcome.Failed.Should().Be(0);
        bundle.Cloud.VerifyNoOtherCalls();
        bundle.Local.VerifyNoOtherCalls();
    }

    // ── Happy path ────────────────────────────────────────────────────────────

    [Fact]
    public async Task PromoteOrderAsync_HappyPath_WritesThreeCloudObjects_FlipsRow_DeletesLocal()
    {
        using var db = CreateDb();
        var upload = SeedUpload(db, StorageLocation.Local, thumbnailPath: $"thumbs/{Guid.NewGuid():N}.jpg");
        var order = SeedOrder(db, OrderStatus.Paid, upload);
        var bundle = CreateSut(db);

        SetupLocalSource(bundle.Local, upload.FilePath, [0xFF, 0xD8, 0xFF, 0xE0]);
        bundle.Local.Setup(s => s.ExistsAsync(upload.ThumbnailPath!, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(true);
        SetupLocalSource(bundle.Local, upload.ThumbnailPath!, [0xFF, 0xD8]);
        bundle.Local.Setup(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
        bundle.Cloud.Setup(s => s.SaveAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

        var outcome = await bundle.Sut.PromoteOrderAsync(order.Id);

        outcome.Promoted.Should().Be(1);
        outcome.Failed.Should().Be(0);
        outcome.TotalBytes.Should().Be(upload.FileSizeBytes);

        var updated = await db.Uploads.FindAsync(upload.Id);
        updated!.StorageLocation.Should().Be(StorageLocation.Cloud);
        updated.LargePreviewPath.Should().Be(StorageKeys.Preview(upload.Id));
        updated.ThumbnailPath.Should().Be(StorageKeys.Thumbnail(upload.Id));

        // Three cloud writes in the right order (no Sequence assertion — only existence + keys).
        bundle.Cloud.Verify(s => s.SaveAsync(
            It.IsAny<Stream>(), upload.FilePath, It.IsAny<CancellationToken>()), Times.Once);
        bundle.Cloud.Verify(s => s.SaveAsync(
            It.IsAny<Stream>(), StorageKeys.Thumbnail(upload.Id), It.IsAny<CancellationToken>()), Times.Once);
        bundle.Cloud.Verify(s => s.SaveAsync(
            It.IsAny<Stream>(), StorageKeys.Preview(upload.Id), It.IsAny<CancellationToken>()), Times.Once);

        // Local litter cleanup attempted.
        bundle.Local.Verify(s => s.DeleteAsync(upload.FilePath, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PromoteOrderAsync_MissingLocalThumbnail_RegeneratesInline()
    {
        using var db = CreateDb();
        var upload = SeedUpload(db, StorageLocation.Local, thumbnailPath: null);
        var order = SeedOrder(db, OrderStatus.Paid, upload);
        var bundle = CreateSut(db);

        SetupLocalSource(bundle.Local, upload.FilePath, [0xFF, 0xD8, 0xFF, 0xE0]);
        bundle.Local.Setup(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
        bundle.Cloud.Setup(s => s.SaveAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

        var outcome = await bundle.Sut.PromoteOrderAsync(order.Id);

        outcome.Promoted.Should().Be(1);
        // Inline thumbnail regeneration ran (path was null → no local thumb to read).
        bundle.ImageProcessor.Verify(
            p => p.GenerateThumbnailAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── Failure modes ─────────────────────────────────────────────────────────

    [Fact]
    public async Task PromoteOrderAsync_LocalOriginalMissing_LeavesRowLocal_CountsFailed()
    {
        using var db = CreateDb();
        var upload = SeedUpload(db, StorageLocation.Local);
        var order = SeedOrder(db, OrderStatus.Paid, upload);
        var bundle = CreateSut(db);

        bundle.Local.Setup(s => s.GetStreamAsync(upload.FilePath, It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new FileNotFoundException("not on disk"));

        var outcome = await bundle.Sut.PromoteOrderAsync(order.Id);

        outcome.Failed.Should().Be(1);
        outcome.Promoted.Should().Be(0);

        var updated = await db.Uploads.FindAsync(upload.Id);
        updated!.StorageLocation.Should().Be(StorageLocation.Local);
        updated.LargePreviewPath.Should().BeNull();

        // No cloud writes should have been attempted once the local read failed.
        bundle.Cloud.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PromoteOrderAsync_CloudOriginalSaveFails_LeavesRowLocal_CountsFailed()
    {
        using var db = CreateDb();
        var upload = SeedUpload(db, StorageLocation.Local);
        var order = SeedOrder(db, OrderStatus.Paid, upload);
        var bundle = CreateSut(db);

        SetupLocalSource(bundle.Local, upload.FilePath, [0xFF, 0xD8, 0xFF, 0xE0]);
        bundle.Cloud.Setup(s => s.SaveAsync(It.IsAny<Stream>(), upload.FilePath, It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new IOException("simulated cloud failure"));

        var outcome = await bundle.Sut.PromoteOrderAsync(order.Id);

        outcome.Failed.Should().Be(1);
        outcome.Promoted.Should().Be(0);

        var updated = await db.Uploads.FindAsync(upload.Id);
        updated!.StorageLocation.Should().Be(StorageLocation.Local);
        // Local litter cleanup must NOT have been attempted — the row never flipped.
        bundle.Local.Verify(
            s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task PromoteOrderAsync_TwoUploads_PartialFailure_OneStaysLocalOnePromoted()
    {
        using var db = CreateDb();
        var ok = SeedUpload(db, StorageLocation.Local);
        var bad = SeedUpload(db, StorageLocation.Local);
        var order = SeedOrder(db, OrderStatus.Paid, ok, bad);
        var bundle = CreateSut(db);

        SetupLocalSource(bundle.Local, ok.FilePath, [0xFF, 0xD8]);
        bundle.Local.Setup(s => s.GetStreamAsync(bad.FilePath, It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new FileNotFoundException("simulate missing"));
        bundle.Local.Setup(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
        bundle.Cloud.Setup(s => s.SaveAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

        var outcome = await bundle.Sut.PromoteOrderAsync(order.Id);

        outcome.Promoted.Should().Be(1);
        outcome.Failed.Should().Be(1);

        var okUpdated = await db.Uploads.FindAsync(ok.Id);
        var badUpdated = await db.Uploads.FindAsync(bad.Id);
        okUpdated!.StorageLocation.Should().Be(StorageLocation.Cloud);
        badUpdated!.StorageLocation.Should().Be(StorageLocation.Local); // partial failure honoured
    }

    // ── EnqueueAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task EnqueueAsync_CloudEnabled_WritesJobToQueue()
    {
        using var db = CreateDb();
        var bundle = CreateSut(db);
        var orderId = Guid.NewGuid();

        await bundle.Sut.EnqueueAsync(orderId);

        bundle.Queue.Verify(q => q.EnqueueAsync(
            It.Is<PromotionJob>(j => j.OrderId == orderId && j.Attempt == 1),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EnqueueAsync_CloudOff_DoesNotEnqueue()
    {
        using var db = CreateDb();
        var bundle = CreateSut(db, cloudEnabled: false);

        await bundle.Sut.EnqueueAsync(Guid.NewGuid());

        bundle.Queue.Verify(q => q.EnqueueAsync(
            It.IsAny<PromotionJob>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EnqueueAsync_ArchiveDisabled_DoesNotEnqueue()
    {
        using var db = CreateDb();
        var bundle = CreateSut(db, enabled: false);

        await bundle.Sut.EnqueueAsync(Guid.NewGuid());

        bundle.Queue.Verify(q => q.EnqueueAsync(
            It.IsAny<PromotionJob>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
