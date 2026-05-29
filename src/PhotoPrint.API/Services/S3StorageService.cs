using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Options;
using PhotoPrint.API.Configuration;
using Polly;
using Polly.Retry;

namespace PhotoPrint.API.Services;

/// <summary>
/// S3-compatible storage adapter (ADR-008). One implementation serves AWS S3, Cloudflare R2,
/// or MinIO — vendor differences are pure config. Recommended production target: R2 (ADR-009).
/// Wrapped in a Polly resilience pipeline that retries transient S3 errors.
/// </summary>
public class S3StorageService : IStorageService
{
    private readonly IAmazonS3 _s3;
    private readonly string _bucket;
    private readonly Protocol _presignProtocol;
    private readonly ResiliencePipeline _pipeline;
    private readonly ILogger<S3StorageService> _logger;

    public S3StorageService(
        IAmazonS3 s3,
        IOptions<StorageSettings> settings,
        ILogger<S3StorageService> logger)
    {
        _s3 = s3;
        var s = settings.Value;
        _bucket = s.Bucket
            ?? throw new InvalidOperationException(
                "Storage:Bucket is required when Storage:Provider=S3.");
        _logger = logger;

        // The SDK defaults presigned URLs to HTTPS regardless of the endpoint scheme.
        // R2 and AWS S3 both want HTTPS, but a local MinIO endpoint (CI / dev) is plain
        // HTTP — and a mismatched scheme produces an unfetchable URL. Honour the
        // EndpointUrl's scheme when one is configured; default HTTPS otherwise.
        _presignProtocol =
            !string.IsNullOrEmpty(s.EndpointUrl)
            && s.EndpointUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                ? Protocol.HTTP
                : Protocol.HTTPS;

        _pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<AmazonS3Exception>(IsTransient),
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                Delay = TimeSpan.FromMilliseconds(200),
            })
            .Build();
    }

    public bool SupportsPresignedUrls => true;

    public async Task SaveAsync(Stream content, string key, CancellationToken ct = default)
    {
        StorageKeys.Validate(key);
        if (content.CanSeek)
            content.Position = 0;

        await _pipeline.ExecuteAsync(async cancel =>
        {
            // TransferUtility handles multipart upload for large objects without
            // buffering the whole stream in memory.
            using var transfer = new TransferUtility(_s3);
            await transfer.UploadAsync(content, _bucket, key, cancel);
        }, ct);

        _logger.LogDebug("S3 saved {Bucket}/{Key}", _bucket, key);
    }

    public async Task DeleteAsync(string key, CancellationToken ct = default)
    {
        StorageKeys.Validate(key);
        await _pipeline.ExecuteAsync(async cancel =>
        {
            await _s3.DeleteObjectAsync(new DeleteObjectRequest
            {
                BucketName = _bucket,
                Key = key,
            }, cancel);
        }, ct);
        _logger.LogDebug("S3 deleted {Bucket}/{Key}", _bucket, key);
    }

    public async Task<Stream> GetStreamAsync(string key, CancellationToken ct = default)
    {
        StorageKeys.Validate(key);
        var response = await _pipeline.ExecuteAsync(async cancel =>
            await _s3.GetObjectAsync(_bucket, key, cancel), ct);
        // ResponseStream is owned by the caller (returned, not used here).
        return response.ResponseStream;
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        StorageKeys.Validate(key);
        try
        {
            await _pipeline.ExecuteAsync(async cancel =>
            {
                await _s3.GetObjectMetadataAsync(_bucket, key, cancel);
            }, ct);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public Task<string> GetPresignedUrlAsync(string key, TimeSpan ttl, CancellationToken ct = default)
    {
        StorageKeys.Validate(key);
        // GetPreSignedURL is synchronous in the SDK; wrap in Task.FromResult for the
        // interface contract. No network call — signing is local with the credentials.
        var url = _s3.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = _bucket,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(ttl),
            Protocol = _presignProtocol,
        });
        return Task.FromResult(url);
    }

    private static bool IsTransient(AmazonS3Exception ex)
    {
        // 5xx, throttling, or explicit slow-down — the canonical transient set for S3.
        if ((int)ex.StatusCode >= 500) return true;
        return ex.ErrorCode is "SlowDown" or "RequestTimeout" or "Throttling"
            or "ThrottlingException" or "ProvisionedThroughputExceededException";
    }
}
