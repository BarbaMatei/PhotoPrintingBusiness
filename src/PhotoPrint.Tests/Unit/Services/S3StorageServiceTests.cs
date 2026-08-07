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
/// the exception-translation contract without needing a running server, so
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
        // without translation the AmazonS3Exception escaped as a 500.
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
    public async Task SaveAsync_TransientFailureThenRetry_ReuploadsFullContent()
    {
        // The stream rewind sat OUTSIDE the Polly retry loop, so a retry
        // after a transient 5xx re-uploaded from EOF — a truncated/empty object that "succeeds",
        // after which promotion deletes the local original (silent data loss). Every attempt
        // must re-send the full payload.
        var payload = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        var bytesPerAttempt = new List<long>();

        var s3 = new Mock<IAmazonS3>();
        s3.SetupGet(x => x.Config).Returns(new AmazonS3Config());
        s3.Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
          .Returns(async (PutObjectRequest req, CancellationToken ct) =>
          {
              // Consume the body exactly like the SDK does, and record how much was available.
              using var sink = new MemoryStream();
              await req.InputStream.CopyToAsync(sink, ct);
              bytesPerAttempt.Add(sink.Length);

              if (bytesPerAttempt.Count == 1)
                  throw new AmazonS3Exception("Internal Error")
                  {
                      StatusCode = HttpStatusCode.InternalServerError,
                      ErrorCode = "InternalError",
                  };
              return new PutObjectResponse();
          });

        var sut = BuildSut(s3.Object);
        using var content = new MemoryStream(payload);

        await sut.SaveAsync(content, "uploads/2026/05/retry.jpg");

        bytesPerAttempt.Should().HaveCount(2);
        bytesPerAttempt[1].Should().Be(payload.Length,
            "the retried attempt must re-send the FULL payload, not the leftovers of a consumed stream");
    }

    [Fact]
    public async Task SaveAsync_NonSeekableStream_FailsLoudlyOnRetryInsteadOfUploadingTruncated()
    {
        // Companion: a non-seekable stream cannot be rewound for a retry. That must surface
        // as an error, never as a silent truncated re-upload.
        // A non-seekable stream routes through the SDK's multipart path; failing its first call
        // simulates a transient error after the stream was (partially) consumed.
        var calls = 0;
        var s3 = new Mock<IAmazonS3>();
        s3.SetupGet(x => x.Config).Returns(new AmazonS3Config());
        s3.Setup(x => x.InitiateMultipartUploadAsync(
                It.IsAny<InitiateMultipartUploadRequest>(), It.IsAny<CancellationToken>()))
          .Returns((InitiateMultipartUploadRequest _, CancellationToken _) =>
          {
              calls++;
              throw new AmazonS3Exception("Internal Error")
              {
                  StatusCode = HttpStatusCode.InternalServerError,
                  ErrorCode = "InternalError",
              };
          });

        var sut = BuildSut(s3.Object);
        using var inner = new MemoryStream(new byte[] { 1, 2, 3 });
        using var nonSeekable = new NonSeekableStream(inner);

        var act = () => sut.SaveAsync(nonSeekable, "uploads/2026/05/nonseekable.jpg");

        await act.Should().ThrowAsync<NotSupportedException>();
        calls.Should().Be(1, "a consumed non-seekable stream must not be re-sent on retry");
    }

    private sealed class NonSeekableStream(Stream inner) : Stream
    {
        public override bool CanRead => inner.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => inner.Position;
            set => throw new NotSupportedException();
        }
        public override void Flush() => inner.Flush();
        public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
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
