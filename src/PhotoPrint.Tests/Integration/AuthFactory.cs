using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PhotoPrint.API.Data;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services;
using PhotoPrint.Tests.Helpers;

namespace PhotoPrint.Tests.Integration;

/// <summary>
/// WebApplicationFactory for auth-endpoint integration tests.
/// Uses InMemory DB, test JWT key, and replaces SMTP with a no-op sender.
/// </summary>
public class AuthFactory : WebApplicationFactory<Program>
{
    // Fixed name so every request scope shares the same InMemory store
    private readonly string _dbName = $"AuthTests_{Guid.NewGuid():N}";
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

            // Suppress all outbound email (fire-and-forget won't cause side-effects)
            services.AddScoped<IEmailService, NoOpEmailService>();
        });
    }

    /// <summary>Seeds a confirmed user and returns their credentials.</summary>
    public async Task<(Guid userId, string email, string password)> SeedConfirmedUserAsync(
        string password = "Test@1234!")
    {
        var email = $"user-{Guid.NewGuid():N}@example.com";

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PhotoPrintDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();

        var user = new User
        {
            Email = email.ToLowerInvariant(),
            NormalizedEmail = email.ToUpperInvariant(),
            FirstName = "Test",
            LastName = "User",
            IsEmailConfirmed = true,
            GdprConsentAccepted = true,
        };
        user.PasswordHash = hasher.HashPassword(user, password);
        db.Users.Add(user);
        await db.SaveChangesAsync();

        return (user.Id, email, password);
    }

    /// <summary>Seeds an unconfirmed user and returns their credentials.</summary>
    public async Task<(Guid userId, string email, string password)> SeedUnconfirmedUserAsync(
        string password = "Test@1234!")
    {
        var email = $"unconfirmed-{Guid.NewGuid():N}@example.com";

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PhotoPrintDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();

        var user = new User
        {
            Email = email.ToLowerInvariant(),
            NormalizedEmail = email.ToUpperInvariant(),
            FirstName = "Unconfirmed",
            LastName = "User",
            IsEmailConfirmed = false,
            GdprConsentAccepted = true,
        };
        user.PasswordHash = hasher.HashPassword(user, password);
        db.Users.Add(user);
        await db.SaveChangesAsync();

        return (user.Id, email, password);
    }
}

/// <summary>No-op email service for integration tests — swallows all send calls.</summary>
file class NoOpEmailService : IEmailService
{
    public Task SendAsync(string to, string subject, string htmlBody,
        CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task SendTemplatedAsync<T>(string to, string subject, string templateName, T model,
        CancellationToken cancellationToken = default) => Task.CompletedTask;
}
