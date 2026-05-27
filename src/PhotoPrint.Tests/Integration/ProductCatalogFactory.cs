using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PhotoPrint.API.Data;
using PhotoPrint.API.Services;
using PhotoPrint.Tests.Helpers;

namespace PhotoPrint.Tests.Integration;

/// <summary>
/// WebApplicationFactory for product catalog integration tests.
/// Uses InMemory DB and calls EnsureCreated() to apply HasData seed.
/// </summary>
public class ProductCatalogFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"ProductCatalogTests_{Guid.NewGuid():N}";

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

            // Suppress all outbound email
            services.AddScoped<IEmailService, CatalogNoOpEmailService>();
        });
    }

    /// <summary>
    /// Applies EnsureCreated so the schema exists, then runs the catalog seed.
    /// Call this once in IAsyncLifetime.InitializeAsync.
    /// </summary>
    public async Task SeedAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PhotoPrintDbContext>();
        await db.Database.EnsureCreatedAsync();
        await PhotoPrint.API.Data.Seed.ProductCatalogSeed.ApplyAsync(db);
    }
}

file class CatalogNoOpEmailService : IEmailService
{
    public Task SendAsync(string to, string subject, string htmlBody,
        CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task SendTemplatedAsync<T>(string to, string subject, string templateName, T model,
        CancellationToken cancellationToken = default) => Task.CompletedTask;
}
