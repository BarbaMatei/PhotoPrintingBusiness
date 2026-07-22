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

public class UploadCleanupJobTests
{
    private static PhotoPrintDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<PhotoPrintDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static IServiceScopeFactory BuildScopeFactory(IStorageService storage, PhotoPrintDbContext db)
    {
        // Route every tier to the single storage mock so existing (Local) tests are unaffected
        // by the bolt-043 switch to IStorageRouter.
        var router = new Mock<IStorageRouter>();
        router.SetupGet(r => r.Local).Returns(storage);
        router.Setup(r => r.For(It.IsAny<StorageLocation>())).Returns(storage);

        var services = new ServiceCollection();
        services.AddSingleton<PhotoPrintDbContext>(_ => db);
        services.AddSingleton<IStorageRouter>(router.Object);
        services.AddLogging();
        return services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
    }

    // Distinct local/cloud stores so a test can assert deletes route to the right tier.
    private static (IServiceScopeFactory factory, Mock<IStorageService> local, Mock<IStorageService> cloud)
        BuildTieredScopeFactory(PhotoPrintDbContext db)
    {
        var local = new Mock<IStorageService>();
        var cloud = new Mock<IStorageService>();
        var router = new Mock<IStorageRouter>();
        router.SetupGet(r => r.CloudEnabled).Returns(true); // this factory provides a cloud store
        router.SetupGet(r => r.Local).Returns(local.Object);
        router.SetupGet(r => r.Cloud).Returns(cloud.Object);
        router.Setup(r => r.For(StorageLocation.Local)).Returns(local.Object);
        router.Setup(r => r.For(StorageLocation.Cloud)).Returns(cloud.Object);

        var services = new ServiceCollection();
        services.AddSingleton<PhotoPrintDbContext>(_ => db);
        services.AddSingleton<IStorageRouter>(router.Object);
        services.AddLogging();
        return (services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>(), local, cloud);
    }

    // Cloud tier disabled: For(Cloud) throws exactly as the real StorageRouter does when _cloud is null.
    private static (IServiceScopeFactory factory, Mock<IStorageService> local)
        BuildCloudDisabledScopeFactory(PhotoPrintDbContext db)
    {
        var local = new Mock<IStorageService>();
        var router = new Mock<IStorageRouter>();
        router.SetupGet(r => r.CloudEnabled).Returns(false);
        router.SetupGet(r => r.Local).Returns(local.Object);
        router.Setup(r => r.For(StorageLocation.Local)).Returns(local.Object);
        router.Setup(r => r.For(StorageLocation.Cloud))
              .Throws(new InvalidOperationException("Cloud storage is not enabled."));

        var services = new ServiceCollection();
        services.AddSingleton<PhotoPrintDbContext>(_ => db);
        services.AddSingleton<IStorageRouter>(router.Object);
        services.AddLogging();
        return (services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>(), local);
    }

    private static IOptionsMonitor<UploadCleanupSettings> Settings(
        int orphanRetentionHours = 24,
        int referencedRetentionDays = 365)
    {
        var value = new UploadCleanupSettings
        {
            OrphanRetentionHours = orphanRetentionHours,
            ReferencedRetentionDays = referencedRetentionDays,
        };
        var monitor = new Mock<IOptionsMonitor<UploadCleanupSettings>>();
        monitor.SetupGet(m => m.CurrentValue).Returns(value);
        return monitor.Object;
    }

    private static Upload MakeUpload(DateTimeOffset uploadedAt, bool softDeleted = false) => new()
    {
        Id = Guid.NewGuid(),
        FilePath = $"uploads/{Guid.NewGuid()}.jpg",
        OriginalFileName = "photo.jpg",
        ContentType = "image/jpeg",
        WidthPx = 1200,
        HeightPx = 1800,
        FileSizeBytes = 1024,
        UploadedAt = uploadedAt,
        DeletedAt = softDeleted ? DateTimeOffset.UtcNow : null,
        GuestSessionId = Guid.NewGuid(), // satisfy non-null FK
    };

    [Fact]
    public async Task OldUndeleted_Upload_IsSoftDeletedAndFileRemoved()
    {
        var db = CreateDb();
        var old = MakeUpload(DateTimeOffset.UtcNow.AddHours(-25));
        await db.Uploads.AddAsync(old);
        await db.SaveChangesAsync();

        var storageMock = new Mock<IStorageService>();
        var job = new UploadCleanupJob(BuildScopeFactory(storageMock.Object, db),
            Settings(),
            Mock.Of<ILogger<UploadCleanupJob>>());

        using var cts = new CancellationTokenSource();
        var method = typeof(UploadCleanupJob).GetMethod("CleanupAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        await (Task<(int, int)>)method!.Invoke(job, new object[] { cts.Token })!;

        var updated = await db.Uploads.FindAsync(old.Id);
        updated!.DeletedAt.Should().NotBeNull();
        storageMock.Verify(s => s.DeleteAsync(old.FilePath!, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RecentUpload_IsSkipped()
    {
        var db = CreateDb();
        var recent = MakeUpload(DateTimeOffset.UtcNow.AddMinutes(-30));
        await db.Uploads.AddAsync(recent);
        await db.SaveChangesAsync();

        var storageMock = new Mock<IStorageService>();
        var job = new UploadCleanupJob(BuildScopeFactory(storageMock.Object, db),
            Settings(),
            Mock.Of<ILogger<UploadCleanupJob>>());

        var method = typeof(UploadCleanupJob).GetMethod("CleanupAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        await (Task<(int, int)>)method!.Invoke(job, new object[] { CancellationToken.None })!;

        var updated = await db.Uploads.FindAsync(recent.Id);
        updated!.DeletedAt.Should().BeNull();
        storageMock.Verify(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AlreadySoftDeleted_Upload_IsSkipped()
    {
        var db = CreateDb();
        var already = MakeUpload(DateTimeOffset.UtcNow.AddHours(-48), softDeleted: true);
        await db.Uploads.AddAsync(already);
        await db.SaveChangesAsync();

        var storageMock = new Mock<IStorageService>();
        var job = new UploadCleanupJob(BuildScopeFactory(storageMock.Object, db),
            Settings(),
            Mock.Of<ILogger<UploadCleanupJob>>());

        var method = typeof(UploadCleanupJob).GetMethod("CleanupAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        await (Task<(int, int)>)method!.Invoke(job, new object[] { CancellationToken.None })!;

        storageMock.Verify(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task StorageDeletionError_IsCountedAsError_ButUploadStillSoftDeleted()
    {
        var db = CreateDb();
        var old = MakeUpload(DateTimeOffset.UtcNow.AddHours(-25));
        await db.Uploads.AddAsync(old);
        await db.SaveChangesAsync();

        var storageMock = new Mock<IStorageService>();
        storageMock.Setup(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                   .ThrowsAsync(new IOException("Disk error"));

        var job = new UploadCleanupJob(BuildScopeFactory(storageMock.Object, db),
            Settings(),
            Mock.Of<ILogger<UploadCleanupJob>>());

        var method = typeof(UploadCleanupJob).GetMethod("CleanupAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var (deleted, errors) = await (Task<(int, int)>)method!.Invoke(job, new object[] { CancellationToken.None })!;

        deleted.Should().Be(1);
        errors.Should().Be(1);

        var updated = await db.Uploads.FindAsync(old.Id);
        updated!.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task MultipleOldUploads_AllSoftDeleted()
    {
        var db = CreateDb();
        var uploads = Enumerable.Range(0, 3)
            .Select(_ => MakeUpload(DateTimeOffset.UtcNow.AddHours(-26)))
            .ToList();
        await db.Uploads.AddRangeAsync(uploads);
        await db.SaveChangesAsync();

        var storageMock = new Mock<IStorageService>();
        var job = new UploadCleanupJob(BuildScopeFactory(storageMock.Object, db),
            Settings(),
            Mock.Of<ILogger<UploadCleanupJob>>());

        var method = typeof(UploadCleanupJob).GetMethod("CleanupAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var (deleted, errors) = await (Task<(int, int)>)method!.Invoke(job, new object[] { CancellationToken.None })!;

        deleted.Should().Be(3);
        errors.Should().Be(0);

        var all = await db.Uploads.ToListAsync();
        all.Should().AllSatisfy(u => u.DeletedAt.Should().NotBeNull());
    }

    // ── Reference-aware cleanup (bolt 033) ─────────────────────────────────────

    private static async Task<(Task task, IStorageService storageMock, Mock<IStorageService> mock, UploadCleanupJob job)>
        BuildJobAsync(PhotoPrintDbContext db, IOptionsMonitor<UploadCleanupSettings> settings)
    {
        await Task.CompletedTask;
        var storageMock = new Mock<IStorageService>();
        var job = new UploadCleanupJob(
            BuildScopeFactory(storageMock.Object, db),
            settings,
            Mock.Of<ILogger<UploadCleanupJob>>());
        return (Task.CompletedTask, storageMock.Object, storageMock, job);
    }

    private static Task<(int, int)> InvokeCleanupAsync(UploadCleanupJob job)
    {
        var method = typeof(UploadCleanupJob).GetMethod(
            "CleanupAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (Task<(int, int)>)method!.Invoke(job, new object[] { CancellationToken.None })!;
    }

    [Fact]
    public async Task Cleanup_skips_upload_referenced_by_cart()
    {
        var db = CreateDb();
        var upload = MakeUpload(DateTimeOffset.UtcNow.AddHours(-25));
        await db.Uploads.AddAsync(upload);
        await db.CartItems.AddAsync(new CartItem
        {
            Id = Guid.NewGuid(),
            UploadId = upload.Id,
            GuestSessionId = upload.GuestSessionId,
            ProductId = Guid.NewGuid(),
            SizeId = Guid.NewGuid(),
            Quantity = 1,
            AddedAt = DateTimeOffset.UtcNow.AddHours(-25),
        });
        await db.SaveChangesAsync();

        var (_, _, storageMock, job) = await BuildJobAsync(db, Settings());

        var (deleted, errors) = await InvokeCleanupAsync(job);

        deleted.Should().Be(0);
        errors.Should().Be(0);

        var after = await db.Uploads.FindAsync(upload.Id);
        after!.DeletedAt.Should().BeNull();
        storageMock.Verify(
            s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Cleanup_skips_upload_referenced_by_order_item()
    {
        var db = CreateDb();
        var upload = MakeUpload(DateTimeOffset.UtcNow.AddHours(-48));
        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = "FT-2026-00001",
            Status = OrderStatus.Paid,
            GuestSessionId = upload.GuestSessionId,
            GuestEmail = "buyer@example.com",
            PaymentProcessor = PaymentProcessor.Stripe,
            DeliveryType = DeliveryType.Courier,
            ShippingAddress = new ShippingAddressSnapshot
            {
                Street = "Strada Exemplu",
                Number = "1",
                City = "București",
                County = "Bucuresti",
                PostalCode = "010101",
                RecipientName = "Test Buyer",
                Phone = "+40712345678",
            },
            ShippingCostRon = 25m,
            SubtotalRon = 100m,
            TotalRon = 125m,
            CreatedAt = DateTimeOffset.UtcNow.AddHours(-30),
        };
        await db.Uploads.AddAsync(upload);
        await db.Orders.AddAsync(order);
        await db.OrderItems.AddAsync(new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            UploadId = upload.Id,
            ProductId = Guid.NewGuid(),
            Quantity = 1,
            UnitPriceRon = 100m,
            LineTotalRon = 100m,
            ProductSnapshot = new ProductSnapshot
            {
                ProductName = "Fotografie clasică",
                Size = "10x15",
                Finish = "Lucioasă",
            },
        });
        await db.SaveChangesAsync();

        var (_, _, storageMock, job) = await BuildJobAsync(db, Settings());

        var (deleted, errors) = await InvokeCleanupAsync(job);

        deleted.Should().Be(0);
        errors.Should().Be(0);

        var after = await db.Uploads.FindAsync(upload.Id);
        after!.DeletedAt.Should().BeNull();
        storageMock.Verify(
            s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Cleanup_deletes_cached_thumbnail_file_alongside_original()
    {
        // BUG-2 (review 042-v1): a previewed-then-expired upload has a second persistent file
        // (its cached thumbnail). Cleanup must delete it too, or it leaks on disk forever.
        var db = CreateDb();
        var upload = MakeUpload(DateTimeOffset.UtcNow.AddHours(-25));
        upload.ThumbnailPath = "thumbs/owner/thumb.jpg";
        await db.Uploads.AddAsync(upload);
        await db.SaveChangesAsync();

        var (_, _, storageMock, job) = await BuildJobAsync(db, Settings());

        var (deleted, errors) = await InvokeCleanupAsync(job);

        deleted.Should().Be(1);
        errors.Should().Be(0);
        storageMock.Verify(s => s.DeleteAsync(upload.FilePath, It.IsAny<CancellationToken>()), Times.Once);
        storageMock.Verify(s => s.DeleteAsync("thumbs/owner/thumb.jpg", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Cleanup_upload_without_thumbnail_only_deletes_original()
    {
        var db = CreateDb();
        var upload = MakeUpload(DateTimeOffset.UtcNow.AddHours(-25)); // ThumbnailPath stays null
        await db.Uploads.AddAsync(upload);
        await db.SaveChangesAsync();

        var (_, _, storageMock, job) = await BuildJobAsync(db, Settings());

        await InvokeCleanupAsync(job);

        storageMock.Verify(s => s.DeleteAsync(upload.FilePath, It.IsAny<CancellationToken>()), Times.Once);
        // No second delete call for a non-existent thumbnail.
        storageMock.Verify(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Cleanup_agedCloudUpload_deletesAllThreeKeysFromCloudTier()
    {
        // F2 (review 043-v1): an aged, promoted (Cloud) upload past ReferencedRetentionDays.
        // Deletes must route to the CLOUD tier and cover all THREE persistent objects
        // (original + thumbnail + large preview). The pre-fix code resolved the local default
        // (a no-op on disk) and never touched LargePreviewPath, orphaning the cloud blobs.
        var db = CreateDb();
        var upload = MakeUpload(DateTimeOffset.UtcNow.AddDays(-400));
        upload.StorageLocation = StorageLocation.Cloud;
        upload.ThumbnailPath = "thumbs/cloud/thumb.jpg";
        upload.LargePreviewPath = "previews/cloud/large.jpg";
        await db.Uploads.AddAsync(upload);
        await db.SaveChangesAsync();

        var (factory, local, cloud) = BuildTieredScopeFactory(db);
        var job = new UploadCleanupJob(factory, Settings(referencedRetentionDays: 365),
            Mock.Of<ILogger<UploadCleanupJob>>());

        var (deleted, errors) = await InvokeCleanupAsync(job);

        deleted.Should().Be(1);
        errors.Should().Be(0);

        cloud.Verify(s => s.DeleteAsync(upload.FilePath!, It.IsAny<CancellationToken>()), Times.Once);
        cloud.Verify(s => s.DeleteAsync("thumbs/cloud/thumb.jpg", It.IsAny<CancellationToken>()), Times.Once);
        cloud.Verify(s => s.DeleteAsync("previews/cloud/large.jpg", It.IsAny<CancellationToken>()), Times.Once);
        local.Verify(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);

        var after = await db.Uploads.FindAsync(upload.Id);
        after!.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Cleanup_cloudRowWithCloudDisabled_skipsItAndStillCleansLocalBatch()
    {
        // F2 (review 043-v3) + D38 (review 043-v5): cloud was enabled, an upload was promoted (Cloud),
        // then Storage:Provider reverted to local, so For(Cloud) would throw. The unroutable Cloud row
        // must not be deleted, and must not block cleanup of the local batch. Since D38 the exclusion
        // is at the QUERY level (the row never enters the batch), so For(Cloud) is never called for it.
        var db = CreateDb();
        var cloudUpload = MakeUpload(DateTimeOffset.UtcNow.AddDays(-400)); // oldest → first in batch
        cloudUpload.StorageLocation = StorageLocation.Cloud;
        var localOrphan = MakeUpload(DateTimeOffset.UtcNow.AddHours(-25)); // Local, aged out
        await db.Uploads.AddRangeAsync(cloudUpload, localOrphan);
        await db.SaveChangesAsync();

        var (factory, local) = BuildCloudDisabledScopeFactory(db);
        var job = new UploadCleanupJob(factory, Settings(referencedRetentionDays: 365),
            Mock.Of<ILogger<UploadCleanupJob>>());

        var (deleted, errors) = await InvokeCleanupAsync(job);

        deleted.Should().Be(1); // only the routable local orphan
        errors.Should().Be(0);
        local.Verify(s => s.DeleteAsync(localOrphan.FilePath!, It.IsAny<CancellationToken>()), Times.Once);

        (await db.Uploads.FindAsync(localOrphan.Id))!.DeletedAt.Should().NotBeNull();
        (await db.Uploads.FindAsync(cloudUpload.Id))!.DeletedAt.Should().BeNull(); // excluded, retried later
    }

    [Fact]
    public async Task Cleanup_manyUnroutableCloudRows_doNotStarveLocalOrphanCleanup()
    {
        // D38 (review 043-v5): with the cloud tier disabled and >= BatchSize aged Cloud rows, the
        // pre-fix code fetched the oldest BatchSize (all Cloud), skipped them post-fetch, and never
        // advanced the OrderBy/Take window to a local orphan sorted after them → local cleanup
        // wedged every sweep. The query-level exclusion must let the batch reach the routable orphan.
        var db = CreateDb();

        // BatchSize (500) aged Cloud rows, all OLDER than the local orphan so — unfiltered — they
        // would fill the entire Take(500) window and the orphan would never be reached.
        var baseTime = DateTimeOffset.UtcNow.AddDays(-500);
        for (var i = 0; i < 500; i++)
        {
            var cloud = MakeUpload(baseTime.AddSeconds(i));
            cloud.StorageLocation = StorageLocation.Cloud;
            await db.Uploads.AddAsync(cloud);
        }
        var localOrphan = MakeUpload(DateTimeOffset.UtcNow.AddHours(-25)); // newest → sorted last
        await db.Uploads.AddAsync(localOrphan);
        await db.SaveChangesAsync();

        var (factory, local) = BuildCloudDisabledScopeFactory(db);
        var job = new UploadCleanupJob(factory, Settings(referencedRetentionDays: 365),
            Mock.Of<ILogger<UploadCleanupJob>>());

        var (deleted, errors) = await InvokeCleanupAsync(job);

        // The local orphan is reached and cleaned despite 500 unroutable Cloud rows ahead of it.
        deleted.Should().Be(1);
        errors.Should().Be(0);
        local.Verify(s => s.DeleteAsync(localOrphan.FilePath!, It.IsAny<CancellationToken>()), Times.Once);
        (await db.Uploads.FindAsync(localOrphan.Id))!.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Cleanup_deletes_orphan_upload_past_referenced_window()
    {
        // Upload IS referenced by a cart, but is older than ReferencedRetentionDays —
        // the long-window branch makes it eligible for deletion anyway.
        var db = CreateDb();
        var upload = MakeUpload(DateTimeOffset.UtcNow.AddDays(-400));
        await db.Uploads.AddAsync(upload);
        await db.CartItems.AddAsync(new CartItem
        {
            Id = Guid.NewGuid(),
            UploadId = upload.Id,
            GuestSessionId = upload.GuestSessionId,
            ProductId = Guid.NewGuid(),
            SizeId = Guid.NewGuid(),
            Quantity = 1,
            AddedAt = DateTimeOffset.UtcNow.AddDays(-400),
        });
        await db.SaveChangesAsync();

        var (_, _, storageMock, job) = await BuildJobAsync(db, Settings(referencedRetentionDays: 365));

        var (deleted, errors) = await InvokeCleanupAsync(job);

        deleted.Should().Be(1);
        errors.Should().Be(0);

        var after = await db.Uploads.FindAsync(upload.Id);
        after!.DeletedAt.Should().NotBeNull();
        storageMock.Verify(
            s => s.DeleteAsync(upload.FilePath!, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
