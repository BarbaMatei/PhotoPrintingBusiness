using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Controllers;
using PhotoPrint.API.DTOs.Uploads;
using PhotoPrint.API.Exceptions;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services;
using Xunit;

namespace PhotoPrint.Tests.Unit.Controllers;

public class UploadsControllerTests
{
    private static IFormFile MakeFormFile(string name, byte[] bytes)
        => new FormFile(new MemoryStream(bytes), 0, bytes.Length, "files", name)
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/octet-stream",
        };

    [Fact]
    public async Task UploadPhotoBatchAsync_RejectedItem_LogsWarningAndStillReturns200()
    {
        // OBS-1 (review 042-v1): a batch item rejection is swallowed into a per-item result
        // (200 overall), so it never reaches ExceptionHandlerMiddleware. The controller must
        // log the rejection itself, otherwise bulk abuse is invisible to ops.
        var uploadService = new Mock<IUploadService>();
        uploadService
            .Setup(s => s.UploadAsync(
                It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<long>(),
                It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnsupportedMediaTypeException("Only images are accepted."));
        var logger = new Mock<ILogger<UploadsController>>();

        var controller = new UploadsController(
            uploadService.Object,
            Mock.Of<IStorageRouter>(),
            Options.Create(new StorageSettings()),
            logger.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };

        var files = new List<IFormFile> { MakeFormFile("bad.pdf", new byte[] { 0x25, 0x50, 0x44, 0x46 }) };

        var result = await controller.UploadPhotoBatchAsync(files, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Which;
        var items = ok.Value.Should().BeAssignableTo<IReadOnlyList<BatchUploadItemResult>>().Which;
        items.Should().ContainSingle().Which.Error.Should().NotBeNull();

        logger.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("uploads.batch.item_rejected")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task UploadPhotoBatchAsync_RejectedItem_SanitizesAndTruncatesFilenameInLog()
    {
        // L6 (review 042-v4): the batch-reject log emitted the raw client filename verbatim and
        // unbounded — a newline forges a fake log line in plain-text sinks, and length is
        // uncapped (volume amplification). Strip control chars and cap length before logging.
        var uploadService = new Mock<IUploadService>();
        uploadService
            .Setup(s => s.UploadAsync(
                It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<long>(),
                It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnsupportedMediaTypeException("nope"));
        var logger = new Mock<ILogger<UploadsController>>();

        var controller = new UploadsController(
            uploadService.Object,
            Mock.Of<IStorageRouter>(),
            Options.Create(new StorageSettings()),
            logger.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };

        var evilName = "line1\nFAKE uploads.done line2_" + new string('z', 200);
        var files = new List<IFormFile> { MakeFormFile(evilName, new byte[] { 0x25, 0x50 }) };

        await controller.UploadPhotoBatchAsync(files, CancellationToken.None);

        logger.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) =>
                    v.ToString()!.Contains("uploads.batch.item_rejected") &&
                    !v.ToString()!.Contains('\n') &&
                    !v.ToString()!.Contains(new string('z', 150))),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task UploadPhotoBatchAsync_DecompressionBomb_EmitsReservedBombEventWithDimensions()
    {
        // M4 (review 042-v4): DecompressionBombException subclasses UnprocessableEntityException,
        // so the batch catch logged only the generic item_rejected event and it never reached the
        // middleware that emits uploads.decompression_bomb.rejected (with dimensions). Ops alerts
        // keyed on that event missed bombs sent via /batch — the code's own "most likely bomb
        // vector". The controller must emit the reserved event (with dimensions) here too.
        var uploadService = new Mock<IUploadService>();
        uploadService
            .Setup(s => s.UploadAsync(
                It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<long>(),
                It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DecompressionBombException(30_000, 30_000, "Image dimensions exceed limits."));
        var logger = new Mock<ILogger<UploadsController>>();

        var controller = new UploadsController(
            uploadService.Object,
            Mock.Of<IStorageRouter>(),
            Options.Create(new StorageSettings()),
            logger.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };

        var files = new List<IFormFile> { MakeFormFile("bomb.png", new byte[] { 0x89, 0x50 }) };

        await controller.UploadPhotoBatchAsync(files, CancellationToken.None);

        logger.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) =>
                    v.ToString()!.Contains("uploads.decompression_bomb.rejected") &&
                    v.ToString()!.Contains("30000")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // ── GetPreviewAsync TOCTOU (F8, review 043-v1) ────────────────────────────

    private static UploadsController BuildPreviewController(
        IUploadService uploadService, IStorageRouter router)
        => new(uploadService, router, Options.Create(new StorageSettings()),
            Mock.Of<ILogger<UploadsController>>())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };

    [Fact]
    public async Task GetPreviewAsync_LocalThumbDeletedMidRequest_ReResolvesToCloud302()
    {
        // GetPreviewAsync resolves Local, then a concurrent promotion best-effort-deletes the
        // local thumb before the controller opens it. The controller must re-resolve (now the
        // upload is Cloud → 302 presigned) rather than surface an unmapped 500 (F8).
        var uploadId = Guid.NewGuid();
        var uploadService = new Mock<IUploadService>();
        uploadService
            .SetupSequence(s => s.GetPreviewAsync(
                uploadId, It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PreviewLocation(uploadId, StorageLocation.Local, "thumbs/local.jpg"))
            .ReturnsAsync(new PreviewLocation(uploadId, StorageLocation.Cloud, "thumbs/cloud.jpg"));

        var local = new Mock<IStorageService>();
        local.Setup(s => s.GetStreamAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .ThrowsAsync(new FileNotFoundException("local thumb gone"));
        var cloud = new Mock<IStorageService>();
        cloud.Setup(s => s.GetPresignedUrlAsync(
                It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync("https://cdn.test/thumbs/cloud.jpg?sig=x");
        var router = new Mock<IStorageRouter>();
        router.SetupGet(r => r.Local).Returns(local.Object);
        router.SetupGet(r => r.Cloud).Returns(cloud.Object);

        var controller = BuildPreviewController(uploadService.Object, router.Object);

        var result = await controller.GetPreviewAsync(uploadId, CancellationToken.None);

        result.Should().BeOfType<RedirectResult>()
              .Which.Url.Should().Contain("cloud.jpg");
        uploadService.Verify(s => s.GetPreviewAsync(
            uploadId, It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task GetPreviewAsync_LocalThumbGoneOnBothResolves_Returns404()
    {
        // Double race (local thumb still gone on the re-resolve, upload still Local): degrade to
        // a clean 404 instead of a 500. Bounded — only one re-resolve.
        var uploadId = Guid.NewGuid();
        var uploadService = new Mock<IUploadService>();
        uploadService
            .Setup(s => s.GetPreviewAsync(
                uploadId, It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PreviewLocation(uploadId, StorageLocation.Local, "thumbs/local.jpg"));

        var local = new Mock<IStorageService>();
        local.Setup(s => s.GetStreamAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .ThrowsAsync(new FileNotFoundException("local thumb gone"));
        var router = new Mock<IStorageRouter>();
        router.SetupGet(r => r.Local).Returns(local.Object);

        var controller = BuildPreviewController(uploadService.Object, router.Object);

        var result = await controller.GetPreviewAsync(uploadId, CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }
}
