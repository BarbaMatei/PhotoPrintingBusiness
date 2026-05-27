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
/// WebApplicationFactory for upload-endpoint integration tests.
/// Uses InMemory DB, replaces IStorageService and IImageProcessor with in-memory fakes.
/// </summary>
public class UploadFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"UploadTests_{Guid.NewGuid():N}";
    // Singleton fakes so that file state persists across scoped requests
    private readonly FakeStorageService _fakeStorage = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins"]               = "https://test.example.com",
                ["RateLimit:WindowSeconds"]            = "60",
                ["RateLimit:Public:PermitLimit"]       = "100",
                ["RateLimit:Auth:PermitLimit"]         = "50",
                ["SecurityHeaders:ContentSecurityPolicy"] = "default-src 'self'",
                ["Email:Provider"]                     = "Smtp",
                ["Email:FromAddress"]                  = "test@fototipar.ro",
                ["Email:FromName"]                     = "FotoTipar Test",
                ["Email:OperatorBcc"]                  = "",
                ["Email:Smtp:Host"]                    = "localhost",
                ["Email:Smtp:Port"]                    = "1025",
                ["Email:Smtp:UseSsl"]                  = "false",
                ["ConnectionStrings:Default"]          = "Host=localhost;Database=test;Username=test;Password=test",
                ["HealthCheck:UploadsPath"]            = "uploads",
                ["JwtSettings:PrivateKeyPem"]          = TestKeys.RsaPrivateKeyPem,
                ["JwtSettings:Issuer"]                 = "fototipar",
                ["JwtSettings:Audience"]               = "fototipar-spa",
                ["JwtSettings:AccessTokenMinutes"]     = "15",
                ["JwtSettings:RefreshTokenDays"]       = "30",
                ["App:BaseUrl"]                        = "http://localhost:4200",
                ["GoogleAuth:ClientId"]                = "test-client-id.apps.googleusercontent.com",
                ["Storage:BasePath"]                   = "/tmp/test-uploads",
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

            // Replace storage with singleton in-memory fake
            var storageDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IStorageService));
            if (storageDescriptor is not null)
                services.Remove(storageDescriptor);
            services.AddSingleton<IStorageService>(_fakeStorage);

            // Replace image processor with a fake that returns fixed dimensions
            var imageProcessorDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IImageProcessor));
            if (imageProcessorDescriptor is not null)
                services.Remove(imageProcessorDescriptor);
            services.AddScoped<IImageProcessor, FakeImageProcessor>();

            // Suppress all outbound email
            services.AddScoped<IEmailService, UploadNoOpEmailService>();
        });
    }

    /// <summary>Seeds a confirmed user and returns their ID and a signed JWT.</summary>
    public async Task<(Guid userId, string bearerToken)> SeedUserWithJwtAsync()
    {
        using var scope = Services.CreateScope();
        var db   = scope.ServiceProvider.GetRequiredService<PhotoPrintDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();

        var user = new User
        {
            Email              = $"upload-{Guid.NewGuid():N}@example.com",
            NormalizedEmail    = $"UPLOAD-{Guid.NewGuid():N}@EXAMPLE.COM",
            FirstName          = "Upload",
            LastName           = "Tester",
            IsEmailConfirmed   = true,
            GdprConsentAccepted = true,
        };
        user.PasswordHash = hasher.HashPassword(user, "Test@1234!");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        return (user.Id, JwtHelper.GenerateBearerToken(user.Id));
    }

    /// <summary>Seeds a valid guest session and returns its token (Guid used as X-Guest-Token).</summary>
    public async Task<Guid> SeedGuestTokenAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PhotoPrintDbContext>();

        var session = new GuestSession
        {
            Email     = "guest@test.com",
            FirstName = "Upload",
            LastName  = "Guest",
            Phone     = "0712345678",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
        };
        db.GuestSessions.Add(session);
        await db.SaveChangesAsync();
        return session.Id;
    }

    /// <summary>Seeds an Upload row directly and returns it (for testing preview access control).</summary>
    public async Task<Upload> SeedUploadAsync(Guid? userId = null, Guid? guestSessionId = null)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PhotoPrintDbContext>();

        var upload = new Upload
        {
            UserId           = userId,
            GuestSessionId   = guestSessionId,
            FilePath         = "seed/preview.jpg",
            OriginalFileName = "preview.jpg",
            ContentType      = "image/jpeg",
            WidthPx          = 800,
            HeightPx         = 600,
            FileSizeBytes    = 1024,
        };
        db.Uploads.Add(upload);
        await db.SaveChangesAsync();

        // Store dummy bytes so FakeStorageService can serve the preview
        _fakeStorage.Store(upload.FilePath, [0xFF, 0xD8, 0xFF, 0xE0]);

        return upload;
    }
}

// ── JWT helper (shared with existing tests) ────────────────────────────────────
file static class JwtHelper
{
    public static string GenerateBearerToken(Guid userId)
    {
        // Reuse the same logic from GuestSessionControllerIntegrationTests
        // Do NOT use `using var` — RSA must outlive the method so the JWT key
        // cache in the middleware does not receive a disposed key reference.
        var rsa = System.Security.Cryptography.RSA.Create();
        rsa.ImportFromPem(TestKeys.RsaPrivateKeyPem);

        var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(
            new Microsoft.IdentityModel.Tokens.RsaSecurityKey(rsa),
            Microsoft.IdentityModel.Tokens.SecurityAlgorithms.RsaSha256);

        var claims = new[]
        {
            new System.Security.Claims.Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub, userId.ToString()),
            new System.Security.Claims.Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email, "uploader@test.com"),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "User"),
            new System.Security.Claims.Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: "fototipar",
            audience: "fototipar-spa",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: creds);

        return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
    }
}

// ── Fake implementations ──────────────────────────────────────────────────────

/// <summary>
/// In-memory storage service. Registered as Singleton so that file state
/// persists across scoped HTTP requests within the same factory instance.
/// </summary>
internal class FakeStorageService : IStorageService
{
    private readonly Dictionary<string, byte[]> _store = new(StringComparer.Ordinal);

    public Task<string> SaveAsync(Stream stream, Guid ownerId, string extension, CancellationToken ct = default, Guid? fileId = null)
    {
        var id = fileId ?? Guid.NewGuid();
        var path = $"{ownerId:N}/{id:N}.{extension}";
        stream.Position = 0;
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        _store[path] = ms.ToArray();
        return Task.FromResult(path);
    }

    public Task DeleteAsync(string storagePath, CancellationToken ct = default)
    {
        _store.Remove(storagePath);
        return Task.CompletedTask;
    }

    public Task<Stream> GetStreamAsync(string storagePath, CancellationToken ct = default)
    {
        if (!_store.TryGetValue(storagePath, out var bytes))
            throw new FileNotFoundException("Stored upload not found.", storagePath);

        return Task.FromResult<Stream>(new MemoryStream(bytes));
    }

    public Task<bool> ExistsAsync(string storagePath, CancellationToken ct = default)
        => Task.FromResult(_store.ContainsKey(storagePath));

    /// <summary>Directly stores bytes for a given path (used when seeding upload rows).</summary>
    public void Store(string path, byte[] bytes) => _store[path] = bytes;
}

/// <summary>Always returns ImageInfo(800, 600) and a minimal JPEG thumbnail.</summary>
internal class FakeImageProcessor : IImageProcessor
{
    private static readonly byte[] ThumbnailBytes = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x01];

    public Task<ImageInfo?> GetInfoAsync(string storagePath, CancellationToken ct = default)
        => Task.FromResult<ImageInfo?>(new ImageInfo(800, 600));

    public Task<MemoryStream> GenerateThumbnailAsync(string storagePath, CancellationToken ct = default)
        => Task.FromResult(new MemoryStream(ThumbnailBytes));
}

internal class UploadNoOpEmailService : IEmailService
{
    public Task SendAsync(string to, string subject, string htmlBody,
        CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task SendTemplatedAsync<T>(string to, string subject, string templateName, T model,
        CancellationToken cancellationToken = default) => Task.CompletedTask;
}
