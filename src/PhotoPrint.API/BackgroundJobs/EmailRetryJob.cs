using Microsoft.EntityFrameworkCore;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services;

namespace PhotoPrint.API.BackgroundJobs;

public class EmailRetryJob : BackgroundService
{
    private static readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(10);
    private static readonly int _batchSize = 10;
    private static readonly int _maxAttempts = 3;

    // Index 0 used by ReliableEmailService for initial queue (1s).
    // Indices 1, 2 used here after first and second retry failures (4s, 16s).
    private static readonly TimeSpan[] _backoffDelays =
    [
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(4),
        TimeSpan.FromSeconds(16),
    ];

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmailRetryJob> _logger;

    public EmailRetryJob(IServiceScopeFactory scopeFactory, ILogger<EmailRetryJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EmailRetryJob started");

        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessPendingEmailsAsync(stoppingToken);

            try
            {
                await Task.Delay(_pollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("EmailRetryJob stopped");
    }

    private async Task ProcessPendingEmailsAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<Data.PhotoPrintDbContext>();
            var sender = scope.ServiceProvider.GetRequiredKeyedService<IEmailSender>(
                Extensions.EmailExtensions.RawSenderKey);

            var now = DateTimeOffset.UtcNow;

            var pending = await db.EmailQueue
                .Where(e => e.Status == EmailStatus.Pending && e.NextRetryAt <= now)
                .OrderBy(e => e.NextRetryAt)
                .Take(_batchSize)
                .ToListAsync(stoppingToken);

            foreach (var email in pending)
            {
                await ProcessSingleAsync(email, sender, stoppingToken);
            }

            if (pending.Count > 0)
            {
                await db.SaveChangesAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Shutdown — exit gracefully
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "EmailRetryJob cycle failed — will retry on next poll");
        }
    }

    private async Task ProcessSingleAsync(
        EmailQueue email,
        IEmailSender sender,
        CancellationToken stoppingToken)
    {
        try
        {
            await sender.SendAsync(email.To, email.Subject, email.HtmlBody, stoppingToken);

            email.Status = EmailStatus.Sent;
            email.SentAt = DateTimeOffset.UtcNow;
            email.LastError = null;

            _logger.LogInformation(
                "EmailRetryJob: delivered {QueueId} to {To} after {Attempts} retry attempt(s)",
                email.Id, email.To, email.Attempts + 1);
        }
        catch (Exception ex)
        {
            email.Attempts++;
            email.LastError = ex.Message;

            if (email.Attempts >= _maxAttempts)
            {
                email.Status = EmailStatus.Failed;

                _logger.LogError(
                    "EmailRetryJob: permanently failed {QueueId} to {To} after {Attempts} attempts. Error: {Error}",
                    email.Id, email.To, email.Attempts, ex.Message);
            }
            else
            {
                email.NextRetryAt = DateTimeOffset.UtcNow.Add(_backoffDelays[email.Attempts]);

                _logger.LogWarning(
                    "EmailRetryJob: retry failed {QueueId} to {To} (attempt {Attempts}/{Max}). Next at {NextRetry}",
                    email.Id, email.To, email.Attempts, _maxAttempts, email.NextRetryAt);
            }
        }
    }
}
