using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using FluentAssertions;
using Microsoft.Extensions.Options;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services;
using PhotoPrint.Tests.Helpers;

namespace PhotoPrint.Tests.Unit.Services;

public class TokenServiceTests
{
    private static TokenService CreateSut() =>
        new(Options.Create(new JwtSettings
        {
            PrivateKeyPem = TestKeys.RsaPrivateKeyPem,
            Issuer = "fototipar",
            Audience = "fototipar-spa",
            AccessTokenMinutes = 15,
            RefreshTokenDays = 30,
        }));

    private static User TestUser => new()
    {
        Id = Guid.NewGuid(),
        Email = "user@example.com",
        NormalizedEmail = "USER@EXAMPLE.COM",
        Role = UserRole.Customer,
    };

    // ── Access token ─────────────────────────────────────────────────────────

    [Fact]
    public void GenerateAccessToken_ValidUser_ReturnsNonEmptyJwt()
    {
        var token = CreateSut().GenerateAccessToken(TestUser);

        token.Should().NotBeNullOrEmpty();
        token.Split('.').Should().HaveCount(3, "a JWT must have three segments");
    }

    [Fact]
    public void GenerateAccessToken_ContainsSubjectAndEmailClaims()
    {
        var user = TestUser;
        var rawToken = CreateSut().GenerateAccessToken(user);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(rawToken);
        jwt.Subject.Should().Be(user.Id.ToString());
        jwt.Claims.Should()
            .Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == user.Email);
    }

    [Fact]
    public void GenerateAccessToken_HasCorrectIssuerAndAudience()
    {
        var rawToken = CreateSut().GenerateAccessToken(TestUser);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(rawToken);
        jwt.Issuer.Should().Be("fototipar");
        jwt.Audiences.Should().Contain("fototipar-spa");
    }

    [Fact]
    public void GenerateAccessToken_ExpiresIn15Minutes()
    {
        var before = DateTime.UtcNow;
        var rawToken = CreateSut().GenerateAccessToken(TestUser);
        var after = DateTime.UtcNow;

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(rawToken);
        jwt.ValidTo.Should().BeAfter(before.AddMinutes(14));
        jwt.ValidTo.Should().BeBefore(after.AddMinutes(16));
    }

    [Fact]
    public void GenerateAccessToken_ContainsUniqueJtiPerCall()
    {
        var sut = CreateSut();
        var user = TestUser;

        var token1 = new JwtSecurityTokenHandler().ReadJwtToken(sut.GenerateAccessToken(user));
        var token2 = new JwtSecurityTokenHandler().ReadJwtToken(sut.GenerateAccessToken(user));

        token1.Id.Should().NotBe(token2.Id);
    }

    // ── Refresh token ─────────────────────────────────────────────────────────

    [Fact]
    public void GenerateRefreshToken_EachCallReturnsDifferentTokens()
    {
        var sut = CreateSut();

        var (raw1, hash1) = sut.GenerateRefreshToken();
        var (raw2, hash2) = sut.GenerateRefreshToken();

        raw1.Should().NotBe(raw2);
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void GenerateRefreshToken_HashIsHexSha256OfRawToken()
    {
        var (raw, hash) = CreateSut().GenerateRefreshToken();

        var expected = TokenService.HashToken(raw);
        hash.Should().Be(expected);
    }
}
