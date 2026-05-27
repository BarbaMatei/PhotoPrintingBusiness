using PhotoPrint.API.Data;
using PhotoPrint.API.Models;

namespace PhotoPrint.API.Services;

public class ReliableEmailService : IEmailService
{
    private static readonly TimeSpan _initialRetryDelay = TimeSpan.FromSeconds(1);

    private readonly IEmailSender _sender;
    private readonly IRazorTemplateService _templateService;
    private readonly PhotoPrintDbContext _db;
    private readonly ILogger<ReliableEmailService> _logger;

    public ReliableEmailService(
        IEmailSender sender,
        IRazorTemplateService templateService,
        PhotoPrintDbContext db,
        ILogger<ReliableEmailService> logger)
    {
        _sender = sender;
        _templateService = templateService;
        _db = db;
        _logger = logger;
    }

    public async Task SendAsync(
        string to,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _sender.SendAsync(to, subject, htmlBody, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Email send failed for {To} | Subject: {Subject} — queued for retry",
                to, subject);

            await QueueForRetryAsync(to, subject, htmlBody, ex.Message, cancellationToken);
        }
    }

    public async Task SendTemplatedAsync<T>(
        string to,
        string subject,
        string templateName,
        T model,
        CancellationToken cancellationToken = default)
    {
        // Template rendering errors are not transient — let them propagate
        var htmlBody = await _templateService.RenderAsync(templateName, model);

        await SendAsync(to, subject, htmlBody, cancellationToken);
    }

    private async Task QueueForRetryAsync(
        string to,
        string subject,
        string htmlBody,
        string errorMessage,
        CancellationToken cancellationToken)
    {
        var entry = new EmailQueue
        {
            Id = Guid.NewGuid(),
            To = to,
            Subject = subject,
            HtmlBody = htmlBody,
            Status = EmailStatus.Pending,
            Attempts = 0,
            NextRetryAt = DateTimeOffset.UtcNow.Add(_initialRetryDelay),
            CreatedAt = DateTimeOffset.UtcNow,
            LastError = errorMessage,
        };

        await _db.EmailQueue.AddAsync(entry, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Email queued for retry | QueueId: {QueueId} | To: {To}",
            entry.Id, to);
    }
}
