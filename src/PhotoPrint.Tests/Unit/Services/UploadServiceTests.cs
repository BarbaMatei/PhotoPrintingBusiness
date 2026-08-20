using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using PhotoPrint.API.Data;
using PhotoPrint.API.DTOs.Uploads;
using PhotoPrint.API.Exceptions;
using PhotoPrint.API.Models;
using PhotoPrint.API.Observability;
using PhotoPrint.API.Services;
using PhotoPrint.Tests.Helpers;

namespace PhotoPrint.Tests.Unit.Services;

public class UploadServiceTests
{
    // Minimal JPEG header bytes (passes MimeValidator, no real image data)
    private static readonly byte[] JpegMagic =
    [
        0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01,
    ];

    private readonly PhotoPrintDbContext _db;
    private readonly DbContextOptions<PhotoPrintDbContext> _options;
    private readonly Mock<IStorageService> _storageMock;
    private readonly Mock<IStorageRouter> _routerMock;
    private readonly Mock<IImageProcessor> _imageProcessorMock;
    private readonly IUploadService _sut;

    public UploadServiceTests()
    {
        _options = new DbContextOptionsBuilder<PhotoPrintDbContext>()
            .UseInMemoryDatabase($"UploadSvc_{Guid.NewGuid():N}")
            .Options;
        _db = new PhotoPrintDbContext(_options);

        // Storage adapter contract is now caller-supplied key. The router
        // returns the same mock for both Local and the per-location lookup, so tests
        // exercise the routing layer without needing two adapters.
        _storageMock = new Mock<IStorageService>();
        _storageMock
            .Setup(s => s.SaveAsync(
                It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _storageMock
            .Setup(s => s.GetStreamAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new MemoryStream(JpegMagic));
        _storageMock
            .Setup(s => s.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // default: cache miss → regenerate

        _routerMock = new Mock<IStorageRouter>();
        _routerMock.Setup(r => r.Local).Returns(_storageMock.Object);
        _routerMock.Setup(r => r.For(It.IsAny<StorageLocation>())).Returns(_storageMock.Object);
        _routerMock.Setup(r => r.CloudEnabled).Returns(false);

        // ImageProcessor's signature changed in bolt 043: it now reads from a Stream
        // supplied by the caller (no IStorageService dependency).
        _imageProcessorMock = new Mock<IImageProcessor>();
        _imageProcessorMock
            .Setup(p => p.GetInfoAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ImageInfo(800, 600));
        _imageProcessorMock
            .Setup(p => p.GenerateThumbnailAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new MemoryStream(JpegMagic));

        _sut = new UploadService(
            _routerMock.Object,
            new MimeValidator(),
            _imageProcessorMock.Object,
            _db,
            Mock.Of<ILogger<UploadService>>());
    }

    private static MemoryStream JpegStream() => new(JpegMagic);

    // ── UploadAsync — guard clauses ───────────────────────────────────────────

    [Fact]
    public async Task UploadAsync_FileSizeExceedsLimit_ThrowsRequestEntityTooLargeException()
    {
        var act = () => _sut.UploadAsync(
            JpegStream(), "photo.jpg", declaredLength: 52_428_801L,
            userId: Guid.NewGuid(), guestSessionId: null);

        await act.Should().ThrowAsync<RequestEntityTooLargeException>();
    }

    [Fact]
    public async Task UploadAsync_UnsupportedFileType_ThrowsUnsupportedMediaTypeException()
    {
        using var pdfStream = new MemoryStream([0x25, 0x50, 0x44, 0x46]); // PDF header

        var act = () => _sut.UploadAsync(
            pdfStream, "file.pdf", declaredLength: 100L,
            userId: Guid.NewGuid(), guestSessionId: null);

        // The copy must not advertise HEIC — dropped end-to-end; the stale
        // promise was reintroduced here.
        await act.Should().ThrowAsync<UnsupportedMediaTypeException>()
            .WithMessage("Only JPEG and PNG files are accepted.");
    }

    [Fact]
    public async Task UploadAsync_OverlongFileName_IsTruncatedToColumnLength()
    {
        // HasMaxLength(260) sizes the column but never truncates — an
        // over-length client filename passed the InMemory tests yet failed on
        // Postgres with a 22001 string-truncation -> 500. Truncate at the service boundary.
        var longName = new string('a', 300) + ".jpg";

        var dto = await _sut.UploadAsync(
            JpegStream(), longName, declaredLength: 100L,
            userId: Guid.NewGuid(), guestSessionId: null);

        dto.OriginalFileName.Length.Should().Be(260);
    }

    [Fact]
    public async Task UploadAsync_NoOwnerProvided_ThrowsBadRequestException()
    {
        var act = () => _sut.UploadAsync(
            JpegStream(), "photo.jpg", declaredLength: 100L,
            userId: null, guestSessionId: null);

        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task UploadAsync_GuestAtUploadCap_ThrowsTooManyRequestsException()
    {
        var guestId = Guid.NewGuid();

        // Seed MaxUploadsPerSession (100) active uploads for this guest session
        for (int i = 0; i < 100; i++)
        {
            _db.Uploads.Add(new Upload
            {
                GuestSessionId = guestId,
                FilePath       = $"guest/{i}.jpg",
                OriginalFileName = $"{i}.jpg",
                ContentType    = "image/jpeg",
                WidthPx        = 100,
                HeightPx       = 100,
                FileSizeBytes  = 1024,
            });
        }
        await _db.SaveChangesAsync();

        var act = () => _sut.UploadAsync(
            JpegStream(), "photo.jpg", declaredLength: 100L,
            userId: null, guestSessionId: guestId);

        await act.Should().ThrowAsync<TooManyRequestsException>();
    }

    [Fact]
    public async Task UploadAsync_ImageProcessorReturnsNull_ThrowsWithoutSavingToStorage()
    {
        // Validation runs BEFORE storage — invalid images never
        // reach the adapter, so there's nothing to clean up.
        _imageProcessorMock
            .Setup(p => p.GetInfoAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ImageInfo?)null);

        var act = () => _sut.UploadAsync(
            JpegStream(), "photo.jpg", declaredLength: 100L,
            userId: Guid.NewGuid(), guestSessionId: null);

        await act.Should().ThrowAsync<UnprocessableEntityException>();
        _storageMock.Verify(
            s => s.SaveAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── UploadAsync — happy path ───────────────────────────────────────────────

    [Fact]
    public async Task UploadAsync_ValidJpegForUser_PersistsUploadWithCorrectFields()
    {
        var userId = Guid.NewGuid();

        await _sut.UploadAsync(
            JpegStream(), "my photo.jpg", declaredLength: (long)JpegMagic.Length,
            userId: userId, guestSessionId: null);

        var upload = await _db.Uploads.SingleAsync();
        upload.UserId.Should().Be(userId);
        upload.GuestSessionId.Should().BeNull();
        upload.ContentType.Should().Be("image/jpeg");
        upload.WidthPx.Should().Be(800);
        upload.HeightPx.Should().Be(600);
        upload.StorageLocation.Should().Be(StorageLocation.Local);
        upload.DeletedAt.Should().BeNull();
    }

    [Fact]
    public async Task UploadAsync_ValidJpeg_WritesToLocalTierWithCallerSuppliedKey()
    {
        // New uploads start on the Local tier and the
        // storage key follows the StorageKeys.Original scheme.
        await _sut.UploadAsync(
            JpegStream(), "photo.jpg", declaredLength: (long)JpegMagic.Length,
            userId: Guid.NewGuid(), guestSessionId: null);

        _routerMock.Verify(r => r.Local, Times.AtLeastOnce);
        _storageMock.Verify(
            s => s.SaveAsync(
                It.IsAny<Stream>(),
                It.Is<string>(k => k.StartsWith("uploads/") && k.EndsWith(".jpg")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UploadAsync_ValidJpeg_ReturnsDtoWithExpectedFields()
    {
        var dto = await _sut.UploadAsync(
            JpegStream(), "photo.jpg", declaredLength: (long)JpegMagic.Length,
            userId: Guid.NewGuid(), guestSessionId: null);

        dto.Should().BeOfType<UploadDto>();
        dto.Id.Should().NotBe(Guid.Empty);
        dto.ContentType.Should().Be("image/jpeg");
        dto.WidthPx.Should().Be(800);
        dto.HeightPx.Should().Be(600);
        dto.OriginalFileName.Should().Be("photo.jpg");
    }

    [Fact]
    public async Task UploadAsync_OriginalFileNameContainsPath_StripsDirComponent()
    {
        var dto = await _sut.UploadAsync(
            JpegStream(), @"C:\Users\evil\traversal.jpg", declaredLength: 100L,
            userId: Guid.NewGuid(), guestSessionId: null);

        dto.OriginalFileName.Should().Be("traversal.jpg");
    }

    [Fact]
    public async Task UploadAsync_GuestSessionUnderCap_PersistsUploadWithGuestSessionId()
    {
        var guestId = Guid.NewGuid();

        await _sut.UploadAsync(
            JpegStream(), "photo.jpg", declaredLength: 100L,
            userId: null, guestSessionId: guestId);

        var upload = await _db.Uploads.SingleAsync();
        upload.GuestSessionId.Should().Be(guestId);
        upload.UserId.Should().BeNull();
    }

    [Fact]
    public async Task UploadAsync_ImageDimensionsExceedLimit_ThrowsAndDoesNotSaveToStorage()
    {
        _imageProcessorMock
            .Setup(p => p.GetInfoAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ImageInfo(30_000, 30_000)); // pixel bomb

        var act = () => _sut.UploadAsync(
            JpegStream(), "huge.jpg", declaredLength: 100L,
            userId: Guid.NewGuid(), guestSessionId: null);

        await act.Should().ThrowAsync<UnprocessableEntityException>()
            .WithMessage("*dimensions exceed*");
        _storageMock.Verify(
            s => s.SaveAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── upload_size_bytes emission ─────────────────────────────────────────────

    [Fact]
    public async Task UploadAsync_RecordsTheStoredByteCountOnUploadSizeBytes()
    {
        using var metrics = new MetricCapture(MetricNames.Instruments.UploadSizeBytes);

        await _sut.UploadAsync(
            JpegStream(), "photo.jpg", declaredLength: (long)JpegMagic.Length,
            userId: Guid.NewGuid(), guestSessionId: null);

        var recorded = metrics.Measurements.Should().ContainSingle().Subject;
        recorded.Instrument.Should().Be(MetricNames.Instruments.UploadSizeBytes);
        recorded.Value.Should().Be(JpegMagic.Length);
        recorded.Tags.Should().BeEmpty("a size histogram with labels would multiply series");
        metrics.ContractViolations().Should().BeEmpty();
    }

    [Fact]
    public async Task UploadAsync_RejectedUpload_RecordsNoUploadSize()
    {
        using var metrics = new MetricCapture(MetricNames.Instruments.UploadSizeBytes);
        using var pdfStream = new MemoryStream([0x25, 0x50, 0x44, 0x46]);

        var act = () => _sut.UploadAsync(
            pdfStream, "doc.pdf", declaredLength: 4L,
            userId: Guid.NewGuid(), guestSessionId: null);

        await act.Should().ThrowAsync<UnsupportedMediaTypeException>();
        metrics.Measurements.Should().BeEmpty();
    }

    // ── GetPreviewAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetPreviewAsync_UploadDoesNotExist_ThrowsNotFoundException()
    {
        var act = () => _sut.GetPreviewAsync(Guid.NewGuid(), userId: Guid.NewGuid(), guestSessionId: null);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetPreviewAsync_SoftDeletedUpload_ThrowsNotFoundException()
    {
        var userId = Guid.NewGuid();
        var upload = SeedUpload(userId: userId, deletedAt: DateTimeOffset.UtcNow.AddHours(-1));
        await _db.SaveChangesAsync();

        var act = () => _sut.GetPreviewAsync(upload.Id, userId: userId, guestSessionId: null);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetPreviewAsync_WrongUserId_ThrowsForbiddenException()
    {
        var upload = SeedUpload(userId: Guid.NewGuid());
        await _db.SaveChangesAsync();

        var act = () => _sut.GetPreviewAsync(upload.Id, userId: Guid.NewGuid(), guestSessionId: null);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task GetPreviewAsync_WrongGuestSessionId_ThrowsForbiddenException()
    {
        var upload = SeedUpload(guestSessionId: Guid.NewGuid());
        await _db.SaveChangesAsync();

        var act = () => _sut.GetPreviewAsync(upload.Id, userId: null, guestSessionId: Guid.NewGuid());

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task GetPreviewAsync_MatchingUserId_ReturnsLocalLocationWithThumbnailKey()
    {
        var userId = Guid.NewGuid();
        var upload = SeedUpload(userId: userId);
        await _db.SaveChangesAsync();

        var loc = await _sut.GetPreviewAsync(upload.Id, userId: userId, guestSessionId: null);

        loc.UploadId.Should().Be(upload.Id);
        loc.Location.Should().Be(StorageLocation.Local);
        loc.ThumbnailKey.Should().Be($"thumbs/{upload.Id:N}.jpg");
    }

    [Fact]
    public async Task GetPreviewAsync_MatchingGuestSessionId_ReturnsLocalLocation()
    {
        var guestId = Guid.NewGuid();
        var upload = SeedUpload(guestSessionId: guestId);
        await _db.SaveChangesAsync();

        var loc = await _sut.GetPreviewAsync(upload.Id, userId: null, guestSessionId: guestId);

        loc.Location.Should().Be(StorageLocation.Local);
        loc.ThumbnailKey.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetPreviewAsync_CloudUpload_ReturnsCloudLocation()
    {
        // A promoted (Cloud) upload returns Location=Cloud; the controller is responsible
        // for translating that into a 302 presigned URL. Cloud tier enabled (the routable case).
        _routerMock.Setup(r => r.CloudEnabled).Returns(true);
        var userId = Guid.NewGuid();
        var upload = SeedUpload(userId: userId);
        upload.StorageLocation = StorageLocation.Cloud;
        upload.ThumbnailPath = $"thumbs/{upload.Id:N}.jpg";
        await _db.SaveChangesAsync();

        // For a Cloud upload, the thumbnail is assumed already present in cloud — the
        // router still routes to our single fake; ExistsAsync returns true so no regen.
        _storageMock
            .Setup(s => s.ExistsAsync($"thumbs/{upload.Id:N}.jpg", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var loc = await _sut.GetPreviewAsync(upload.Id, userId, guestSessionId: null);

        loc.Location.Should().Be(StorageLocation.Cloud);
        loc.ThumbnailKey.Should().Be($"thumbs/{upload.Id:N}.jpg");
    }

    [Fact]
    public async Task GetPreviewAsync_CloudUploadWithCloudDisabled_ThrowsNotFound()
    {
        // Class-sweep: a Cloud-located upload with the cloud tier disabled
        // (Storage:Provider reverted to local) is unroutable — For(Cloud) would throw
        // InvalidOperationException, unmapped -> 500 on the customer preview. Degrade to a clean 404.
        // The shared setup already has CloudEnabled = false.
        var userId = Guid.NewGuid();
        var upload = SeedUpload(userId: userId);
        upload.StorageLocation = StorageLocation.Cloud;
        await _db.SaveChangesAsync();

        await _sut.Invoking(s => s.GetPreviewAsync(upload.Id, userId, guestSessionId: null))
            .Should().ThrowAsync<NotFoundException>();
    }

    // ── GetPreviewAsync — thumbnail caching ────────────────────────

    [Fact]
    public async Task GetPreviewAsync_SecondCall_StreamsCacheWithoutRegenerating()
    {
        var userId = Guid.NewGuid();
        var upload = SeedUpload(userId: userId);
        await _db.SaveChangesAsync();

        // The first GetPreviewAsync never queries ExistsAsync (ThumbnailPath is null, the
        // null check short-circuits). It regenerates, sets ThumbnailPath, and saves.
        // The second call asks ExistsAsync(thumbKey) — we say true to model a cache hit.
        var thumbKey = $"thumbs/{upload.Id:N}.jpg";
        _storageMock
            .Setup(s => s.ExistsAsync(thumbKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _sut.GetPreviewAsync(upload.Id, userId, guestSessionId: null);
        await _sut.GetPreviewAsync(upload.Id, userId, guestSessionId: null);

        // Cache hit on the second call — no thumbnail regeneration.
        _imageProcessorMock.Verify(
            p => p.GenerateThumbnailAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetPreviewAsync_CachedFileMissing_RegeneratesThumbnail()
    {
        var userId = Guid.NewGuid();
        var upload = SeedUpload(userId: userId);
        upload.ThumbnailPath = "thumbs/stale.jpg";   // recorded, but file is gone
        await _db.SaveChangesAsync();

        _storageMock
            .Setup(s => s.ExistsAsync("thumbs/stale.jpg", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await _sut.GetPreviewAsync(upload.Id, userId, guestSessionId: null);

        _imageProcessorMock.Verify(
            p => p.GenerateThumbnailAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── GetPreviewAsync — ported bolt-042 hardening (adapted to the router design) ─────

    [Fact]
    public async Task GetPreviewAsync_OriginalBlobGone_ThrowsNotFoundAndSignals()
    {
        // M6/F5: FilePath is recorded but the blob is physically gone (ops-side
        // deletion / cleanup race). Unmapped, GetStreamAsync's FileNotFoundException would surface
        // as a 500; it must be a clean 404 + a reserved signal so the storage-integrity incident
        // is not lost in ordinary 404 noise.
        var userId = Guid.NewGuid();
        var upload = SeedUpload(userId: userId);
        await _db.SaveChangesAsync();

        _storageMock
            .Setup(s => s.GetStreamAsync(upload.FilePath!, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FileNotFoundException("original gone"));

        var (sut, logger) = NewSutWithLogger();

        await sut.Invoking(s => s.GetPreviewAsync(upload.Id, userId, guestSessionId: null))
            .Should().ThrowAsync<NotFoundException>();

        VerifyLogged(logger, "uploads.original.missing_file");
    }

    [Fact]
    public async Task GetPreviewAsync_RowSoftDeletedDuringGeneration_DeletesOrphanAndSignals()
    {
        // M1/F6: the cleanup job can soft-delete the row between the live read
        // and the ThumbnailPath write (which keys only on Id — no DeletedAt guard). A thumbnail
        // written onto a now-dead row is never revisited by cleanup, so the write must detect the
        // row is gone and delete the just-written orphan.
        var userId = Guid.NewGuid();
        var upload = SeedUpload(userId: userId);
        await _db.SaveChangesAsync();

        var thumbKey = $"thumbs/{upload.Id:N}.jpg";
        _storageMock.Setup(s => s.DeleteAsync(thumbKey, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // GenerateThumbnailAsync runs AFTER the live read and BEFORE the persist; soft-deleting via
        // a separate context here (as the cleanup job would) reproduces the race deterministically.
        _imageProcessorMock
            .Setup(p => p.GenerateThumbnailAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                using var ctx = new PhotoPrintDbContext(_options);
                var row = ctx.Uploads.First(u => u.Id == upload.Id);
                row.DeletedAt = DateTimeOffset.UtcNow;
                ctx.SaveChanges();
                return new MemoryStream(JpegMagic);
            });

        var (sut, logger) = NewSutWithLogger(new PhotoPrintDbContext(_options));

        await sut.GetPreviewAsync(upload.Id, userId, guestSessionId: null);

        _storageMock.Verify(s => s.DeleteAsync(thumbKey, It.IsAny<CancellationToken>()), Times.Once);
        VerifyLogged(logger, "uploads.thumbnail.deleted_row_race");
    }

    [Fact]
    public async Task GetPreviewAsync_PersistFailsAfterThumbnailWritten_DeletesOrphanAndSignals()
    {
        // L4: on cache-miss the thumbnail is written to storage, then the
        // ThumbnailPath persist runs. If that commit throws, the file is on disk but the row never
        // references it, so the cleanup job (which keys on ThumbnailPath) can never reclaim it —
        // a silent orphan. Signal + best-effort delete before rethrowing.
        var userId = Guid.NewGuid();
        var upload = SeedUpload(userId: userId);
        await _db.SaveChangesAsync();

        var thumbKey = $"thumbs/{upload.Id:N}.jpg";
        _storageMock.Setup(s => s.DeleteAsync(thumbKey, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var throwingDb = new SaveThrowingDbContext(_options);
        var (sut, logger) = NewSutWithLogger(throwingDb);

        await sut.Invoking(s => s.GetPreviewAsync(upload.Id, userId, guestSessionId: null))
            .Should().ThrowAsync<InvalidOperationException>();

        _storageMock.Verify(s => s.DeleteAsync(thumbKey, It.IsAny<CancellationToken>()), Times.Once);
        VerifyLogged(logger, "uploads.thumbnail.orphaned_on_commit_failure");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private (IUploadService sut, Mock<ILogger<UploadService>> logger) NewSutWithLogger(
        PhotoPrintDbContext? db = null)
    {
        var logger = new Mock<ILogger<UploadService>>();
        var sut = new UploadService(
            _routerMock.Object, new MimeValidator(), _imageProcessorMock.Object, db ?? _db, logger.Object);
        return (sut, logger);
    }

    private static void VerifyLogged(Mock<ILogger<UploadService>> logger, string marker)
        => logger.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains(marker)),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

    // A DbContext whose SaveChangesAsync always throws — models a transient commit fault after the
    // thumbnail bytes are already written to storage.
    private sealed class SaveThrowingDbContext(DbContextOptions<PhotoPrintDbContext> options)
        : PhotoPrintDbContext(options)
    {
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("simulated commit failure");
    }

    private Upload SeedUpload(Guid? userId = null, Guid? guestSessionId = null, DateTimeOffset? deletedAt = null)
    {
        var upload = new Upload
        {
            UserId           = userId,
            GuestSessionId   = guestSessionId,
            FilePath         = "uploads/2026/05/file.jpg",
            OriginalFileName = "file.jpg",
            ContentType      = "image/jpeg",
            WidthPx          = 800,
            HeightPx         = 600,
            FileSizeBytes    = 1024,
            DeletedAt        = deletedAt,
            // StorageLocation defaults to Local via the Upload model's initializer
        };
        _db.Uploads.Add(upload);
        return upload;
    }
}
