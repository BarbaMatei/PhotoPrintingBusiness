using PhotoPrint.API.Configuration;
using PhotoPrint.API.Services;

namespace PhotoPrint.API.Extensions;

public static class SocialAuthExtensions
{
    public static IServiceCollection AddSocialAuth(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<GoogleAuthSettings>(configuration.GetSection("GoogleAuth"));

        services.AddHttpClient("Google", client =>
        {
            client.BaseAddress = new Uri("https://oauth2.googleapis.com/");
            client.Timeout = TimeSpan.FromSeconds(5);
        });

        services.AddScoped<IGoogleTokenValidator, GoogleTokenValidator>();
        services.AddScoped<ISocialAuthService, SocialAuthService>();

        return services;
    }
}
