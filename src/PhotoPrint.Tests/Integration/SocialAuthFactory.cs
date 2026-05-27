using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PhotoPrint.API.Data;
using PhotoPrint.API.Exceptions;
using PhotoPrint.API.Services;
using PhotoPrint.Tests.Helpers;

namespace PhotoPrint.Tests.Integration;

/// <summary>
/// WebApplicationFactory for social-auth integration tests.
/// Replaces IGoogleTokenValidator with a configurable fake so no real HTTP calls go to Google.
/// </summary>
public class SocialAuthFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"SocialAuthTests_{Guid.NewGuid():N}";

    /// <summary>Configure this before each test to control what Google validates.</summary>
    public FakeGoogleTokenValidator GoogleValidator { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins"] = "https://test.example.com",
                ["RateLimit:WindowSeconds"] = "60",
                ["RateLimit:Public:PermitLimit"] = "100",
                ["RateLimit:Auth:PermitLimit"] = "50",
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
                ["GoogleAuth:ClientId"] = "test-client-id.apps.googleusercontent.com",
            });
        });

        builder.ConfigureServices(services =>
        {
            // Replace PostgreSQL with InMemory
            var dbDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<PhotoPrintDbContext>));
            if (dbDescriptor is not null)
                services.Remove(dbDescriptor);

            var dbName = _dbName;
            services.AddDbContext<PhotoPrintDbContext>(options =>
                options.UseInMemoryDatabase(dbName));

            // Replace IGoogleTokenValidator with controllable fake (singleton so tests can configure it)
            var validatorDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IGoogleTokenValidator));
            if (validatorDescriptor is not null)
                services.Remove(validatorDescriptor);

            services.AddSingleton<IGoogleTokenValidator>(GoogleValidator);

            // Suppress all outbound email
            services.AddScoped<IEmailService, NoOpEmailService>();
        });
    }
}

/// <summary>
/// Controllable fake validator. Set Payload for success or leave null for 401. Set Unreachable for 502.
/// </summary>
public class FakeGoogleTokenValidator : IGoogleTokenValidator
{
    public GooglePayload? Payload { get; set; }
    public bool Unreachable { get; set; }

    public Task<GooglePayload> ValidateAsync(string idToken, CancellationToken ct = default)
    {
        if (Unreachable)
            throw new BadGatewayException("Serviciu extern indisponibil.");

        if (Payload is null)
            throw new UnauthorizedException("Autentificarea Google a eșuat.");

        return Task.FromResult(Payload);
    }
}

file class NoOpEmailService : IEmailService
{
    public Task SendAsync(string to, string subject, string htmlBody,
        CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task SendTemplatedAsync<T>(string to, string subject, string templateName, T model,
        CancellationToken cancellationToken = default) => Task.CompletedTask;
}
