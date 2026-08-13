using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Data;
using PhotoPrint.API.Models;
using PhotoPrint.API.Observability;

namespace PhotoPrint.API.Services.Invoicing.Anaf;

/// <summary>
/// Background worker that drives <see cref="Invoice"/> rows through their
/// ANAF lifecycle (<c>Pending → Submitted → Accepted | Rejected → Failed</c>).
/// Pulls work from the DB on a <c>PeriodicTimer</c> every
/// <c>Anaf:PollIntervalMinutes</c> (default 30) per ADR-023 — no
/// in-process <c>Channel&lt;T&gt;</c>.
///
/// Concurrency: multi-replica safe via ADR-015 (ANAF dedupes on
/// <c>InvoiceNumber</c>) + ADR-016 (CAS via <c>InvoiceLifecycle</c>).
/// </summary>
public sealed class InvoiceUploadJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AnafSettings _settings;
    private readonly TimeProvider _clock;
    private readonly ILogger<InvoiceUploadJob> _logger;

    public InvoiceUploadJob(
        IServiceScopeFactory scopeFactory,
        IOptions<AnafSettings> settings,
        TimeProvider clock,
        ILogger<InvoiceUploadJob> logger)
    {
        _scopeFactory = scopeFactory;
        _settings = settings.Value;
        _clock = clock;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(Math.Max(1, _settings.PollIntervalMinutes));
        using var timer = new PeriodicTimer(interval);

        _logger.LogInformation(
            "anaf.upload-job.started interval_minutes={Interval} batch_size={Batch}",
            interval.TotalMinutes, _settings.MaxBatchSize);

        // Tick immediately on startup so a freshly-deployed instance picks up
        // anything that accumulated while the previous instance was down.
        await ProcessBatchAsync(stoppingToken);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await ProcessBatchAsync(stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<PhotoPrintDbContext>();

            var pendingStatus   = InvoiceAnafStatus.Pending;
            var submittedStatus = InvoiceAnafStatus.Submitted;

            var batch = await db.Invoices
                .Where(i => i.AnafStatus == pendingStatus || i.AnafStatus == submittedStatus)
                .OrderBy(i => i.CreatedAt)
                .Take(_settings.MaxBatchSize)
                .Select(i => new { i.Id, i.OrderId, i.AnafStatus })
                .ToListAsync(ct);

            if (batch.Count == 0) return;

            _logger.LogInformation("anaf.upload-job.batch size={Count}", batch.Count);

            foreach (var row in batch)
            {
                if (ct.IsCancellationRequested) break;
                try
                {
                    using var perRowScope = _scopeFactory.CreateScope();
                    await ProcessOneAsync(perRowScope.ServiceProvider, row.Id, row.OrderId, row.AnafStatus, ct);
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "anaf.upload-job.row-failed invoice_id={InvoiceId}", row.Id);
                }
            }
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "anaf.upload-job.batch-failed");
        }
    }

    private async Task ProcessOneAsync(
        IServiceProvider sp, Guid invoiceId, Guid orderId,
        InvoiceAnafStatus status, CancellationToken ct)
    {
        switch (status)
        {
            case InvoiceAnafStatus.Pending:
                await UploadPendingAsync(sp, invoiceId, orderId, ct);
                break;
            case InvoiceAnafStatus.Submitted:
                await PollSubmittedAsync(sp, invoiceId, ct);
                break;
        }
    }

    private async Task UploadPendingAsync(
        IServiceProvider sp, Guid invoiceId, Guid orderId, CancellationToken ct)
    {
        var db          = sp.GetRequiredService<PhotoPrintDbContext>();
        var xmlBuilder  = sp.GetRequiredService<IInvoiceXmlBuilder>();
        var pdfRenderer = sp.GetRequiredService<IInvoicePdfRenderer>();
        var storageRouter = sp.GetRequiredService<IStorageRouter>();
        var storage     = storageRouter.CloudEnabled ? storageRouter.Cloud : storageRouter.Local;
        var anafClient  = sp.GetRequiredService<IAnafSpvClient>();
        var lifecycle   = sp.GetRequiredService<IInvoiceLifecycle>();
        var sellerOpts  = sp.GetRequiredService<IOptions<SellerSettings>>();
        var notifier    = sp.GetRequiredService<InvoicePdfReadyNotifier>();

        var (invoice, order) = await LoadPairAsync(db, invoiceId, orderId, ct);
        if (invoice is null || order is null)
        {
            _logger.LogWarning(
                "anaf.upload-job.row-missing invoice_id={InvoiceId} order_id={OrderId}",
                invoiceId, orderId);
            return;
        }

        // Step 1: build XML if not already cached on the row.
        if (string.IsNullOrEmpty(invoice.XmlPayload))
        {
            var xmlBytes = xmlBuilder.Build(order, invoice, sellerOpts.Value);
            invoice.XmlPayload = Encoding.UTF8.GetString(xmlBytes);
            invoice.UpdatedAt  = _clock.GetUtcNow();
            await db.SaveChangesAsync(ct);
            _logger.LogInformation(
                "anaf.upload-job.xml-built invoice_id={InvoiceId} bytes={Bytes}",
                invoiceId, xmlBytes.Length);
        }

        // Step 2: render PDF and store it.
        if (string.IsNullOrEmpty(invoice.PdfStoragePath))
        {
            var pdfBytes = pdfRenderer.Render(order, invoice, sellerOpts.Value);
            var key = InvoiceStorageKeys.ForPdf(invoice);
            using (var ms = new MemoryStream(pdfBytes, writable: false))
                await storage.SaveAsync(ms, key, ct);
            invoice.PdfStoragePath = key;
            invoice.UpdatedAt      = _clock.GetUtcNow();
            await db.SaveChangesAsync(ct);
            _logger.LogInformation(
                "anaf.upload-job.pdf-rendered invoice_id={InvoiceId} key={Key}",
                invoiceId, key);
            await notifier.NotifyAsync(invoice, order, ct);
        }

        // Step 3: upload to ANAF.
        try
        {
            var xmlBytes = Encoding.UTF8.GetBytes(invoice.XmlPayload!);
            var result = await anafClient.UploadAsync(xmlBytes, ct);

            var ok = await lifecycle.MarkSubmittedAsync(invoiceId, result.UploadId, ct);

            if (ok) IncrementAnafStatusMetric(MetricNames.AnafStatusValues.Pending);
            // Note: the meter tracks observed-status transitions; "pending"
            // is the moment we crossed from build to upload-attempted. The
            // accepted/rejected/failed observations land during polling.
        }
        catch (AnafUploadException ex)
        {
            // 200-with-errors: store the message and stay Pending; next tick retries.
            await lifecycle.RecordPendingErrorAsync(invoiceId, ex.Message, ct);
            _logger.LogWarning(ex,
                "anaf.upload-job.upload-errors invoice_id={InvoiceId}", invoiceId);
        }
        // AnafAuthException / AnafUnreachableException propagate to the
        // batch loop's catch — the next tick will retry naturally.
    }

    private async Task PollSubmittedAsync(
        IServiceProvider sp, Guid invoiceId, CancellationToken ct)
    {
        var db         = sp.GetRequiredService<PhotoPrintDbContext>();
        var anafClient = sp.GetRequiredService<IAnafSpvClient>();
        var lifecycle  = sp.GetRequiredService<IInvoiceLifecycle>();

        var invoice = await db.Invoices.AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == invoiceId, ct);
        if (invoice is null || string.IsNullOrEmpty(invoice.AnafUploadId)) return;

        var result = await anafClient.GetStatusAsync(invoice.AnafUploadId, ct);

        switch (result.Status)
        {
            case AnafExternalStatus.Validated:
                {
                    var ok = await lifecycle.MarkAcceptedAsync(invoiceId, ct);
                    if (ok) IncrementAnafStatusMetric(MetricNames.AnafStatusValues.Accepted);
                    break;
                }
            case AnafExternalStatus.Rejected:
                {
                    var msg = result.ErrorMessage ?? "ANAF rejected the invoice.";
                    var budgetExhausted = IsBudgetExhausted(invoice);

                    var ok = budgetExhausted
                        ? await lifecycle.MarkFailedAsync(invoiceId, msg, ct)
                        : await lifecycle.MarkRejectedAsync(invoiceId, msg, ct);

                    if (ok)
                    {
                        IncrementAnafStatusMetric(
                            budgetExhausted
                                ? MetricNames.AnafStatusValues.Failed
                                : MetricNames.AnafStatusValues.Rejected);
                    }
                    break;
                }
            case AnafExternalStatus.InProgress:
            case AnafExternalStatus.Unknown:
                // No transition — re-poll next tick.
                break;
        }
    }

    private bool IsBudgetExhausted(Invoice invoice)
    {
        var sumHours = _settings.BackoffHours.Sum();
        var elapsed  = _clock.GetUtcNow() - invoice.CreatedAt;
        return elapsed.TotalHours > sumHours;
    }

    private static async Task<(Invoice? invoice, Order? order)> LoadPairAsync(
        PhotoPrintDbContext db, Guid invoiceId, Guid orderId, CancellationToken ct)
    {
        var invoice = await db.Invoices.FirstOrDefaultAsync(i => i.Id == invoiceId, ct);
        if (invoice is null) return (null, null);

        var order = await db.Orders
            .Include(o => o.Items)
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);
        return (invoice, order);
    }

    private static void IncrementAnafStatusMetric(string statusLabel)
    {
        FotoMetrics.InvoiceAnafStatus.Add(1,
            new TagList
            {
                { MetricNames.Labels.Status, statusLabel },
            });
    }
}
