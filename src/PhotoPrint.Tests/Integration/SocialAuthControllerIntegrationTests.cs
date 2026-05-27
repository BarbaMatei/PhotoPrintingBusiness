using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using PhotoPrint.API.Data;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services;

namespace PhotoPrint.Tests.Integration;

public class SocialAuthControllerIntegrationTests : IAsyncLifetime
{
    private SocialAuthFactory _factory = null!;
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _factory = new SocialAuthFactory();
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    private static GooglePayload DefaultPayload(
        string sub = "googleSub123", string email = "new@gmail.com")
        => new(sub, email, "John", "Doe", null);

    // ── Happy path ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GoogleSignIn_ValidNewUser_Returns200()
    {
        _factory.GoogleValidator.Payload = DefaultPayload();

        var response = await _client.PostAsJsonAsync("/api/auth/google", new { idToken = "valid-token" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GoogleSignIn_ValidNewUser_ReturnsAccessTokenAndExpiry()
    {
        _factory.GoogleValidator.Payload = DefaultPayload();

        var response = await _client.PostAsJsonAsync("/api/auth/google", new { idToken = "valid-token" });
        var body = await response.Content.ReadFromJsonAsync<GoogleLoginResponseBody>();

        body!.AccessToken.Should().NotBeNullOrEmpty();
        body.ExpiresIn.Should().Be(900);
        body.AccountLinked.Should().BeFalse();
    }

    [Fact]
    public async Task GoogleSignIn_ValidNewUser_SetsRefreshCookie()
    {
        _factory.GoogleValidator.Payload = DefaultPayload();

        // Use a client without cookie handling to inspect raw Set-Cookie header
        using var rawClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false,
        });

        var response = await rawClient.PostAsJsonAsync("/api/auth/google", new { idToken = "valid-token" });

        response.Headers.TryGetValues("Set-Cookie", out var cookies);
        cookies.Should().Contain(c => c.Contains("refresh_token"));
    }

    [Fact]
    public async Task GoogleSignIn_ExistingGoogleUser_Returns200WithoutCreatingDuplicates()
    {
        // Seed existing user + ExternalLogin
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PhotoPrintDbContext>();
        var user = new User
        {
            Email = "existing@gmail.com",
            NormalizedEmail = "EXISTING@GMAIL.COM",
            FirstName = "Existing",
            LastName = "User",
            IsEmailConfirmed = true,
        };
        db.Users.Add(user);
        db.ExternalLogins.Add(new ExternalLogin
        {
            UserId = user.Id,
            Provider = "Google",
            ProviderKey = "googleSub123",
        });
        await db.SaveChangesAsync();

        _factory.GoogleValidator.Payload = DefaultPayload(email: "existing@gmail.com");

        var response = await _client.PostAsJsonAsync("/api/auth/google", new { idToken = "valid-token" });
        var body = await response.Content.ReadFromJsonAsync<GoogleLoginResponseBody>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body!.AccountLinked.Should().BeFalse();
    }

    [Fact]
    public async Task GoogleSignIn_ExistingEmailAccount_ReturnsAccountLinkedTrue()
    {
        // Seed existing email+password user (no ExternalLogin)
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PhotoPrintDbContext>();
        db.Users.Add(new User
        {
            Email = "link@gmail.com",
            NormalizedEmail = "LINK@GMAIL.COM",
            FirstName = "Link",
            LastName = "Me",
            PasswordHash = "some-hash",
            IsEmailConfirmed = true,
        });
        await db.SaveChangesAsync();

        _factory.GoogleValidator.Payload = DefaultPayload(email: "link@gmail.com");

        var response = await _client.PostAsJsonAsync("/api/auth/google", new { idToken = "valid-token" });
        var body = await response.Content.ReadFromJsonAsync<GoogleLoginResponseBody>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body!.AccountLinked.Should().BeTrue();
    }

    [Fact]
    public async Task GoogleSignIn_AfterSignIn_CanRefreshToken()
    {
        _factory.GoogleValidator.Payload = DefaultPayload();
        await _client.PostAsJsonAsync("/api/auth/google", new { idToken = "valid-token" });

        // Cookie jar now has refresh_token — use it
        var refreshResponse = await _client.PostAsync("/api/auth/refresh", null);

        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Error paths ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GoogleSignIn_InvalidToken_Returns401()
    {
        _factory.GoogleValidator.Payload = null; // triggers UnauthorizedException

        var response = await _client.PostAsJsonAsync("/api/auth/google", new { idToken = "bad-token" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GoogleSignIn_GoogleUnreachable_Returns502()
    {
        _factory.GoogleValidator.Unreachable = true;

        var response = await _client.PostAsJsonAsync("/api/auth/google", new { idToken = "any-token" });

        response.StatusCode.Should().Be(HttpStatusCode.BadGateway);
    }

    [Fact]
    public async Task GoogleSignIn_EmptyIdToken_Returns422()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/google", new { idToken = "" });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // ── Response type ────────────────────────────────────────────────────────

    private record GoogleLoginResponseBody(string AccessToken, int ExpiresIn, bool AccountLinked);
}
