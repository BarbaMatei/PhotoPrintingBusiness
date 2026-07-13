using System.Security.Cryptography;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services;

namespace PhotoPrint.API.Extensions;

public static class AuthExtensions
{
    public const string RegisterRateLimitPolicy = "register";
    public const string ResendConfirmationRateLimitPolicy = "resend-confirmation";
    public const string ForgotPasswordRateLimitPolicy = "forgot-password";

    public static IServiceCollection AddAuthCore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── Options ───────────────────────────────────────────────────────────
        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
        services.Configure<AppSettings>(configuration.GetSection("App"));

        // ── Identity password hasher (no full Identity stack) ─────────────────
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

        // ── Auth services ─────────────────────────────────────────────────────
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IEmailTokenService, EmailTokenService>();
        services.AddScoped<IAuthService, AuthService>();

        // ── JWT Bearer ────────────────────────────────────────────────────────
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>()
                ?? throw new InvalidOperationException("JwtSettings configuration is missing.");

            if (string.IsNullOrWhiteSpace(jwtSettings.PrivateKeyPem))
            {
                throw new InvalidOperationException(
                    "JwtSettings:PrivateKeyPem is required but was empty. Provide an RSA " +
                    "private key out of source control via appsettings.{Environment}.Local.json " +
                    "(gitignored) or `dotnet user-secrets set \"JwtSettings:PrivateKeyPem\"`. " +
                    "Generate a dev keypair with scripts/gen-dev-keys.sh (or .ps1). " +
                    "See README -> First-time setup.");
            }

            var rsa = RSA.Create();
            rsa.ImportFromPem(jwtSettings.PrivateKeyPem);

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtSettings.Audience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new RsaSecurityKey(rsa),
                ClockSkew = TimeSpan.Zero,
            };
        });

        services.AddAuthorization();

        return services;
    }

    /// <summary>
    /// Registers the per-endpoint auth rate-limit policies (register, resend, forgot-password).
    /// Must be called inside the existing AddRateLimiter call or added separately.
    /// </summary>
    public static Microsoft.AspNetCore.RateLimiting.RateLimiterOptions AddAuthRateLimitPolicies(this Microsoft.AspNetCore.RateLimiting.RateLimiterOptions options)
    {
        options.AddFixedWindowLimiter(RegisterRateLimitPolicy, limiterOptions =>
        {
            limiterOptions.PermitLimit = 5;
            limiterOptions.Window = TimeSpan.FromHours(1);
            limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            limiterOptions.QueueLimit = 0;
        });

        options.AddFixedWindowLimiter(ResendConfirmationRateLimitPolicy, limiterOptions =>
        {
            limiterOptions.PermitLimit = 3;
            limiterOptions.Window = TimeSpan.FromHours(1);
            limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            limiterOptions.QueueLimit = 0;
        });

        options.AddFixedWindowLimiter(ForgotPasswordRateLimitPolicy, limiterOptions =>
        {
            limiterOptions.PermitLimit = 3;
            limiterOptions.Window = TimeSpan.FromHours(1);
            limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            limiterOptions.QueueLimit = 0;
        });

        return options;
    }
}
