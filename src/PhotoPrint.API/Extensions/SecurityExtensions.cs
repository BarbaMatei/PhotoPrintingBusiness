using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Middleware;

namespace PhotoPrint.API.Extensions;

public static class SecurityExtensions
{
    public const string CorsPolicyName = "AllowAngularApp";
    public const string AuthRateLimitPolicy = "auth";
    public const string CouponRateLimitPolicy = "coupon";

    private static string CouponRateLimitPartitionKey(HttpContext context)
    {
        var userId = context.User.GetUserIdOrNull();
        if (userId is not null) return $"user:{userId}";

        var guestSessionId = context.User.GetGuestSessionIdOrNull();
        if (guestSessionId is not null) return $"guest:{guestSessionId}";

        return $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
    }

    public static IServiceCollection AddSecurityBaselines(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── CORS ──────────────────────────────────────────────────────────────
        var corsSettings = configuration.GetSection("Cors").Get<CorsSettings>()
            ?? new CorsSettings();

        var origins = corsSettings.GetOrigins();

        if (origins.Length == 0)
        {
            throw new InvalidOperationException(
                "Cors:AllowedOrigins is required. Provide at least one exact origin " +
                "(e.g., https://fototipar.ro). Wildcards are not permitted.");
        }

        services.AddCors(options =>
        {
            options.AddPolicy(CorsPolicyName, policy =>
            {
                policy
                    .WithOrigins(origins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials(); // Required for HttpOnly refresh-token cookie
            });
        });

        // ── HSTS ──────────────────────────────────────────────────────────────
        services.AddHsts(options =>
        {
            options.MaxAge = TimeSpan.FromDays(365);
            options.IncludeSubDomains = true;
            options.Preload = false; // Not appropriate until domain is on preload list
        });

        // ── Rate Limiting ─────────────────────────────────────────────────────
        var rateLimitSettings = configuration.GetSection("RateLimit").Get<RateLimitSettings>()
            ?? new RateLimitSettings();

        var window = TimeSpan.FromSeconds(rateLimitSettings.WindowSeconds);

        services.AddRateLimiter(options =>
        {
            // Global "public" limiter — applies to every request (100/min per IP)
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimitSettings.Public.PermitLimit,
                        Window = window,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                    }));

            // Named "auth" limiter — applied additionally on auth endpoints (10/min per IP).
            // Auth endpoints satisfy BOTH limiters; effective limit is min(100, 10) = 10/min.
            options.AddFixedWindowLimiter(AuthRateLimitPolicy, limiterOptions =>
            {
                limiterOptions.PermitLimit = rateLimitSettings.Auth.PermitLimit;
                limiterOptions.Window = window;
                limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                limiterOptions.QueueLimit = 0;
            });

            options.AddPolicy(CouponRateLimitPolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: CouponRateLimitPartitionKey(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimitSettings.Coupon.PermitLimit,
                        Window = window,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                    }));

            options.AddAuthRateLimitPolicies();

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (ctx, token) =>
            {
                ctx.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                if (ctx.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    ctx.HttpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString();
                }
                else
                {
                    ctx.HttpContext.Response.Headers.RetryAfter =
                        rateLimitSettings.WindowSeconds.ToString();
                }

                await ctx.HttpContext.Response.WriteAsync(
                    "Too many requests. Please try again later.", token);
            };
        });

        // ── Security Headers ──────────────────────────────────────────────────
        services.Configure<SecurityHeadersOptions>(configuration.GetSection("SecurityHeaders"));

        return services;
    }

    public static WebApplication UseSecurityBaselines(this WebApplication app)
    {
        // HSTS + HTTPS redirection are production-only. In Development the app serves HTTP
        // only, so UseHttpsRedirection can't determine an HTTPS port (logs a warning); and a
        // dev HSTS header would get permanently cached by browsers.
        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
            app.UseHttpsRedirection();
        }
        app.UseMiddleware<SecurityHeadersMiddleware>();
        app.UseCors(CorsPolicyName);
        app.UseRateLimiter();

        return app;
    }
}
