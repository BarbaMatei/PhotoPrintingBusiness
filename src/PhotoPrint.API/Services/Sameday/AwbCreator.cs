using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Data;
using PhotoPrint.API.Exceptions;
using PhotoPrint.API.Models;

namespace PhotoPrint.API.Services.Sameday;

/// <summary>
/// Default <see cref="IAwbCreator"/>. Loads the order, performs the
/// load-bearing status/AWB re-check, maps to <see cref="AwbCreationRequest"/>, calls Sameday,
/// and persists <c>AwbNumber</c> + <c>AwbLabelUrl</c>. Scoped lifetime — holds
/// a <see cref="PhotoPrintDbContext"/>; the dispatcher creates a scope per
/// job.
/// </summary>
public sealed class AwbCreator : IAwbCreator
{
    private readonly PhotoPrintDbContext _db;
    private readonly ISamedayClient _sameday;
    private readonly SamedaySettings _samedaySettings;
    private readonly TimeProvider _clock;
    private readonly ILogger<AwbCreator> _logger;

    public AwbCreator(
        PhotoPrintDbContext db,
        ISamedayClient sameday,
        IOptions<SamedaySettings> samedaySettings,
        TimeProvider clock,
        ILogger<AwbCreator> logger)
    {
        _db = db;
        _sameday = sameday;
        _samedaySettings = samedaySettings.Value;
        _clock = clock;
        _logger = logger;
    }

    public async Task<AwbCreationOutcome> CreateForOrderAsync(
        Guid orderId, int attempt, CancellationToken ct = default)
    {
        var order = await _db.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .Include(o => o.EasyboxLocker)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (order is null)
            return new AwbCreationOutcome.Skipped("order not found");

        // Load-bearing re-check: the order may have changed between enqueue and now.
        if (order.Status != OrderStatus.Paid)
            return new AwbCreationOutcome.Skipped($"status is {order.Status}, not Paid");
        if (!string.IsNullOrWhiteSpace(order.AwbNumber))
            return new AwbCreationOutcome.Skipped("AwbNumber already populated");

        AwbCreationRequest request;
        try
        {
            request = OrderToAwbRequestMapper.ToRequest(order, _samedaySettings);
        }
        catch (ArgumentException ex)
        {
            return new AwbCreationOutcome.GiveUp($"invalid request: {ex.Message}");
        }

        // Durable per-order claim: atomically reserve the order before the vendor call so a
        // concurrent creator (retry re-enqueue, second replica, duplicate webhook) backs off
        // instead of billing a second label. Reclaimable after the TTL so a crashed worker
        // cannot strand the order. The vendor idempotency key covers the crash-window residual.
        var now = _clock.GetUtcNow();
        var claimTtl = TimeSpan.FromMinutes(Math.Max(1, _samedaySettings.Jobs.AwbClaimTtlMinutes));
        var claimed = await _db.Orders
            .Where(o => o.Id == orderId
                        && o.AwbNumber == null
                        && o.Status == OrderStatus.Paid
                        && (o.AwbClaimedAt == null || o.AwbClaimedAt < now - claimTtl))
            .ExecuteUpdateAsync(s => s.SetProperty(o => o.AwbClaimedAt, (DateTimeOffset?)now), ct);

        if (claimed == 0)
            return new AwbCreationOutcome.Skipped("another worker holds a fresh AWB claim");

        var outcome = await CreateAndPersistAsync(order, orderId, request, attempt, ct);

        // Release OUR claim on a failure outcome so an in-process retry re-claims promptly
        // instead of waiting out the TTL. Match the exact claim time so a newer claim is never
        // cleared. Best-effort: a failed release just means the claim expires via the TTL.
        // EXCEPT when the outcome may have left a billable AWB at the vendor (timeout / post-create
        // persist failure): hold the claim through its TTL so the re-attempt is deferred past the
        // vendor round-trip rather than re-calling in ~30s and risking a duplicate label.
        var preserveClaim = outcome is AwbCreationOutcome.RetryLater { PreserveClaim: true };
        if (!preserveClaim && outcome is AwbCreationOutcome.RetryLater or AwbCreationOutcome.GiveUp)
        {
            try
            {
                await _db.Orders
                    .Where(o => o.Id == orderId && o.AwbClaimedAt == now)
                    .ExecuteUpdateAsync(s => s.SetProperty(o => o.AwbClaimedAt, (DateTimeOffset?)null), ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "sameday.awb.claim-release-failed order_id={OrderId}", orderId);
            }
        }
        else if (preserveClaim)
        {
            _logger.LogInformation(
                "sameday.awb.claim-held order_id={OrderId} — outcome may have billed an AWB; deferring the re-attempt past the claim TTL",
                orderId);
        }

        return outcome;
    }

    private static string Truncate(string? s, int max) =>
        string.IsNullOrEmpty(s) ? "(none)" : s.Length <= max ? s : s[..max] + "…";

    private async Task<AwbCreationOutcome> CreateAndPersistAsync(
        Order order, Guid orderId, AwbCreationRequest request, int attempt, CancellationToken ct)
    {
        AwbCreationResult result;
        try
        {
            result = await _sameday.CreateAwbAsync(request, ct);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            // HttpClient timeout, not shutdown — transient; the AWB may already have been created,
            // so hold the claim (PreserveClaim) to defer the re-attempt past the vendor round-trip.
            return new AwbCreationOutcome.RetryLater("Sameday call timed out", IsTransient: true, PreserveClaim: true);
        }
        catch (SamedayUnreachableException ex)
        {
            // A retryable status (HttpStatus set) means the vendor received the request and may have
            // created the AWB — hold the claim like the timeout path. A pure transport failure (no
            // status: never connected) is pre-create, so release for a prompt in-process retry.
            var mayHaveBilled = ex.HttpStatus is not null;
            return new AwbCreationOutcome.RetryLater(ex.Message, IsTransient: true, PreserveClaim: mayHaveBilled);
        }
        catch (SamedayAuthException ex)
        {
            // Credentials need ops attention — wait for retry job (which will
            // keep enqueuing until the operator fixes the secret).
            return new AwbCreationOutcome.RetryLater(ex.Message, IsTransient: false);
        }
        catch (SamedayProtocolException ex)
        {
            // Vendor contract drift — same handling as auth: retry job keeps
            // polling, but in-process backoff won't help in the short term.
            return new AwbCreationOutcome.RetryLater(ex.Message, IsTransient: false);
        }
        catch (SamedayValidationException ex)
        {
            // Surface the vendor's field-level reason (truncated to limit echoed PII) so ops can
            // diagnose the permanent failure — ex.Message is generic; retrying the same input is pointless.
            _logger.LogWarning(
                "sameday.awb.vendor-rejected order_id={OrderId} status={Status} body={Body}",
                orderId, ex.HttpStatus, Truncate(ex.ResponseBody, 300));
            return new AwbCreationOutcome.GiveUp(ex.Message);
        }

        // Log the billable AWB BEFORE persisting so a DB failure leaves it recoverable.
        _logger.LogInformation(
            "sameday.awb.created order_id={OrderId} awb={Awb} attempt={Attempt}",
            order.Id, result.AwbNumber, attempt);

        // A vendor label URL longer than the column bound would throw on the Postgres column AFTER
        // the AWB is already billed — caught below as transient, so every retry re-calls the vendor
        // and re-bills. Drop the over-length URL instead; the label stays fetchable by AWB number.
        var labelUrl = result.LabelUrl;
        if (labelUrl is { Length: > Order.MaxAwbLabelUrlLength })
        {
            _logger.LogWarning(
                "sameday.awb.label-url-too-long order_id={OrderId} awb={Awb} length={Length} — storing null label",
                orderId, result.AwbNumber, labelUrl.Length);
            labelUrl = null;
        }

        // != Cancelled, not == Paid: an admin may advance Paid→Printing mid-call and that
        // order still needs its label; the retry sweep only re-picks Paid, so a Printing
        // order that lost the write here would never recover.
        int affected;
        try
        {
            affected = await _db.Orders
                .Where(o => o.Id == orderId
                            && o.AwbNumber == null
                            && o.Status != OrderStatus.Cancelled)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(o => o.AwbNumber,   result.AwbNumber)
                    .SetProperty(o => o.AwbLabelUrl, labelUrl)
                    .SetProperty(o => o.UpdatedAt,   (DateTimeOffset?)_clock.GetUtcNow()),
                    ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex,
                "sameday.awb.persist-failed order_id={OrderId} awb={Awb} — will retry",
                orderId, result.AwbNumber);
            // The AWB is created+billed but not persisted; hold the claim so the re-attempt waits
            // out the TTL instead of re-calling the vendor in ~30s and billing a second label.
            return new AwbCreationOutcome.RetryLater(
                "AWB persist failed after vendor create", IsTransient: true, PreserveClaim: true);
        }

        if (affected == 1)
            return new AwbCreationOutcome.Created(result.AwbNumber, result.LabelUrl);

        // affected == 0: cancelled or already carries an AWB. Read it back to tell a benign
        // vendor-dedup convergence (same number) from a genuine orphan (different/absent).
        var persisted = await _db.Orders
            .AsNoTracking()
            .Where(o => o.Id == orderId)
            .Select(o => o.AwbNumber)
            .FirstOrDefaultAsync(ct);

        if (persisted == result.AwbNumber)
        {
            _logger.LogInformation(
                "sameday.awb.converged order_id={OrderId} awb={Awb} — vendor deduped on order reference",
                orderId, result.AwbNumber);
            return new AwbCreationOutcome.Skipped("AWB already persisted with the same number (converged)");
        }

        // Error-level: a real billable label exists that no order references and the vendor
        // has no void endpoint here — ops must reconcile it manually.
        _logger.LogError(
            "sameday.awb.orphaned order_id={OrderId} created_awb={Created} persisted_awb={Persisted} — created AWB needs a manual void",
            orderId, result.AwbNumber, persisted);
        return new AwbCreationOutcome.Skipped($"order no longer writable; AWB {result.AwbNumber} may be orphaned");
    }
}
