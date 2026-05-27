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

    private readonly PhotoPrintDbContext _db;
    private readonly Mock<IStorageService> _storageMock;
    private readonly Mock<IImageProcessor> _imageProcessorMock;
    private readonly IUploadService _sut;

    public UploadServiceTests()
    {
        var options = new DbContextOptionsBuilder<PhotoPrintDbContext>()
            .UseInMemoryDatabase($"UploadSvc_{Guid.NewGuid():N}")
            .Options;
        _db = new PhotoPrintDbContext(options);

        _storageMock = new Mock<IStorageService>();
        _storageMock
            .Setup(s => s.SaveAsync(
                It.IsAny<Stream>(), It.IsAny<Guid>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>(), It.IsAny<Guid?>()))
            .ReturnsAsync("owner/abc.jpg");

        _imageProcessorMock = new Mock<IImageProcessor>();
        _imageProcessorMock
            .Setup(p => p.GetInfoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ImageInfo(800, 600));
        _imageProcessorMock
            .Setup(p => p.GenerateThumbnailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new MemoryStream(JpegMagic));

        _sut = new UploadService(
            _storageMock.Object,
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
