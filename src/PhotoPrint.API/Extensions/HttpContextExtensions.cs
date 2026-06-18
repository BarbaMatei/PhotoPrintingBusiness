namespace PhotoPrint.API.Extensions;

/// <summary>
/// Typed accessors over <see cref="HttpContext.Items"/> so call sites stop coupling to
/// raw string keys (QUAL-5, review 035-v1). The correlation id is stamped by
/// <c>CorrelationIdMiddleware</c> and read by the exception handler and controllers.
/// </summary>
public static class HttpContextExtensions
{
    /// <summary>The <see cref="HttpContext.Items"/> key under which the per-request
    /// correlation id is stored. Prefer <see cref="GetCorrelationId"/> over this key.</summary>
    public const string CorrelationIdItemKey = "CorrelationId";

    /// <summary>The correlation id stamped on this request, or <c>null</c> if none was set.</summary>
    public static string? GetCorrelationId(this HttpContext context)
        => context.Items[CorrelationIdItemKey]?.ToString();
}
