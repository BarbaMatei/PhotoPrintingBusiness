using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Data;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services;

namespace PhotoPrint.Tests.Unit.Services;

public class SocialAuthServiceTests
{
    private readonly PhotoPrintDbContext _db;
    private readonly Mock<IGoogleTokenValidator> _mockValidator;
    private readonly Mock<ITokenService> _mockTokenService;
    private readonly ISocialAuthService _sut;

    public SocialAuthServiceTests()
    {
        var options = new DbContextOptionsBuilder<PhotoPrintDbContext>()
            .UseInMemoryDatabase($"SocialAuth_{Guid.NewGuid():N}")
            .Options;
        _db = new PhotoPrintDbContext(options);

        _mockValidator = new Mock<IGoogleTokenValidator>();
        _mockTokenService = new Mock<ITokenService>();
        _mockTokenService.Setup(s => s.GenerateAccessToken(It.IsAny<User>())).Returns("access-token");
        _mockTokenService.Setup(s => s.GenerateRefreshToken()).Returns(("raw-refresh", HashToken("raw-refresh")));

        var jwtSettings = Options.Create(new JwtSettings
        {
            PrivateKeyPem = "",
            Issuer = "fototipar",
            Audience = "fototipar-spa",
            AccessTokenMinutes = 15,
            RefreshTokenDays = 30,
        });

        _sut = new SocialAuthService(
            _db,
            _mockValidator.Object,
            _mockTokenService.Object,
            jwtSettings,
            Mock.Of<ILogger<SocialAuthService>>());
    }

    private static string HashToken(string raw)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static GooglePayload TestPayload(
        string sub = "googleSub123", string email = "user@gmail.com")
        => new(sub, email, "John", "Doe", null);

    private static HttpResponse MockHttpResponse()
    {
        var mockCookies = new Mock<IResponseCookies>();
        var mock = new Mock<HttpResponse>();
        mock.Setup(r => r.Cookies).Returns(mockCookies.Object);
        mock.Setup(r => r.HttpContext).Returns((HttpContext)null!);
        return mock.Object;
    }

    [Fact]
    public async Task GoogleSignInAsync_NewUser_CreatesUserRow()
    {
        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<string>(), default))
            .ReturnsAsync(TestPayload());

        await _sut.GoogleSignInAsync("id-token", "127.0.0.1", MockHttpResponse());

        var user = await _db.Users.SingleAsync();
        user.Email.Should().Be("user@gmail.com");
        user.FirstName.Should().Be("John");
        user.IsEmailConfirmed.Should().BeTrue();
        user.PasswordHash.Should().BeNull();
    }

    [Fact]
    public async Task GoogleSignInAsync_NewUser_CreatesExternalLoginRow()
    {
        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<string>(), default))
            .ReturnsAsync(TestPayload());

        await _sut.GoogleSignInAsync("id-token", "127.0.0.1", MockHttpResponse());

        var login = await _db.ExternalLogins.SingleAsync();
        login.Provider.Should().Be("Google");
        login.ProviderKey.Should().Be("googleSub123");
        login.UserId.Should().Be((await _db.Users.SingleAsync()).Id);
    }

    [Fact]
    public async Task GoogleSignInAsync_NewUser_ReturnsAccountLinkedFalse()
    {
        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<string>(), default))
            .ReturnsAsync(TestPayload());

        var result = await _sut.GoogleSignInAsync("id-token", "127.0.0.1", MockHttpResponse());

        result.AccountLinked.Should().BeFalse();
    }

    [Fact]
    public async Task GoogleSignInAsync_ExistingGoogleLogin_DoesNotCreateNewUser()
    {
        var user = new User
        {
            Email = "user@gmail.com",
            NormalizedEmail = "USER@GMAIL.COM",
            FirstName = "John",
            LastName = "Doe",
            IsEmailConfirmed = true,
        };
        _db.Users.Add(user);
        _db.ExternalLogins.Add(new ExternalLogin
        {
            UserId = user.Id,
            Provider = "Google",
            ProviderKey = "googleSub123",
        });
        await _db.SaveChangesAsync();

        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<string>(), default))
            .ReturnsAsync(TestPayload());

        await _sut.GoogleSignInAsync("id-token", "127.0.0.1", MockHttpResponse());

        _db.Users.Count().Should().Be(1);
        _db.ExternalLogins.Count().Should().Be(1);
    }

    [Fact]
    public async Task GoogleSignInAsync_ExistingGoogleLogin_ReturnsAccountLinkedFalse()
    {
        var user = new User
        {
            Email = "user@gmail.com",
            NormalizedEmail = "USER@GMAIL.COM",
            FirstName = "John",
            LastName = "Doe",
            IsEmailConfirmed = true,
        };
        _db.Users.Add(user);
        _db.ExternalLogins.Add(new ExternalLogin
        {
            UserId = user.Id,
            Provider = "Google",
            ProviderKey = "googleSub123",
        });
        await _db.SaveChangesAsync();

        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<string>(), default))
            .ReturnsAsync(TestPayload());

        var result = await _sut.GoogleSignInAsync("id-token", "127.0.0.1", MockHttpResponse());

        result.AccountLinked.Should().BeFalse();
    }

    [Fact]
    public async Task GoogleSignInAsync_ExistingEmailAccount_CreatesExternalLoginAndReturnsAccountLinkedTrue()
    {
        var user = new User
        {
            Email = "user@gmail.com",
            NormalizedEmail = "USER@GMAIL.COM",
            FirstName = "Jane",
            LastName = "Doe",
            PasswordHash = "some-hash",
            IsEmailConfirmed = true,
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<string>(), default))
            .ReturnsAsync(TestPayload());

        var result = await _sut.GoogleSignInAsync("id-token", "127.0.0.1", MockHttpResponse());

        result.AccountLinked.Should().BeTrue();
        _db.Users.Count().Should().Be(1);
        _db.ExternalLogins.Count().Should().Be(1);
    }

    [Fact]
    public async Task GoogleSignInAsync_ValidSignIn_StoresRefreshToken()
    {
        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<string>(), default))
            .ReturnsAsync(TestPayload());

        await _sut.GoogleSignInAsync("id-token", "127.0.0.1", MockHttpResponse());

        _db.RefreshTokens.Count().Should().Be(1);
    }

    [Fact]
    public async Task GoogleSignInAsync_ValidSignIn_ReturnsAccessTokenAndExpiry()
    {
        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<string>(), default))
            .ReturnsAsync(TestPayload());

        var result = await _sut.GoogleSignInAsync("id-token", "127.0.0.1", MockHttpResponse());

        result.AccessToken.Should().Be("access-token");
        result.ExpiresIn.Should().Be(900);
    }
}
