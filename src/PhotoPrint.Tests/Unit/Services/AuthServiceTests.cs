using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Data;
using PhotoPrint.API.DTOs.Auth;
using PhotoPrint.API.Exceptions;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services;
using PhotoPrint.Tests.Helpers;

namespace PhotoPrint.Tests.Unit.Services;

public class AuthServiceTests
{
    // ── Fixtures ──────────────────────────────────────────────────────────────

    private static PhotoPrintDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<PhotoPrintDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static AuthService CreateSut(
        PhotoPrintDbContext? db = null,
        ITokenService? tokenService = null,
        IEmailTokenService? emailTokenService = null,
        IEmailService? emailService = null)
    {
        db ??= CreateDb();

        var tokenSvc = tokenService ?? CreateTokenServiceMock().Object;
        var emailTokenSvc = emailTokenService ?? new EmailTokenService();
        var emailSvc = emailService ?? Mock.Of<IEmailService>();

        return new AuthService(
            db,
            tokenSvc,
            emailTokenSvc,
            emailSvc,
            new PasswordHasher<User>(),
            Options.Create(new JwtSettings
            {
                PrivateKeyPem = TestKeys.RsaPrivateKeyPem,
                AccessTokenMinutes = 15,
                RefreshTokenDays = 30,
            }),
            Options.Create(new AppSettings { BaseUrl = "http://localhost:4200" }),
            NullLogger<AuthService>.Instance);
    }

    private static Mock<ITokenService> CreateTokenServiceMock()
    {
        var mock = new Mock<ITokenService>();
        mock.Setup(t => t.GenerateAccessToken(It.IsAny<User>())).Returns("access-token");
        mock.Setup(t => t.GenerateRefreshToken()).Returns(("raw-refresh", TokenService.HashToken("raw-refresh")));
        return mock;
    }

    private static HttpResponse CreateHttpResponseMock()
    {
        var cookiesMock = new Mock<IResponseCookies>();
        var responseMock = new Mock<HttpResponse>();
        responseMock.SetupGet(r => r.Cookies).Returns(cookiesMock.Object);
        return responseMock.Object;
    }

    private static RegisterRequest ValidRegisterRequest(string email = "new@example.com") =>
        new("Andrei", "Pop", email, "Password@1", "Password@1", null, true);

    // ── Register ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task RegisterAsync_NewUser_SavesUserToDatabase()
    {
        var db = CreateDb();
        var sut = CreateSut(db);

        await sut.RegisterAsync(ValidRegisterRequest());

        var user = await db.Users.SingleAsync();
        user.Email.Should().Be("new@example.com");
        user.IsEmailConfirmed.Should().BeFalse();
        user.GdprConsentAccepted.Should().BeTrue();
    }

    [Fact]
    public async Task RegisterAsync_NewUser_StoresHashedPassword()
    {
        var db = CreateDb();
        var sut = CreateSut(db);

        await sut.RegisterAsync(ValidRegisterRequest());

        var user = await db.Users.SingleAsync();
        user.PasswordHash.Should().NotBeNullOrEmpty();
        user.PasswordHash.Should().NotBe("Password@1", "plain-text password must not be stored");
    }

    [Fact]
    public async Task RegisterAsync_NewUser_CreatesEmailConfirmationToken()
    {
        var db = CreateDb();
        var sut = CreateSut(db);

        await sut.RegisterAsync(ValidRegisterRequest());

        var token = await db.EmailConfirmationTokens.SingleAsync();
        token.TokenHash.Should().NotBeNullOrEmpty();
        token.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow.AddHours(23));
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ThrowsConflictException()
    {
        var db = CreateDb();
        var sut = CreateSut(db);

        await sut.RegisterAsync(ValidRegisterRequest());

        var act = () => sut.RegisterAsync(ValidRegisterRequest());

        await act.Should().ThrowAsync<ConflictException>();
    }

    // ── Login ─────────────────────────────────────────────────────────────────

    private static async Task<User> SeedConfirmedUserAsync(PhotoPrintDbContext db, string password = "Password@1")
    {
        var hasher = new PasswordHasher<User>();
        var user = new User
        {
            Email = "confirmed@example.com",
            NormalizedEmail = "CONFIRMED@EXAMPLE.COM",
            FirstName = "User",
            LastName = "Test",
            IsEmailConfirmed = true,
            GdprConsentAccepted = true,
        };
        user.PasswordHash = hasher.HashPassword(user, password);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsAccessToken()
    {
        var db = CreateDb();
        await SeedConfirmedUserAsync(db);
        var sut = CreateSut(db);

        var result = await sut.LoginAsync(
            new LoginRequest("confirmed@example.com", "Password@1"),
            "127.0.0.1",
            CreateHttpResponseMock());

        result.AccessToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_StoresRefreshToken()
    {
        var db = CreateDb();
        await SeedConfirmedUserAsync(db);
        var sut = CreateSut(db);

        await sut.LoginAsync(
            new LoginRequest("confirmed@example.com", "Password@1"),
            "127.0.0.1",
            CreateHttpResponseMock());

        var token = await db.RefreshTokens.SingleAsync();
        token.TokenHash.Should().NotBeNullOrEmpty();
        token.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow.AddDays(29));
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ThrowsUnauthorizedException()
    {
        var db = CreateDb();
        await SeedConfirmedUserAsync(db);
        var sut = CreateSut(db);

        var act = () => sut.LoginAsync(
            new LoginRequest("confirmed@example.com", "wrong-password"),
            "127.0.0.1",
            CreateHttpResponseMock());

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_IncrementsFailedLoginCount()
    {
        var db = CreateDb();
        await SeedConfirmedUserAsync(db);
        var sut = CreateSut(db);

        try
        {
            await sut.LoginAsync(new LoginRequest("confirmed@example.com", "bad"), "::1", CreateHttpResponseMock());
        }
        catch (UnauthorizedException) { /* expected */ }

        var user = await db.Users.FirstAsync();
        user.FailedLoginCount.Should().Be(1);
    }

    [Fact]
    public async Task LoginAsync_FiveFailedAttempts_LocksAccount()
    {
        var db = CreateDb();
        await SeedConfirmedUserAsync(db);
        var sut = CreateSut(db);

        for (var i = 0; i < 5; i++)
        {
            try
            {
                await sut.LoginAsync(new LoginRequest("confirmed@example.com", "bad"), "::1", CreateHttpResponseMock());
            }
            catch (UnauthorizedException) { /* expected */ }
        }

        var user = await db.Users.FirstAsync();
        user.LockoutEnd.Should().NotBeNull();
        user.LockoutEnd!.Value.Should().BeAfter(DateTimeOffset.UtcNow.AddMinutes(14));
    }

    [Fact]
    public async Task LoginAsync_LockedAccount_ThrowsUnauthorizedException()
    {
        var db = CreateDb();
        var user = await SeedConfirmedUserAsync(db);
        user.LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(15);
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var act = () => sut.LoginAsync(
            new LoginRequest("confirmed@example.com", "Password@1"),
            "::1",
            CreateHttpResponseMock());

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task LoginAsync_UnconfirmedEmail_ThrowsForbiddenException()
    {
        var db = CreateDb();
        var hasher = new PasswordHasher<User>();
        var user = new User
        {
            Email = "unconfirmed@example.com",
            NormalizedEmail = "UNCONFIRMED@EXAMPLE.COM",
            IsEmailConfirmed = false,
        };
        user.PasswordHash = hasher.HashPassword(user, "Password@1");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var act = () => sut.LoginAsync(
            new LoginRequest("unconfirmed@example.com", "Password@1"),
            "::1",
            CreateHttpResponseMock());

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    // ── Refresh token ─────────────────────────────────────────────────────────

    [Fact]
    public async Task RefreshTokenAsync_ActiveToken_ReturnsNewAccessToken()
    {
        var db = CreateDb();
        var user = await SeedConfirmedUserAsync(db);
        var emailTokenSvc = new EmailTokenService();
        var tokenSvc = CreateTokenServiceMock();
        // GenerateRefreshToken called once by RefreshTokenAsync → returns "raw2"
        tokenSvc.Setup(t => t.GenerateRefreshToken())
            .Returns(("raw2", TokenService.HashToken("raw2")));

        var sut = CreateSut(db, tokenSvc.Object, emailTokenSvc);

        // Seed an active refresh token
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = TokenService.HashToken("raw1"),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30),
        });
        await db.SaveChangesAsync();

        var result = await sut.RefreshTokenAsync("raw1", "::1", CreateHttpResponseMock());

        result.AccessToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RefreshTokenAsync_ActiveToken_RotatesRefreshToken()
    {
        var db = CreateDb();
        var user = await SeedConfirmedUserAsync(db);
        var tokenSvc = CreateTokenServiceMock();
        // RefreshTokenAsync calls GenerateRefreshToken once → must return a DIFFERENT hash from "raw1"
        tokenSvc.Setup(t => t.GenerateRefreshToken())
            .Returns(("raw2", TokenService.HashToken("raw2")));

        var sut = CreateSut(db, tokenSvc.Object);

        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = TokenService.HashToken("raw1"),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30),
        });
        await db.SaveChangesAsync();

        await sut.RefreshTokenAsync("raw1", "::1", CreateHttpResponseMock());

        var tokens = await db.RefreshTokens.ToListAsync();
        tokens.Should().HaveCount(2, "old token is revoked and new one is created");
        tokens.Single(t => t.TokenHash == TokenService.HashToken("raw1")).RevokedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task RefreshTokenAsync_ExpiredToken_ThrowsUnauthorizedException()
    {
        var db = CreateDb();
        var user = await SeedConfirmedUserAsync(db);

        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = TokenService.HashToken("old-token"),
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(-1),  // already expired
        });
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var act = () => sut.RefreshTokenAsync("old-token", "::1", CreateHttpResponseMock());

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    // ── Email confirmation ────────────────────────────────────────────────────

    [Fact]
    public async Task ConfirmEmailAsync_ValidToken_SetsIsEmailConfirmed()
    {
        var db = CreateDb();
        var emailTokenSvc = new EmailTokenService();
        var (raw, hash) = emailTokenSvc.GenerateEmailToken();

        var user = new User { Email = "u@test.com", NormalizedEmail = "U@TEST.COM" };
        var token = new EmailConfirmationToken
        {
            UserId = user.Id,
            TokenHash = hash,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(24),
        };
        db.Users.Add(user);
        db.EmailConfirmationTokens.Add(token);
        await db.SaveChangesAsync();

        var sut = CreateSut(db, emailTokenService: emailTokenSvc);

        await sut.ConfirmEmailAsync(user.Id, raw);

        var updated = await db.Users.FindAsync(user.Id);
        updated!.IsEmailConfirmed.Should().BeTrue();
    }

    [Fact]
    public async Task ConfirmEmailAsync_ValidToken_FiresWelcomeEmail()
    {
        var db = CreateDb();
        var emailTokenSvc = new EmailTokenService();
        var (raw, hash) = emailTokenSvc.GenerateEmailToken();

        var user = new User { FirstName = "Ion", Email = "ion@test.com", NormalizedEmail = "ION@TEST.COM" };
        db.Users.Add(user);
        db.EmailConfirmationTokens.Add(new EmailConfirmationToken
        {
            UserId = user.Id,
            TokenHash = hash,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(24),
        });
        await db.SaveChangesAsync();

        var emailMock = new Mock<IEmailService>();
        emailMock
            .Setup(e => e.SendTemplatedAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut(db, emailTokenService: emailTokenSvc, emailService: emailMock.Object);
        await sut.ConfirmEmailAsync(user.Id, raw);
        await Task.Delay(200); // allow fire-and-forget to complete

        emailMock.Verify(e => e.SendTemplatedAsync(
            "ion@test.com",
            It.IsAny<string>(),
            "Welcome",
            It.IsAny<object>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmEmailAsync_ValidToken_DeletesConfirmationTokens()
    {
        var db = CreateDb();
        var emailTokenSvc = new EmailTokenService();
        var (raw, hash) = emailTokenSvc.GenerateEmailToken();

        var user = new User { Email = "u@test.com", NormalizedEmail = "U@TEST.COM" };
        db.Users.Add(user);
        db.EmailConfirmationTokens.Add(new EmailConfirmationToken
        {
            UserId = user.Id,
            TokenHash = hash,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(24),
        });
        await db.SaveChangesAsync();

        var sut = CreateSut(db, emailTokenService: emailTokenSvc);
        await sut.ConfirmEmailAsync(user.Id, raw);

        var remaining = await db.EmailConfirmationTokens.CountAsync();
        remaining.Should().Be(0);
    }

    [Fact]
    public async Task ConfirmEmailAsync_AlreadyConfirmed_IsIdempotent()
    {
        var db = CreateDb();
        var user = new User
        {
            Email = "u@test.com",
            NormalizedEmail = "U@TEST.COM",
            IsEmailConfirmed = true,
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var act = () => sut.ConfirmEmailAsync(user.Id, "any-token");

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ConfirmEmailAsync_WrongToken_ThrowsUnauthorizedException()
    {
        var db = CreateDb();
        var emailTokenSvc = new EmailTokenService();
        var (_, hash) = emailTokenSvc.GenerateEmailToken();

        var user = new User { Email = "u@test.com", NormalizedEmail = "U@TEST.COM" };
        db.Users.Add(user);
        db.EmailConfirmationTokens.Add(new EmailConfirmationToken
        {
            UserId = user.Id,
            TokenHash = hash,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(24),
        });
        await db.SaveChangesAsync();

        var sut = CreateSut(db, emailTokenService: emailTokenSvc);

        var act = () => sut.ConfirmEmailAsync(user.Id, "wrong-token");

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task ConfirmEmailAsync_ExpiredToken_ThrowsUnauthorizedException()
    {
        var db = CreateDb();
        var emailTokenSvc = new EmailTokenService();
        var (raw, hash) = emailTokenSvc.GenerateEmailToken();

        var user = new User { Email = "u@test.com", NormalizedEmail = "U@TEST.COM" };
        db.Users.Add(user);
        db.EmailConfirmationTokens.Add(new EmailConfirmationToken
        {
            UserId = user.Id,
            TokenHash = hash,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(-1),  // expired
        });
        await db.SaveChangesAsync();

        var sut = CreateSut(db, emailTokenService: emailTokenSvc);

        var act = () => sut.ConfirmEmailAsync(user.Id, raw);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    // ── Password reset ────────────────────────────────────────────────────────

    [Fact]
    public async Task ForgotPasswordAsync_UnknownEmail_DoesNothing()
    {
        var db = CreateDb();
        var sut = CreateSut(db);

        var act = () => sut.ForgotPasswordAsync("ghost@example.com");

        await act.Should().NotThrowAsync();
        (await db.PasswordResetTokens.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task ForgotPasswordAsync_KnownEmail_CreatesPasswordResetToken()
    {
        var db = CreateDb();
        var user = await SeedConfirmedUserAsync(db);
        var sut = CreateSut(db);

        await sut.ForgotPasswordAsync("confirmed@example.com");

        var resetToken = await db.PasswordResetTokens.SingleAsync();
        resetToken.UserId.Should().Be(user.Id);
        resetToken.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow.AddMinutes(59));
    }

    [Fact]
    public async Task ResetPasswordAsync_ValidToken_UpdatesPasswordHash()
    {
        var db = CreateDb();
        var emailTokenSvc = new EmailTokenService();
        var (raw, hash) = emailTokenSvc.GenerateEmailToken();

        var user = await SeedConfirmedUserAsync(db);
        db.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = hash,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
        });
        await db.SaveChangesAsync();

        var sut = CreateSut(db, emailTokenService: emailTokenSvc);
        await sut.ResetPasswordAsync(user.Id, raw, "NewPassword@99");

        var updated = await db.Users.FindAsync(user.Id);
        var hasher = new PasswordHasher<User>();
        hasher.VerifyHashedPassword(updated!, updated!.PasswordHash!, "NewPassword@99")
              .Should().Be(PasswordVerificationResult.Success);
    }

    [Fact]
    public async Task ResetPasswordAsync_ValidToken_RevokesActiveRefreshTokens()
    {
        var db = CreateDb();
        var emailTokenSvc = new EmailTokenService();
        var (raw, hash) = emailTokenSvc.GenerateEmailToken();

        var user = await SeedConfirmedUserAsync(db);
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = "some-hash",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30),
        });
        db.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = hash,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
        });
        await db.SaveChangesAsync();

        var sut = CreateSut(db, emailTokenService: emailTokenSvc);
        await sut.ResetPasswordAsync(user.Id, raw, "NewPassword@99");

        var refreshToken = await db.RefreshTokens.FirstAsync();
        refreshToken.RevokedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ResetPasswordAsync_InvalidToken_ThrowsUnauthorizedException()
    {
        var db = CreateDb();
        var user = await SeedConfirmedUserAsync(db);
        db.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = TokenService.HashToken("correct-token"),
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
        });
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var act = () => sut.ResetPasswordAsync(user.Id, "wrong-token", "NewPass@1");

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    // ── Logout ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task RevokeRefreshTokenAsync_ActiveToken_SetsRevokedAt()
    {
        var db = CreateDb();
        var user = await SeedConfirmedUserAsync(db);
        var (raw, hash) = CreateTokenServiceMock().Object.GenerateRefreshToken();
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = hash,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30),
        });
        await db.SaveChangesAsync();

        var sut = CreateSut(db);
        await sut.RevokeRefreshTokenAsync(raw);

        var token = await db.RefreshTokens.SingleAsync();
        token.RevokedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_UnknownToken_DoesNotThrow()
    {
        var db = CreateDb();
        var sut = CreateSut(db);

        var act = () => sut.RevokeRefreshTokenAsync("nonexistent-token");

        await act.Should().NotThrowAsync();
    }
}
