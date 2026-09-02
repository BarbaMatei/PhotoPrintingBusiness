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
    // Buffering runs before authentication of any kind, so both numbers cap what an unauthenticated caller can make this filter hold; the buffer limit sits above the peek so the peek never trips it.
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
        request.EnableBuffering(bufferLimit: BufferLimitBytes);

        using var peek = new MemoryStream();
        var chunk = new byte[8192];
        while (peek.Length < PeekBytes)
        {
            var wanted = (int)Math.Min(chunk.Length, PeekBytes - peek.Length);
            var read = await request.Body.ReadAsync(chunk.AsMemory(0, wanted));
            if (read == 0) break;
            peek.Write(chunk, 0, read);
        }
        var body = Encoding.UTF8.GetString(peek.GetBuffer(), 0, (int)peek.Length);
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
