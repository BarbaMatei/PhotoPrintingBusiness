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

public class SentryIntegrationFactory : WebApplicationFactory<Program>
{
    public const string Dsn = "https://dummy@sentry.invalid/0";

    public List<SentryEvent> CapturedEvents { get; } = new();
    public Dictionary<string, string> CapturedTags { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Program.cs reads Sentry:Enabled from builder.Configuration before the host is built,
        // which is earlier than ConfigureAppConfiguration runs. UseSetting travels as a
        // command-line argument to the entry point, so it arrives in time — and unlike an
        // environment variable it belongs to this host alone.
        builder.UseSetting("Sentry:Enabled", "true");
        builder.UseSetting("Sentry:Dsn", Dsn);

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

            var hubMock = new Mock<IHub>();
            hubMock.SetupGet(h => h.IsEnabled).Returns(true);

            // Optional arguments are not allowed in expression trees (CS0854), so every
            // CaptureEvent parameter has to be named here.
            hubMock.Setup(h => h.CaptureEvent(
                    It.IsAny<SentryEvent>(),
                    It.IsAny<Scope?>(),
                    It.IsAny<SentryHint?>()))
                .Callback<SentryEvent, Scope?, SentryHint?>((evt, _, _) => CapturedEvents.Add(evt))
                .Returns(SentryId.Empty);

            hubMock.Setup(h => h.ConfigureScope(It.IsAny<Action<Scope>>()))
                .Callback<Action<Scope>>(action =>
                {
                    var scope = new Scope(new SentryOptions { Dsn = Dsn });
                    action(scope);
                    foreach (var (k, v) in scope.Tags)
                        CapturedTags[k] = v;
                });

            services.AddSingleton<IHub>(hubMock.Object);
        });
    }
}
