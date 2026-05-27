using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using PhotoPrint.API.Authentication;
using PhotoPrint.API.Services;

namespace PhotoPrint.API.Extensions;

public static class GuestSessionExtensions
{
    public const string DualAuthPolicy = "DualAuth";

    public static IServiceCollection AddGuestSessions(this IServiceCollection services)
    {
        // Add GuestToken as a secondary authentication scheme
        services.AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, GuestAuthenticationHandler>(
                GuestAuthenticationHandler.SchemeName, _ => { });

        // Add DualAuth policy: accepts Bearer JWT OR X-Guest-Token
        services.AddAuthorization(options =>
        {
            options.AddPolicy(DualAuthPolicy, policy =>
                policy
                    .AddAuthenticationSchemes(
                        JwtBearerDefaults.AuthenticationScheme,
                        GuestAuthenticationHandler.SchemeName)
                    .RequireAuthenticatedUser());
        });

        services.AddScoped<IGuestSessionService, GuestSessionService>();
        services.AddHostedService<BackgroundJobs.GuestSessionCleanupJob>();

        return services;
    }
}
