using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
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
/// Verifies the cloud-tier branch of <c>GET /api/uploads/{id}/preview</c> (bolt 043, story 002):
/// a Cloud-located upload returns <c>302 Found</c> with <c>Location</c> pointing at a
/// pre-signed URL and <c>Cache-Control: private, max-age=3600</c>. Authorization runs in the
/// service before any URL is generated, so an unauthorized caller never sees the URL.
/// </summary>
/// <remarks>
/// Uses an in-memory "fake cloud" adapter (capability flag = true, deterministic URL).
/// The real S3 round-trip is exercised by <see cref="S3StorageServiceIntegrationTests"/>.
/// </remarks>
public class CloudPreviewIntegrationTests : IAsyncLifetime
{
    private CloudUploadFactory _factory = null!;
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _factory = new CloudUploadFactory();
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,   // we want to see the 302, not follow it
        });
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    // ── Happy path: Cloud upload → 302 + presigned URL ────────────────────────

    [Fact]
    public async Task GetPreview_CloudUpload_Returns302WithPresignedUrl()
    {
        var (userId, token) = await _factory.SeedUserWithJwtAsync();
        var upload = await _factory.SeedCloudUploadAsync(userId: userId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync($"/api/uploads/{upload.Id}/preview");

        response.StatusCode.Should().Be(HttpStatusCode.Found);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString()
            .Should().Contain($"thumbs/{upload.Id:N}.jpg")
            .And.Contain("sig="); // fake cloud's deterministic signature marker
    }

    [Fact]
    public async Task GetPreview_CloudUpload_SetsPrivateMaxAgeOneHour()
    {
        var (userId, token) = await _factory.SeedUserWithJwtAsync();
        var upload = await _factory.SeedCloudUploadAsync(userId: userId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync($"/api/uploads/{upload.Id}/preview");

        // 'private' so intermediate caches don't share one user's signed URL with another;
        // 1 h matches the presign TTL so a stale URL is naturally refetched.
        var cache = response.Headers.CacheControl;
        cache!.Private.Should().BeTrue();
        cache.MaxAge.Should().Be(TimeSpan.FromHours(1));
    }

    // ── Authz runs BEFORE any URL is issued ───────────────────────────────────

    [Fact]
    public async Task GetPreview_CloudUpload_DifferentUser_Returns403_NeverIssuesUrl()
    {
        var (ownerUserId, _) = await _factory.SeedUserWithJwtAsync();
        var (_, attackerToken) = await _factory.SeedUserWithJwtAsync();
        var upload = await _factory.SeedCloudUploadAsync(userId: ownerUserId);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", attackerToken);

        var response = await _client.GetAsync($"/api/uploads/{upload.Id}/preview");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Location.Should().BeNull();
    }

    [Fact]
    public async Task GetPreview_CloudUpload_NoAuth_Returns401()
    {
        var (userId, _) = await _factory.SeedUserWithJwtAsync();
        var upload = await _factory.SeedCloudUploadAsync(userId: userId);

        var response = await _client.GetAsync($"/api/uploads/{upload.Id}/preview");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Location.Should().BeNull();
    }
}

/// <summary>
/// Variant of <see cref="UploadFactory"/> that wires a fake "cloud" adapter (keyed <c>"cloud"</c>)
/// alongside the local one — so the router has both tiers and the controller can branch.
/// </summary>
public class CloudUploadFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"CloudUploadTests_{Guid.NewGuid():N}";
    private readonly FakeStorageService _localFake = new();
    private readonly FakeCloudStorageService _cloudFake = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins"]                = "https://test.example.com",
                ["RateLimit:WindowSeconds"]             = "60",
                ["RateLimit:Public:PermitLimit"]        = "100",
                ["RateLimit:Auth:PermitLimit"]          = "50",
                ["SecurityHeaders:ContentSecurityPolicy"] = "default-src 'self'",
                ["Email:Provider"]                      = "Smtp",
                ["Email:FromAddress"]                   = "test@fototipar.ro",
                ["Email:FromName"]                      = "FotoTipar Test",
                ["Email:OperatorBcc"]                   = "",
                ["Email:Smtp:Host"]                     = "localhost",
                ["Email:Smtp:Port"]                     = "1025",
                ["Email:Smtp:UseSsl"]                   = "false",
                ["ConnectionStrings:Default"]           = "Host=localhost;Database=test;Username=test;Password=test",
                ["HealthCheck:UploadsPath"]             = "uploads",
                ["JwtSettings:PrivateKeyPem"]           = TestKeys.RsaPrivateKeyPem,
                ["JwtSettings:Issuer"]                  = "fototipar",
                ["JwtSettings:Audience"]                = "fototipar-spa",
                ["JwtSettings:AccessTokenMinutes"]      = "15",
                ["JwtSettings:RefreshTokenDays"]        = "30",
                ["App:BaseUrl"]                         = "http://localhost:4200",
                ["GoogleAuth:ClientId"]                 = "test-client-id.apps.googleusercontent.com",
                ["Storage:Provider"]                    = "S3",      // pretend the cloud tier is on
                ["Storage:BasePath"]                    = "/tmp/test-uploads",
                ["Storage:Bucket"]                      = "test-bucket",
                ["Storage:Region"]                      = "us-east-1",
                ["Storage:AccessKey"]                   = "test-key",
                ["Storage:SecretKey"]                   = "test-secret",
                ["Storage:PresignTtlMinutes"]           = "60",
            });
        });

        builder.ConfigureServices(services =>
        {
            var dbDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<PhotoPrintDbContext>));
            if (dbDescriptor is not null)
                services.Remove(dbDescriptor);
            services.AddDbContext<PhotoPrintDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));

            // Replace EVERY IStorageService registration with the local + cloud fakes.
            var storageDescriptors = services
                .Where(d => d.ServiceType == typeof(IStorageService))
                .ToList();
            foreach (var d in storageDescriptors)
                services.Remove(d);

            services.AddSingleton<IStorageService>(_localFake);
            services.AddKeyedSingleton<IStorageService>("local", _localFake);
            services.AddKeyedSingleton<IStorageService>("cloud", _cloudFake);

            // Drop the real S3BucketVerifier (it'd try to connect at boot).
            var verifier = services.SingleOrDefault(d => d.ImplementationType == typeof(S3BucketVerifier));
            if (verifier is not null) services.Remove(verifier);

            // Drop the real IAmazonS3 (not needed; cloud fake replaces it).
            var s3 = services.SingleOrDefault(d => d.ServiceType.FullName == "Amazon.S3.IAmazonS3");
            if (s3 is not null) services.Remove(s3);

            // Replace image processor with the same fixed-dims fake used elsewhere.
            var imageProcessorDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IImageProcessor));
            if (imageProcessorDescriptor is not null)
                services.Remove(imageProcessorDescriptor);
            services.AddScoped<IImageProcessor, FakeImageProcessor>();

            services.AddScoped<IEmailService, UploadNoOpEmailService>();
        });
    }

    public async Task<(Guid userId, string bearerToken)> SeedUserWithJwtAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PhotoPrintDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();

        var user = new User
        {
            Email               = $"cloud-{Guid.NewGuid():N}@example.com",
            NormalizedEmail     = $"CLOUD-{Guid.NewGuid():N}@EXAMPLE.COM",
            FirstName           = "Cloud",
            LastName            = "Tester",
            IsEmailConfirmed    = true,
            GdprConsentAccepted = true,
        };
        user.PasswordHash = hasher.HashPassword(user, "Test@1234!");
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return (user.Id, CloudJwtHelper.GenerateBearerToken(user.Id));
    }

    /// <summary>
    /// Seeds an Upload row in the Cloud tier with a thumbnail already present in the
    /// cloud fake's store, modelling a fully-promoted (post-payment) upload.
    /// </summary>
    public async Task<Upload> SeedCloudUploadAsync(Guid userId)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PhotoPrintDbContext>();

        var upload = new Upload
        {
            UserId           = userId,
            FilePath         = "uploads/2026/05/cloud-original.jpg",
            ThumbnailPath    = null,    // will be set by the controller on first preview
            StorageLocation  = StorageLocation.Cloud,
            OriginalFileName = "cloud.jpg",
            ContentType      = "image/jpeg",
            WidthPx          = 800,
            HeightPx         = 600,
            FileSizeBytes    = 1024,
        };
        db.Uploads.Add(upload);
        await db.SaveChangesAsync();

        // Place a dummy "original" + "thumbnail" in the cloud fake so the controller doesn't
        // need to regenerate the thumb (cleaner assertion on the URL).
        _cloudFake.Store(upload.FilePath, [0xFF, 0xD8, 0xFF, 0xE0]);
        var thumbKey = $"thumbs/{upload.Id:N}.jpg";
        _cloudFake.Store(thumbKey, [0xFF, 0xD8, 0xFF, 0xE0]);

        // Pre-set ThumbnailPath so GetPreview hits the cache path (no regen → cleaner test).
        upload.ThumbnailPath = thumbKey;
        await db.SaveChangesAsync();

        return upload;
    }
}

/// <summary>
/// Cloud-tier fake — same in-memory store as <see cref="FakeStorageService"/> but with the
/// capability flag set and a deterministic presigned URL (so tests can assert on it).
/// </summary>
internal class FakeCloudStorageService : IStorageService
{
    private readonly Dictionary<string, byte[]> _store = new(StringComparer.Ordinal);

    public bool SupportsPresignedUrls => true;

    public Task SaveAsync(Stream content, string key, CancellationToken ct = default)
    {
        if (content.CanSeek) content.Position = 0;
        using var ms = new MemoryStream();
        content.CopyTo(ms);
        _store[key] = ms.ToArray();
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string key, CancellationToken ct = default)
    {
        _store.Remove(key);
        return Task.CompletedTask;
    }

    public Task<Stream> GetStreamAsync(string key, CancellationToken ct = default)
    {
        if (!_store.TryGetValue(key, out var bytes))
            throw new FileNotFoundException("Stored object not found.", key);
        return Task.FromResult<Stream>(new MemoryStream(bytes));
    }

    public Task<bool> ExistsAsync(string key, CancellationToken ct = default)
        => Task.FromResult(_store.ContainsKey(key));

    public Task<string> GetPresignedUrlAsync(string key, TimeSpan ttl, CancellationToken ct = default)
        => Task.FromResult($"https://fake-cdn.test/{key}?sig=test&exp={(int)ttl.TotalSeconds}");

    public void Store(string key, byte[] bytes) => _store[key] = bytes;
}

/// <summary>JWT helper duplicated locally because the existing one in UploadFactory is `file`-scoped.</summary>
file static class CloudJwtHelper
{
    public static string GenerateBearerToken(Guid userId)
    {
        var rsa = System.Security.Cryptography.RSA.Create();
        rsa.ImportFromPem(TestKeys.RsaPrivateKeyPem);

        var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(
            new Microsoft.IdentityModel.Tokens.RsaSecurityKey(rsa),
            Microsoft.IdentityModel.Tokens.SecurityAlgorithms.RsaSha256);

        var claims = new[]
        {
            new System.Security.Claims.Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub, userId.ToString()),
            new System.Security.Claims.Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email, "cloud@test.com"),
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
