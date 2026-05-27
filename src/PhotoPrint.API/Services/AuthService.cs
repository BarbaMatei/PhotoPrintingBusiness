using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Data;
using PhotoPrint.API.DTOs.Auth;
using PhotoPrint.API.Exceptions;
using PhotoPrint.API.Models;

namespace PhotoPrint.API.Services;

public class AuthService : IAuthService
{
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan EmailConfirmationExpiry = TimeSpan.FromHours(24);
    private static readonly TimeSpan PasswordResetExpiry = TimeSpan.FromHours(1);

    private readonly PhotoPrintDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly IEmailTokenService _emailTokenService;
    private readonly IEmailService _emailService;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly JwtSettings _jwt;
    private readonly AppSettings _app;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        PhotoPrintDbContext db,
        ITokenService tokenService,
        IEmailTokenService emailTokenService,
        IEmailService emailService,
        IPasswordHasher<User> passwordHasher,
        IOptions<JwtSettings> jwt,
        IOptions<AppSettings> app,
        ILogger<AuthService> logger)
    {
        _db = db;
        _tokenService = tokenService;
        _emailTokenService = emailTokenService;
        _emailService = emailService;
        _passwordHasher = passwordHasher;
        _jwt = jwt.Value;
        _app = app.Value;
        _logger = logger;
    }

    public async Task<LoginResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.ToUpperInvariant();

        var exists = await _db.Users
            .AnyAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);

        if (exists)
        {
            throw new ConflictException("O persoană cu această adresă de email există deja.");
        }

        var user = new User
        {
            Email = request.Email.ToLowerInvariant(),
            NormalizedEmail = normalizedEmail,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Phone = request.Phone,
            GdprConsentAccepted = request.GdprConsentAccepted,
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        _db.Users.Add(user);

        var (rawToken, tokenHash) = _emailTokenService.GenerateEmailToken();
        _db.EmailConfirmationTokens.Add(new EmailConfirmationToken
        {
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAt = DateTimeOffset.UtcNow.Add(EmailConfirmationExpiry),
        });

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("New user registered: {UserId} {Email}", user.Id, user.Email);

        SendConfirmationEmailFireAndForget(user, rawToken);

        // Registration returns a 201 response body; we don't issue tokens yet
        // (user must confirm email before logging in)
        return new LoginResponse("", 0);
    }

    public async Task<LoginResponse> LoginAsync(
        LoginRequest request,
        string ipAddress,
        HttpResponse httpResponse,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.ToUpperInvariant();

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);

        if (user is null)
        {
            throw new UnauthorizedException("Credențiale invalide.");
        }

        if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow)
        {
            throw new UnauthorizedException("Contul este blocat temporar. Vă rugăm încercați mai târziu.");
        }

        if (user.PasswordHash is null)
        {
            throw new UnauthorizedException("Autentificarea cu parolă nu este disponibilă pentru acest cont.");
        }

        var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);

        if (verificationResult == PasswordVerificationResult.Failed)
        {
            user.FailedLoginCount++;

            if (user.FailedLoginCount >= MaxFailedAttempts)
            {
                user.LockoutEnd = DateTimeOffset.UtcNow.Add(LockoutDuration);
                user.FailedLoginCount = 0;
                _logger.LogWarning("Account locked: {UserId}", user.Id);
            }

            await _db.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedException("Credențiale invalide.");
        }

        if (!user.IsEmailConfirmed)
        {
            throw new ForbiddenException("Adresa de email nu a fost confirmată.");
        }

        user.FailedLoginCount = 0;
        user.LockoutEnd = null;

        var accessToken = _tokenService.GenerateAccessToken(user);
        var (rawRefresh, refreshHash) = _tokenService.GenerateRefreshToken();

        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshHash,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(_jwt.RefreshTokenDays),
        });

        await _db.SaveChangesAsync(cancellationToken);

        SetRefreshTokenCookie(httpResponse, rawRefresh);

        return new LoginResponse(accessToken, _jwt.AccessTokenMinutes * 60);
    }

    public async Task<LoginResponse> RefreshTokenAsync(
        string rawRefreshToken,
        string ipAddress,
        HttpResponse httpResponse,
        CancellationToken cancellationToken = default)
    {
        var hash = TokenService.HashToken(rawRefreshToken);

        var token = await _db.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (token is null || !token.IsActive)
        {
            throw new UnauthorizedException("Token de reîmprospătare invalid sau expirat.");
        }

        // Rotate: revoke old, issue new
        token.RevokedAt = DateTimeOffset.UtcNow;

        var (newRaw, newHash) = _tokenService.GenerateRefreshToken();

        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = token.UserId,
            TokenHash = newHash,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(_jwt.RefreshTokenDays),
        });

        await _db.SaveChangesAsync(cancellationToken);

        var accessToken = _tokenService.GenerateAccessToken(token.User);
        SetRefreshTokenCookie(httpResponse, newRaw);

        return new LoginResponse(accessToken, _jwt.AccessTokenMinutes * 60);
    }

    public async Task RevokeRefreshTokenAsync(string rawRefreshToken, CancellationToken cancellationToken = default)
    {
        var hash = TokenService.HashToken(rawRefreshToken);

        var token = await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (token is null || !token.IsActive)
        {
            return; // Silently succeed — idempotent logout
        }

        token.RevokedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task ConfirmEmailAsync(Guid userId, string token, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FindAsync(new object[] { userId }, cancellationToken);

        if (user is null)
        {
            throw new NotFoundException("Utilizatorul nu a fost găsit.");
        }

        if (user.IsEmailConfirmed)
        {
            return; // Idempotent
        }

        var confirmToken = await _db.EmailConfirmationTokens
            .Where(t => t.UserId == userId && t.ExpiresAt > DateTimeOffset.UtcNow)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (confirmToken is null || !_emailTokenService.VerifyEmailToken(token, confirmToken.TokenHash))
        {
            throw new UnauthorizedException("Link de confirmare invalid sau expirat.");
        }

        user.IsEmailConfirmed = true;
        _db.EmailConfirmationTokens.Remove(confirmToken);

        await _db.SaveChangesAsync(cancellationToken);

        SendWelcomeEmailFireAndForget(user);
        _logger.LogInformation("Email confirmed for user {UserId}", userId);
    }

    public async Task ResendConfirmationAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.ToUpperInvariant();
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);

        // Always return success to avoid enumeration
        if (user is null || user.IsEmailConfirmed)
        {
            return;
        }

        var (rawToken, tokenHash) = _emailTokenService.GenerateEmailToken();

        // Remove any existing unconfirmed tokens
        var existing = _db.EmailConfirmationTokens.Where(t => t.UserId == user.Id);
        _db.EmailConfirmationTokens.RemoveRange(existing);

        _db.EmailConfirmationTokens.Add(new EmailConfirmationToken
        {
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAt = DateTimeOffset.UtcNow.Add(EmailConfirmationExpiry),
        });

        await _db.SaveChangesAsync(cancellationToken);

        SendConfirmationEmailFireAndForget(user, rawToken);
    }

    public async Task ForgotPasswordAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.ToUpperInvariant();
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);

        // Always return success to avoid enumeration
        if (user is null)
        {
            return;
        }

        var (rawToken, tokenHash) = _emailTokenService.GenerateEmailToken();

        // Revoke previous reset tokens
        var existing = _db.PasswordResetTokens.Where(t => t.UserId == user.Id);
        _db.PasswordResetTokens.RemoveRange(existing);

        _db.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAt = DateTimeOffset.UtcNow.Add(PasswordResetExpiry),
        });

        await _db.SaveChangesAsync(cancellationToken);

        SendPasswordResetEmailFireAndForget(user, rawToken);
    }

    public async Task ResetPasswordAsync(Guid userId, string token, string newPassword, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FindAsync(new object[] { userId }, cancellationToken);

        if (user is null)
        {
            throw new NotFoundException("Utilizatorul nu a fost găsit.");
        }

        var resetToken = await _db.PasswordResetTokens
            .Where(t => t.UserId == userId && t.ExpiresAt > DateTimeOffset.UtcNow)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (resetToken is null || !_emailTokenService.VerifyEmailToken(token, resetToken.TokenHash))
        {
            throw new UnauthorizedException("Link de resetare invalid sau expirat.");
        }

        user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
        user.FailedLoginCount = 0;
        user.LockoutEnd = null;

        _db.PasswordResetTokens.Remove(resetToken);

        // Revoke all refresh tokens on password change
        var refreshTokens = await _db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var rt in refreshTokens)
        {
            rt.RevokedAt = DateTimeOffset.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Password reset for user {UserId}", userId);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

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

    private void SendWelcomeEmailFireAndForget(User user)
    {
        var email = user.Email;
        var firstName = user.FirstName;

        _ = Task.Run(async () =>
        {
            try
            {
                await _emailService.SendTemplatedAsync(
                    email,
                    "Bine ai venit la FotoTipar!",
                    "Welcome",
                    new { FirstName = firstName });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send welcome email to {Email}", email);
            }
        });
    }

    private void SendConfirmationEmailFireAndForget(User user, string rawToken)
    {
        var confirmUrl = $"{_app.BaseUrl}/auth/confirm-email?userId={user.Id}&token={Uri.EscapeDataString(rawToken)}";

        _ = Task.Run(async () =>
        {
            try
            {
                await _emailService.SendTemplatedAsync(
                    user.Email,
                    "Confirmă adresa de email — FotoTipar",
                    "EmailConfirmation",
                    new { UserName = user.FirstName, ConfirmUrl = confirmUrl });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send confirmation email to {Email}", user.Email);
            }
        });
    }

    private void SendPasswordResetEmailFireAndForget(User user, string rawToken)
    {
        var resetUrl = $"{_app.BaseUrl}/auth/reset-password?userId={user.Id}&token={Uri.EscapeDataString(rawToken)}";

        _ = Task.Run(async () =>
        {
            try
            {
                await _emailService.SendTemplatedAsync(
                    user.Email,
                    "Resetare parolă — FotoTipar",
                    "PasswordReset",
                    new { UserName = user.FirstName, ResetUrl = resetUrl });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send password-reset email to {Email}", user.Email);
            }
        });
    }
}
