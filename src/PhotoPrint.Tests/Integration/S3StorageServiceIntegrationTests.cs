using System.Net;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Services;
using Xunit;

namespace PhotoPrint.Tests.Integration;

/// <summary>
/// Real S3-protocol integration tests for <see cref="S3StorageService"/>, exercised against
/// a MinIO service container in CI (story 001 / bolt 043 / ADR-008). Skipped locally when the
/// <c>STORAGE_TEST_ENDPOINT</c> env var is unset — see <c>.github/workflows/ci.yml</c>.
/// </summary>
public sealed class S3StorageServiceIntegrationTests : IClassFixture<MinioFixture>, IAsyncLifetime
{
    private readonly MinioFixture _fx;
    private readonly List<string> _writtenKeys = new();

    public S3StorageServiceIntegrationTests(MinioFixture fx) => _fx = fx;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        // Best-effort per-test cleanup.
        if (!_fx.Available) return;
        foreach (var key in _writtenKeys)
        {
            try { await _fx.Sut.DeleteAsync(key); } catch { /* ignore */ }
        }
    }

    private string TrackKey(string key)
    {
        _writtenKeys.Add(key);
        return key;
    }

    // ── Capability ────────────────────────────────────────────────────────────

    [SkippableFact]
    public void SupportsPresignedUrls_IsTrue()
    {
        Skip.IfNot(_fx.Available, MinioFixture.SkipReason);

        _fx.Sut.SupportsPresignedUrls.Should().BeTrue();
    }

    // ── Save / GetStream — round-trip ─────────────────────────────────────────

    [SkippableFact]
    public async Task SaveAsync_ThenGetStream_RoundTripsBytes()
    {
        Skip.IfNot(_fx.Available, MinioFixture.SkipReason);

        var key = TrackKey($"uploads/2026/05/{Guid.NewGuid():N}.bin");
        var payload = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0xCA, 0xFE };

        await _fx.Sut.SaveAsync(new MemoryStream(payload), key);

        await using var stream = await _fx.Sut.GetStreamAsync(key);
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        ms.ToArray().Should().Equal(payload);
    }

    // ── Exists ────────────────────────────────────────────────────────────────

    [SkippableFact]
    public async Task ExistsAsync_AfterSave_ReturnsTrue()
    {
        Skip.IfNot(_fx.Available, MinioFixture.SkipReason);

        var key = TrackKey($"uploads/2026/05/{Guid.NewGuid():N}.bin");
        await _fx.Sut.SaveAsync(new MemoryStream(new byte[] { 1, 2, 3 }), key);

        (await _fx.Sut.ExistsAsync(key)).Should().BeTrue();
    }

    [SkippableFact]
    public async Task ExistsAsync_MissingKey_ReturnsFalse()
    {
        Skip.IfNot(_fx.Available, MinioFixture.SkipReason);

        (await _fx.Sut.ExistsAsync($"thumbs/{Guid.NewGuid():N}.jpg")).Should().BeFalse();
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [SkippableFact]
    public async Task DeleteAsync_RemovesObject()
    {
        Skip.IfNot(_fx.Available, MinioFixture.SkipReason);

        var key = $"thumbs/{Guid.NewGuid():N}.jpg";
        await _fx.Sut.SaveAsync(new MemoryStream(new byte[] { 1 }), key);

        await _fx.Sut.DeleteAsync(key);

        (await _fx.Sut.ExistsAsync(key)).Should().BeFalse();
    }

    // ── Presign — URL shape + actual fetch ────────────────────────────────────

    [SkippableFact]
    public async Task GetPresignedUrlAsync_ReturnsSignedUrlPointingAtConfiguredEndpoint()
    {
        Skip.IfNot(_fx.Available, MinioFixture.SkipReason);

        var key = TrackKey($"thumbs/{Guid.NewGuid():N}.jpg");
        await _fx.Sut.SaveAsync(new MemoryStream(new byte[] { 0xFF, 0xD8 }), key);

        var url = await _fx.Sut.GetPresignedUrlAsync(key, TimeSpan.FromMinutes(5));

        url.Should().StartWith(_fx.Endpoint);
        url.Should().Contain(key);
        url.Should().Contain("X-Amz-Signature");
    }

    [SkippableFact]
    public async Task GetPresignedUrlAsync_UrlFetchesObjectBytes()
    {
        Skip.IfNot(_fx.Available, MinioFixture.SkipReason);

        var key = TrackKey($"uploads/2026/05/{Guid.NewGuid():N}.jpg");
        var payload = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10 };
        await _fx.Sut.SaveAsync(new MemoryStream(payload), key);

        var url = await _fx.Sut.GetPresignedUrlAsync(key, TimeSpan.FromMinutes(5));

        using var http = new HttpClient();
        var response = await http.GetAsync(url);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsByteArrayAsync()).Should().Equal(payload);
    }
}

/// <summary>
/// One-time MinIO setup: build an <see cref="IAmazonS3"/> against the configured endpoint,
/// create the test bucket if absent, and expose a real <see cref="S3StorageService"/>.
/// <see cref="Available"/> is false when the env vars are unset (local runs without Docker),
/// causing every <c>[SkippableFact]</c> to skip gracefully.
/// </summary>
public sealed class MinioFixture : IAsyncLifetime
{
    public const string SkipReason =
        "MinIO endpoint not configured (set STORAGE_TEST_ENDPOINT to run). " +
        "These tests run in CI via the MinIO service container.";

    private readonly string? _endpoint = Environment.GetEnvironmentVariable("STORAGE_TEST_ENDPOINT");
    private readonly string? _accessKey = Environment.GetEnvironmentVariable("STORAGE_TEST_ACCESS_KEY");
    private readonly string? _secretKey = Environment.GetEnvironmentVariable("STORAGE_TEST_SECRET_KEY");
    private readonly string? _bucket = Environment.GetEnvironmentVariable("STORAGE_TEST_BUCKET");

    private IAmazonS3? _s3;

    public string Endpoint => _endpoint ?? string.Empty;
    public bool Available { get; private set; }
    public S3StorageService Sut { get; private set; } = default!;

    public async Task InitializeAsync()
    {
        Available =
            !string.IsNullOrEmpty(_endpoint) &&
            !string.IsNullOrEmpty(_accessKey) &&
            !string.IsNullOrEmpty(_secretKey) &&
            !string.IsNullOrEmpty(_bucket);

        if (!Available) return;

        var cfg = new AmazonS3Config
        {
            ServiceURL = _endpoint!,
            ForcePathStyle = true,        // MinIO requires path-style addressing
            AuthenticationRegion = "us-east-1",
        };
        _s3 = new AmazonS3Client(new BasicAWSCredentials(_accessKey, _secretKey), cfg);

        // Ensure the bucket exists (one-shot — production-style verification lives in
        // S3BucketVerifier which expects an out-of-band create).
        try
        {
            await _s3.PutBucketAsync(new PutBucketRequest { BucketName = _bucket });
        }
        catch (AmazonS3Exception ex) when (ex.ErrorCode is "BucketAlreadyOwnedByYou" or "BucketAlreadyExists")
        {
            // Already there — fine.
        }

        var settings = Options.Create(new StorageSettings
        {
            Provider = "S3",
            Bucket = _bucket,
            Region = "us-east-1",
            EndpointUrl = _endpoint,
            ForcePathStyle = true,
            AccessKey = _accessKey,
            SecretKey = _secretKey,
        });
        Sut = new S3StorageService(_s3, settings, Mock.Of<ILogger<S3StorageService>>());
    }

    public Task DisposeAsync()
    {
        _s3?.Dispose();
        return Task.CompletedTask;
    }
}
