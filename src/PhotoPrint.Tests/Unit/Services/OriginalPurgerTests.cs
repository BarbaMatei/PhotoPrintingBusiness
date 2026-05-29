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
/// Unit tests for <see cref="OriginalPurger"/> — the intent-024 story-001 orchestrator.
/// Exercises Confirmed-Delete-Then-Update (mirror of ADR-011), per-upload idempotency,
/// partial-failure semantics, and the cloud-off / archive-disabled refusal posture.
/// </summary>
public class OriginalPurgerTests
{
    // ── Fixtures ──────────────────────────────────────────────────────────────

    private static PhotoPrintDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<PhotoPrintDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private sealed record SutBundle(
        OriginalPurger Sut,
        Mock<IStorageRouter> Router,
        Mock<IStorageService> Cloud);

    private static SutBundle CreateSut(
        PhotoPrintDbContext db,
        bool cloudEnabled = true,
        bool enabled = true)
    {
        var cloud = new Mock<IStorageService>(MockBehavior.Strict);
        var router = new Mock<IStorageRouter>();
        router.SetupGet(r => r.Cloud).Returns(cloud.Object);
        router.SetupGet(r => r.CloudEnabled).Returns(cloudEnabled);

        var settings = Options.Create(new ArchiveSettings { Enabled = enabled });

        var sut = new OriginalPurger(
            router.Object, db, settings, Mock.Of<ILogger<OriginalPurger>>());
        return new SutBundle(sut, router, cloud);
    }

    private static Upload SeedUpload(
        PhotoPrintDbContext db,
        StorageLocation loc = StorageLocation.Cloud,
        string? filePath = null,
        string? previewKey = "previews/dummy.jpg",
        string? thumbKey = "thumbs/dummy.jpg")
    {
        var id = Guid.NewGuid();
        var upload = new Upload
        {
            Id = id,
            UserId = Guid.NewGuid(),
            FilePath = filePath ?? $"uploads/2025/01/{id:N}.jpg",
            ThumbnailPath = thumbKey,
            LargePreviewPath = previewKey,
            StorageLocation = loc,
            OriginalFileName = "photo.jpg",
            ContentType = "image/jpeg",
            WidthPx = 4000, HeightPx = 3000, FileSizeBytes = 5_000_000,
            UploadedAt = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
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
            PaidAt = status >= OrderStatus.Paid ? DateTimeOffset.UtcNow.AddDays(-7) : null,
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

    // ── Pre-flight refusals ───────────────────────────────────────────────────

    [Fact]
    public async Task PurgeOrderOriginalsAsync_OrderMissing_ReturnsEmpty()
    {
        using var db = CreateDb();
        var bundle = CreateSut(db);

        var outcome = await bundle.Sut.PurgeOrderOriginalsAsync(Guid.NewGuid());

        outcome.Should().Be(PurgeOutcome.Empty);
        bundle.Cloud.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PurgeOrderOriginalsAsync_CloudTierOff_ReturnsEmpty_NoCloudCalls()
    {
        using var db = CreateDb();
        var upload = SeedUpload(db);
        var order = SeedOrder(db, OrderStatus.Shipped, upload);
        var bundle = CreateSut(db, cloudEnabled: false);

        var outcome = await bundle.Sut.PurgeOrderOriginalsAsync(order.Id);

        outcome.Should().Be(PurgeOutcome.Empty);
        bundle.Cloud.VerifyNoOtherCalls();
        (await db.Uploads.FindAsync(upload.Id))!.FilePath.Should().NotBeNull();
    }

    [Fact]
    public async Task PurgeOrderOriginalsAsync_ArchiveDisabled_ReturnsEmpty()
    {
        using var db = CreateDb();
        var upload = SeedUpload(db);
        var order = SeedOrder(db, OrderStatus.Shipped, upload);
        var bundle = CreateSut(db, enabled: false);

        var outcome = await bundle.Sut.PurgeOrderOriginalsAsync(order.Id);

        outcome.Should().Be(PurgeOutcome.Empty);
        bundle.Cloud.VerifyNoOtherCalls();
    }

    // ── Idempotency ───────────────────────────────────────────────────────────

    [Fact]
    public async Task PurgeOrderOriginalsAsync_AlreadyPurged_Skips()
    {
        using var db = CreateDb();
        // FilePath == null means "already purged" — should be a no-op skip.
        var upload = SeedUpload(db, filePath: null);
        upload.FilePath = null;
        upload.OriginalPurgedAt = DateTimeOffset.UtcNow.AddDays(-3);
        var order = SeedOrder(db, OrderStatus.Shipped, upload);
        var bundle = CreateSut(db);

        var outcome = await bundle.Sut.PurgeOrderOriginalsAsync(order.Id);

        outcome.Purged.Should().Be(0);
        outcome.Skipped.Should().Be(1);
        bundle.Cloud.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PurgeOrderOriginalsAsync_LocalUpload_SkipsWithoutDeleting()
    {
        // Defence in depth: a Local upload reaching the purger means something's wrong
        // upstream. We don't delete from the wrong tier — skip + log.
        using var db = CreateDb();
        var upload = SeedUpload(db, loc: StorageLocation.Local);
        var order = SeedOrder(db, OrderStatus.Shipped, upload);
        var bundle = CreateSut(db);

        var outcome = await bundle.Sut.PurgeOrderOriginalsAsync(order.Id);

        outcome.Purged.Should().Be(0);
        outcome.Skipped.Should().Be(1);
        bundle.Cloud.VerifyNoOtherCalls();
        (await db.Uploads.FindAsync(upload.Id))!.FilePath.Should().NotBeNull();
    }

    // ── Happy path ────────────────────────────────────────────────────────────

    [Fact]
    public async Task PurgeOrderOriginalsAsync_HappyPath_DeletesCloudOriginal_FlipsRow()
    {
        using var db = CreateDb();
        var upload = SeedUpload(db);
        var originalKey = upload.FilePath!;
        var order = SeedOrder(db, OrderStatus.Shipped, upload);
        var bundle = CreateSut(db);

        bundle.Cloud.Setup(s => s.DeleteAsync(originalKey, It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

        var outcome = await bundle.Sut.PurgeOrderOriginalsAsync(order.Id);

        outcome.Purged.Should().Be(1);
        outcome.Failed.Should().Be(0);
        outcome.BytesFreed.Should().Be(upload.FileSizeBytes);

        var updated = await db.Uploads.FindAsync(upload.Id);
        updated!.FilePath.Should().BeNull();
        updated.OriginalPurgedAt.Should().NotBeNull();
        updated.StorageLocation.Should().Be(StorageLocation.Cloud);  // unchanged

        bundle.Cloud.Verify(
            s => s.DeleteAsync(originalKey, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PurgeOrderOriginalsAsync_LargePreviewAndThumbnailPreserved()
    {
        using var db = CreateDb();
        var preview = "previews/keep-me.jpg";
        var thumb = "thumbs/keep-me.jpg";
        var upload = SeedUpload(db, previewKey: preview, thumbKey: thumb);
        var order = SeedOrder(db, OrderStatus.Shipped, upload);
        var bundle = CreateSut(db);

        bundle.Cloud.Setup(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

        await bundle.Sut.PurgeOrderOriginalsAsync(order.Id);

        var updated = await db.Uploads.FindAsync(upload.Id);
        updated!.LargePreviewPath.Should().Be(preview);
        updated.ThumbnailPath.Should().Be(thumb);

        // Only the original key was deleted — never the preview / thumb.
        bundle.Cloud.Verify(
            s => s.DeleteAsync(preview, It.IsAny<CancellationToken>()), Times.Never);
        bundle.Cloud.Verify(
            s => s.DeleteAsync(thumb, It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Failure modes ─────────────────────────────────────────────────────────

    [Fact]
    public async Task PurgeOrderOriginalsAsync_CloudDeleteFails_LeavesRowIntact()
    {
        using var db = CreateDb();
        var upload = SeedUpload(db);
        var order = SeedOrder(db, OrderStatus.Shipped, upload);
        var bundle = CreateSut(db);

        bundle.Cloud.Setup(s => s.DeleteAsync(upload.FilePath!, It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new IOException("simulated S3 failure"));

        var outcome = await bundle.Sut.PurgeOrderOriginalsAsync(order.Id);

        outcome.Failed.Should().Be(1);
        outcome.Purged.Should().Be(0);

        // Row untouched — next sweep retries (S3 DeleteObject is idempotent on a hit
        // that finally succeeds, and the row only flips after the cloud delete).
        var updated = await db.Uploads.FindAsync(upload.Id);
        updated!.FilePath.Should().NotBeNull();
        updated.OriginalPurgedAt.Should().BeNull();
    }

    [Fact]
    public async Task PurgeOrderOriginalsAsync_TwoUploads_PartialFailure_OneStaysOnePurged()
    {
        using var db = CreateDb();
        var ok = SeedUpload(db);
        var bad = SeedUpload(db);
        var order = SeedOrder(db, OrderStatus.Shipped, ok, bad);
        var bundle = CreateSut(db);

        bundle.Cloud.Setup(s => s.DeleteAsync(ok.FilePath!, It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
        bundle.Cloud.Setup(s => s.DeleteAsync(bad.FilePath!, It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new IOException("simulated"));

        var outcome = await bundle.Sut.PurgeOrderOriginalsAsync(order.Id);

        outcome.Purged.Should().Be(1);
        outcome.Failed.Should().Be(1);

        (await db.Uploads.FindAsync(ok.Id))!.FilePath.Should().BeNull();
        (await db.Uploads.FindAsync(bad.Id))!.FilePath.Should().NotBeNull();
    }
}
