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

// Multi-replica safety relies on the per-row ClaimedAt+TTL claim below, plus ANAF's own InvoiceNumber dedupe as a crash-window fallback.
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
        await RunTickAsync(stoppingToken);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunTickAsync(stoppingToken);
        }
    }

    private async Task RunTickAsync(CancellationToken stoppingToken)
    {
        try
        {
            await ProcessBatchAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
        {
            // A client timeout in ANAF, storage or the DB surfaces as cancellation, and a BackgroundService that throws stops the host.
            _logger.LogWarning("anaf.upload-job.tick-cancelled — a timeout inside the tick was not a shutdown");
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

            // One dead credential fails every row, so the first auth failure silences the rest of the tick.
            var authFailed = false;
            var skippedAfterAuthFailure = 0;

            foreach (var row in batch)
            {
                if (ct.IsCancellationRequested) break;

                if (authFailed && row.AnafStatus == InvoiceAnafStatus.Submitted)
                {
                    // Polling is pure ANAF work, so it cannot succeed; a Pending row still has local XML/PDF work worth doing.
                    skippedAfterAuthFailure++;
                    continue;
                }

                using var perRowScope = _scopeFactory.CreateScope();
                try
                {
                    await ProcessOneAsync(perRowScope.ServiceProvider, row.Id, row.OrderId, row.AnafStatus, ct);
                }
                catch (OperationCanceledException) { throw; }
                catch (AnafAuthException ex)
                {
                    await RecordAuthFailureAsync(perRowScope.ServiceProvider, row.Id, row.AnafStatus, ex, ct);

                    if (!authFailed)
                    {
                        authFailed = true;
                        // Urgent (expiring cert / revoked credential) — no per-replica-safe counter backs "escalate after N tries", so treat as urgent on first sight.
                        _logger.LogError(ex, "anaf.upload-job.auth-failed invoice_id={InvoiceId}", row.Id);
                        perRowScope.ServiceProvider.GetService<Sentry.IHub>()?.CaptureException(ex);
                    }
                    else
                    {
                        skippedAfterAuthFailure++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "anaf.upload-job.row-failed invoice_id={InvoiceId}", row.Id);
                }
            }

            if (skippedAfterAuthFailure > 0)
            {
                _logger.LogWarning(
                    "anaf.upload-job.auth-failure-skipped count={Count} — one credential failure, not {Count} incidents",
                    skippedAfterAuthFailure, skippedAfterAuthFailure);
            }
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "anaf.upload-job.batch-failed");
        }
    }

    private async Task RecordAuthFailureAsync(
        IServiceProvider sp, Guid invoiceId, InvoiceAnafStatus status, Exception ex, CancellationToken ct)
    {
        try
        {
            // Without this the admin list shows a stuck invoice with no reason at all.
            var lifecycle = sp.GetRequiredService<IInvoiceLifecycle>();
            await lifecycle.RecordErrorAsync(invoiceId, ex.Message, status, ct);
        }
        catch (Exception recordEx) when (recordEx is not OperationCanceledException)
        {
            _logger.LogWarning(recordEx,
                "anaf.upload-job.auth-error-not-recorded invoice_id={InvoiceId}", invoiceId);
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

        var claimedAt = _clock.GetUtcNow();
        var claimTtl = TimeSpan.FromMinutes(Math.Max(2, _settings.ClaimTtlMinutes));
        var claimed = await db.Invoices
            .Where(i => i.Id == invoiceId
                        && i.AnafStatus == InvoiceAnafStatus.Pending
                        && (i.ClaimedAt == null || i.ClaimedAt < claimedAt - claimTtl))
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.ClaimedAt, (DateTimeOffset?)claimedAt), ct);

        if (claimed == 0)
        {
            _logger.LogInformation(
                "anaf.upload-job.claim-lost invoice_id={InvoiceId} — another worker holds a fresh claim",
                invoiceId);
            return;
        }

        var invoice = await db.Invoices.FirstOrDefaultAsync(i => i.Id == invoiceId, ct);
        if (invoice is null)
        {
            _logger.LogWarning(
                "anaf.upload-job.row-missing invoice_id={InvoiceId} order_id={OrderId}",
                invoiceId, orderId);
            return;
        }

        var needsOrder = string.IsNullOrEmpty(invoice.XmlPayload) || string.IsNullOrEmpty(invoice.PdfStoragePath);
        Order? order = null;
        if (needsOrder)
        {
            order = await db.Orders
                .Include(o => o.Items)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == orderId, ct);
            if (order is null)
            {
                _logger.LogWarning(
                    "anaf.upload-job.row-missing invoice_id={InvoiceId} order_id={OrderId}",
                    invoiceId, orderId);
                return;
            }
        }

        try
        {
            // Step 1: build XML if not already cached on the row.
            if (string.IsNullOrEmpty(invoice.XmlPayload))
            {
                var xmlBytes = xmlBuilder.Build(order!, invoice, sellerOpts.Value);
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
                var pdfBytes = pdfRenderer.Render(order!, invoice, sellerOpts.Value);
                var key = InvoiceStorageKeys.ForPdf(invoice);
                using (var ms = new MemoryStream(pdfBytes, writable: false))
                    await storage.SaveAsync(ms, key, ct);
                invoice.PdfStoragePath = key;
                invoice.UpdatedAt      = _clock.GetUtcNow();
                await db.SaveChangesAsync(ct);
                _logger.LogInformation(
                    "anaf.upload-job.pdf-rendered invoice_id={InvoiceId} key={Key}",
                    invoiceId, key);
                await notifier.NotifyAsync(invoice, order!, ct);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Otherwise a bad snapshot loops silently forever: no LastError, no claim release, no admin-visible signal.
            await lifecycle.RecordPendingErrorAsync(invoiceId, ex.Message, ct);
            _logger.LogError(ex, "anaf.upload-job.build-failed invoice_id={InvoiceId}", invoiceId);
            await ReleaseClaimAsync(db, invoiceId, claimedAt, ct);
            return;
        }

        // Step 3: upload to ANAF.
        try
        {
            var xmlBytes = Encoding.UTF8.GetBytes(invoice.XmlPayload!);
            var result = await anafClient.UploadAsync(xmlBytes, ct);

            bool ok;
            try
            {
                ok = await lifecycle.MarkSubmittedAsync(invoiceId, result.UploadId, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // ANAF already has this invoice — only the local status write failed.
                _logger.LogError(ex,
                    "anaf.upload-job.submitted-but-not-recorded invoice_id={InvoiceId} anaf_upload_id={AnafUploadId}",
                    invoiceId, result.UploadId);
                throw;
            }

            if (ok) IncrementAnafStatusMetric(MetricNames.AnafStatusValues.Pending);
            // The meter tracks status transitions; accepted/rejected/failed land during polling.
        }
        catch (AnafUploadException ex)
        {
            // 200-with-errors: store the message and stay Pending; next tick retries.
            await lifecycle.RecordPendingErrorAsync(invoiceId, ex.Message, ct);
            _logger.LogWarning(ex,
                "anaf.upload-job.upload-errors invoice_id={InvoiceId}", invoiceId);
            await ReleaseClaimAsync(db, invoiceId, claimedAt, ct);
        }
        catch (AnafUnreachableException ex)
        {
            // Also covers a hard content rejection (HTTP 400) AnafSpvClient can't tell apart from a real outage.
            await lifecycle.RecordPendingErrorAsync(invoiceId, ex.Message, ct);
            _logger.LogWarning(ex,
                "anaf.upload-job.unreachable invoice_id={InvoiceId} status={HttpStatus}",
                invoiceId, ex.HttpStatus);
            await ReleaseClaimAsync(db, invoiceId, claimedAt, ct);
        }
        catch (AnafUploadTimeoutException ex)
        {
            // Claim deliberately NOT released: ANAF may hold this invoice already, so hold the row until the TTL expires rather than re-uploading on the next tick.
            await lifecycle.RecordPendingErrorAsync(invoiceId, ex.Message, ct);
            _logger.LogError(ex,
                "anaf.upload-job.upload-outcome-unknown invoice_id={InvoiceId} — held until the claim expires",
                invoiceId);
        }
        // AnafAuthException propagates to the batch loop's catch — the claim just holds through its TTL.
    }

    private async Task ReleaseClaimAsync(PhotoPrintDbContext db, Guid invoiceId, DateTimeOffset claimedAt, CancellationToken ct)
    {
        try
        {
            await db.Invoices
                .Where(i => i.Id == invoiceId && i.ClaimedAt == claimedAt)
                .ExecuteUpdateAsync(s => s.SetProperty(i => i.ClaimedAt, (DateTimeOffset?)null), ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "anaf.upload-job.claim-release-failed invoice_id={InvoiceId}", invoiceId);
        }
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
            case AnafExternalStatus.Unknown:
                _logger.LogWarning("anaf.upload-job.status-unknown invoice_id={InvoiceId}", invoiceId);
                break;
            case AnafExternalStatus.InProgress:
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

    private static void IncrementAnafStatusMetric(string statusLabel)
    {
        FotoMetrics.InvoiceAnafStatus.Add(1,
            new TagList
            {
                { MetricNames.Labels.Status, statusLabel },
            });
    }
}
