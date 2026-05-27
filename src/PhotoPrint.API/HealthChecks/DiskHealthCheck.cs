using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using PhotoPrint.API.Configuration;

namespace PhotoPrint.API.HealthChecks;

public class DiskHealthCheck : IHealthCheck
{
    private readonly HealthCheckSettings _settings;

    public DiskHealthCheck(IOptions<HealthCheckSettings> settings)
    {
        _settings = settings.Value;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var path = Path.IsPathRooted(_settings.UploadsPath)
                ? _settings.UploadsPath
                : Path.Combine(AppContext.BaseDirectory, _settings.UploadsPath);

            var root = Path.GetPathRoot(path) ?? path;
            var drive = new DriveInfo(root);
            var freeGb = Math.Round(drive.AvailableFreeSpace / 1_073_741_824.0, 1);

            return Task.FromResult(HealthCheckResult.Healthy(
                "OK",
                new Dictionary<string, object> { ["freeGb"] = freeGb }));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Error", ex));
        }
    }
}
