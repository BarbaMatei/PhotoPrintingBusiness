using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Data;
using PhotoPrint.API.Exceptions;
using PhotoPrint.API.Models;

namespace PhotoPrint.API.Services.Sameday;

/// <summary>
/// Default <see cref="IAwbCreator"/>. Loads the order, performs the
/// ADR-015 re-check, maps to <see cref="AwbCreationRequest"/>, calls Sameday,
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
            .Include(o => o.Items)
            .Include(o => o.EasyboxLocker)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (order is null)
            return new AwbCreationOutcome.Skipped("order not found");

        // ADR-015 load-bearing re-check.
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

        AwbCreationResult result;
        try
        {
            result = await _sameday.CreateAwbAsync(request, ct);
        }
        catch (SamedayUnreachableException ex)
        {
            return new AwbCreationOutcome.RetryLater(ex.Message, IsTransient: true);
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
            // Our request is wrong — retrying with the same input is pointless.
            return new AwbCreationOutcome.GiveUp(ex.Message);
        }

        // Log the billable AWB BEFORE persisting so a DB failure leaves it recoverable.
        _logger.LogInformation(
            "sameday.awb.created order_id={OrderId} awb={Awb} attempt={Attempt}",
            order.Id, result.AwbNumber, attempt);

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
                    .SetProperty(o => o.AwbLabelUrl, result.LabelUrl)
                    .SetProperty(o => o.UpdatedAt,   (DateTimeOffset?)_clock.GetUtcNow()),
                    ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex,
                "sameday.awb.persist-failed order_id={OrderId} awb={Awb} — will retry",
                orderId, result.AwbNumber);
            return new AwbCreationOutcome.RetryLater("AWB persist failed after vendor create", IsTransient: true);
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

        _logger.LogWarning(
            "sameday.awb.orphaned order_id={OrderId} created_awb={Created} persisted_awb={Persisted} — created AWB may need a manual void",
            orderId, result.AwbNumber, persisted);
        return new AwbCreationOutcome.Skipped($"order no longer writable; AWB {result.AwbNumber} may be orphaned");
    }
}
