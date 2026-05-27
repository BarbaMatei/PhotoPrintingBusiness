using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Data;
using PhotoPrint.API.DTOs.Auth;
using PhotoPrint.API.Models;

namespace PhotoPrint.API.Services;

public class SocialAuthService : ISocialAuthService
{
    private const string GoogleProvider = "Google";

    private readonly PhotoPrintDbContext _db;
    private readonly IGoogleTokenValidator _googleTokenValidator;
    private readonly ITokenService _tokenService;
    private readonly JwtSettings _jwt;
    private readonly ILogger<SocialAuthService> _logger;

    public SocialAuthService(
        PhotoPrintDbContext db,
        IGoogleTokenValidator googleTokenValidator,
        ITokenService tokenService,
        IOptions<JwtSettings> jwt,
        ILogger<SocialAuthService> logger)
    {
        _db = db;
        _googleTokenValidator = googleTokenValidator;
        _tokenService = tokenService;
        _jwt = jwt.Value;
        _logger = logger;
    }

    public async Task<GoogleLoginResponse> GoogleSignInAsync(
        string idToken,
        string ipAddress,
        HttpResponse httpResponse,
        CancellationToken ct = default)
    {
        var payload = await _googleTokenValidator.ValidateAsync(idToken, ct);

        var normalizedEmail = payload.Email.ToUpperInvariant();
        var accountLinked = false;

        // 1. Existing Google login → retrieve user
        var existingLogin = await _db.ExternalLogins
            .Include(el => el.User)
            .FirstOrDefaultAsync(
                el => el.Provider == GoogleProvider && el.ProviderKey == payload.Sub, ct);

        User user;

        if (existingLogin is not null)
        {
            user = existingLogin.User;
        }
        else
        {
            // 2. Existing email+password account → link it
            var existingUser = await _db.Users
                .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, ct);

            if (existingUser is not null)
            {
                user = existingUser;
                _db.ExternalLogins.Add(new ExternalLogin
                {
                    UserId = user.Id,
                    Provider = GoogleProvider,
                    ProviderKey = payload.Sub,
                });
                accountLinked = true;
            }
            else
            {
                // 3. Brand-new user
                user = new User
                {
                    Email = payload.Email.ToLowerInvariant(),
                    NormalizedEmail = normalizedEmail,
                    FirstName = payload.GivenName,
                    LastName = payload.FamilyName,
                    IsEmailConfirmed = true,
                    GdprConsentAccepted = false,
                };
                _db.Users.Add(user);
                _db.ExternalLogins.Add(new ExternalLogin
                {
                    UserId = user.Id,
                    Provider = GoogleProvider,
                    ProviderKey = payload.Sub,
                });
            }
        }

        var accessToken = _tokenService.GenerateAccessToken(user);
        var (rawRefresh, refreshHash) = _tokenService.GenerateRefreshToken();

        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshHash,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(_jwt.RefreshTokenDays),
        });

        await _db.SaveChangesAsync(ct);

        SetRefreshTokenCookie(httpResponse, rawRefresh);

        _logger.LogInformation(
            "Google sign-in: UserId={UserId} Email={Email} AccountLinked={AccountLinked}",
            user.Id, user.Email, accountLinked);

        return new GoogleLoginResponse(accessToken, _jwt.AccessTokenMinutes * 60, accountLinked);
    }

    private void SetRefreshTokenCookie(HttpResponse response, string rawToken)
    {
        response.Cookies.Append("refresh_token", rawToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = response.HttpContext?.Request?.IsHttps ?? true,
            SameSite = SameSiteMode.Strict,
            Path = "/api/auth",
            Expires = DateTimeOffset.UtcNow.AddDays(_jwt.RefreshTokenDays),
        });
    }
}
