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

// Multi-replica safety is the per-row ClaimedAt+TTL claim below. Nothing here can tell a duplicate filing from a first one, so an upload whose outcome ANAF never confirmed is counted on the row and capped instead of retried for ever.
public sealed class InvoiceUploadJob : BackgroundService
{
    // A row that just failed sits out one poll interval, so consecutive auth attempts on it are up to two intervals apart; four clears that with margin, the floor keeps the window positive (MemoryCache rejects a non-positive expiry, and the throw would land inside the auth catch), and the validator's 1440-minute ceiling on the interval caps the widest window at 96 h — inside the 5-business-day submission deadline.
    private const int AuthOutageAlertWindowIntervals = 4;
    private static readonly TimeSpan MinAuthOutageAlertWindow = TimeSpan.FromHours(2);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AnafSettings _settings;
    private readonly AnafOutageRegistry _outages;
    private readonly TimeProvider _clock;
    private readonly ILogger<InvoiceUploadJob> _logger;
    private readonly int _pollIntervalMinutes;
    private readonly int _maxUnknownUploadOutcomes;
    private readonly TimeSpan _authOutageAlertWindow;

    public InvoiceUploadJob(
        IServiceScopeFactory scopeFactory,
        IOptions<AnafSettings> settings,
        AnafOutageRegistry outages,
        TimeProvider clock,
        ILogger<InvoiceUploadJob> logger)
    {
        _scopeFactory = scopeFactory;
        _settings = settings.Value;
        _outages = outages;
        _clock = clock;
        _logger = logger;
        _pollIntervalMinutes = Math.Max(1, _settings.PollIntervalMinutes);
        _maxUnknownUploadOutcomes = Math.Max(1, _settings.MaxUnknownUploadOutcomes);
        _authOutageAlertWindow = AuthOutageAlertWindowFor(_pollIntervalMinutes);
    }

    private static TimeSpan AuthOutageAlertWindowFor(int pollIntervalMinutes)
    {
        var scaled = TimeSpan.FromMinutes(AuthOutageAlertWindowIntervals * (long)pollIntervalMinutes);
        return scaled > MinAuthOutageAlertWindow ? scaled : MinAuthOutageAlertWindow;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(_pollIntervalMinutes);
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
            var rejectedStatus  = InvoiceAnafStatus.Rejected;

            // A row that just failed gets a cooldown, or the oldest broken invoice heads every batch
            // forever and starves the healthy ones out of a batch capped at MaxBatchSize.
            var now = _clock.GetUtcNow();
            var retryNotBefore = now - TimeSpan.FromMinutes(Math.Max(2, _settings.PollIntervalMinutes));

            // Coarse filter only: the exact schedule needs CreatedAt too, so due-ness is settled in memory below.
            var rejectedNotBefore = now - TimeSpan.FromHours(Math.Max(1, MinBackoffHours));

            var batch = await db.Invoices
                .Where(i => ((i.AnafStatus == pendingStatus || i.AnafStatus == submittedStatus)
                             && (i.LastError == null || i.UpdatedAt == null || i.UpdatedAt < retryNotBefore))
                            || (i.AnafStatus == rejectedStatus
                                && (i.UpdatedAt == null || i.UpdatedAt < rejectedNotBefore)))
                .OrderBy(i => i.CreatedAt)
                .Take(_settings.MaxBatchSize)
                .Select(i => new BatchRow(i.Id, i.OrderId, i.AnafStatus, i.CreatedAt, i.UpdatedAt))
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
                    await ProcessOneAsync(perRowScope.ServiceProvider, row, ct);
                }
                catch (OperationCanceledException) { throw; }
                catch (AnafAuthException ex)
                {
                    await RecordAuthFailureAsync(perRowScope.ServiceProvider, row.Id, row.AnafStatus, ex, ct);

                    if (!authFailed)
                    {
                        authFailed = true;
                        if (_outages.MarkOutageOnce("auth", _authOutageAlertWindow))
                        {
                            // Urgent (expiring cert / revoked credential) — no per-replica-safe counter backs "escalate after N tries", so treat as urgent on first sight.
                            IncrementAnafStatusMetric(MetricNames.AnafStatusValues.Retrying);
                            _logger.LogError(ex, "anaf.upload-job.auth-failed invoice_id={InvoiceId}", row.Id);
                            perRowScope.ServiceProvider.GetService<Sentry.IHub>()?.CaptureException(ex);
                        }
                        else
                        {
                            // With the page suppressed, this is the operator's only per-tick evidence that the outage has not recovered.
                            _logger.LogWarning(
                                "anaf.upload-job.auth-outage-continues invoice_id={InvoiceId} alert_window_minutes={WindowMinutes} interval_minutes={IntervalMinutes}",
                                row.Id, _authOutageAlertWindow.TotalMinutes, _pollIntervalMinutes);
                        }
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

    private sealed record BatchRow(
        Guid Id, Guid OrderId, InvoiceAnafStatus AnafStatus,
        DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt);

    private int MinBackoffHours =>
        _settings.BackoffHours.Length == 0 ? 1 : _settings.BackoffHours.Min();

    private async Task ProcessOneAsync(
        IServiceProvider sp, BatchRow row, CancellationToken ct)
    {
        switch (row.AnafStatus)
        {
            case InvoiceAnafStatus.Pending:
                await UploadPendingAsync(sp, row.Id, row.OrderId, ct);
                break;
            case InvoiceAnafStatus.Submitted:
                await PollSubmittedAsync(sp, row.Id, ct);
                break;
            case InvoiceAnafStatus.Rejected:
                await ResubmitRejectedAsync(sp, row, ct);
                break;
        }
    }

    // Rejections are re-submitted on the configured schedule, then given up on: a rejected row nobody
    // resubmits silently outlives the 5-business-day ANAF submission deadline.
    private async Task ResubmitRejectedAsync(
        IServiceProvider sp, BatchRow row, CancellationToken ct)
    {
        var lifecycle = sp.GetRequiredService<IInvoiceLifecycle>();
        var lastChange = row.UpdatedAt ?? row.CreatedAt;
        var now = _clock.GetUtcNow();

        var dueAt = NextResubmitAt(row.CreatedAt, lastChange);
        if (dueAt is null)
        {
            var gaveUp = await lifecycle.GiveUpOnRejectedAsync(
                row.Id,
                $"ANAF rejected this invoice on every attempt within {_settings.BackoffHours.Sum()}h; manual correction required.",
                ct);
            if (gaveUp) IncrementAnafStatusMetric(MetricNames.AnafStatusValues.Failed);
            return;
        }

        if (now < dueAt)
        {
            _logger.LogDebug(
                "anaf.upload-job.rejected-not-due invoice_id={InvoiceId} due_at={DueAt}", row.Id, dueAt);
            return;
        }

        var requeued = await lifecycle.RetryAsync(row.Id, InvoiceAnafStatus.Rejected, ct);
        if (requeued)
        {
            IncrementAnafStatusMetric(MetricNames.AnafStatusValues.Retrying);
            _logger.LogInformation(
                "anaf.upload-job.rejected-resubmitted invoice_id={InvoiceId} due_at={DueAt}", row.Id, dueAt);
        }
    }

    // Attempt count is not persisted: the next slot is the first cumulative milestone after the last transition.
    private DateTimeOffset? NextResubmitAt(DateTimeOffset createdAt, DateTimeOffset lastChange)
    {
        var cumulativeHours = 0d;
        foreach (var step in _settings.BackoffHours)
        {
            cumulativeHours += step;
            var slot = createdAt.AddHours(cumulativeHours);
            if (slot > lastChange) return slot;
        }
        return null;
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
                // Stamped with the path in one save, so the row always records where its bytes went.
                invoice.StorageLocation = storageRouter.CloudEnabled ? StorageLocation.Cloud : StorageLocation.Local;
                invoice.UpdatedAt      = _clock.GetUtcNow();
                await db.SaveChangesAsync(ct);
                _logger.LogInformation(
                    "anaf.upload-job.pdf-rendered invoice_id={InvoiceId} key={Key}",
                    invoiceId, key);
                await notifier.NotifyAsync(invoice, order!, ct);
            }
        }
        catch (InvoiceNotBuildableException ex)
        {
            // No retry can repair the order behind it, so re-attempting only hides a paid order behind a permanent 404.
            var parked = await lifecycle.ParkUnbuildableAsync(invoiceId, NotBuildableMessage(ex), ct);
            if (parked)
            {
                IncrementAnafStatusMetric(MetricNames.AnafStatusValues.Failed);
            }
            else
            {
                // A lost CAS must still leave a reason on the row, or the admin list shows a stuck invoice with none.
                await lifecycle.RecordPendingErrorAsync(invoiceId, NotBuildableMessage(ex), ct);
                await ReleaseClaimAsync(db, invoiceId, claimedAt, ct);
            }
            _logger.LogError(ex,
                "anaf.upload-job.not-buildable invoice_id={InvoiceId} order_id={OrderId} parked={Parked} — retrying repeats this failure until the order data is corrected",
                invoiceId, orderId, parked);
            return;
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

                var lost = await lifecycle.RecordUnknownUploadOutcomeAsync(
                    invoiceId,
                    $"Uploaded to ANAF as {result.UploadId} but the status write failed: {ex.Message}",
                    BudgetSpentMessage(ex),
                    _maxUnknownUploadOutcomes,
                    ct);
                IncrementAnafStatusMetric(lost.Parked
                    ? MetricNames.AnafStatusValues.Failed
                    : MetricNames.AnafStatusValues.Retrying);
                return;
            }

            if (ok) IncrementAnafStatusMetric(MetricNames.AnafStatusValues.Pending);
            // The meter tracks status transitions; accepted/rejected/failed land during polling.
        }
        catch (AnafUploadException ex)
        {
            // 200-with-errors: store the message and stay Pending; next tick retries.
            await lifecycle.RecordPendingErrorAsync(invoiceId, ex.Message, ct);
            IncrementAnafStatusMetric(MetricNames.AnafStatusValues.Retrying);
            _logger.LogWarning(ex,
                "anaf.upload-job.upload-errors invoice_id={InvoiceId}", invoiceId);
            await ReleaseClaimAsync(db, invoiceId, claimedAt, ct);
        }
        catch (AnafContentRejectedException ex)
        {
            // ANAF refused the document itself, so no number of retries changes the answer.
            await lifecycle.ParkUnbuildableAsync(invoiceId, ex.Message, ct);
            IncrementAnafStatusMetric(MetricNames.AnafStatusValues.Failed);
            _logger.LogError(ex,
                "anaf.upload-job.content-rejected invoice_id={InvoiceId} status={HttpStatus} — parked as Failed for an admin",
                invoiceId, ex.HttpStatus);
        }
        catch (AnafUnreachableException ex)
        {
            // On the upload leg an outage is an unknown outcome: ANAF may hold this number already.
            var outcome = await lifecycle.RecordUnknownUploadOutcomeAsync(
                invoiceId, ex.Message, BudgetSpentMessage(ex), _maxUnknownUploadOutcomes, ct);

            if (outcome.Parked)
            {
                IncrementAnafStatusMetric(MetricNames.AnafStatusValues.Failed);
                _logger.LogError(ex,
                    "anaf.upload-job.blind-repost-budget-spent invoice_id={InvoiceId} outcomes={Outcomes} max={Max} — parked as Failed, reconcile the invoice number in ANAF SPV",
                    invoiceId, outcome.Outcomes, _maxUnknownUploadOutcomes);
            }
            else
            {
                // The claim stays so a second replica cannot re-post a row whose answer is unknown.
                IncrementAnafStatusMetric(MetricNames.AnafStatusValues.Retrying);
                _logger.LogWarning(ex,
                    "anaf.upload-job.unreachable invoice_id={InvoiceId} status={HttpStatus} outcomes={Outcomes}",
                    invoiceId, ex.HttpStatus, outcome.Outcomes);
            }
        }
        catch (AnafUploadTimeoutException ex)
        {
            var outcome = await lifecycle.RecordUnknownUploadOutcomeAsync(
                invoiceId, ex.Message, BudgetSpentMessage(ex), _maxUnknownUploadOutcomes, ct);

            if (outcome.Parked)
            {
                IncrementAnafStatusMetric(MetricNames.AnafStatusValues.Failed);
                _logger.LogError(ex,
                    "anaf.upload-job.blind-repost-budget-spent invoice_id={InvoiceId} outcomes={Outcomes} max={Max} — parked as Failed, reconcile the invoice number in ANAF SPV",
                    invoiceId, outcome.Outcomes, _maxUnknownUploadOutcomes);
            }
            else if (outcome.Outcomes == 0)
            {
                _logger.LogWarning(ex,
                    "anaf.upload-job.upload-outcome-unknown-not-recorded invoice_id={InvoiceId} — the row left Pending while the upload was in flight",
                    invoiceId);
            }
            else
            {
                _logger.LogError(ex,
                    "anaf.upload-job.upload-outcome-unknown invoice_id={InvoiceId} outcomes={Outcomes} max={Max}",
                    invoiceId, outcome.Outcomes, _maxUnknownUploadOutcomes);
            }
            // The claim is left to expire: it keeps a second replica out of a row whose ANAF answer nobody has. What gates the next attempt is whichever is longer, this TTL or the per-row cooldown — at the defaults the cooldown, so the hold costs nothing. Parking clears it.
        }
        // AnafAuthException propagates to the batch loop's catch — the claim just holds through its TTL.
    }

    private static string NotBuildableMessage(InvoiceNotBuildableException ex) =>
        $"Invoice cannot be built and will not be retried: {ex.Message} " +
        "Retrying repeats the same failure until the order's own data is corrected.";

    private string BudgetSpentMessage(Exception ex) =>
        $"{ex.Message} {_maxUnknownUploadOutcomes} attempts ended without an answer, so no further upload will be made: reconcile this invoice number in ANAF SPV, then retry the invoice.";

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
