using System.Security.Claims;
using Sentry;

namespace PhotoPrint.API.Middleware;

/// <summary>
/// Per-request Sentry scope enricher. Stamps every event captured during the
/// request with <c>correlation_id</c> and <c>user_id</c> (when authenticated).
/// Other static tags — <c>environment</c> and <c>release</c> — are set once
/// from SDK options at boot.
///
/// Registered AFTER <see cref="CorrelationIdMiddleware"/> (so context.Items
/// already carries the id) AND AFTER <c>UseAuthentication/UseAuthorization</c>
/// (so context.User is populated). Resolves <see cref="IHub"/> from
/// per-request DI rather than the static <c>SentrySdk</c> so each
/// WebApplicationFactory in tests uses its own hub; safe to keep wired even
/// with <c>Sentry:Enabled=false</c> (IHub will be absent, middleware no-ops).
/// </summary>
public sealed class SentryScopeEnricherMiddleware : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var hub = context.RequestServices.GetService<IHub>();
        if (hub is not null && hub.IsEnabled)
        {
            hub.ConfigureScope(scope =>
            {
                if (context.Items.TryGetValue("CorrelationId", out var corr) && corr is not null)
                    scope.SetTag("correlation_id", corr.ToString()!);

                var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                    scope.User = new SentryUser { Id = userId };
            });
        }

        await next(context);
    }
}
