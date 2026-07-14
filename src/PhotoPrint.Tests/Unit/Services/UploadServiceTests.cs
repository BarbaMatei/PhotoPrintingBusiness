using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using PhotoPrint.API.Data;
using PhotoPrint.API.DTOs.Uploads;
using PhotoPrint.API.Exceptions;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services;

namespace PhotoPrint.Tests.Unit.Services;

public class UploadServiceTests
{
    // Minimal JPEG header bytes (passes MimeValidator, no real image data)
    private static readonly byte[] JpegMagic =
    [
        0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01,
    ];

    // The service-under-test uses a DbContext SEPARATE from the arrange/assert context
    // (_db). Both share the same in-memory database NAME (unique per test instance), so
    // contexts see each other's committed data via EF's default store — mirroring
    // production's fresh-scoped-context-per-request, so a persistence bug can no longer hide
    // behind a single shared change-tracker (TEST-3).
    private readonly string _dbName = $"UploadSvc_{Guid.NewGuid():N}";
    private readonly PhotoPrintDbContext _db;
    private readonly Mock<IStorageService> _storageMock;
    private readonly Mock<IImageProcessor> _imageProcessorMock;
    private readonly IUploadService _sut;

    private PhotoPrintDbContext NewContext() =>
        new(new DbContextOptionsBuilder<PhotoPrintDbContext>()
            .UseInMemoryDatabase(_dbName)
            .Options);

    private IUploadService NewSut(PhotoPrintDbContext db) =>
        new UploadService(
            _storageMock.Object,
            new MimeValidator(),
            _imageProcessorMock.Object,
            db,
            Mock.Of<ILogger<UploadService>>());

    public UploadServiceTests()
    {
        _db = NewContext();

        _storageMock = new Mock<IStorageService>();
        // Echo the deterministic path the real storage would produce so tests can assert on it.
        _storageMock
            .Setup(s => s.SaveAsync(
                It.IsAny<Stream>(), It.IsAny<Guid>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>(), It.IsAny<Guid?>(), It.IsAny<string?>()))
            .ReturnsAsync((Stream stream, Guid owner, string ext, CancellationToken ct, Guid? fid, string? prefix) =>
            {
                var id = fid ?? Guid.NewGuid();
                var dir = prefix is null ? owner.ToString("N") : $"{prefix}/{owner:N}";
                return $"{dir}/{id:N}.{ext}";
            });
        _storageMock
            .Setup(s => s.GetStreamAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new MemoryStream(JpegMagic));

        _imageProcessorMock = new Mock<IImageProcessor>();
        _imageProcessorMock
            .Setup(p => p.GetInfoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ImageInfo(800, 600));
        _imageProcessorMock
            .Setup(p => p.GenerateThumbnailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new MemoryStream(JpegMagic));

        _sut = NewSut(NewContext());
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

        await act.Should().ThrowAsync<UnsupportedMediaTypeException>();
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
    public async Task UploadAsync_ImageProcessorReturnsNull_DeletesStorageFileAndThrows()
    {
        _imageProcessorMock
            .Setup(p => p.GetInfoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ImageInfo?)null);

        var act = () => _sut.UploadAsync(
            JpegStream(), "photo.jpg", declaredLength: 100L,
            userId: Guid.NewGuid(), guestSessionId: null);

        await act.Should().ThrowAsync<UnprocessableEntityException>();
        _storageMock.Verify(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
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
        upload.DeletedAt.Should().BeNull();
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

        var dto = await _sut.UploadAsync(
            JpegStream(), "photo.jpg", declaredLength: 100L,
            userId: null, guestSessionId: guestId);

        var upload = await _db.Uploads.SingleAsync();
        upload.GuestSessionId.Should().Be(guestId);
        upload.UserId.Should().BeNull();
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
    public async Task GetPreviewAsync_MatchingUserId_ReturnsJpegThumbnailStream()
    {
        var userId = Guid.NewGuid();
        var upload = SeedUpload(userId: userId);
        await _db.SaveChangesAsync();

        var (stream, contentType) = await _sut.GetPreviewAsync(upload.Id, userId: userId, guestSessionId: null);

        contentType.Should().Be("image/jpeg");
        stream.Should().NotBeNull();
        stream.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetPreviewAsync_MatchingGuestSessionId_ReturnsJpegThumbnailStream()
    {
        var guestId = Guid.NewGuid();
        var upload = SeedUpload(guestSessionId: guestId);
        await _db.SaveChangesAsync();

        var (stream, contentType) = await _sut.GetPreviewAsync(upload.Id, userId: null, guestSessionId: guestId);

        contentType.Should().Be("image/jpeg");
        stream.Length.Should().BeGreaterThan(0);
    }

    // ── GetPreviewAsync — thumbnail caching (bolt 042) ────────────────────────

    [Fact]
    public async Task GetPreviewAsync_SecondCall_StreamsCacheWithoutRegenerating()
    {
        var userId = Guid.NewGuid();
        var upload = SeedUpload(userId: userId);
        await _db.SaveChangesAsync();

        // Once generated, the stored thumbnail exists on subsequent reads.
        _storageMock
            .Setup(s => s.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var (first, _) = await _sut.GetPreviewAsync(upload.Id, userId, guestSessionId: null);
        first.Dispose();
        var (second, _) = await _sut.GetPreviewAsync(upload.Id, userId, guestSessionId: null);
        second.Dispose();

        // Cache hit on the second call — no thumbnail regeneration.
        _imageProcessorMock.Verify(
            p => p.GenerateThumbnailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetPreviewAsync_SecondRequestFreshContext_UsesPersistedThumbnail()
    {
        // TEST-3: prove the thumbnail path is PERSISTED (SaveChanges ran), not merely cached
        // in a shared change-tracker. Each request uses its own context, like a new HTTP
        // request — if the write were missing, the second request would regenerate.
        var userId = Guid.NewGuid();
        var upload = SeedUpload(userId: userId);
        await _db.SaveChangesAsync();

        _storageMock
            .Setup(s => s.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut1 = NewSut(NewContext());
        (await sut1.GetPreviewAsync(upload.Id, userId, guestSessionId: null)).stream.Dispose();

        var sut2 = NewSut(NewContext());
        (await sut2.GetPreviewAsync(upload.Id, userId, guestSessionId: null)).stream.Dispose();

        _imageProcessorMock.Verify(
            p => p.GenerateThumbnailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);

        await using var verifyCtx = NewContext();
        var persisted = await verifyCtx.Uploads.AsNoTracking().FirstAsync(u => u.Id == upload.Id);
        persisted.ThumbnailPath.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPreviewAsync_CacheMiss_SavesThumbnailUnderDeterministicNamespacedKey()
    {
        // BUG-3/REQ-2: the thumbnail is stored deterministically from the upload id in the
        // "thumbs" namespace — it can't collide with the original ({owner}/{id}.jpg), and a
        // racing/cancelled write overwrites the same key instead of orphaning a random file.
        var userId = Guid.NewGuid();
        var upload = SeedUpload(userId: userId);
        await _db.SaveChangesAsync();

        (await _sut.GetPreviewAsync(upload.Id, userId, guestSessionId: null)).stream.Dispose();

        _storageMock.Verify(s => s.SaveAsync(
            It.IsAny<Stream>(), userId, "jpg", It.IsAny<CancellationToken>(),
            (Guid?)upload.Id, "thumbs"), Times.Once);

        await using var verifyCtx = NewContext();
        var persisted = await verifyCtx.Uploads.AsNoTracking().FirstAsync(u => u.Id == upload.Id);
        persisted.ThumbnailPath.Should().Be($"thumbs/{userId:N}/{upload.Id:N}.jpg");
    }

    [Fact]
    public async Task GetPreviewAsync_CachedFileMissing_RegeneratesThumbnail()
    {
        var userId = Guid.NewGuid();
        var upload = SeedUpload(userId: userId);
        upload.ThumbnailPath = "owner/missing-thumb.jpg";   // recorded, but file is gone
        await _db.SaveChangesAsync();

        _storageMock
            .Setup(s => s.ExistsAsync("owner/missing-thumb.jpg", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var (stream, _) = await _sut.GetPreviewAsync(upload.Id, userId, guestSessionId: null);
        stream.Dispose();

        _imageProcessorMock.Verify(
            p => p.GenerateThumbnailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetPreviewAsync_CacheMissWithMissingOriginal_ThrowsNotFoundNot500()
    {
        // M6 (review 042-v4): the original blob was deleted (ops-side or the cleanup race) though
        // the row/DeletedAt survives. The cache-miss decode reads the original via GetStreamAsync,
        // which throws FileNotFoundException — outside the ImageFormatException catch and unmapped,
        // so it surfaced as a 500. It must surface a clean 4xx instead.
        var userId = Guid.NewGuid();
        var upload = SeedUpload(userId: userId);
        await _db.SaveChangesAsync();

        _imageProcessorMock
            .Setup(p => p.GenerateThumbnailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FileNotFoundException("Stored upload not found."));

        var act = () => _sut.GetPreviewAsync(upload.Id, userId, guestSessionId: null);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetPreviewAsync_RowSoftDeletedDuringWrite_DeletesOrphanedThumbnail()
    {
        // M1 (review 042-v4): the cleanup job can soft-delete the upload between the preview's
        // live read (DeletedAt null) and its ThumbnailPath write (which keys only on Id, no
        // DeletedAt guard). A thumbnail written onto the now-dead row is never revisited by
        // cleanup -> permanent orphan. The write must detect the row is no longer live and
        // delete the just-written thumbnail.
        var userId = Guid.NewGuid();
        var upload = SeedUpload(userId: userId);
        await _db.SaveChangesAsync();

        var thumbKey = $"thumbs/{userId:N}/{upload.Id:N}.jpg";

        // GenerateThumbnailAsync runs AFTER the live read and BEFORE the persist, so soft-deleting
        // here (via a separate context, like the cleanup job) reproduces the race deterministically.
        _imageProcessorMock
            .Setup(p => p.GenerateThumbnailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                using var ctx = NewContext();
                var row = ctx.Uploads.First(u => u.Id == upload.Id);
                row.DeletedAt = DateTimeOffset.UtcNow;
                ctx.SaveChanges();
                return new MemoryStream(JpegMagic);
            });

        (await _sut.GetPreviewAsync(upload.Id, userId, guestSessionId: null)).stream.Dispose();

        _storageMock.Verify(
            s => s.DeleteAsync(thumbKey, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadAsync_ImageDimensionsExceedLimit_ThrowsUnprocessableEntityException()
    {
        _imageProcessorMock
            .Setup(p => p.GetInfoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ImageInfo(30_000, 30_000)); // pixel bomb

        var act = () => _sut.UploadAsync(
            JpegStream(), "huge.jpg", declaredLength: 100L,
            userId: Guid.NewGuid(), guestSessionId: null);

        await act.Should().ThrowAsync<UnprocessableEntityException>()
            .WithMessage("*dimensions exceed*");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Upload SeedUpload(Guid? userId = null, Guid? guestSessionId = null, DateTimeOffset? deletedAt = null)
    {
        var upload = new Upload
        {
            UserId           = userId,
            GuestSessionId   = guestSessionId,
            FilePath         = "owner/file.jpg",
            OriginalFileName = "file.jpg",
            ContentType      = "image/jpeg",
            WidthPx          = 800,
            HeightPx         = 600,
            FileSizeBytes    = 1024,
            DeletedAt        = deletedAt,
        };
        _db.Uploads.Add(upload);
        return upload;
    }
}
