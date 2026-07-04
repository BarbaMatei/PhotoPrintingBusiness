using Microsoft.AspNetCore.Mvc.Filters;
using PhotoPrint.API.Exceptions;
using PhotoPrint.API.Extensions;

namespace PhotoPrint.API.Filters;

/// <summary>
/// Owns Idempotency-Key handling for the payment endpoints (QUAL-3, review 035-v1):
/// reads the <c>Idempotency-Key</c> header once, normalizes whitespace-only to null,
/// stashes it in <see cref="HttpContext.Items"/> for the action (read via
/// <see cref="HttpContextExtensions.GetIdempotencyKey"/>), and logs the transitional
/// missing-key event (at Information — OBS-3, review 035-v8) with the correlation id,
/// instead of each endpoint repeating the extraction + logging. See OPS-1: the missing-key
/// log escalates to a 400 (and back to Warning) once the FE always sends a key.
/// </summary>
public sealed class IdempotencyKeyFilter : IActionFilter
{
    public const string HeaderName = "Idempotency-Key";

    /// <summary>Domain-spec ceiling for the key length (ddd-01: 1..80) and the width of
    /// the <c>Orders.IdempotencyKey</c> column.</summary>
    public const int MaxKeyLength = 80;

    private readonly ILogger<IdempotencyKeyFilter> _logger;

    public IdempotencyKeyFilter(ILogger<IdempotencyKeyFilter> logger) => _logger = logger;

    public void OnActionExecuting(ActionExecutingContext context)
    {
        var raw = context.HttpContext.Request.Headers[HeaderName].ToString();

        // SEC-2 (review 035-v8): trim before storing. The value becomes the EXACT unique-index
        // key, so an untrimmed key means "abc", " abc" and "abc " are three distinct keys — a
        // client or buggy proxy/retry layer that resends the same logical key once padded would
        // then defeat dedupe and create a second order + second PaymentIntent (double charge).
        // Whitespace-only still normalizes to null (IsNullOrWhiteSpace catches it first).
        var key = string.IsNullOrWhiteSpace(raw) ? null : raw.Trim();

        // SEC-2 (review 035-v5): enforce the documented 1..80 length here. Without this an
        // over-length key passes dev/test (SQLite ignores varchar(80)) and then fails the
        // prod Postgres INSERT with a truncation DbUpdateException → a 500 for what is a
        // client input error. Reject it up front with a 400 instead.
        if (key is not null && key.Length > MaxKeyLength)
            throw new BadRequestException(
                $"Idempotency-Key must be at most {MaxKeyLength} characters.");

        context.HttpContext.Items[HttpContextExtensions.IdempotencyKeyItemKey] = key;

        if (key is null)
        {
            // OPS-1 (review 035-v1): transitional. Once the FE always sends an
            // Idempotency-Key, escalate a missing key from this log to a 400 (breaking
            // change). Tracked in memory-bank/bolts/035-payment-idempotency (ddd-02) + the
            // bolt walkthrough. TODO(bolt-035-followup): enforce required key.
            //
            // OBS-3 (review 035-v8): log at Information, NOT Warning. While the FE hasn't
            // adopted the header, a missing key is the expected (transitional) state on 100%
            // of payment requests — a Warning here is constant noise that can trip
            // warning-rate alerts. Raise back to Warning only once the key is meant to be
            // present (i.e. just before the 400 escalation above).
            _logger.LogInformation(
                "payments.idempotency.missing-key endpoint={Endpoint} correlation_id={CorrelationId}",
                EndpointLabel(context), context.HttpContext.GetCorrelationId());
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }

    // "api/payments/stripe/intent" → "stripe.intent" (preserves the prior log label).
    private static string EndpointLabel(ActionExecutingContext context)
    {
        var template = context.ActionDescriptor.AttributeRouteInfo?.Template ?? string.Empty;
        const string prefix = "api/payments/";
        if (template.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            template = template[prefix.Length..];
        return template.Replace('/', '.');
    }
}
