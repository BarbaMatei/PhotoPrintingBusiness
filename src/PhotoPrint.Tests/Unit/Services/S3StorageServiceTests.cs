using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Services;
using Xunit;

namespace PhotoPrint.Tests.Unit.Services;

/// <summary>
/// Fast unit tests for <see cref="S3StorageService"/> with a mocked <see cref="IAmazonS3"/>.
/// The real S3-protocol round-trip is covered by the MinIO integration tests; this file pins
/// the exception-translation contract (F3, review 043-v1) without needing a running server, so
/// a regression reddens on every developer machine, not only in CI.
/// </summary>
public class S3StorageServiceTests
{
    private static S3StorageService BuildSut(IAmazonS3 s3) =>
        new(s3, Options.Create(new StorageSettings
        {
            Provider  = "S3",
            Bucket    = "test-bucket",
            Region    = "us-east-1",
            AccessKey = "k",
            SecretKey = "s",
        }), NullLogger<S3StorageService>.Instance);

    [Fact]
    public async Task GetStreamAsync_MissingObject_TranslatesS3NotFoundToFileNotFound()
    {
        // A missing cloud object throws AmazonS3Exception(NotFound). Callers such as
        // UploadService.GetPreviewAsync catch FileNotFoundException to return a clean 404;
        // without translation the AmazonS3Exception escaped as a 500 (F3, review 043-v1).
        var s3 = new Mock<IAmazonS3>();
        s3.Setup(x => x.GetObjectAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
          .ThrowsAsync(new AmazonS3Exception("The specified key does not exist.")
          {
              StatusCode = HttpStatusCode.NotFound,
              ErrorCode = "NoSuchKey",
          });

        var sut = BuildSut(s3.Object);

        var act = () => sut.GetStreamAsync("uploads/2026/05/missing.jpg");

        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task GetStreamAsync_NonNotFoundS3Error_IsNotTranslated()
    {
        // Only 404 maps to FileNotFoundException; other S3 errors (e.g. 403) must surface as
        // themselves so they aren't misread as "object absent".
        var s3 = new Mock<IAmazonS3>();
        s3.Setup(x => x.GetObjectAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
          .ThrowsAsync(new AmazonS3Exception("Access denied")
          {
              StatusCode = HttpStatusCode.Forbidden,
              ErrorCode = "AccessDenied",
          });

        var sut = BuildSut(s3.Object);

        var act = () => sut.GetStreamAsync("uploads/2026/05/forbidden.jpg");

        await act.Should().ThrowAsync<AmazonS3Exception>();
    }

    [Fact]
    public async Task GetStreamAsync_ObjectPresent_ReturnsResponseStream()
    {
        var payload = new byte[] { 1, 2, 3, 4 };
        var s3 = new Mock<IAmazonS3>();
        s3.Setup(x => x.GetObjectAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
          .ReturnsAsync(new GetObjectResponse { ResponseStream = new MemoryStream(payload) });

        var sut = BuildSut(s3.Object);

        await using var stream = await sut.GetStreamAsync("uploads/2026/05/present.jpg");
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        ms.ToArray().Should().Equal(payload);
    }
}
