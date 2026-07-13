using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using PhotoPrint.API.Controllers;
using PhotoPrint.API.DTOs.Uploads;
using PhotoPrint.API.Exceptions;
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

        var controller = new UploadsController(uploadService.Object, logger.Object)
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
}
