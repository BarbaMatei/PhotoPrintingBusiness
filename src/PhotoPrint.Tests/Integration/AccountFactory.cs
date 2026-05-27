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
/// WebApplicationFactory for account-endpoint integration tests.
/// </summary>
public class AccountFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"AccountTests_{Guid.NewGuid():N}";

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
            var dbDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<PhotoPrintDbContext>));
            if (dbDescriptor is not null)
                services.Remove(dbDescriptor);

            var dbName = _dbName;
            services.AddDbContext<PhotoPrintDbContext>(options =>
                options.UseInMemoryDatabase(dbName));

            services.AddScoped<IEmailService, NoOpEmailService>();
        });
    }

    /// <summary>Seeds a confirmed user and returns their JWT access token.</summary>
    public async Task<(Guid userId, string accessToken)> SeedAndLoginAsync(
        string password = "Test@1234!")
    {
        var email = $"user-{Guid.NewGuid():N}@example.com";

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PhotoPrintDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();

        var user = new User
        {
            Email = email.ToLowerInvariant(),
            NormalizedEmail = email.ToUpperInvariant(),
            FirstName = "Test",
            LastName = "User",
            Phone = "0712345678",
            IsEmailConfirmed = true,
            GdprConsentAccepted = true,
        };
        user.PasswordHash = hasher.HashPassword(user, password);
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var token = tokenService.GenerateAccessToken(user);
        return (user.Id, token);
    }
}

/// <summary>No-op email service for account integration tests.</summary>
file class NoOpEmailService : PhotoPrint.API.Services.IEmailService
{
    public Task SendAsync(string to, string subject, string htmlBody,
        CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task SendTemplatedAsync<T>(string to, string subject, string templateName, T model,
        CancellationToken cancellationToken = default) => Task.CompletedTask;
}
