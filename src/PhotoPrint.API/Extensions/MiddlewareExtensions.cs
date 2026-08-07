using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using PhotoPrint.API.HealthChecks;
using PhotoPrint.API.Middleware;

namespace PhotoPrint.API.Extensions;

public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CorrelationIdMiddleware>();
    }

    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionHandlerMiddleware>();
    }

    public static IApplicationBuilder UseSentryScopeEnricher(this IApplicationBuilder app)
    {
        return app.UseMiddleware<SentryScopeEnricherMiddleware>();
    }

    public static IEndpointRouteBuilder MapHealthEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = HealthCheckResponseWriter.WriteAsync,
        });

        return endpoints;
    }
}
