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
/// Unit tests for <see cref="ArchiveRetentionJob"/> — the intent-024 story-002 sweep.
/// Targets the <c>SweepAsync</c> tick directly (the <c>ExecuteAsync</c> loop is a
/// PeriodicTimer wrapper; not worth driving in tests).
/// </summary>
public class ArchiveRetentionJobTests
{
    // ── Fixtures ──────────────────────────────────────────────────────────────

    private static PhotoPrintDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<PhotoPrintDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static IServiceScopeFactory BuildScopes(PhotoPrintDbContext db, IStorageRouter router)
    {
        var services = new ServiceCollection();
        services.AddSingleton(db);
        services.AddSingleton(router);
        services.AddLogging();
        return services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
    }

    private static Mock<IStorageRouter> Router(bool cloudEnabled, Mock<IStorageService>? cloud = null)
    {
        var r = new Mock<IStorageRouter>();
        r.SetupGet(x => x.CloudEnabled).Returns(cloudEnabled);
        if (cloud is not null)
            r.SetupGet(x => x.Cloud).Returns(cloud.Object);
        return r;
    }

    private static IOptions<ArchiveSettings> Settings(int retentionMonths = 12, int batchSize = 500)
        => Options.Create(new ArchiveSettings
        {
            Enabled = true,
            RetentionMonths = retentionMonths,
            BatchSize = batchSize,
            JobIntervalHours = 6,
        });

    private static Upload SeedUpload(
        PhotoPrintDbContext db,
        string? previewKey = "previews/dummy.jpg",
        string? thumbKey = "thumbs/dummy.jpg",
        StorageLocation loc = StorageLocation.Cloud)
    {
        var id = Guid.NewGuid();
        var u = new Upload
        {
            Id = id,
            UserId = Guid.NewGuid(),
            FilePath = null,  // post-purge state for typical retention sweep targets
            ThumbnailPath = thumbKey,
            LargePreviewPath = previewKey,
            StorageLocation = loc,
            OriginalFileName = "photo.jpg",
            ContentType = "image/jpeg",
            WidthPx = 100, HeightPx = 100, FileSizeBytes = 1,
            UploadedAt = DateTimeOffset.UtcNow.AddYears(-2),
            OriginalPurgedAt = DateTimeOffset.UtcNow.AddMonths(-13),
        };
        db.Uploads.Add(u);
        return u;
    }

    private static void SeedOrderItem(PhotoPrintDbContext db, Upload upload, DateTimeOffset paidAt)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = "FT-" + Random.Shared.Next(100_000, 999_999),
            Status = OrderStatus.Delivered,
            ShippingAddress = new ShippingAddressSnapshot
            {
                RecipientName = "x", Phone = "x",
                Street = "x", Number = "1",
                City = "x", County = "x", PostalCode = "x",
            },
            DeliveryType = DeliveryType.Easybox,
            PaidAt = paidAt,
        };
        db.Orders.Add(order);
        db.OrderItems.Add(new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id, Order = order,
            UploadId = upload.Id, Upload = upload,
            ProductId = Guid.NewGuid(),
            Quantity = 1, UnitPriceRon = 1, LineTotalRon = 1,
            ProductSnapshot = new ProductSnapshot
            {
                ProductName = "x", Size = "x", Finish = "x",
            },
        });
        db.SaveChanges();
    }

    private static ArchiveRetentionJob BuildSut(
        PhotoPrintDbContext db,
        IStorageRouter router,
        IOptions<ArchiveSettings>? settings = null) =>
        new(BuildScopes(db, router), settings ?? Settings(),
            Mock.Of<ILogger<ArchiveRetentionJob>>());

    /// <summary>
    /// Invokes the <c>internal SweepAsync</c> via reflection — matches
    /// <c>UploadCleanupJobTests</c>'s pattern of test-driving a hosted service's
    /// inner work loop without exposing the seam on the public API.
    /// </summary>
    private static async Task<(int uploads, int blobs, int failed)> SweepAsync(
        ArchiveRetentionJob job, CancellationToken ct)
    {
        var method = typeof(ArchiveRetentionJob).GetMethod("SweepAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var task = (Task<(int, int, int)>)method!.Invoke(job, new object[] { ct })!;
        var result = await task;
        return result;
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task SweepAsync_NoExpiredUploads_NoOp()
    {
        using var db = CreateDb();
        // Fresh upload, paid yesterday — well within the 12-month window.
        var u = SeedUpload(db);
        SeedOrderItem(db, u, DateTimeOffset.UtcNow.AddDays(-1));

        var cloud = new Mock<IStorageService>(MockBehavior.Strict);
        var sut = BuildSut(db, Router(true, cloud).Object);

        var (cleaned, blobs, failed) = await SweepAsync(sut, CancellationToken.None);

        cleaned.Should().Be(0);
        blobs.Should().Be(0);
        failed.Should().Be(0);
        cloud.VerifyNoOtherCalls();

        var updated = await db.Uploads.FindAsync(u.Id);
        updated!.LargePreviewPath.Should().NotBeNull();
        updated.ThumbnailPath.Should().NotBeNull();
    }

    [Fact]
    public async Task SweepAsync_ExpiredUpload_DeletesBothBlobs_NullsBothKeys()
    {
        using var db = CreateDb();
        var u = SeedUpload(db, previewKey: "previews/expired.jpg", thumbKey: "thumbs/expired.jpg");
        // Paid 13 months ago — past the default 12-month retention window.
        SeedOrderItem(db, u, DateTimeOffset.UtcNow.AddMonths(-13));

        var cloud = new Mock<IStorageService>(MockBehavior.Strict);
        cloud.Setup(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .Returns(Task.CompletedTask);

        var sut = BuildSut(db, Router(true, cloud).Object);

        var (cleaned, blobs, failed) = await SweepAsync(sut, CancellationToken.None);

        cleaned.Should().Be(1);
        blobs.Should().Be(2);
        failed.Should().Be(0);

        var updated = await db.Uploads.FindAsync(u.Id);
        updated!.LargePreviewPath.Should().BeNull();
        updated.ThumbnailPath.Should().BeNull();
        updated.StorageLocation.Should().Be(StorageLocation.Cloud);  // unchanged

        cloud.Verify(s => s.DeleteAsync("previews/expired.jpg", It.IsAny<CancellationToken>()), Times.Once);
        cloud.Verify(s => s.DeleteAsync("thumbs/expired.jpg", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SweepAsync_AlreadyExpired_BothKeysNull_NotPickedUp()
    {
        using var db = CreateDb();
        var u = SeedUpload(db, previewKey: null, thumbKey: null);  // already fully expired
        SeedOrderItem(db, u, DateTimeOffset.UtcNow.AddMonths(-24));

        var cloud = new Mock<IStorageService>(MockBehavior.Strict);
        var sut = BuildSut(db, Router(true, cloud).Object);

        var (cleaned, _, _) = await SweepAsync(sut, CancellationToken.None);

        cleaned.Should().Be(0);  // filtered out by the LargePreview/Thumb non-null predicate
        cloud.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SweepAsync_OnlyPreviewLeft_ThumbAlreadyNull_DeletesPreviewOnly()
    {
        using var db = CreateDb();
        var u = SeedUpload(db, previewKey: "previews/half.jpg", thumbKey: null);
        SeedOrderItem(db, u, DateTimeOffset.UtcNow.AddMonths(-13));

        var cloud = new Mock<IStorageService>(MockBehavior.Strict);
        cloud.Setup(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .Returns(Task.CompletedTask);

        var sut = BuildSut(db, Router(true, cloud).Object);

        var (cleaned, blobs, _) = await SweepAsync(sut, CancellationToken.None);

        cleaned.Should().Be(1);
        blobs.Should().Be(1);
        cloud.Verify(s => s.DeleteAsync("previews/half.jpg", It.IsAny<CancellationToken>()), Times.Once);
        cloud.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SweepAsync_DeleteFails_FailedCounted_OtherUploadsContinue()
    {
        using var db = CreateDb();
        var ok = SeedUpload(db, previewKey: "previews/ok.jpg", thumbKey: "thumbs/ok.jpg");
        SeedOrderItem(db, ok, DateTimeOffset.UtcNow.AddMonths(-13));
        var bad = SeedUpload(db, previewKey: "previews/bad.jpg", thumbKey: "thumbs/bad.jpg");
        SeedOrderItem(db, bad, DateTimeOffset.UtcNow.AddMonths(-13));

        var cloud = new Mock<IStorageService>(MockBehavior.Strict);
        cloud.Setup(s => s.DeleteAsync("previews/ok.jpg", It.IsAny<CancellationToken>()))
             .Returns(Task.CompletedTask);
        cloud.Setup(s => s.DeleteAsync("thumbs/ok.jpg", It.IsAny<CancellationToken>()))
             .Returns(Task.CompletedTask);
        cloud.Setup(s => s.DeleteAsync("previews/bad.jpg", It.IsAny<CancellationToken>()))
             .ThrowsAsync(new IOException("simulated"));

        var sut = BuildSut(db, Router(true, cloud).Object);

        var (cleaned, _, failed) = await SweepAsync(sut, CancellationToken.None);

        cleaned.Should().Be(2);  // both uploads visited
        failed.Should().Be(1);

        var okUpdated = await db.Uploads.FindAsync(ok.Id);
        var badUpdated = await db.Uploads.FindAsync(bad.Id);
        okUpdated!.LargePreviewPath.Should().BeNull();
        okUpdated.ThumbnailPath.Should().BeNull();
        // bad's preview delete threw — both keys remain (the catch was BEFORE either null).
        badUpdated!.LargePreviewPath.Should().NotBeNull();
        badUpdated.ThumbnailPath.Should().NotBeNull();
    }

    [Fact]
    public async Task SweepAsync_CloudTierOff_NoOp()
    {
        using var db = CreateDb();
        var u = SeedUpload(db);
        SeedOrderItem(db, u, DateTimeOffset.UtcNow.AddMonths(-13));

        var sut = BuildSut(db, Router(cloudEnabled: false).Object);

        var (cleaned, blobs, failed) = await SweepAsync(sut, CancellationToken.None);

        cleaned.Should().Be(0);
        blobs.Should().Be(0);
        failed.Should().Be(0);
    }

    [Fact]
    public async Task SweepAsync_LocalUpload_NotPickedUp()
    {
        // The sweep filters on StorageLocation == Cloud. A Local upload is never expired —
        // it's pre-promotion / pre-purge and a different lifecycle entirely.
        using var db = CreateDb();
        var u = SeedUpload(db, loc: StorageLocation.Local);
        SeedOrderItem(db, u, DateTimeOffset.UtcNow.AddMonths(-13));

        var cloud = new Mock<IStorageService>(MockBehavior.Strict);
        var sut = BuildSut(db, Router(true, cloud).Object);

        var (cleaned, _, _) = await SweepAsync(sut, CancellationToken.None);

        cleaned.Should().Be(0);
        cloud.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SweepAsync_ShortRetentionWindow_HitsRecentOrders()
    {
        // RetentionMonths = 1 — paid more than 1 month ago should sweep.
        using var db = CreateDb();
        var u = SeedUpload(db);
        SeedOrderItem(db, u, DateTimeOffset.UtcNow.AddMonths(-2));

        var cloud = new Mock<IStorageService>(MockBehavior.Strict);
        cloud.Setup(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .Returns(Task.CompletedTask);

        var sut = BuildSut(db, Router(true, cloud).Object, Settings(retentionMonths: 1));

        var (cleaned, _, _) = await SweepAsync(sut, CancellationToken.None);

        cleaned.Should().Be(1);
    }

    // ── D50 (review 043-v7): shared uploads across orders ─────────────────────

    [Fact]
    public async Task SweepAsync_UploadSharedWithInWindowOrder_IsNotExpired()
    {
        // Retention keyed on ANY referencing order aging out deleted previews a NEWER paid
        // order was still entitled to view — permanent loss once the original is purged.
        // Delete only when NO referencing order is inside the window.
        using var db = CreateDb();
        var u = SeedUpload(db);
        SeedOrderItem(db, u, DateTimeOffset.UtcNow.AddMonths(-13)); // aged out
        SeedOrderItem(db, u, DateTimeOffset.UtcNow.AddMonths(-1));  // still in-window

        var cloud = new Mock<IStorageService>(MockBehavior.Strict);
        var sut = BuildSut(db, Router(true, cloud).Object);

        var (cleaned, blobs, _) = await SweepAsync(sut, CancellationToken.None);

        cleaned.Should().Be(0);
        blobs.Should().Be(0);
        cloud.VerifyNoOtherCalls();
        var after = await db.Uploads.FindAsync(u.Id);
        after!.LargePreviewPath.Should().NotBeNull();
        after.ThumbnailPath.Should().NotBeNull();
    }

    [Fact]
    public async Task SweepAsync_D50Query_TranslatesOnSqlite()
    {
        // The InMemory provider runs LINQ-to-objects and proves nothing about SQL translation
        // (data-stack parity rule). The D50 fix added a second correlated NOT-EXISTS to the
        // candidate query; an untranslatable shape throws at ToListAsync regardless of rows,
        // so an empty-DB sweep on real SQLite pins translatability. Filtering semantics are
        // covered by SweepAsync_UploadSharedWithInWindowOrder_IsNotExpired (InMemory); the
        // full relational-graph seeding belongs to the deferred D20 Testcontainers track.
        using var conn = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
        conn.Open();
        var options = new DbContextOptionsBuilder<PhotoPrintDbContext>()
            .UseSqlite(conn).Options;
        using var db = new PhotoPrintDbContext(options);
        db.Database.EnsureCreated();

        var cloud = new Mock<IStorageService>(MockBehavior.Strict);
        var sut = BuildSut(db, Router(true, cloud).Object);

        var (cleaned, blobs, failed) = await SweepAsync(sut, CancellationToken.None);

        (cleaned, blobs, failed).Should().Be((0, 0, 0));
    }

    // ── D56 (review 043-v7): ArchiveExpired audit must follow the durable commit ──

    private sealed class ThrowingSaveDbContext : PhotoPrintDbContext
    {
        public ThrowingSaveDbContext(DbContextOptions<PhotoPrintDbContext> options) : base(options) { }
        public override Task<int> SaveChangesAsync(CancellationToken ct = default)
            => throw new InvalidOperationException("simulated save failure");
    }

    private static bool LogsArchiveExpired(Mock<ILogger<ArchiveRetentionJob>> logger, Times times)
    {
        logger.Verify(l => l.Log(
            It.IsAny<LogLevel>(),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains("ArchiveExpired")),
            It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times);
        return true;
    }

    [Fact]
    public async Task SweepAsync_SaveFails_EmitsNoArchiveExpiredAudit()
    {
        // The audit event fired per-upload BEFORE the batched SaveChanges: a failed save left
        // ArchiveExpired on record for rows never persisted, re-emitted every subsequent tick
        // (duplicate/false audit trail). No commit -> no audit event.
        var storeName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<PhotoPrintDbContext>()
            .UseInMemoryDatabase(storeName).Options;
        using (var seedDb = new PhotoPrintDbContext(options))
        {
            var u = SeedUpload(seedDb);
            SeedOrderItem(seedDb, u, DateTimeOffset.UtcNow.AddMonths(-13));
        }

        using var throwingDb = new ThrowingSaveDbContext(options);
        var cloud = new Mock<IStorageService>();
        cloud.Setup(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .Returns(Task.CompletedTask);
        var logger = new Mock<ILogger<ArchiveRetentionJob>>();
        var sut = new ArchiveRetentionJob(
            BuildScopes(throwingDb, Router(true, cloud).Object), Settings(), logger.Object);

        var act = () => SweepAsync(sut, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        LogsArchiveExpired(logger, Times.Never());
    }

    [Fact]
    public async Task SweepAsync_SaveSucceeds_EmitsArchiveExpiredAuditPerUpload()
    {
        using var db = CreateDb();
        var u = SeedUpload(db);
        SeedOrderItem(db, u, DateTimeOffset.UtcNow.AddMonths(-13));

        var cloud = new Mock<IStorageService>();
        cloud.Setup(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .Returns(Task.CompletedTask);
        var logger = new Mock<ILogger<ArchiveRetentionJob>>();
        var sut = new ArchiveRetentionJob(
            BuildScopes(db, Router(true, cloud).Object), Settings(), logger.Object);

        await SweepAsync(sut, CancellationToken.None);

        LogsArchiveExpired(logger, Times.Once());
    }
}
