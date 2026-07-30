using Amazon.S3;
using Amazon.S3.Util;
using Microsoft.Extensions.Options;
using PhotoPrint.API.Configuration;

namespace PhotoPrint.API.Services;

/// <summary>
/// Fail-fast probe: on startup, verifies the configured S3 bucket exists and is
/// reachable with the configured credentials. Throwing from <see cref="StartAsync"/> aborts
/// the host so a misconfigured bucket is caught at boot, never on the first upload.
/// </summary>
/// <remarks>
/// Registered only when <c>Storage:Provider == "S3"</c>; never runs in dev / Provider=Local.
/// </remarks>
public class S3BucketVerifier : IHostedService
{
    private readonly IAmazonS3 _s3;
    private readonly StorageSettings _settings;
    private readonly ILogger<S3BucketVerifier> _logger;

    public S3BucketVerifier(
        IAmazonS3 s3,
        IOptions<StorageSettings> settings,
        ILogger<S3BucketVerifier> logger)
    {
        _s3 = s3;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var bucket = _settings.Bucket
            ?? throw new InvalidOperationException(
                "Storage:Bucket is required when Storage:Provider=S3.");

        try
        {
            var exists = await AmazonS3Util.DoesS3BucketExistV2Async(_s3, bucket);
            if (!exists)
            {
                throw new InvalidOperationException(
                    $"S3 bucket '{bucket}' does not exist or is not visible to the configured credentials. " +
                    $"Endpoint={_settings.EndpointUrl ?? "<aws-default>"}, Region={_settings.Region}. " +
                    "Create the bucket out-of-band (one-shot ops task) before starting the API.");
            }

            _logger.LogInformation(
                "S3 bucket '{Bucket}' verified at boot. Endpoint={Endpoint}, Region={Region}, ForcePathStyle={ForcePathStyle}",
                bucket,
                _settings.EndpointUrl ?? "<aws-default>",
                _settings.Region,
                _settings.ForcePathStyle);
        }
        catch (AmazonS3Exception ex)
        {
            throw new InvalidOperationException(
                $"S3 bucket '{bucket}' could not be verified at boot: " +
                $"{ex.StatusCode} {ex.ErrorCode} — {ex.Message}. " +
                "Check Storage:Bucket, AccessKey/SecretKey, EndpointUrl, and Region.", ex);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
