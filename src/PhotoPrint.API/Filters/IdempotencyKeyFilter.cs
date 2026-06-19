using Microsoft.AspNetCore.Mvc.Filters;
using PhotoPrint.API.Extensions;

namespace PhotoPrint.API.Filters;

/// <summary>
/// Owns Idempotency-Key handling for the payment endpoints (QUAL-3, review 035-v1):
/// reads the <c>Idempotency-Key</c> header once, normalizes whitespace-only to null,
/// stashes it in <see cref="HttpContext.Items"/> for the action (read via
/// <see cref="HttpContextExtensions.GetIdempotencyKey"/>), and logs the transitional
/// missing-key warning with the correlation id — instead of each endpoint repeating
/// the extraction + warning. See OPS-1: the warning escalates to a 400 once the FE
/// always sends a key.
/// </summary>
public sealed class IdempotencyKeyFilter : IActionFilter
{
    public const string HeaderName = "Idempotency-Key";
    private readonly ILogger<IdempotencyKeyFilter> _logger;

    public IdempotencyKeyFilter(ILogger<IdempotencyKeyFilter> logger) => _logger = logger;

    public void OnActionExecuting(ActionExecutingContext context)
    {
        var raw = context.HttpContext.Request.Headers[HeaderName].ToString();
        var key = string.IsNullOrWhiteSpace(raw) ? null : raw;
        context.HttpContext.Items[HttpContextExtensions.IdempotencyKeyItemKey] = key;

        if (key is null)
        {
            _logger.LogWarning(
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
