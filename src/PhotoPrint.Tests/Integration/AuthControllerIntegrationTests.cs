using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PhotoPrint.API.Data;
using PhotoPrint.API.Services;

namespace PhotoPrint.Tests.Integration;

public class AuthControllerIntegrationTests : IAsyncLifetime
{
    private readonly AuthFactory _factory = new();
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    // ── Register ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_ValidRequest_Returns201()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            firstName = "Ion",
            lastName = "Popescu",
            email = $"new-{Guid.NewGuid():N}@example.com",
            password = "Test@12345",
            confirmPassword = "Test@12345",
            gdprConsentAccepted = true,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var email = $"dup-{Guid.NewGuid():N}@example.com";
        var payload = new
        {
            firstName = "Ion",
            lastName = "Popescu",
            email,
            password = "Test@12345",
            confirmPassword = "Test@12345",
            gdprConsentAccepted = true,
        };

        await _client.PostAsJsonAsync("/api/auth/register", payload);
        var response = await _client.PostAsJsonAsync("/api/auth/register", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_MissingEmail_Returns422()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            firstName = "Ion",
            lastName = "Popescu",
            email = "",
            password = "Test@12345",
            confirmPassword = "Test@12345",
            gdprConsentAccepted = true,
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Register_PasswordTooShort_Returns422()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            firstName = "Ion",
            lastName = "Popescu",
            email = $"short-{Guid.NewGuid():N}@example.com",
            password = "Short1!",
            confirmPassword = "Short1!",
            gdprConsentAccepted = true,
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Register_GdprNotAccepted_Returns422()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            firstName = "Ion",
            lastName = "Popescu",
            email = $"gdpr-{Guid.NewGuid():N}@example.com",
            password = "Test@12345",
            confirmPassword = "Test@12345",
            gdprConsentAccepted = false,
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // ── Login ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_ValidConfirmedUser_Returns200WithAccessToken()
    {
        var (_, email, password) = await _factory.SeedConfirmedUserAsync();

        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<LoginResponseBody>();
        body!.AccessToken.Should().NotBeNullOrEmpty();
        body.ExpiresIn.Should().Be(900); // 15 min × 60
    }

    [Fact]
    public async Task Login_ValidConfirmedUser_SetsRefreshTokenCookie()
    {
        var (_, email, password) = await _factory.SeedConfirmedUserAsync();

        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password,
        });

        response.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();
        cookies!.Should().Contain(c => c.Contains("refresh_token"));
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var (_, email, _) = await _factory.SeedConfirmedUserAsync();

        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "WrongPassword@99",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_UnconfirmedUser_Returns403()
    {
        var (_, email, password) = await _factory.SeedUnconfirmedUserAsync();

        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Login_InvalidEmailFormat_Returns422()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "not-an-email",
            password = "Password@1",
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // ── Refresh ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Refresh_NoCookie_Returns401()
    {
        var response = await _client.PostAsync("/api/auth/refresh", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_AfterLogin_Returns200WithNewToken()
    {
        var (_, email, password) = await _factory.SeedConfirmedUserAsync();

        // Login to obtain the refresh-token cookie (HttpClient handles Set-Cookie automatically)
        await _client.PostAsJsonAsync("/api/auth/login", new { email, password });

        var refreshResponse = await _client.PostAsync("/api/auth/refresh", null);

        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await refreshResponse.Content.ReadFromJsonAsync<LoginResponseBody>();
        body!.AccessToken.Should().NotBeNullOrEmpty();
    }

    // ── Logout ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Logout_Returns204()
    {
        var response = await _client.PostAsync("/api/auth/logout", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // ── Confirm email ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ConfirmEmail_InvalidToken_Returns401()
    {
        // Seed a user with no confirmation tokens — service will get null token → 401
        var (userId, _, _) = await _factory.SeedUnconfirmedUserAsync();

        var response = await _client.GetAsync(
            $"/api/auth/confirm-email?userId={userId}&token=not-a-valid-token");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ConfirmEmail_ValidToken_Returns204()
    {
        // Register a user (creates confirmation token)
        var email = $"confirm-{Guid.NewGuid():N}@example.com";
        await _client.PostAsJsonAsync("/api/auth/register", new
        {
            firstName = "Ion",
            lastName = "Pop",
            email,
            password = "Test@12345",
            confirmPassword = "Test@12345",
            gdprConsentAccepted = true,
        });

        // Retrieve token from DB (bypassing email)
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PhotoPrintDbContext>();
        var emailTokenSvc = new EmailTokenService();

        var user = await db.Users.FirstAsync(u => u.Email == email.ToLowerInvariant());
        var tokenEntity = await db.EmailConfirmationTokens
            .FirstAsync(t => t.UserId == user.Id);

        // We stored the hash but not the raw token; we need to re-generate a matching pair.
        // This test verifies the full flow by using the API to register, then directly
        // calling confirm-email with a seeded known token.
        var (raw, hash) = emailTokenSvc.GenerateEmailToken();
        tokenEntity.TokenHash = hash;
        tokenEntity.ExpiresAt = DateTimeOffset.UtcNow.AddHours(24);
        await db.SaveChangesAsync();

        var response = await _client.GetAsync(
            $"/api/auth/confirm-email?userId={user.Id}&token={Uri.EscapeDataString(raw)}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // ── Forgot / Reset password ───────────────────────────────────────────────

    [Fact]
    public async Task ForgotPassword_AnyEmail_Returns204()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/forgot-password", new
        {
            email = "nonexistent@example.com",
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ResetPassword_InvalidToken_Returns401()
    {
        // Seed a confirmed user (no reset tokens in DB) — service will get null token → 401
        var (userId, _, _) = await _factory.SeedConfirmedUserAsync();

        var response = await _client.PostAsJsonAsync("/api/auth/reset-password", new
        {
            userId,
            token = "invalid-token",
            newPassword = "NewTest@12345",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ResendConfirmation_AnyEmail_Returns204()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/resend-confirmation", new
        {
            email = "nobody@example.com",
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // ── DTO for deserialization ───────────────────────────────────────────────

    private record LoginResponseBody(string AccessToken, int ExpiresIn);
}
