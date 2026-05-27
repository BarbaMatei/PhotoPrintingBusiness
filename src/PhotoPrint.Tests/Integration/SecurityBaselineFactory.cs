using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PhotoPrint.API.Data;
using Microsoft.Extensions.Configuration;
using System.Threading.RateLimiting;
using PhotoPrint.Tests.Helpers;

namespace PhotoPrint.Tests.Integration;

/// <summary>
/// WebApplicationFactory wired for security baseline integration tests.
/// Replaces PostgreSQL with InMemory DB and provides all required configuration.
/// </summary>
public class SecurityBaselineFactory : WebApplicationFactory<Program>
{
    /// <summary>Limit for the "public" rate limiter (overridable for rate-limit tests).</summary>
    public int PublicPermitLimit { get; init; } = 100;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // "Testing" is not "Development" → HSTS middleware is active
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins"] = "https://test.example.com",
                ["RateLimit:WindowSeconds"] = "60",
                ["RateLimit:Public:PermitLimit"] = PublicPermitLimit.ToString(),
                ["RateLimit:Auth:PermitLimit"] = "10",
                ["SecurityHeaders:ContentSecurityPolicy"] = "default-src 'self'",
                ["Email:Provider"] = "Smtp",
                ["Email:FromAddress"] = "test@fototipar.ro",
                ["Email:FromName"] = "FotoTipar Test",
                ["Email:OperatorBcc"] = "",
                ["Email:Smtp:Host"] = "localhost",
                ["Email:Smtp:Port"] = "1025",
                ["Email:Smtp:UseSsl"] = "false",
                // Fake connection string — replaced by InMemory below
                ["ConnectionStrings:Default"] = "Host=localhost;Database=test;Username=test;Password=test",
                ["HealthCheck:UploadsPath"] = "uploads",
                ["JwtSettings:PrivateKeyPem"] = TestKeys.RsaPrivateKeyPem,
                ["JwtSettings:Issuer"] = "fototipar",
                ["JwtSettings:Audience"] = "fototipar-spa",
                ["JwtSettings:AccessTokenMinutes"] = "15",
                ["JwtSettings:RefreshTokenDays"] = "30",
                ["App:BaseUrl"] = "http://localhost:4200",
            });
        });

        builder.ConfigureServices(services =>
        {
            // Replace PostgreSQL with InMemory so tests run without a live DB
            var dbDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<PhotoPrintDbContext>));
            if (dbDescriptor is not null)
                services.Remove(dbDescriptor);

            services.AddDbContext<PhotoPrintDbContext>(options =>
                options.UseInMemoryDatabase($"SecurityTests_{Guid.NewGuid()}"));

            // Override the global rate limiter so per-class PublicPermitLimit takes effect.
            // AddSecurityBaselines reads config directly at service-registration time (before
            // ConfigureAppConfiguration runs), so we override via PostConfigure instead.
            var permitLimit = PublicPermitLimit;
            services.PostConfigure<RateLimiterOptions>(options =>
            {
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                    context => RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = permitLimit,
                            Window = TimeSpan.FromSeconds(60),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0,
                        }));
            });
        });
    }
}
