using Microsoft.Extensions.Diagnostics.HealthChecks;
using PhotoPrint.API.Data;

namespace PhotoPrint.API.HealthChecks;

public class DbHealthCheck : IHealthCheck
{
    private readonly PhotoPrintDbContext _dbContext;

    public DbHealthCheck(PhotoPrintDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            var canConnect = await _dbContext.Database.CanConnectAsync(cts.Token);

            return canConnect
                ? HealthCheckResult.Healthy("OK")
                : HealthCheckResult.Unhealthy("Cannot connect to database");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Error", ex);
        }
    }
}
