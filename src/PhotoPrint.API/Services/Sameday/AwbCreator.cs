using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Data;
using PhotoPrint.API.Exceptions;
using PhotoPrint.API.Models;
using PhotoPrint.API.Observability;

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
        var outcome = await CreateForOrderInternalAsync(orderId, attempt, ct);
        RecordOutcome(outcome);
        return outcome;
    }

    /// <summary>
    /// Observability (bolt 044): awb_creation_total{result}. Mapped from
    /// the discriminated <see cref="AwbCreationOutcome"/> union — one
    /// increment per CreateForOrderAsync invocation.
    /// </summary>
    private static void RecordOutcome(AwbCreationOutcome outcome)
    {
        var result = outcome switch
        {
            AwbCreationOutcome.Created    => MetricNames.AwbResultValues.Ok,
            AwbCreationOutcome.Skipped    => MetricNames.AwbResultValues.Skipped,
            AwbCreationOutcome.RetryLater => MetricNames.AwbResultValues.RetryLater,
            AwbCreationOutcome.GiveUp     => MetricNames.AwbResultValues.GiveUp,
            _                             => "unknown",
        };
        FotoMetrics.AwbCreation.Add(1,
            new TagList { { MetricNames.Labels.Result, result } });
    }

    private async Task<AwbCreationOutcome> CreateForOrderInternalAsync(
        Guid orderId, int attempt, CancellationToken ct)
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

        order.AwbNumber   = result.AwbNumber;
        order.AwbLabelUrl = result.LabelUrl;
        order.UpdatedAt   = _clock.GetUtcNow();
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "sameday.awb.created order_id={OrderId} awb={Awb} attempt={Attempt}",
            order.Id, result.AwbNumber, attempt);

        return new AwbCreationOutcome.Created(result.AwbNumber, result.LabelUrl);
    }
}
