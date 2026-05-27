using Microsoft.EntityFrameworkCore;
using PhotoPrint.API.Data;

namespace PhotoPrint.API.BackgroundJobs;

public class GuestSessionCleanupJob : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<GuestSessionCleanupJob> _logger;

    public GuestSessionCleanupJob(
        IServiceScopeFactory scopeFactory,
        ILogger<GuestSessionCleanupJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                var deleted = await CleanupAsync(stoppingToken);
                _logger.LogInformation(
                    "Guest session cleanup: {Count} sessions deleted", deleted);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during guest session cleanup");
            }
        }
    }

    private async Task<int> CleanupAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PhotoPrintDbContext>();

        var now = DateTimeOffset.UtcNow;

        // Delete expired unclaimed sessions
        var expired = await db.GuestSessions
            .Where(gs => gs.ExpiresAt < now && gs.ClaimedByUserId == null)
            .ToListAsync(ct);

        if (expired.Count == 0) return 0;

        db.GuestSessions.RemoveRange(expired);
        await db.SaveChangesAsync(ct);
        return expired.Count;
    }
}
