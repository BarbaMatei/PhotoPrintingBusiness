using Microsoft.Extensions.Options;
using PhotoPrint.API.Configuration;

namespace PhotoPrint.API.Middleware;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SecurityHeadersOptions _options;

    public SecurityHeadersMiddleware(RequestDelegate next, IOptions<SecurityHeadersOptions> options)
    {
        _next = next;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Register headers via OnStarting — fires just before the first byte is written,
        // ensuring headers are always added regardless of where in the pipeline the response
        // originates (controllers, exception handlers, rate limiter, etc.).
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["Content-Security-Policy"] = _options.ContentSecurityPolicy;
            return Task.CompletedTask;
        });

        await _next(context);
    }
}
