using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using PhotoPrint.API.Data;
using PhotoPrint.Tests.Helpers;
using Sentry;

namespace PhotoPrint.Tests.Integration;

/// <summary>
/// WebApplicationFactory wired for the Sentry integration test. Boots the API
/// with <c>Sentry:Enabled=true</c> so the scope-enricher middleware registers,
/// then REPLACES the DI registration of <see cref="IHub"/> with a Moq fake
/// that captures every <c>CaptureEvent</c> call.
///
/// Why a mock instead of a real SDK + custom transport: Sentry's static SDK
/// is process-global; other test factories in the same run pollute it.
/// Our production middleware resolves <see cref="IHub"/> from
/// <c>context.RequestServices</c>, so a per-factory replacement isolates this
/// test from anyone else's Sentry state.
/// </summary>
public class SentryIntegrationFactory : WebApplicationFactory<Program>
{
    /// <summary>
    /// Program.cs reads <c>Sentry:Enabled</c> from <c>builder.Configuration</c>
    /// during host setup, BEFORE WAF's <c>ConfigureAppConfiguration</c>
    /// callback fires. Environment variables ARE visible at that point.
    /// </summary>
    static SentryIntegrationFactory()
    {
        Environment.SetEnvironmentVariable("Sentry__Enabled", "true");
        Environment.SetEnvironmentVariable("Sentry__Dsn", "https://dummy@sentry.invalid/0");
    }

    public List<SentryEvent> CapturedEvents { get; } = new();
    public Dictionary<string, string> CapturedTags { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins"] = "https://test.example.com",
                ["RateLimit:WindowSeconds"] = "60",
                ["RateLimit:Public:PermitLimit"] = "10000",
                ["RateLimit:Auth:PermitLimit"] = "10",
                ["SecurityHeaders:ContentSecurityPolicy"] = "default-src 'self'",
                ["Email:Provider"] = "Smtp",
                ["Email:FromAddress"] = "test@fototipar.ro",
                ["Email:FromName"] = "FotoTipar Test",
                ["Email:OperatorBcc"] = "",
                ["Email:Smtp:Host"] = "localhost",
                ["Email:Smtp:Port"] = "1025",
                ["Email:Smtp:UseSsl"] = "false",
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
            var dbDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<PhotoPrintDbContext>));
            if (dbDescriptor is not null)
                services.Remove(dbDescriptor);
            services.AddDbContext<PhotoPrintDbContext>(o =>
                o.UseInMemoryDatabase($"SentryTests_{Guid.NewGuid()}"));

            // Replace Sentry's IHub with a Moq fake that records every captured event.
            // Both ExceptionHandlerMiddleware (CaptureException) and the scope enricher
            // (ConfigureScope) resolve IHub from per-request DI, so this fake captures
            // exactly what the production code sends.
            var hubMock = new Mock<IHub>();
            hubMock.SetupGet(h => h.IsEnabled).Returns(true);

            // CaptureException(Exception) is an extension method that calls
            // ISentryClient.CaptureEvent(SentryEvent, Scope?, SentryHint?). All
            // three parameters must be specified explicitly in the Moq expression
            // because optional arguments aren't allowed in expression trees (CS0854).
            hubMock.Setup(h => h.CaptureEvent(
                    It.IsAny<SentryEvent>(),
                    It.IsAny<Scope?>(),
                    It.IsAny<SentryHint?>()))
                .Callback<SentryEvent, Scope?, SentryHint?>((evt, _, _) => CapturedEvents.Add(evt))
                .Returns(SentryId.Empty);

            // ConfigureScope: capture tag writes from SentryScopeEnricherMiddleware.
            hubMock.Setup(h => h.ConfigureScope(It.IsAny<Action<Scope>>()))
                .Callback<Action<Scope>>(action =>
                {
                    // Run the action against a real Scope so it executes its SetTag
                    // calls — then mirror those tags into our captured dict.
                    var opts = new SentryOptions { Dsn = "https://x@x.invalid/0" };
                    var scope = new Scope(opts);
                    action(scope);
                    foreach (var (k, v) in scope.Tags)
                        CapturedTags[k] = v;
                });

            services.AddSingleton<IHub>(hubMock.Object);
        });
    }
}
