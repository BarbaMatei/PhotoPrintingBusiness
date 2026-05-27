using Microsoft.EntityFrameworkCore;
using PhotoPrint.API.Data;

namespace PhotoPrint.API.BackgroundJobs;

/// <summary>
/// Runs once per day. Hard-deletes user accounts where the deletion was requested
/// more than 30 days ago. Related data (SavedAddresses, RefreshTokens, ExternalLogins,
/// CartItems) is cascade-deleted by the database. Orders are preserved with UserId = null.
/// </summary>
public class AccountDeletionJob : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromDays(1);
    private static readonly TimeSpan RetentionPeriod = TimeSpan.FromDays(30);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AccountDeletionJob> _logger;

    public AccountDeletionJob(
        IServiceScopeFactory scopeFactory,
        ILogger<AccountDeletionJob> logger)
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
                var deleted = await DeleteExpiredAccountsAsync(stoppingToken);
                _logger.LogInformation(
                    "Account deletion job: {Count} account(s) permanently deleted", deleted);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during account deletion job");
            }
        }
    }

    internal async Task<int> DeleteExpiredAccountsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PhotoPrintDbContext>();

        var cutoff = DateTimeOffset.UtcNow.Subtract(RetentionPeriod);

        var toDelete = await db.Users
            .Where(u => u.DeletionRequestedAt != null && u.DeletionRequestedAt < cutoff)
            .ToListAsync(ct);

        if (toDelete.Count == 0) return 0;

        db.Users.RemoveRange(toDelete);
        await db.SaveChangesAsync(ct);
        return toDelete.Count;
    }
}
