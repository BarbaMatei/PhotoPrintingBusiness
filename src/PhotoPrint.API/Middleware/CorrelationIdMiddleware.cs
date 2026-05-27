using Serilog;
using Serilog.Context;

namespace PhotoPrint.API.Middleware;

public class CorrelationIdMiddleware : IMiddleware
{
    private const string HeaderName = "X-Correlation-Id";
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(ILogger<CorrelationIdMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var correlationId = ResolveCorrelationId(context);

        context.Items["CorrelationId"] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            context.Response.OnStarting(() =>
            {
                context.Response.Headers[HeaderName] = correlationId.ToString();
                return Task.CompletedTask;
            });

            await next(context);
        }
    }

    private static Guid ResolveCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(HeaderName, out var headerValue)
            && Guid.TryParse(headerValue, out var parsed))
        {
            return parsed;
        }

        return Guid.NewGuid();
    }
}
