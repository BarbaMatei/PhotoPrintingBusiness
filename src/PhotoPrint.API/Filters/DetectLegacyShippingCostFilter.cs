using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Filters;

namespace PhotoPrint.API.Filters;

/// <summary>
/// Transitional observability filter for bolt 034. The frontend may still
/// serialize the now-deprecated <c>shippingCostRon</c> field in its payment
/// request bodies. The DTO no longer contains it (so the value is silently
/// dropped by System.Text.Json), but we still want a signal in the logs so
/// the team can track when the FE has fully migrated. Once <c>WARN</c> counts
/// reach zero in production logs, this filter and its registration can be
/// removed.
/// </summary>
public sealed class DetectLegacyShippingCostFilter : IAsyncResourceFilter
{
    // Buffering runs before authentication of any kind, so both numbers cap what an unauthenticated caller can make this filter hold; the buffer sits above the peek so the peek never trips it.
    private const int PeekBytes = 64 * 1024;
    private const int BufferLimitBytes = PeekBytes + 4096;

    private readonly ILogger<DetectLegacyShippingCostFilter> _logger;

    public DetectLegacyShippingCostFilter(ILogger<DetectLegacyShippingCostFilter> logger)
    {
        _logger = logger;
    }

    public async Task OnResourceExecutionAsync(
        ResourceExecutingContext context,
        ResourceExecutionDelegate next)
    {
        var request = context.HttpContext.Request;
        request.EnableBuffering(bufferThreshold: PeekBytes, bufferLimit: BufferLimitBytes);

        var buffer = new byte[PeekBytes];
        var read = 0;
        while (read < buffer.Length)
        {
            var chunk = await request.Body.ReadAsync(buffer.AsMemory(read));
            if (chunk == 0) break;
            read += chunk;
        }
        var body = Encoding.UTF8.GetString(buffer, 0, read);
        request.Body.Position = 0;

        if (ContainsLegacyShippingCostKey(body))
        {
            _logger.LogWarning(
                "payments.shipping-cost-tampering-attempt path={Path}",
                request.Path.Value);
        }

        await next();
    }

    private static bool ContainsLegacyShippingCostKey(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return false;

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return false;

            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (string.Equals(prop.Name, "shippingCostRon", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
